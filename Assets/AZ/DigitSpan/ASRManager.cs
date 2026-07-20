using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using MemoryGameTools; 

[System.Serializable]
public class ASRUploadResponse
{
    public string status;
    public string task_id;
    public string raw_transcription;
}

/// <summary>
/// 【已修改】
/// 负责录音、上传到ASR服务器，并获取原始识别结果。
/// 现在使用 "点击-开始 / 点击-停止" 逻辑。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ASRManager : MonoBehaviour
{
    public event Action<string> OnASRResultReady;

    private const string TempAudioFileName = "latest_recording.wav";

    [Header("网络设置 (Network Settings)")]
    [Tooltip("你的 app.py 服务器地址 (Your app.py server URL)")]
    public string uploadURL = "http://your_server_ip:5001/upload";

    [Header("UI 元素 (UI Elements)")]
    [Tooltip("用于显示 ASR 原始结果 (ASR raw result text)")]
    public TextMeshProUGUI asrResultText;
    
    [Tooltip("点击开始/停止录音的按钮 (Record button)")]
    public Button recordButton;
    
    [Tooltip("按钮上显示的文本 (Button text)")]
    public TextMeshProUGUI buttonText;

    [Header("UI 状态文本 (UI State Texts)")]
    // --- 【修改】更新了UI提示文本 ---
    public string text_StartRecording = "点击录音"; 
    public string text_StopRecording = "停止录音"; 
    public string text_Processing = "处理中...";
    public string text_Ready = "准备就绪";
    public string text_UploadFailed = "上传失败: ";

    // 内部状态 (Internal State)
    private AudioSource audioSource;
    private bool isRecording;
    private bool isProcessing; 
    private string microphoneDeviceName;

    private void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("未找到麦克风设备！");
            if (recordButton != null) recordButton.interactable = false;
            if (buttonText != null) buttonText.text = "无麦克风";
            return;
        }

        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();

        if (recordButton == null || buttonText == null || asrResultText == null)
        {
            Debug.LogError("请在 Inspector 中关联所有 UI 元素！");
            return;
        }
        
        // --- 【核心修改】 ---
        // 绑定到常规的 onClick 事件，而不是 EventTrigger
        recordButton.onClick.AddListener(OnRecordButtonPressed);
        // ------------------
        
        asrResultText.text = text_Ready;
        UpdateUI();
    }
    

    // --- 【新增】点击事件处理器 ---
    /// <summary>
    /// 当录音按钮被点击时调用
    /// </summary>
    public void OnRecordButtonPressed()
    {
        // 如果正在上传，不允许任何操作
        if (isProcessing) return; 

        // 如果正在录音，则停止
        if (isRecording)
        {
            StopRecordingAndProcess();
        }
        // 否则，开始录音
        else
        {
            StartRecording();
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    private void StartRecording()
    {
        if (!recordButton.interactable) return;

        asrResultText.text = "正在录音...";
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 44100);
        isRecording = true;
        UpdateUI();
    }

    /// <summary>
    /// 停止录音、处理并上传
    /// </summary>
    private void StopRecordingAndProcess()
    {
        var lastSamplePosition = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false; // 立刻设置状态

        if (lastSamplePosition <= 0)
        {
            Debug.LogWarning("录音时间过短或没有录到声音。");
            asrResultText.text = text_Ready;
            UpdateUI();
            return;
        }

        var originalClip = audioSource.clip;
        var audioData = new float[lastSamplePosition * originalClip.channels];
        originalClip.GetData(audioData, 0);

        isProcessing = true;
        UpdateUI();
        
        // 异步处理
        ProcessAndUploadAsync(audioData, originalClip.channels, originalClip.frequency);
    }
    // ---------------------------------

    private async void ProcessAndUploadAsync(float[] audioData, int channels, int frequency)
    {
        await SaveAndUploadAsync(audioData, channels, frequency);
    }

    private async Task SaveAndUploadAsync(float[] samples, int channels, int frequency)
    {
        var savePath = Application.persistentDataPath;
        var fullFilePath = Path.Combine(savePath, TempAudioFileName);
        
        var success = await Task.Run(() => SavWav.Save(fullFilePath, samples, frequency, channels));

        if (success)
        {
            StartCoroutine(UploadAudio(fullFilePath));
        }
        else
        {
            Debug.LogError($"本地保存文件失败！");
            asrResultText.text = "本地保存失败";
            isProcessing = false;
            UpdateUI();
        }
    }

    private IEnumerator UploadAudio(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"文件在 {filePath} 未找到，上传中止。");
            isProcessing = false;
            UpdateUI();
            yield break;
        }

        asrResultText.text = "正在上传并识别...";
        var form = new WWWForm();
        var fileData = File.ReadAllBytes(filePath);
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav");

        using (var www = UnityWebRequest.Post(uploadURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                var errorText = text_UploadFailed + www.error;
                Debug.LogError(errorText);
                asrResultText.text = errorText;
            }
            else
            {
                var jsonResponse = www.downloadHandler.text;
                Debug.Log("服务器响应: " + jsonResponse);
                var response = JsonUtility.FromJson<ASRUploadResponse>(jsonResponse);

                if (response != null && (response.status == "pending" || response.status == "success"))
                {
                    asrResultText.text = "识别结果: " + response.raw_transcription;
                    OnASRResultReady?.Invoke(response.raw_transcription);
                }
                else
                {
                    asrResultText.text = "服务器处理失败: " + jsonResponse;
                }
            }
        }
        
        File.Delete(filePath);
        isProcessing = false;
        UpdateUI();
    }

    /// <summary>
    /// 更新按钮的文本和可交互状态
    /// </summary>
    private void UpdateUI()
    {
        if (recordButton == null || buttonText == null) return;

        // --- 【修改】更新UI逻辑以匹配开关状态 ---
        if (isProcessing)
        {
            buttonText.text = text_Processing;
        }
        else if (isRecording)
        {
            buttonText.text = text_StopRecording;
        }
        else
        {
            buttonText.text = text_StartRecording;
        }
        // ------------------------------------
    }
    
    /// <summary>
    /// 允许 MemoryGameManager 从外部控制此按钮是否可用
    /// </summary>
    public void SetRecordButtonActive(bool isActive)
    {
        if (recordButton != null)
        {
            recordButton.interactable = isActive;
            if (!isActive)
            {
                // 如果游戏结束（按钮被禁用），确保重置录音状态
                if (isRecording)
                {
                    Microphone.End(microphoneDeviceName);
                    isRecording = false;
                }
                buttonText.text = text_StartRecording;
            }
        }
    }
}