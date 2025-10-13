using System.Collections.Generic;
using UnityEngine;

// --- 橡皮功能 ---
/// <summary>
/// 定义画笔的模式：绘画或橡皮
/// </summary>
public enum DrawingMode
{
    Draw,
    Erase
}

/// <summary>
///     通过一个3D物体在另一个3D物体（白板）上实现带笔迹平滑的画板功能。
///     从原始的 DrawingBoard 脚本重构而来。
///     
///     重构亮点：
///     - 移除了基于UI的 IPointer... 事件处理器。
///     - 使用 Physics.Raycast 来检测3D画笔物体与白板的接触。
///     - 使用 RaycastHit.textureCoord 直接获取UV坐标，替代了屏幕坐标转换。
///     - 核心绘制逻辑（样条插值、撤销、橡皮）保持不变。
/// </summary>
public class DrawingBoard : MonoBehaviour
{
    [Header("场景对象")] 
    [Tooltip("代表笔尖的3D物体。它的Z轴（蓝色箭头）应指向绘画方向。")]
    public Transform brushTip;
    
    [Tooltip("从笔尖向前发射射线的最大距离，用于检测与白板的接触。")]
    public float brushRaycastDistance = 0.1f;

    [Header("画布纹理设置")] 
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    [Header("基础绘图设置")] 
    public float brushSize = 10f;
    public Color brushColor = Color.black;

    [Header("笔迹平滑 (抖动修正)")] 
    [Tooltip("设置相邻两个采样点的最小距离，距离太近的点会被忽略。")] 
    [Range(0.1f, 20f)]
    public float minPointDistance = 1.5f;

    [Tooltip("每个曲线段的细分程度，数值越高，曲线越平滑。")] 
    [Range(2, 20)]
    public int lineSubdivisions = 10;
    
    // --- 橡皮功能 ---
    private DrawingMode currentMode;
    private Color currentColor;
    // 橡皮使用完全透明的白色，这样可以擦除任何颜色
    private readonly Color eraserColor = new Color(1, 1, 1, 0); 

    // --- 私有字段 ---
    private Texture2D drawingTexture;
    private bool isDrawingOnBoard;
    private Color[] pixels;
    private readonly List<Vector2> strokePoints = new();
    private Collider boardCollider;
    
    // --- 撤销功能 ---
    private readonly Stack<Color[]> history = new Stack<Color[]>();

    private void Start()
    {
        // 1. 获取白板的碰撞体，用于射线检测
        boardCollider = GetComponent<Collider>();
        if (boardCollider == null)
        {
            Debug.LogError("WhiteboardDrawer 脚本需要附加到一个带有碰撞体（Collider）的游戏对象上！", this);
            return;
        }

        // 2. 初始化画布纹理
        drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        drawingTexture.filterMode = FilterMode.Bilinear; // Bilinear 对3D对象看起来更平滑
        drawingTexture.wrapMode = TextureWrapMode.Clamp;

        // 3. 将此纹理应用到当前物体（白板）的材质上
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // 创建材质实例，避免修改项目中的共享材质
            rend.material = new Material(rend.material); 
            rend.material.mainTexture = drawingTexture;
        }
        else
        {
            Debug.LogError("WhiteboardDrawer 需要一个 Renderer 组件来显示画板。", this);
        }

        pixels = new Color[drawingTexture.width * drawingTexture.height];
        ClearCanvasAndHistory();

