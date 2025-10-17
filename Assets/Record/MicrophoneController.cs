using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;
using OpenapiDemo;

// ... (UploadResponse 和 ResultResponse 类定义保持不变)
[System.Serializable]
public class UploadResponse { public string status; public string task_id; public string raw_transcription; }
[System.Serializable]
public class ResultResponse { public string status; public string transcription; }


[RequireComponent(typeof(AudioSource))]
public class MicrophoneController : MonoBehaviour
{
    [Header("网络设置")]
    public string uploadURL = "http://your_domain_or_ip/upload";
    
    [Header("输出设置")]
    public TextMeshProUGUI rawAsrTextOutput;
    public TextMeshProUGUI transcriptionTextOutput;

    [Header("UI元素关联")]
    public Button recordButton;
    public TextMeshProUGUI buttonText;
    
    [Header("功能关联")]
    [Tooltip("将场景中的TtsManager对象拖拽到这里")]
    public TtsManager ttsManager; 
    
    [Header("自定义按钮外观")]
    public string startRecordingText = "开始录音";
    public string stopRecordingText = "停止录音";
    public string savingText = "处理中...";
    public Color idleColor = Color.white;
    public Color recordingColor = Color.red;
    
    // ######## 移除音频文件设置，因为我们将在代码中固定文件名 ########

    private AudioSource audioSource;
    private bool isRecording = false;
    private bool isSaving = false;
    private string microphoneDeviceName;
    private const string TempAudioFileName = "latest_recording.wav"; // 固定文件名

    void Start()
    {
        if (ttsManager == null) Debug.LogWarning("TtsManager未在Inspector中指定，TTS功能将不可用。");
        if (rawAsrTextOutput != null) rawAsrTextOutput.text = "ASR原始识别结果将显示在这里...";
        if (transcriptionTextOutput != null) transcriptionTextOutput.text = "Gemini润色后结果将显示在这里...";

        if (Microphone.devices.Length == 0)
        {
            if (recordButton != null)
            {
                recordButton.interactable = false;
                if (buttonText != null) buttonText.text = "无麦克风";
            }
            Debug.LogError("未找到麦克风设备！");
            return;
        }

        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();
        
        if (recordButton == null || buttonText == null)
        {
            Debug.LogError("请在Inspector中关联录音按钮和按钮文本！");
            return;
        }
        recordButton.onClick.AddListener(OnRecordButtonPressed);
        UpdateUI();
    }

    private void OnRecordButtonPressed() 
    { 
        if (isRecording) { StopRecordingAndProcess(); } 
        else if (!isSaving) { StartRecording(); } 
    }

