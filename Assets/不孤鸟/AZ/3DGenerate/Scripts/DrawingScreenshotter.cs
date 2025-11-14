using UnityEngine;
using System.IO;

/// <summary>
/// 【新版 - 相机过滤法】
/// 负责使用一个专用的、已设置好 Culling Mask 的相机来截图，
/// 从而“过滤”掉UI元素。
/// </summary>
public class DrawingScreenshotter : MonoBehaviour
{
    [Header("截图相机 (必须)")]
    [Tooltip("拖入场景中你创建的那个'ScreenshotCamera'")]
    public Camera screenshotCamera;

    [Header("截图分辨率")]
    [Tooltip("截图的宽度 (像素)")]
    public int screenshotWidth = 1024;
    [Tooltip("截图的高度 (像素)")]
    public int screenshotHeight = 1024;

    /// <summary>
    /// 捕获 screenshotCamera 的内容并保存到指定路径
    /// </summary>
    /// <param name="savePath">包含文件名的完整保存路径</param>
    /// <returns>如果成功则返回路径，否则返回 null</returns>
    public string CaptureAndSaveToFile(string savePath)
    {
        if (screenshotCamera == null)
        {
            Debug.LogError("DrawingScreenshotter: 'screenshotCamera' 未设置! 无法截图。");
            return null;
        }

        try
        {
            // 1. 创建一个临时的 RenderTexture
            // 使用 ARGB32 格式以支持可能的透明背景
            RenderTexture rt = new RenderTexture(screenshotWidth, screenshotHeight, 24, RenderTextureFormat.ARGB32);

            // 2. 将此 RenderTexture 分配给截图相机
            screenshotCamera.targetTexture = rt;

            // 3. 手动命令相机渲染这一帧 (因为它平时是禁用的)
            // 这是关键一步：相机将只渲染它 Culling Mask 允许的图层 (即 'DrawingBoard')
            screenshotCamera.Render();

            // 4. 从 RenderTexture 中读取像素
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false); // 使用 ARGB32 格式
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // 5. 清理
            screenshotCamera.targetTexture = null;
            Destroy(rt);

            // 6. 将 Texture2D 编码为 PNG 并保存
            byte[] bytes = tex.EncodeToPNG(); // PNG 支持透明通道
            Destroy(tex);
            File.WriteAllBytes(savePath, bytes);
            
            Debug.Log($"[相机过滤法] 截图成功保存至: {savePath}");
            return savePath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[相机过滤法] 截图失败: {e.Message}");
            if (screenshotCamera.targetTexture != null)
            {
                screenshotCamera.targetTexture = null;
            }
            return null;
        }
    }
}