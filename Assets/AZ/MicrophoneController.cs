using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions; // [新增] 引入正则表达式命名空间
using OpenapiDemo; // 如果您有这个命名空间的话

// The CustomizableUITexts, UploadResponse, and ResultResponse classes remain unchanged.
[System.Serializable]
public class CustomizableUITexts
{
    [Header("初始状态文本")]
    public string initialRawAsrText = "ASR原始识别结果将显示在这里...";
    public string initialTranscriptionText = "Gemini润色后结果将显示在这里...";
    public string noMicrophoneFound = "无麦 ensimmäinen";
    [Header("过程状态文本")]
    public string recording = "正在录音...";
    public string uploading = "正在上传并获取原始文本...";
    public string waitingForFinalResult = "等待最终结果...";
    public string fetchingFinalResult = "正在获取Gemini润色结果...";
    [Header("结果/错误提示前缀或全文")]
    public string rawAsrResultPrefix = "【ASR原始结果】\n";
    public string finalResultPrefix = "【Gemini润色结果】\n";
    public string uploadFailedPrefix = "上传失败: ";
    public string serverErrorPrefix = "服务器处理失败: ";
    public string timeoutError = "获取Gemini结果超时！";
}
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
    
    [Header("动画控制器")]
    [Tooltip("将带有 OverrideAnimationManager 脚本的角色对象拖拽到这里")]
    public OverrideAnimationManager animationManager;
    
    [Header("自定义按钮外观")]
    public string startRecordingText = "开始录音";
    public string stopRecordingText = "停止录音";
    public string savingText = "处理中...";
    public Color idleColor = Color.white;
    public Color recordingColor = Color.red;

    [Header("自定义UI文本")]
    public CustomizableUITexts uiTexts;
    
    private AudioSource audioSource;
    private bool isRecording = false;
    private bool isSaving = false;
    private string microphoneDeviceName;
    private const string TempAudioFileName = "latest_recording.wav"; 

    // [新增] 用于处理情绪标签的正则表达式
    private readonly Regex emotionRegex = new Regex(@"^\[(开心|伤心)\]");

    // Start() 和其他方法保持不变...
    void Start()
    {
        if (ttsManager == null) Debug.LogWarning("TtsManager未在Inspector中指定，TTS功能将不可用。");
        if (animationManager == null) Debug.LogWarning("Override Animation Manager未在Inspector中指定，角色动画将不会播放。");
        if (rawAsrTextOutput != null) rawAsrTextOutput.text = uiTexts.initialRawAsrText;
        if (transcriptionTextOutput != null) transcriptionTextOutput.text = uiTexts.initialTranscriptionText;
        if (Microphone.devices.Length == 0)
        {
            if (recordButton != null) { recordButton.interactable = false; if (buttonText != null) buttonText.text = uiTexts.noMicrophoneFound; }
            Debug.LogError("未找到麦克风设备！");
            return;
        }
        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();
        if (recordButton == null || buttonText == null) { Debug.LogError("请在Inspector中关联录音按钮和按钮文本！"); return; }
        recordButton.onClick.AddListener(OnRecordButtonPressed);
        UpdateUI();
    }
    private void OnRecordButtonPressed() { if (isRecording) { StopRecordingAndProcess(); } else if (!isSaving) { StartRecording(); } }
    private void StartRecording() { if (rawAsrTextOutput != null) rawAsrTextOutput.text = uiTexts.recording; if (transcriptionTextOutput != null) transcriptionTextOutput.text = uiTexts.recording; audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 44100); isRecording = true; UpdateUI(); }
    private async void StopRecordingAndProcess() { int lastSamplePosition = Microphone.GetPosition(microphoneDeviceName); Microphone.End(microphoneDeviceName); isRecording = false; if (lastSamplePosition <= 0) { Debug.LogWarning("录音时间过短或没有录到声音。"); if (rawAsrTextOutput != null) rawAsrTextOutput.text = uiTexts.initialRawAsrText; if (transcriptionTextOutput != null) transcriptionTextOutput.text = uiTexts.initialTranscriptionText; UpdateUI(); return; } AudioClip originalClip = audioSource.clip; float[] audioData = new float[lastSamplePosition * originalClip.channels]; originalClip.GetData(audioData, 0); isSaving = true; UpdateUI(); await SaveAndUploadAsync(audioData, originalClip.channels, originalClip.frequency); }
    private async Task SaveAndUploadAsync(float[] samples, int channels, int frequency) { string savePath = Application.persistentDataPath; string fullFilePath = Path.Combine(savePath, TempAudioFileName); bool success = await Task.Run(() => SavWav.Save(fullFilePath, samples, frequency, channels)); if (success) { Debug.Log($"临时录音文件已成功保存到: {fullFilePath}"); StartCoroutine(UploadAudio(fullFilePath)); } else { Debug.LogError($"本地保存文件失败！路径: {fullFilePath}"); isSaving = false; UpdateUI(); } }
    IEnumerator UploadAudio(string filePath) { if (!File.Exists(filePath)) { Debug.LogError($"严重错误: 文件在 {filePath} 未找到，上传中止。"); isSaving = false; UpdateUI(); yield break; } WWWForm form = new WWWForm(); byte[] fileData = File.ReadAllBytes(filePath); form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav"); using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form)) { if (rawAsrTextOutput != null) rawAsrTextOutput.text = uiTexts.uploading; if (transcriptionTextOutput != null) transcriptionTextOutput.text = uiTexts.waitingForFinalResult; yield return www.SendWebRequest(); if (www.result != UnityWebRequest.Result.Success) { string errorText = uiTexts.uploadFailedPrefix + www.error; Debug.LogError(errorText); if (rawAsrTextOutput != null) rawAsrTextOutput.text = errorText; isSaving = false; UpdateUI(); } else { string jsonResponse = www.downloadHandler.text; Debug.Log("第一次响应成功！服务器响应: " + jsonResponse); UploadResponse response = JsonUtility.FromJson<UploadResponse>(jsonResponse); if (response != null && response.status == "pending") { if (rawAsrTextOutput != null) rawAsrTextOutput.text = uiTexts.rawAsrResultPrefix + response.raw_transcription; StartCoroutine(PollForResult(response.task_id)); } else { string errorText = uiTexts.serverErrorPrefix + jsonResponse; Debug.LogError(errorText); if (rawAsrTextOutput != null) rawAsrTextOutput.text = errorText; isSaving = false; UpdateUI(); } } } }

    IEnumerator PollForResult(string taskId)
    {
        float timeout = 30f;
        float elapsedTime = 0f;

        if (transcriptionTextOutput != null)
            transcriptionTextOutput.text = uiTexts.fetchingFinalResult;

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
                        // ######## [核心修改区域] ########

                        string rawFinalText = response.transcription;
                        string cleanedText = rawFinalText; // 默认情况下，干净文本就是原始文本

                        Match match = emotionRegex.Match(rawFinalText);
                        if (match.Success)
                        {
                            string emotion = match.Groups[1].Value;
                            
                            // 1. 根据标签决定要播放哪个Trigger集合
                            if (emotion == "开心" && animationManager != null)
                            {
                                animationManager.PlayRandomAnimation("TriggerHappy");
                            }
                            else if (emotion == "伤心" && animationManager != null)
                            {
                                animationManager.PlayRandomAnimation("TriggerSad");
                            }
                            
                            // 2. 移除标签，得到干净的文本
                            cleanedText = emotionRegex.Replace(rawFinalText, "").TrimStart();
                        }
                        
                        // 3. 使用【干净的文本】来更新UI
                        if (transcriptionTextOutput != null)
                            transcriptionTextOutput.text = uiTexts.finalResultPrefix + cleanedText;
                        
                        // 4. 使用【干净的文本】来播放TTS
                        if (ttsManager != null)
                        {
                            ttsManager.SynthesizeAndPlay(cleanedText);
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
        if (transcriptionTextOutput != null) transcriptionTextOutput.text = uiTexts.timeoutError;
        isSaving = false;
        UpdateUI();
    }

    private void UpdateUI() { if (recordButton == null || buttonText == null) return; if (isSaving) { recordButton.interactable = false; buttonText.text = savingText; } else if (isRecording) { recordButton.interactable = true; buttonText.text = stopRecordingText; recordButton.GetComponent<Image>().color = recordingColor; } else { recordButton.interactable = true; buttonText.text = startRecordingText; recordButton.GetComponent<Image>().color = idleColor; } }
}