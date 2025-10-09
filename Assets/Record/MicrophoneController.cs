using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

// 用于解析服务器返回JSON的数据结构
[System.Serializable]
public class UploadResponse // 第一次上传返回的结构
{
    public string status;
    public string task_id;
    public string raw_transcription;
}

[System.Serializable]
public class ResultResponse // 第二次查询结果返回的结构
{
    public string status;
    public string transcription;
}

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
    
    [Header("自定义按钮外观")]
    public string startRecordingText = "开始录音";
    public string stopRecordingText = "停止录音";
    public string savingText = "处理中...";
    public Color idleColor = Color.white;
    public Color recordingColor = Color.red;

    [Header("音频文件设置")]
    public string outputFileName = "latest_recording";
    
    private AudioSource audioSource;
    private bool isRecording = false;
    private bool isSaving = false;
    private string microphoneDeviceName;
    private string fullFilePath;

    // --- Start 方法（已修正格式） ---
    void Start()
    {
        if (rawAsrTextOutput != null) rawAsrTextOutput.text = "ASR原始识别结果将显示在这里...";
        if (transcriptionTextOutput != null) transcriptionTextOutput.text = "Gemini润色后结果将显示在这里...";

        if (Microphone.devices.Length == 0)
        {
            if (recordButton != null)
            {
                recordButton.interactable = false;
                buttonText.text = "无麦克风";
            }
            if (transcriptionTextOutput != null)
            {
                transcriptionTextOutput.text = "未找到麦克风";
            }
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

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        fullFilePath = Path.Combine(savePath, outputFileName);
        if (recordButton == null || buttonText == null)
        {
            return;
        }
        recordButton.onClick.AddListener(OnRecordButtonPressed);
        UpdateUI();
    }

    // --- UploadAudio 协程 ---
    IEnumerator UploadAudio(string filePath)
    {
        WWWForm form = new WWWForm();
        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        form.AddBinaryData("file", fileData, fileName, "audio/wav");
        
        using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form))
        {
            if (rawAsrTextOutput != null) rawAsrTextOutput.text = "正在上传并获取原始文本...";
            if (transcriptionTextOutput != null) transcriptionTextOutput.text = "等待最终结果...";

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorText = "上传失败: " + www.error;
                Debug.LogError(errorText);
                if (rawAsrTextOutput != null) rawAsrTextOutput.text = errorText;
                if (transcriptionTextOutput != null) transcriptionTextOutput.text = errorText;

                isSaving = false;
                UpdateUI();
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("第一次响应成功！服务器响应: " + jsonResponse);
                
                UploadResponse response = JsonUtility.FromJson<UploadResponse>(jsonResponse);

                if (response.status == "pending")
                {
                    if (rawAsrTextOutput != null)
                        rawAsrTextOutput.text = "【ASR原始结果】\n" + response.raw_transcription;
                    StartCoroutine(PollForResult(response.task_id));
                }
                else
                {
                    string errorText = "处理失败: " + jsonResponse;
                    if (rawAsrTextOutput != null) rawAsrTextOutput.text = errorText;
                    if (transcriptionTextOutput != null) transcriptionTextOutput.text = errorText;
                    isSaving = false;
                    UpdateUI();
                }
            }
        }
    }

    // --- PollForResult 协程 ---
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
                    string jsonResponse = www.downloadHandler.text;
                    ResultResponse response = JsonUtility.FromJson<ResultResponse>(jsonResponse);

                    if (response.status == "success")
                    {
                        if (transcriptionTextOutput != null)
                            transcriptionTextOutput.text = "【Gemini润色结果】\n" + response.transcription;
                        
                        isSaving = false;
                        UpdateUI();
                        yield break; 
                    }
                    else if (response.status == "processing")
                    {
                        Debug.Log("结果仍在处理中，0.5秒后重试...");
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
    
    // --- 其他方法（已修正格式） ---
    private void OnRecordButtonPressed() 
    { 
        if (isRecording) { StopRecordingAndProcess(); } 
        else if (!isSaving) { StartRecording(); } 
    }

    private void StartRecording() 
    { 
        if (rawAsrTextOutput != null) rawAsrTextOutput.text = "正在录音..."; 
        if (transcriptionTextOutput != null) transcriptionTextOutput.text = "正在录音..."; 
        if (audioSource.isPlaying) { audioSource.Stop(); } 
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 22050); 
        isRecording = true; 
        UpdateUI(); 
    }

    private void StopRecordingAndProcess() 
    { 
        int lastSamplePosition = Microphone.GetPosition(microphoneDeviceName); 
        Microphone.End(microphoneDeviceName); 
        isRecording = false; 
        if (lastSamplePosition <= 0) { UpdateUI(); return; } 
        
        AudioClip originalClip = audioSource.clip; 
        float[] audioData = new float[lastSamplePosition * originalClip.channels]; 
        originalClip.GetData(audioData, 0); 
        
        int channels = originalClip.channels; 
        int frequency = originalClip.frequency; 
        
        isSaving = true; 
        UpdateUI(); 
        
        SaveAudioAsync(audioData, channels, frequency); 
    }

    private async void SaveAudioAsync(float[] samples, int channels, int frequency) 
    { 
        string wavFilePath = fullFilePath + ".wav"; 
        bool success = await Task.Run(() => SavWav.Save(wavFilePath, samples, frequency, channels)); 
        if (success) 
        { 
            StartCoroutine(UploadAudio(wavFilePath)); 
        } 
        else 
        { 
            Debug.LogError("本地保存文件失败！");
            isSaving = false; 
            UpdateUI(); 
        } 
    }

    private void UpdateUI() 
    { 
        if (isSaving) 
        { 
            recordButton.interactable = false; 
            buttonText.text = savingText; 
        } 
        else if (isRecording) 
        { 
            recordButton.interactable = true; 
            buttonText.text = stopRecordingText; 
            recordButton.GetComponent<Image>().color = recordingColor; 
        } 
        else 
        { 
            recordButton.interactable = true; 
            buttonText.text = startRecordingText; 
            recordButton.GetComponent<Image>().color = idleColor; 
        } 
    }
}