        // 4. 默认启动时为绘画模式
        SetBrushMode(DrawingMode.Draw);
    }

    private void Update()
    {
        // 从笔尖位置沿着其Z轴（前方）发射一条短射线
        bool hitWhiteboard = Physics.Raycast(
            brushTip.position, 
            brushTip.forward, 
            out RaycastHit hit, 
            brushRaycastDistance);

        // 检查射线是否击中了我们这个白板物体
        if (hitWhiteboard && hit.collider == boardCollider)
        {
            // 将3D碰撞点的UV坐标转换为像素坐标
            Vector2 pixelCoord = new Vector2(
                hit.textureCoord.x * textureWidth,
                hit.textureCoord.y * textureHeight
            );

            // 如果这是第一次接触，则开始新的笔画
            if (!isDrawingOnBoard)
            {
                StartStroke(pixelCoord);
            }
            // 如果已经在绘画中，则继续笔画
            else
            {
                ContinueStroke(pixelCoord);
            }
        }
        // 如果射线没有击中，或者击中了其他物体，则结束笔画
        else if (isDrawingOnBoard)
        {
            EndStroke();
        }
    }

    /// <summary>
    /// 开始一个新的笔画 (相当于 OnPointerDown)
    /// </summary>
    private void StartStroke(Vector2 pixelCoord)
    {
        isDrawingOnBoard = true;
        SaveHistory();
        strokePoints.Clear();

        // 将第一个点重复加入，为 Catmull-Rom 计算做准备
        strokePoints.Add(pixelCoord);
        strokePoints.Add(pixelCoord);

        // 立刻画一个点，提供即时反馈
        DrawCircle(pixelCoord, brushSize);
        UpdateTexture();
    }

    /// <summary>
    /// 继续当前的笔画 (相当于 OnDrag)
    /// </summary>
    private void ContinueStroke(Vector2 pixelCoord)
    {
        // 检查与上一个点的距离，如果太近则忽略
        if (Vector2.Distance(pixelCoord, strokePoints[strokePoints.Count - 1]) < minPointDistance) return;

        strokePoints.Add(pixelCoord);

        // 当有足够点时，绘制样条曲线段
        if (strokePoints.Count >= 4)
        {
            var p0 = strokePoints[strokePoints.Count - 4];
            var p1 = strokePoints[strokePoints.Count - 3];
            var p2 = strokePoints[strokePoints.Count - 2];
            var p3 = strokePoints[strokePoints.Count - 1];

            DrawSplineSegment(p0, p1, p2, p3);
            UpdateTexture();
        }
    }

    /// <summary>
    /// 结束当前的笔画 (相当于 OnPointerUp)
    /// </summary>
    private void EndStroke()
    {
        isDrawingOnBoard = false;

        // 处理笔画的末尾
        if (strokePoints.Count >= 3)
        {
            // 复制最后一个点以完成曲线
            strokePoints.Add(strokePoints[strokePoints.Count - 1]);

            var p0 = strokePoints[strokePoints.Count - 4];
            var p1 = strokePoints[strokePoints.Count - 3];
            var p2 = strokePoints[strokePoints.Count - 2];
            var p3 = strokePoints[strokePoints.Count - 1];

            DrawSplineSegment(p0, p1, p2, p3);
            UpdateTexture();
        }
        strokePoints.Clear();
    }


    // --- 以下是您的原始代码，几乎无需修改 ---

    #region 公开方法 (用于UI按钮)

    /// <summary>
    /// 设置当前的画笔模式（绘画或橡皮）。
    /// </summary>
    public void SetBrushMode(DrawingMode mode)
    {
        currentMode = mode;
        currentColor = (currentMode == DrawingMode.Draw) ? brushColor : eraserColor;
    }

    /// <summary>
    /// 公开一个int版本，方便Unity Editor中的UI事件直接调用 (0=Draw, 1=Erase)
    /// </summary>
    public void SetBrushMode(int mode)
    {
        SetBrushMode((DrawingMode)mode);
    }
    
    /// <summary>
    /// 由滑动条控制的笔刷大小调整方法
    /// </summary>
    public void SetBrushSize(float value)
    {
        brushSize = value;
    }

    /// <summary>
    /// 撤销上一步操作
    /// </summary>
    public void Undo()
    {
        if (history.Count > 0)
        {
            var previousPixels = history.Pop();
            System.Array.Copy(previousPixels, pixels, previousPixels.Length);
            UpdateTexture();
        }
        else
        {
            Debug.Log("没有更多历史记录可供撤销。");
        }
    }

    /// <summary>
    /// 清空画布（此操作可被撤销）
    /// </summary>
    public void ClearCanvas()
    {
        SaveHistory();
        var fillColor = eraserColor; // 使用透明背景
        for (var i = 0; i < pixels.Length; i++) pixels[i] = fillColor;
        UpdateTexture();
    }
    
    /// <summary>
    /// 清空画布并重置所有历史记录
    /// </summary>
    public void ClearCanvasAndHistory()
    {
        history.Clear();
        var fillColor = eraserColor; // 使用透明背景
        for (var i = 0; i < pixels.Length; i++) pixels[i] = fillColor;
        UpdateTexture();
    }

    #endregion

    #region 核心绘制逻辑 (基本不变)
    
    private void SaveHistory()
    {
        var pixelsCopy = new Color[pixels.Length];
        System.Array.Copy(pixels, pixelsCopy, pixels.Length);
        history.Push(pixelsCopy);
    }

    private void DrawSplineSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var lastPoint = p1;
        for (var i = 1; i <= lineSubdivisions; i++)
        {
            var t = (float)i / lineSubdivisions;
            var currentPoint = GetCatmullRomPosition(t, p0, p1, p2, p3);
            DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
    }

    private Vector2 GetCatmullRomPosition(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var a = 2f * p1;
        var b = p2 - p0;
        var c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        var d = -p0 + 3f * p1 - 3f * p2 + p3;
        return 0.5f * (a + b * t + c * t * t + d * t * t * t);
    }
    
    private void DrawLine(Vector2 start, Vector2 end)
    {
        int x0 = (int)start.x, y0 = (int)start.y;
        int x1 = (int)end.x, y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            DrawCircle(new Vector2(x0, y0), brushSize);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
    
    private void DrawCircle(Vector2 center, float radius)
    {
        int cx = (int)center.x;
        int cy = (int)center.y;
        int r = Mathf.CeilToInt(radius);

        int startX = Mathf.Max(0, cx - r);
        int endX = Mathf.Min(textureWidth, cx + r + 1);
        int startY = Mathf.Max(0, cy - r);
        int endY = Mathf.Min(textureHeight, cy + r + 1);

        float rSq = radius * radius;

        for (var y = startY; y < endY; y++)
        for (var x = startX; x < endX; x++)
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= rSq)
            {
                // 使用Alpha混合，使橡皮擦（透明色）能正确地与现有颜色混合
                pixels[y * textureWidth + x] = BlendAlpha(pixels[y * textureWidth + x], currentColor);
            }
    }

    // 简单的Alpha混合函数，确保橡皮擦能正确工作
    private Color BlendAlpha(Color existingColor, Color newColor)
    {
        if (newColor.a == 1) return newColor; // 如果新颜色不透明，直接覆盖
        if (newColor.a == 0) return existingColor; // 如果新颜色完全透明，不做任何事 (虽然我们的橡皮擦是透明的，但混合算法可以处理)

        // 标准的 Alpha 混合公式
        float outA = newColor.a + existingColor.a * (1 - newColor.a);
        if (outA == 0) return Color.clear; // 避免除以零

        Color result;
        result.r = (newColor.r * newColor.a + existingColor.r * existingColor.a * (1 - newColor.a)) / outA;
        result.g = (newColor.g * newColor.a + existingColor.g * existingColor.a * (1 - newColor.a)) / outA;
        result.b = (newColor.b * newColor.a + existingColor.b * existingColor.a * (1 - newColor.a)) / outA;
        result.a = outA;
        return result;
    }

    private void UpdateTexture()
    {
        drawingTexture.SetPixels(pixels);
        drawingTexture.Apply(false);
    }
    #endregion
}