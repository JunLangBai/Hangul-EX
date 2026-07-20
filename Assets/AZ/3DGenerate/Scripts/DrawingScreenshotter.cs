using System;
using UnityEngine;
using System.IO;
using TripoForUnity;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 【新版 - 相机过滤法】
/// 负责使用一个专用的、已设置好 Culling Mask 的相机来截图，
/// 从而“过滤”掉UI元素。
/// </summary>
public class DrawingScreenshotter : MonoBehaviour
{
    public Slider progressSlider;
    public GameObject simpleModel;
    
    private DrawingBoard drawingBoard;

    // 定义你要保存的子文件夹名
    private string saveFolderName = "SavedImages";
    
    // 定义要保存的文件名（确保每次不同，或者可以覆盖）
    private string filename = "myCapturedDrawing.png";
    
    bool isGenerating = false;

    private TripoRuntimeCore tripoRuntime;
    private void Start()
    {
        simpleModel.GetComponent<BoxCollider>().enabled = false;
        tripoRuntime = FindObjectOfType<TripoRuntimeCore>().GetComponent<TripoRuntimeCore>();
        drawingBoard = FindObjectOfType<DrawingBoard>().GetComponent<DrawingBoard>();
    }

    private void Update()
    {
        if (isGenerating)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = tripoRuntime.imageToModelProgress;
        }
        else
        {
            progressSlider.value = 0;
            progressSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 捕获 screenshotCamera 的内容并保存到指定路径
    /// </summary>
    /// <param name="savePath">包含文件名的完整保存路径</param>
    /// <returns>如果成功则返回路径，否则返回 null</returns>
    public void CaptureAndSaveToFile()
    {
        string directoryPath = ""; // 存储目录路径
        string fullFilePath = ""; // 存储完整文件路径

        // 2. [关键] 使用预处理指令区分平台
#if UNITY_EDITOR
        // 平台：Unity编辑器
        // 路径：Assets/SavedImages
        directoryPath = Path.Combine(Application.dataPath, saveFolderName);
#else
        // 平台：安卓设备 (或 iOS, PC build 等)
        // 路径：Application.persistentDataPath/SavedImages
        directoryPath = Path.Combine(Application.persistentDataPath, saveFolderName);
#endif

        // 3. 检查并创建目录
        if (!Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (IOException e)
            {
                Debug.LogError($"创建目录失败: {e.Message}");
                return; // 创建失败，无法继续
            }
        }

        // 4. 组合成最终的完整文件路径
        fullFilePath = Path.Combine(directoryPath, filename);

        // 5. 检查画板和贴图 (你的原始逻辑)
        if (drawingBoard == null || drawingBoard.drawingTexture == null)
        {
            Debug.LogError("画板或画板贴图 (drawingTexture) 未设置！");
            return;
        }
        
        // 6. 编码并保存文件
        try
        {
            byte[] bytes = drawingBoard.drawingTexture.EncodeToPNG();
            File.WriteAllBytes(fullFilePath, bytes);
            
            Debug.Log($"图片成功保存至: {fullFilePath}");
            tripoRuntime.SetImagePath(fullFilePath).ImageToModel();
            isGenerating = true;
            simpleModel.GetComponent<BoxCollider>().enabled = true;
        }
        catch (IOException e)
        {
            Debug.LogError($"保存文件失败: {e.Message}");
            return;
        }

        // 7. [仅限编辑器] 刷新 AssetDatabase
        // 这是为了让你保存在 Assets 文件夹中的图片能立刻显示在 Project 窗口
#if UNITY_EDITOR
        AssetDatabase.Refresh();
        Debug.Log("编辑器环境：已刷新 AssetDatabase。");
#endif
    }

    public void ChangeBool()
    {
        isGenerating = false;
    }

}