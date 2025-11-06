using System; // <-- 新增，为了使用 Action
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using MemoryGameTools; 

/// <summary>
/// ASR 服务器返回的初步响应
/// </summary>
[System.Serializable]
public class ASRUploadResponse
{
    public string status;
    public string task_id;
    public string raw_transcription;
}

/// <summary>
/// 负责录音、上传到ASR服务器，并获取原始识别结果。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ASRManager : MonoBehaviour
{
    // --- 【新增】事件 ---
    // 当 ASR 成功获取到结果时，会触发此事件
    // MemoryGameManager 将会订阅这个事件
    public event Action<string> OnASRResultReady;
    // --------------------

    private const string TempAudioFileName = "latest_recording.wav";

    [Header("网络设置")]
    [Tooltip("你的 app.py 服务器地址, 例如: http://192.168.1.10:5001/upload")]
    public string uploadURL = "http://your_server_ip:5001/upload";

    [Header("UI 元素")]
    [Tooltip("用于显示 ASR 原始结果 (例如 '一', '二')")]
    public TextMeshProUGUI asrResultText;
    
    [Tooltip("点击开始/停止录音的按钮")]
    public Button recordButton;
    
    [Tooltip("按钮上显示的文本 (例如 '开始录音')")]
    public TextMeshProUGUI buttonText;

    [Header("UI 状态文本")]
    public string text_StartRecording = "按住说话"; // 修改了提示
    public string text_StopRecording = "松开识别"; // 修改了提示
    public string text_Processing = "处理中...";
    public string text_Ready = "准备就绪";
    public string text_UploadFailed = "上传失败: ";

    // 内部状态
    private AudioSource audioSource;
    private bool isRecording;
    private bool isProcessing; // 是否正在保存或上传
    private string microphoneDeviceName;

    private void Start()
    {
        // 1. 检查麦克风
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("未找到麦克风设备！");
            if (recordButton != null) recordButton.interactable = false;
            if (buttonText != null) buttonText.text = "无麦克风";
            return;
        }

        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();

        // 2. 绑定按钮事件
        if (recordButton == null || buttonText == null || asrResultText == null)
        {
            Debug.LogError("请在 Inspector 中关联所有 UI 元素！");
            return;
        }

        // ---【修改】使用 EventTrigger 来处理“按下”和“抬起”---
        // 你需要在 Unity 编辑器中为 recordButton 添加 EventTrigger 组件
        // 并添加 PointerDown 和 PointerUp 事件
        
        // recordButton.onClick.AddListener(OnRecordButtonPressed); // 旧的点击逻辑不再适用
        
        // 更好的方式是让 MemoryGameManager 来控制按钮是否可用
        // 我们只在这里更新文本
        
        asrResultText.text = text_Ready;
        UpdateUI();
    }
    
    // --- 【修改】你需要将这两个方法绑定到 EventTrigger ---
    // 1. 在 Unity 编辑器中，选中你的 recordButton
    // 2. 添加 "Event Trigger" 组件
    // 3. 点击 "Add New Event Type" -> "PointerDown" (按下)
    // 4. 拖拽 ASRManager 组件到 OnClick() 列表，选择 ASRManager -> OnPointerDownRecord
    // 5. 点击 "Add New Event Type" -> "PointerUp" (抬起)
    // 6. 拖拽 ASRManager 组件到 OnClick() 列表，选择 ASRManager -> OnPointerUpRecord

    /// <summary>
    /// 当手指/鼠标按下录音按钮时调用 (需在 EventTrigger 中绑定)
    /// </summary>
    public void OnPointerDownRecord()
    {
        if (isProcessing || !recordButton.interactable) return;
        
        asrResultText.text = "正在录音...";
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 44100);
        isRecording = true;
        UpdateUI();
    }

    /// <summary>
    /// 当手指/鼠标松开录音按钮时调用 (需在 EventTrigger 中绑定)
    /// </summary>
    public void OnPointerUpRecord()
    {
        if (!isRecording) return;
        
        var lastSamplePosition = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false;

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
        
        // 使用 async void 启动异步任务
        ProcessAndUploadAsync(audioData, originalClip.channels, originalClip.frequency);
    }

    /// <summary>
    /// 异步保存并上传 (从 OnPointerUpRecord 中分离出来)
    /// </summary>
    private async void ProcessAndUploadAsync(float[] audioData, int channels, int frequency)
    {
        await SaveAndUploadAsync(audioData, channels, frequency);
    }

    // (此方法已重命名为 private)
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
                    // *** 成功获取 ASR 原始结果 ***
                    asrResultText.text = "识别结果: " + response.raw_transcription;

                    // --- 【核心修改】 ---
                    // 广播这个结果给 MemoryGameManager
                    OnASRResultReady?.Invoke(response.raw_transcription);
                    // --------------------
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

        if (isProcessing)
        {
            // recordButton.interactable = false; // 由 GameManager 控制
            buttonText.text = text_Processing;
        }
        else if (isRecording)
        {
            // recordButton.interactable = true;
            buttonText.text = text_StopRecording;
        }
        else
        {
            // recordButton.interactable = true;
            buttonText.text = text_StartRecording;
        }
    }
    
    // --- 【新增】 ---
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
                buttonText.text = text_StartRecording;
            }
        }
    }
    // ----------------
}