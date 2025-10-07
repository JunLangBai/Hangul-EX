using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO; // 【新增】引入System.IO来处理文件路径

[RequireComponent(typeof(AudioSource))]
public class MicrophoneController : MonoBehaviour
{
    // ... (您之前的UI关联和自定义按钮外观变量) ...
    [Header("UI元素关联")]
    [Tooltip("请将您在场景中创建的Button-TMP拖到这里")]
    public Button recordButton;
    [Tooltip("请将您按钮下的TextMeshPro文本组件拖到这里")]
    public TextMeshProUGUI buttonText;

    [Header("自定义按钮外观")]
    [Tooltip("未录音时，按钮上显示的文字")]
    public string startRecordingText = "开始录音";
    [Tooltip("正在录音时，按钮上显示的文字")]
    public string stopRecordingText = "停止并播放";
    [Tooltip("未录音时，按钮的颜色")]
    public Color idleColor = Color.white;
    [Tooltip("正在录音时，按钮的颜色")]
    public Color recordingColor = Color.red;

    // 【新增】文件保存设置
    [Header("音频文件设置")]
    [Tooltip("保存的音频文件名（无需后缀）")]
    public string outputFileName = "my_recorded_audio";

    // ... (私有变量) ...
    private AudioSource audioSource;
    private bool isRecording = false;
    private string microphoneDeviceName;
    private string fullFilePath; // 【新增】用于存储完整文件路径

    void Start()
    {
        // 1. 检查麦克风
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

        // 2. 获取组件
        microphoneDeviceName = Microphone.devices[0];
        audioSource = GetComponent<AudioSource>();

        // 3. 【核心修改】根据不同平台设置保存路径
        string savePath;

#if UNITY_EDITOR
        // 在Unity编辑器中，保存在 "Assets/Recordings" 文件夹下
        savePath = Path.Combine(Application.dataPath, "Recordings");
#else
        // 在发布的游戏中，保存在安全的 persistentDataPath
        savePath = Application.persistentDataPath;
#endif

        // 如果文件夹不存在，则创建它
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // 组合最终的文件路径（这里我们先不加后缀，方便后面切换wav/mp3）
        fullFilePath = Path.Combine(savePath, outputFileName);
        
        // 在编辑器模式下，让Unity刷新资源数据库，这样新文件才能显示出来
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        
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

    private void OnRecordButtonPressed()
    {
        if (isRecording)
        {
            // --- 停止录音 ---
            Microphone.End(microphoneDeviceName);
            isRecording = false;
            Debug.Log("停止录音...");

            // 【新增】调用保存方法
            SaveAudio();

            // 播放录制的音频
            if (audioSource.clip != null)
            {
                audioSource.Play();
            }
        }
        else
        {
            // --- 开始录音 ---
            // 开始前先确保之前的播放已停止
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            Debug.Log("开始录音...");
            audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 44100); // 录制300秒 (5分钟)
            isRecording = true;
        }

        UpdateUI();
    }

    /// <summary>
    /// 【新增】保存音频文件的方法
    /// </summary>
    private void SaveAudio()
    {
        if (audioSource.clip == null)
        {
            Debug.LogError("没有可保存的音频片段！");
            return;
        }
        
        // 使用我们的帮助类来保存文件
        Debug.Log($"正在保存音频文件到: {fullFilePath}");
        SavWav.Save(fullFilePath, audioSource.clip);
    }

    private void UpdateUI()
    {
        // ... (此部分代码无需改变) ...
        if (isRecording)
        {
            buttonText.text = stopRecordingText;
            recordButton.GetComponent<Image>().color = recordingColor;
        }
        else
        {
            buttonText.text = startRecordingText;
            recordButton.GetComponent<Image>().color = idleColor;
        }
    }
}