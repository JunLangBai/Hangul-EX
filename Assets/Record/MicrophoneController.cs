using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks; // 引入异步任务命名空间

[RequireComponent(typeof(AudioSource))]
public class MicrophoneController : MonoBehaviour
{
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
    public string savingText = "保存中...";

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
    private bool isSaving = false; // 状态旗标，表示是否正在保存
    private string microphoneDeviceName;
    private string fullFilePath;

    void Start()
    {
        // 1. 检查麦克风设备
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("未找到任何麦克风设备。");
            if (recordButton != null)
            {
                recordButton.interactable = false;
                buttonText.text = "无麦克风";
            }
            return;
        }

        // 2. 初始化组件和设备
        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();

        // 3. 根据不同平台设置保存路径
        string savePath;
#if UNITY_EDITOR
        // 在Unity编辑器中，保存在 "Assets/Recordings" 文件夹下
        savePath = Path.Combine(Application.dataPath, "Recordings");
#else
        // 在发布的游戏中（包括安卓），保存在安全的 persistentDataPath
        savePath = Application.persistentDataPath;
#endif

        // 如果文件夹不存在，则创建它
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // 组合最终的文件路径（先不加后缀）
        fullFilePath = Path.Combine(savePath, outputFileName);
        Debug.Log($"音频文件将被保存到: {Path.GetFullPath(fullFilePath)}");
        
        // 4. 检查UI关联
        if (recordButton == null || buttonText == null)
        {
            Debug.LogError("请在Inspector面板中关联Record Button和Button Text！");
            return;
        }
        
        // 5. 添加按钮监听
        recordButton.onClick.AddListener(OnRecordButtonPressed);

        // 6. 初始化UI
        UpdateUI();
    }

    /// <summary>
    /// 统一的按钮点击事件处理器
    /// </summary>
    private void OnRecordButtonPressed()
    {
        if (isRecording)
        {
            StopRecordingAndProcess();
        }
        else if (!isSaving)
        {
            StartRecording();
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    private void StartRecording()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        Debug.Log("开始录音...");
        // 预分配一个较长的缓冲区，并使用一个对语音足够高的采样率（22050Hz）
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 22050); 
        isRecording = true;
        UpdateUI();
    }

    /// <summary>
    /// 停止录音并处理音频数据（已移除自动播放）
    /// </summary>
    private void StopRecordingAndProcess()
    {
        int lastSamplePosition = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false;
        Debug.Log("停止录音...");

        if (lastSamplePosition <= 0)
        {
            Debug.LogWarning("录音时间过短，不进行处理。");
            UpdateUI();
            return;
        }

        AudioClip originalClip = audioSource.clip;
        
        // 从主线程准备好所有需要传递给后台线程的数据
        float[] audioData = new float[lastSamplePosition * originalClip.channels];
        originalClip.GetData(audioData, 0);
        
        int channels = originalClip.channels;
        int frequency = originalClip.frequency;
        
        // 进入“保存中”状态并更新UI
        isSaving = true;
        UpdateUI();

        // 异步调用保存方法，这部分保持不变
        SaveAudioAsync(audioData, channels, frequency);

    }
    
    /// <summary>
    /// 异步保存音频文件
    /// </summary>
    private async void SaveAudioAsync(float[] samples, int channels, int frequency)
    {
        string wavFilePath = fullFilePath + ".wav";
        Debug.Log("开始异步保存...");

        // 使用 Task.Run 将耗时的文件写入操作放到后台线程
        await Task.Run(() => 
        {
            SavWav.Save(wavFilePath, samples, frequency, channels);
        });

        // 当后台任务完成后，代码会回到主线程继续执行
        Debug.Log("异步保存完成！");
        
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        // 更新状态并刷新UI
        isSaving = false;
        UpdateUI();
    }

    /// <summary>
    /// 根据当前状态（录音、保存、空闲）更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (isSaving)
        {
            recordButton.interactable = false; // 保存时禁用按鈕
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