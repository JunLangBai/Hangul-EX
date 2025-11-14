using System;
using System.Collections;
using System.IO;
using System.Collections.Generic; // <--- 确保 List 被引用
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using MemoryGameTools; 
using TripoForUnity;

// ASR服务器响应的数据结构 (假设它在 ASRManager.cs 中定义)
// [System.Serializable]
// public class ASRUploadResponse { ... } 

/// <summary>
/// 【最终安全版】
/// 结合使用 CanvasGroup (用于UI) 和 SetActive(false) (用于3D物体)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ExternalInputManager : MonoBehaviour
{
    [Header("API Key (必须)")]
    public string apiKey = "在此处粘贴你的API Key"; 

    [Header("新的UI按钮")]
    public Button btnStartASR;
    public TMP_Text asrButtonText;
    public Button btnStartScreenshot;

    // --- 【修改】我们现在需要两个列表 ---
    [Header("要隐藏的UI (2D)")]
    [Tooltip("拖入所有UI面板 (每个都必须有CanvasGroup)")]
    public List<CanvasGroup> UIsToHide; 

    [Header("要隐藏的物体 (3D)")]
    [Tooltip("拖入所有3D物体 (例如你的'画笔'模型)")]
    public List<GameObject> objectsToHide; 
    // --- 【修改结束】 ---

    [Header("模型显示时的UI (新)")]
    [Tooltip("此按钮在模型生成后出现，点击可隐藏模型并恢复主UI")]
    public Button btnHideModelAndShowMainUI;

    [Header("目标 UI Manager (必须)")]
    public TripoSimpleUI_Manager targetUIManager;

    [Header("功能组件 (必须)")]
    public BordController bordController;
    public DrawingScreenshotter screenshotter; 

    [Header("ASR (语音) 设置")]
    public string asrUploadURL = "http://your_server_ip:5001/upload";
    private const string TempAudioFileName = "latest_recording.wav";

    [Header("ASR 按钮状态文本")]
    public string text_StartRecording = "点击开始录音";
    public string text_StopRecording = "停止录音并生成";
    public string text_Processing = "处理中...";
    public string text_MicError = "无麦克风";
        
    private AudioSource audioSource;
    private string microphoneDeviceName;
    private bool isRecording = false;
    private bool isProcessing = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (UIsToHide == null || UIsToHide.Count == 0)
        {
            Debug.LogWarning("ExternalInputManager: 'UIsToHide' 列表为空。将无法隐藏2D UI。");
        }
        
        // 【新增】检查 3D 物体列表
        if (objectsToHide == null || objectsToHide.Count == 0)
        {
            Debug.LogWarning("ExternalInputManager: 'objectsToHide' 列表为空。将无法隐藏3D物体。");
        }
        
        if (btnHideModelAndShowMainUI == null)
        {
            Debug.LogError("ExternalInputManager: 'btnHideModelAndShowMainUI' 未设置!", this.gameObject);
        }
        else
        {
            btnHideModelAndShowMainUI.onClick.AddListener(OnHideModelButtonPress);
            btnHideModelAndShowMainUI.gameObject.SetActive(false); // 默认隐藏
        }

        if (targetUIManager == null)
            Debug.LogError("ExternalInputManager: 'targetUIManager' 未设置!");
        if (bordController == null)
            Debug.LogError("ExternalInputManager: 'bordController' 未设置!");
        if (screenshotter == null)
            Debug.LogError("ExternalInputManager: 'screenshotter' 未设置!");

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("未找到麦克风设备！");
            if (btnStartASR != null) btnStartASR.interactable = false;
            if (asrButtonText != null) asrButtonText.text = text_MicError;
            return;
        }
        microphoneDeviceName = Microphone.devices[0];

        if (btnStartASR != null)
            btnStartASR.onClick.AddListener(OnAsrButtonPress);
            
        if (btnStartScreenshot != null)
            btnStartScreenshot.onClick.AddListener(OnScreenshotButtonPress);

        UpdateAsrButtonText();
        
        if (targetUIManager != null)
        {
            if (targetUIManager.ApiKeyInputField != null)
            {
                targetUIManager.ApiKeyInputField.text = apiKey;
                targetUIManager.btnConfirmApiKey.onClick.Invoke();
                Debug.Log("API Key 已自动设置。");
            }
            else
            {
                Debug.LogError("在 'targetUIManager' 上找不到 ApiKeyInputField!");
            }
            
            var runtimeCore = targetUIManager.GetComponent<TripoRuntimeCore>();
            if (runtimeCore != null)
            {
                runtimeCore.OnModelGenerateComplete.AddListener(OnGenerationComplete);
            }
            else
            {
                Debug.LogError("在 'targetUIManager' 上找不到 TripoRuntimeCore! 无法重新显示UI。");
            }
        }
    }

    #region ASR (语音) 逻辑
    
    // ... (StartRecording, StopRecordingAndProcess, SaveAndUploadCoroutine 保持不变) ...
    
    private void OnAsrButtonPress()
    {
        if (isProcessing) return; 
        if (isRecording)
        {
            StopRecordingAndProcess();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        Debug.Log("开始录音...");
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 300, 44100);
        isRecording = true;
        UpdateAsrButtonText();
    }

    private void StopRecordingAndProcess()
    {
        var lastSamplePosition = Microphone.GetPosition(microphoneDeviceName);
        Microphone.End(microphoneDeviceName);
        isRecording = false; 

        if (lastSamplePosition <= 0)
        {
            Debug.LogWarning("录音时间过短或没有录到声音。");
            UpdateAsrButtonText();
            return;
        }

        var originalClip = audioSource.clip;
        var audioData = new float[lastSamplePosition * originalClip.channels];
        originalClip.GetData(audioData, 0);

        isProcessing = true;
        UpdateAsrButtonText();
        
        StartCoroutine(SaveAndUploadCoroutine(audioData, originalClip.channels, originalClip.frequency));
    }
    
    private IEnumerator SaveAndUploadCoroutine(float[] samples, int channels, int frequency)
    {
        var savePath = Application.persistentDataPath;
        var fullFilePath = Path.Combine(savePath, TempAudioFileName);

        bool success = false;
        bool isDone = false; 

        System.Threading.ThreadPool.QueueUserWorkItem((state) => 
        {
            try
            {
                success = SavWav.Save(fullFilePath, samples, frequency, channels);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Thread] 保存WAV文件时出错: {e.Message}");
                success = false;
            }
            finally
            {
                isDone = true; 
            }
        });
        
        while (!isDone)
        {
            yield return null; 
        }
        
        if (success)
        {
            StartCoroutine(UploadAudio(fullFilePath));
        }
        else
        {
            Debug.LogError($"本地保存文件失败！");
            isProcessing = false;
            UpdateAsrButtonText();
        }
    }


    private IEnumerator UploadAudio(string filePath)
    {
        if (!File.Exists(filePath))
        {
            isProcessing = false;
            UpdateAsrButtonText();
            yield break;
        }

        var form = new WWWForm();
        var fileData = File.ReadAllBytes(filePath);
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "audio/wav");

        using (var www = UnityWebRequest.Post(asrUploadURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"上传失败: {www.error}");
            }
            else
            {
                var jsonResponse = www.downloadHandler.text;
                var response = JsonUtility.FromJson<ASRUploadResponse>(jsonResponse);

                if (response != null && !string.IsNullOrEmpty(response.raw_transcription))
                {
                    Debug.Log("ASR 识别结果: " + response.raw_transcription);
                        
                    if (targetUIManager != null)
                    {
                        HideMainUI(); // <--- 调用混合隐藏
                        targetUIManager.TextPromptInputField.text = response.raw_transcription;
                        targetUIManager.btnTextToModelGenerate.onClick.Invoke();
                    }
                }
                else
                {
                    Debug.LogError("ASR 服务器处理失败或返回空文本: " + jsonResponse);
                }
            }
        }
            
        if (File.Exists(filePath)) File.Delete(filePath);
        isProcessing = false;
    }

    private void UpdateAsrButtonText()
    {
        if (asrButtonText == null || btnStartASR == null) return;
        if (isProcessing)
        {
            asrButtonText.text = text_Processing;
            btnStartASR.interactable = false;
        }
        else if (isRecording)
        {
            asrButtonText.text = text_StopRecording;
            btnStartASR.interactable = true;
        }
        else
        {
            asrButtonText.text = text_StartRecording;
            btnStartASR.interactable = true;
        }
    }
    #endregion

    #region Screenshot (画板) 逻辑
    
    private void OnScreenshotButtonPress()
    {
        bordController.ShowBord();
        StartCoroutine(TakeScreenshotAndProcessImage());
    }

    private IEnumerator TakeScreenshotAndProcessImage()
    {
        string screenshotPath = GetDynamicSavePath("drawing_screenshot.png");
        yield return new WaitForEndOfFrame(); 

        string capturedPath = screenshotter.CaptureAndSaveToFile(screenshotPath);

        if (!string.IsNullOrEmpty(capturedPath))
        {
            Debug.Log("截图成功，发送到 Tripo 图生模型...");
                
            if (targetUIManager != null)
            {
                HideMainUI(); // <--- 调用混合隐藏
                targetUIManager.ImagePathInputField.text = capturedPath;
                targetUIManager.btnLoadImage.onClick.Invoke(); 
                targetUIManager.btnImageToMdelGenerate.onClick.Invoke();
            }
        }
        else
        {
            Debug.LogError("截图失败，图生模型任务中止。");
        }
    }
    #endregion

    #region 辅助函数

    // --- 【修改】这两个方法现在会处理两种列表 ---
    
    /// <summary>
    /// 隐藏所有在列表中的UI和3D物体
    /// </summary>
    private void HideMainUI()
    {
        // 1. 隐藏 2D UI (CanvasGroup)
        if (UIsToHide != null && UIsToHide.Count > 0)
        {
            Debug.Log($"隐藏 {UIsToHide.Count} 个UI组...");
            foreach (CanvasGroup cg in UIsToHide) 
            {
                if (cg != null)
                {
                    cg.alpha = 0;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }
        }
        
        // 2. 隐藏 3D 物体 (GameObject)
        if (objectsToHide != null && objectsToHide.Count > 0)
        {
            Debug.Log($"隐藏 {objectsToHide.Count} 个3D物体...");
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 显示所有在列表中的UI和3D物体
    /// </summary>
    private void ShowMainUI()
    {
        // 1. 显示 2D UI (CanvasGroup)
        if (UIsToHide != null && UIsToHide.Count > 0)
        {
            Debug.Log($"显示 {UIsToHide.Count} 个UI组...");
            foreach (CanvasGroup cg in UIsToHide) 
            {
                if (cg != null)
                {
                    cg.alpha = 1;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }
        
        // 2. 显示 3D 物体 (GameObject)
        if (objectsToHide != null && objectsToHide.Count > 0)
        {
            Debug.Log($"显示 {objectsToHide.Count} 个3D物体...");
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
        
        UpdateAsrButtonText();
    }

    /// <summary>
    /// 当模型生成完毕时，由 TripoRuntimeCore 的事件调用
    /// </summary>
    private void OnGenerationComplete(string modelUrl)
    {
        Debug.Log("模型生成完毕，显示'隐藏模型'按钮。");
        
        if (btnHideModelAndShowMainUI != null)
        {
            btnHideModelAndShowMainUI.gameObject.SetActive(true);
        }

        if (targetUIManager != null && targetUIManager.SimpleModel != null)
        {
            targetUIManager.SimpleModel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 由 btnHideModelAndShowMainUI 按钮调用
    /// </summary>
    private void OnHideModelButtonPress()
    {
        Debug.Log("隐藏模型并恢复主UI...");
        
        // 1. 隐藏生成的模型
        if (targetUIManager != null && targetUIManager.SimpleModel != null)
        {
            targetUIManager.SimpleModel.SetActive(false);
        }
        
        // 2. 恢复主UI和3D物体
        ShowMainUI();
        
        // 3. 隐藏自己
        if (btnHideModelAndShowMainUI != null)
        {
            btnHideModelAndShowMainUI.gameObject.SetActive(false);
        }
    }
    
    // --- 【修改结束】 ---

    private string GetDynamicSavePath(string fileName)
    {
        #if UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, fileName);
        #elif UNITY_ANDROID
            return Path.Combine(Application.persistentDataPath, fileName);
        #else
            return Path.Combine(Application.persistentDataPath, fileName);
        #endif
    }
    #endregion
}