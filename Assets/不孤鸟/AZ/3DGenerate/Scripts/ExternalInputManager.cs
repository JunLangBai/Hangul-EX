using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
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
/// 【修改版】
/// 只负责 ASR (语音) 功能的管理。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ExternalInputManager : MonoBehaviour
{
    [Header("API Key (必须)")]
    public string apiKey = "在此处粘贴你的API Key"; 

    [Header("新的UI按钮")]
    public Button btnStartASR;
    public TMP_Text asrButtonText;
    // (截图按钮已移除)
    
    [Header("模型显示时的UI (新)")]
    [Tooltip("此按钮在模型生成后出现，点击可隐藏模型并恢复主UI")]
    public Button btnHideModelAndShowMainUI;

    [Header("目标 UI Manager (必须)")]
    public TripoSimpleUI_Manager targetUIManager;

    [Header("功能组件 (必须)")]
    // (截图器已移除)
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

        if (GlobalUIManager.Instance == null)
        {
            Debug.LogError("ExternalInputManager: 场景中未找到 GlobalUIManager! ", this);
            this.enabled = false;
            return;
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
        
        // (截图器检查已移除)
        
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
            
        // (截图按钮监听已移除)

        UpdateAsrButtonText();
        
        // 此脚本负责设置API Key
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
    
    // (这部分代码无变化)
    
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
        var savePath = GetDynamicSavePath(TempAudioFileName); // <--- 使用了 GetDynamicSavePath

        bool success = false;
        bool isDone = false; 

        System.Threading.ThreadPool.QueueUserWorkItem((state) => 
        {
            try
            {
                success = SavWav.Save(savePath, samples, frequency, channels);
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
            StartCoroutine(UploadAudio(savePath));
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
                        GlobalUIManager.Instance.HideAllManagedItems(); // <--- 调用 GlobalUIManager
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

    // (截图区域已删除)

    #region 辅助函数
    
    // (这部分代码被保留，因为两个脚本都需要它)

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
        
        if (targetUIManager != null && targetUIManager.SimpleModel != null)
        {
            targetUIManager.SimpleModel.SetActive(false);
        }
        
        GlobalUIManager.Instance.ShowAllManagedItems(); 
        
        // 恢复UI后，刷新一下按钮文本
        UpdateAsrButtonText();
        
        if (btnHideModelAndShowMainUI != null)
        {
            btnHideModelAndShowMainUI.gameObject.SetActive(false);
        }
    }
    
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