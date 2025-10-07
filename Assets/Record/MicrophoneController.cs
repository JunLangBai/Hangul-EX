using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneController : MonoBehaviour
{
    // =================================================================
    // V V V  就是下面这几行代码，会在检视面板中生成“网络设置” V V V
    [Header("网络设置")]
    [Tooltip("您的Python上传脚本的完整URL地址")]
    public string uploadURL = "http://your_domain_or_ip/upload";
    // =================================================================


    [Header("UI元素关联")]
    [Tooltip("请将您在场景中创建的Button-TMP拖到这里")]
    public Button recordButton;
    [Tooltip("请将您按钮下的TextMeshPro文本组件拖到这里")]
    public TextMeshProUGUI buttonText;

    [Header("自定义按钮外观")]
    [Tooltip("未录音时，按钮上显示的文字")]
    public string startRecordingText = "开始录音";
    [Tooltip("正在录音时，按钮上显示的文字")]
    public string stopRecordingText = "停止录音";
    [Tooltip("保存音频时，按钮上显示的文字")]
    public string savingText = "上传中...";

    [Tooltip("未录音时，按钮的颜色")]
    public Color idleColor = Color.white;
    [Tooltip("正在录音时，按钮的颜色")]
    public Color recordingColor = Color.red;

    [Header("音频文件设置")]
    [Tooltip("保存的音频文件名（无需后缀）")]
    public string outputFileName = "my_recorded_audio";
    
    // --- 私有变量 ---
    private AudioSource audioSource;
    private bool isRecording = false;
    private bool isSaving = false;
    private string microphoneDeviceName;
    private string fullFilePath;

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("未找到任何麦克风设备。");
            if (recordButton != null) { recordButton.interactable = false; buttonText.text = "无麦克风"; }
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
    }

    private void OnRecordButtonPressed() { if (isRecording) { StopRecordingAndProcess(); } else if (!isSaving) { StartRecording(); } }

    private void StartRecording() { if (audioSource.isPlaying) { audioSource.Stop(); } Debug.Log("开始录音..."); audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 22050); isRecording = true; UpdateUI(); }

    private void StopRecordingAndProcess()
    {
        int lastSamplePosition = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false;
        Debug.Log("停止录音...");
        if (lastSamplePosition <= 0) { Debug.LogWarning("录音时间过短，不进行处理。"); UpdateUI(); return; }
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
        Debug.Log("开始异步保存到本地...");
        bool success = await Task.Run(() => SavWav.Save(wavFilePath, samples, frequency, channels));
        if (success)
        {
            Debug.Log("本地保存成功！开始上传...");
            StartCoroutine(UploadAudio(wavFilePath));
        }
        else
        {
            Debug.LogError("本地保存失败！");
            isSaving = false;
            UpdateUI();
        }
    }

    IEnumerator UploadAudio(string filePath)
    {
        WWWForm form = new WWWForm();
        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        form.AddBinaryData("file", fileData, fileName, "audio/wav");
        using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form))
        {
            Debug.Log("正在上传文件...");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("上传失败: " + www.error);
                Debug.LogError("服务器响应: " + www.downloadHandler.text);
            }
            else
            {
                Debug.Log("上传成功！服务器响应: " + www.downloadHandler.text);
            }
        }
        isSaving = false;
        UpdateUI();
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    
    private void UpdateUI() { if (isSaving) { recordButton.interactable = false; buttonText.text = savingText; } else if (isRecording) { recordButton.interactable = true; buttonText.text = stopRecordingText; recordButton.GetComponent<Image>().color = recordingColor; } else { recordButton.interactable = true; buttonText.text = startRecordingText; recordButton.GetComponent<Image>().color = idleColor; } }
}