    private void StartRecording() 
    { 
        if (rawAsrTextOutput != null) rawAsrTextOutput.text = "正在录音..."; 
        if (transcriptionTextOutput != null) transcriptionTextOutput.text = "正在录音..."; 
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 44100); 
        isRecording = true; 
        UpdateUI(); 
    }

    private async void StopRecordingAndProcess() 
    { 
        int lastSamplePosition = Microphone.GetPosition(microphoneDeviceName); 
        Microphone.End(microphoneDeviceName); 
        isRecording = false; 
        
        if (lastSamplePosition <= 0) 
        { 
            Debug.LogWarning("录音时间过短或没有录到声音。");
            UpdateUI(); 
            return; 
        } 
        
        AudioClip originalClip = audioSource.clip; 
        float[] audioData = new float[lastSamplePosition * originalClip.channels]; 
        originalClip.GetData(audioData, 0); 
        
        isSaving = true; 
        UpdateUI(); 
        
        await SaveAndUploadAsync(audioData, originalClip.channels, originalClip.frequency);
    }
    
    // ######## 修改后的保存和上传逻辑 ########
    private async Task SaveAndUploadAsync(float[] samples, int channels, int frequency) 
    { 
        // 构造一个在 Editor 和设备上都有效的标准路径
        string savePath = Application.persistentDataPath;
        string fullFilePath = Path.Combine(savePath, TempAudioFileName);

        // 在后台线程调用您提供的 SavWav.Save 方法
        bool success = await Task.Run(() => SavWav.Save(fullFilePath, samples, frequency, channels)); 
        
        // 回到主线程
        if (success) 
        { 
            Debug.Log($"临时录音文件已成功保存到: {fullFilePath}");
            // 确认保存成功后，再开始上传
            StartCoroutine(UploadAudio(fullFilePath)); 
        } 
        else 
        { 
            Debug.LogError($"本地保存文件失败！路径: {fullFilePath}");
            isSaving = false; 
            UpdateUI(); 
        } 
    }

    IEnumerator UploadAudio(string filePath)
    {
        // 增加一道保险检查，确保文件真的存在
        if (!File.Exists(filePath))
        {
            Debug.LogError($"严重错误: 文件在 {filePath} 未找到，上传中止。");
            isSaving = false;
            UpdateUI();
            yield break;
        }

        WWWForm form = new WWWForm();
        byte[] fileData = File.ReadAllBytes(filePath);
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav");
        
        using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form))
        {
            if (rawAsrTextOutput != null) rawAsrTextOutput.text = "正在上传并获取原始文本...";
            if (transcriptionTextOutput != null) transcriptionTextOutput.text = "等待最终结果...";

            yield return www.SendWebRequest();

            // ... (上传成功或失败的后续处理逻辑，与之前版本相同)
            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorText = "上传失败: " + www.error;
                Debug.LogError(errorText);
                if (rawAsrTextOutput != null) rawAsrTextOutput.text = errorText;
                isSaving = false;
                UpdateUI();
            }
            else
            {
                // ... (解析服务器响应，与之前版本相同)
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("第一次响应成功！服务器响应: " + jsonResponse);
                UploadResponse response = JsonUtility.FromJson<UploadResponse>(jsonResponse);

                if (response != null && response.status == "pending")
                {
                    if (rawAsrTextOutput != null)
                        rawAsrTextOutput.text = "【ASR原始结果】\n" + response.raw_transcription;
                    StartCoroutine(PollForResult(response.task_id));
                }
                else
                {
                    string errorText = "服务器处理失败: " + jsonResponse;
                    Debug.LogError(errorText);
                    if (rawAsrTextOutput != null) rawAsrTextOutput.text = errorText;
                    isSaving = false;
                    UpdateUI();
                }
            }
        }
    }
    
    // ... (PollForResult 和 UpdateUI 方法保持不变)
    IEnumerator PollForResult(string taskId)
    {
        float timeout = 30f;
        float elapsedTime = 0f;

        if (transcriptionTextOutput != null)
            transcriptionTextOutput.text = "正在获取Gemini润色结果...";

        while (elapsedTime < timeout)
        {
            string resultURL = uploadURL.Replace("/upload", "/result/" + taskId);
            
            using (UnityWebRequest www = UnityWebRequest.Get(resultURL))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    ResultResponse response = JsonUtility.FromJson<ResultResponse>(www.downloadHandler.text);

                    if (response.status == "success")
                    {
                        string finalText = response.transcription;
                        if (transcriptionTextOutput != null)
                            transcriptionTextOutput.text = "【Gemini润色结果】\n" + finalText;
                        
                        if (ttsManager != null)
                        {
                            ttsManager.SynthesizeAndPlay(finalText);
                        }
                        
                        isSaving = false;
                        UpdateUI();
                        yield break; 
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }

        Debug.LogError("获取最终结果超时！");
        if (transcriptionTextOutput != null)
            transcriptionTextOutput.text = "获取Gemini结果超时！";
        
        isSaving = false;
        UpdateUI();
    }

    private void UpdateUI() 
    { 
        if (recordButton == null || buttonText == null) return;
        if (isSaving) { recordButton.interactable = false; buttonText.text = savingText; } 
        else if (isRecording) { recordButton.interactable = true; buttonText.text = stopRecordingText; recordButton.GetComponent<Image>().color = recordingColor; } 
        else { recordButton.interactable = true; buttonText.text = startRecordingText; recordButton.GetComponent<Image>().color = idleColor; } 
    }
}