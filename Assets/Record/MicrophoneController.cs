using UnityEngine;
using UnityEngine.UI;
using TMPro; // 【新增】引入TextMeshPro的命名空间
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

// 【新增】创建一个类来匹配服务器返回的JSON结构
// [System.Serializable] 属性是必须的，这样Unity的JsonUtility才能处理它
[System.Serializable]
public class TranscriptionResponse
{
    public string status;
    public string message;
    public string filename;
    public string transcription;
}


[RequireComponent(typeof(AudioSource))]
public class MicrophoneController : MonoBehaviour
{
    [Header("网络设置")]
    public string uploadURL = "http://your_domain_or_ip/upload";
    
    // --- 【新增】用于显示结果的UI ---
    [Header("输出设置")]
    [Tooltip("请将用于显示识别结果的TextMeshProUGUI组件拖到这里")]
    public TextMeshProUGUI transcriptionTextOutput;
    // --- 【新增部分结束】---


    [Header("UI元素关联")]
    public Button recordButton;
    public TextMeshProUGUI buttonText;

    // ... 其他变量保持不变 ...
    #region Unchanged Variables
    [Header("自定义按钮外观")]
    public string startRecordingText = "开始录音";
    public string stopRecordingText = "停止录音";
    public string savingText = "上传中...";
    public Color idleColor = Color.white;
    public Color recordingColor = Color.red;

    [Header("音频文件设置")]
    public string outputFileName = "latest_recording";
    
    private AudioSource audioSource;
    private bool isRecording = false;
    private bool isSaving = false;
    private string microphoneDeviceName;
    private string fullFilePath;
    #endregion

    void Start()
    {
        // --- 【新增】在开始时清空一下文本框 ---
        if (transcriptionTextOutput != null)
        {
            transcriptionTextOutput.text = "请开始录音...";
        }
        // --- 【新增部分结束】---
        
        // ... Start方法的其余部分保持不变 ...
        #region Unchanged Start Logic
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("未找到任何麦克风设备。");
            if (recordButton != null) { recordButton.interactable = false; buttonText.text = "无麦克风"; }
            if (transcriptionTextOutput != null) { transcriptionTextOutput.text = "未找到麦克风"; }
            return;
        }
        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();
        string savePath;
#if UNITY_EDITOR
        savePath = Path.Combine(Application.dataPath, "Recordings");
#else
        savePath = Application.persistentDataPath;
#endif
        if (!Directory.Exists(savePath)) { Directory.CreateDirectory(savePath); }
        fullFilePath = Path.Combine(savePath, outputFileName);
        Debug.Log($"音频文件将被保存到: {Path.GetFullPath(fullFilePath)}");
        if (recordButton == null || buttonText == null)
        {
            Debug.LogError("请在Inspector面板中关联Record Button和Button Text！");
            return;
        }
        recordButton.onClick.AddListener(OnRecordButtonPressed);
        UpdateUI();
        #endregion
    }

    // --- 【修改】UploadAudio协程，以解析JSON并更新UI ---
    IEnumerator UploadAudio(string filePath)
    {
        WWWForm form = new WWWForm();
        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        form.AddBinaryData("file", fileData, fileName, "audio/wav");
        
        using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form))
        {
            if (transcriptionTextOutput != null)
                transcriptionTextOutput.text = "正在上传并识别...";

            Debug.Log("正在上传文件...");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("上传失败: " + www.error);
                Debug.LogError("服务器响应: " + www.downloadHandler.text);
                if (transcriptionTextOutput != null)
                    transcriptionTextOutput.text = "上传失败: " + www.error;
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("上传成功！服务器响应: " + jsonResponse);

                // 使用JsonUtility将JSON字符串转换为我们的数据类对象
                TranscriptionResponse response = JsonUtility.FromJson<TranscriptionResponse>(jsonResponse);

                if (transcriptionTextOutput != null)
                {
                    if (response.status == "success")
                    {
                        // 如果成功，显示识别出的文字
                        transcriptionTextOutput.text = response.transcription;
                    }
                    else
                    {
                        // 如果服务器返回错误状态，显示错误信息
                        transcriptionTextOutput.text = "识别失败: " + response.message;
                    }
                }
            }
        }
        
        isSaving = false;
        UpdateUI();
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    // --- 【修改部分结束】---
    
    // ... 其余方法保持不变 ...
    #region Unchanged Methods
    private void OnRecordButtonPressed() { if (isRecording) { StopRecordingAndProcess(); } else if (!isSaving) { StartRecording(); } }
    private void StartRecording() { if (transcriptionTextOutput != null) { transcriptionTextOutput.text = "正在录音..."; } if (audioSource.isPlaying) { audioSource.Stop(); } Debug.Log("开始录音..."); audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 22050); isRecording = true; UpdateUI(); }
    private void StopRecordingAndProcess() { int lastSamplePosition = Microphone.GetPosition(microphoneDeviceName); Microphone.End(microphoneDeviceName); isRecording = false; Debug.Log("停止录音..."); if (lastSamplePosition <= 0) { Debug.LogWarning("录音时间过短，不进行处理。"); UpdateUI(); return; } AudioClip originalClip = audioSource.clip; float[] audioData = new float[lastSamplePosition * originalClip.channels]; originalClip.GetData(audioData, 0); int channels = originalClip.channels; int frequency = originalClip.frequency; isSaving = true; UpdateUI(); SaveAudioAsync(audioData, channels, frequency); }
    private async void SaveAudioAsync(float[] samples, int channels, int frequency) { string wavFilePath = fullFilePath + ".wav"; Debug.Log("开始异步保存到本地..."); bool success = await Task.Run(() => SavWav.Save(wavFilePath, samples, frequency, channels)); if (success) { Debug.Log("本地保存成功！开始上传..."); StartCoroutine(UploadAudio(wavFilePath)); } else { Debug.LogError("本地保存失败！"); isSaving = false; UpdateUI(); } }
    private void UpdateUI() { if (isSaving) { recordButton.interactable = false; buttonText.text = savingText; } else if (isRecording) { recordButton.interactable = true; buttonText.text = stopRecordingText; recordButton.GetComponent<Image>().color = recordingColor; } else { recordButton.interactable = true; buttonText.text = startRecordingText; recordButton.GetComponent<Image>().color = idleColor; } }
    #endregion
}