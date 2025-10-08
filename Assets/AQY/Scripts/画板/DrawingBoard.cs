using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
///     在 RawImage 上实现带笔迹平滑（抖动修正）的画板功能
///     重构亮点：
///     - 引入 Catmull-Rom 样条插值算法，对输入点进行平滑处理，解决线条抖动问题。
///     - 添加最“小点距”判断，避免在原地不动时产生大量冗余数据点。
///     - 添加“线段细分”参数，可以控制曲线的平滑程度。
///     - 优化了数据结构和绘制逻辑，只在需要时才绘制新的曲线段。
///     - 新增了撤销功能 (Undo)。
/// </summary>
public class DrawingBoard : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IDragHandler,
    IInitializePotentialDragHandler
{
    [Header("UI 组件")] public RawImage drawingImage;

    [Header("基础绘图设置")] public float brushSize = 5f;

    public Color brushColor = Color.black;

    [Header("笔迹平滑 (抖动修正)")] [Tooltip("设置相邻两个采样点的最小距离，距离太近的点会被忽略，可以有效过滤静止时的抖动。")] [Range(0.1f, 10f)]
    public float minPointDistance = 1.5f;

    [Tooltip("每个曲线段的细分程度，数值越高，曲线越平滑，但计算量也越大。")] [Range(2, 20)]
    public int lineSubdivisions = 10;

    // 私有字段
    private Texture2D drawingTexture;
    private bool isDrawing;
    private Color[] pixels;

    // 用于存储当前笔画的所有采样点
    private readonly List<Vector2> strokePoints = new();

    // --- 撤销功能 ---
    // 用于存储历史记录的栈
    private readonly Stack<Color[]> history = new Stack<Color[]>();


    private void Start()
    {
        // 初始化画布
        // 建议使用 ARGB32 格式，因为它支持透明度，为未来实现橡皮擦功能做准备
        drawingTexture = new Texture2D(512, 512, TextureFormat.ARGB32, false);
        drawingTexture.filterMode = FilterMode.Point;
        drawingTexture.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color[drawingTexture.width * drawingTexture.height];
        ClearCanvasAndHistory(); // 初始时清空画布和历史记录
        drawingImage.texture = drawingTexture;
    }

    /// <summary>
    ///     拖拽时，持续添加点并绘制平滑曲线
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        var coord = GetTextureCoord(eventData.position, eventData.pressEventCamera);
        if (!IsValidCoordinate(coord))
        {
            // 如果拖出范围，则结束当前笔画
            OnPointerUp(eventData);
            return;
        }

        // 检查与上一个点的距离，如果太近则忽略，防止点过于密集
        var dist = Vector2.Distance(coord, strokePoints[strokePoints.Count - 1]);
        if (dist < minPointDistance) return;

        strokePoints.Add(coord);

        // 当我们有足够多的点（至少4个）时，就可以开始绘制样条曲线了
        // 我们总是绘制倒数第二段曲线（从 P1 到 P2），因为它现在有了完整的4个控制点（P0, P1, P2, P3）
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

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    /// <summary>
    ///     按下时，开始一个新的笔画
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // --- 撤销功能 ---
        // 在开始新的笔画之前，保存当前画布状态
        SaveHistory();

        isDrawing = true;

        // 清空上一笔的点
        strokePoints.Clear();

        var coord = GetTextureCoord(eventData.position, eventData.pressEventCamera);
        if (!IsValidCoordinate(coord))
        {
            isDrawing = false;
            return;
        }

        // 将第一个点重复加入，为 Catmull-Rom 计算做准备
        // 样条曲线需要4个点来定义一段，我们通过复制首尾点来处理笔画的开始和结束
        strokePoints.Add(coord);
        strokePoints.Add(coord);

        // 立刻画一个点，提供即时反馈
        DrawCircle(coord, brushSize);
        UpdateTexture();
    }



    /// <summary>
    ///     抬起时，结束笔画并处理最后一段曲线
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrawing) return;
        isDrawing = false;

        // 处理笔画的末尾
        if (strokePoints.Count >= 3)
        {
            // 为了补完最后一段，我们复制最后一个点作为终点控制点
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
    
    // --- 撤销功能 ---
    /// <summary>
    /// 撤销上一步操作
    /// </summary>
    public void Undo()
    {
        if (history.Count > 0)
        {
            // 从栈中弹出上一个状态的像素数据
            var previousPixels = history.Pop();
            
            // 将当前像素数组恢复到上一个状态
            // System.Array.Copy 比 for 循环更快
            System.Array.Copy(previousPixels, pixels, previousPixels.Length);
            
            // 更新纹理以显示变化
            UpdateTexture();
        }
        else
        {
            Debug.Log("没有更多历史记录可供撤销。");
        }
    }

    // --- 撤销功能 ---
    /// <summary>
    /// 保存当前画布状态到历史记录
    /// </summary>
    private void SaveHistory()
    {
        // 创建当前像素数组的副本
        var pixelsCopy = new Color[pixels.Length];
        System.Array.Copy(pixels, pixelsCopy, pixels.Length);
        
        // 将副本压入栈中
        history.Push(pixelsCopy);
    }


    /// <summary>
    ///     绘制一段 Catmull-Rom 样条曲线（从 p1 到 p2）
    /// </summary>
    private void DrawSplineSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var lastPoint = p1;

        // 通过在 p1 和 p2 之间进行插值来创建平滑曲线
        // lineSubdivisions 决定了这条曲线由多少个小直线段构成
        for (var i = 1; i <= lineSubdivisions; i++)
        {
            var t = (float)i / lineSubdivisions;
            var currentPoint = GetCatmullRomPosition(t, p0, p1, p2, p3);
            DrawLine(lastPoint, currentPoint); // 用短直线连接插值点，形成曲线
            lastPoint = currentPoint;
        }
    }

    /// <summary>
    ///     Catmull-Rom 样条插值函数
    /// </summary>
    private Vector2 GetCatmullRomPosition(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        // Catmull-Rom样条公式
        var a = 2f * p1;
        var b = p2 - p0;
        var c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        var d = -p0 + 3f * p1 - 3f * p2 + p3;

        return 0.5f * (a + b * t + c * t * t + d * t * t * t);
    }

    // --- 以下是基本绘图和坐标转换函数 ---

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
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void DrawCircle(Vector2 center, float radius)
    {
        var cx = (int)center.x;
        var cy = (int)center.y;
        var r = Mathf.CeilToInt(radius);

        var startX = Mathf.Max(0, cx - r);
        var endX = Mathf.Min(drawingTexture.width, cx + r + 1);
        var startY = Mathf.Max(0, cy - r);
        var endY = Mathf.Min(drawingTexture.height, cy + r + 1);

        var rSq = radius * radius;

        for (var y = startY; y < endY; y++)
        for (var x = startX; x < endX; x++)
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= rSq)
                pixels[y * drawingTexture.width + x] = brushColor;
    }

    private Vector2 GetTextureCoord(Vector2 screenPoint, Camera cam)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawingImage.rectTransform, screenPoint, cam, out var localPoint))
        {
            var rect = drawingImage.rectTransform.rect;
            var w = rect.width;
            var h = rect.height;

            var uv = new Vector2(
                (localPoint.x + w * 0.5f) / w,
                (localPoint.y + h * 0.5f) / h
            );

            if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
                return new Vector2(uv.x * drawingTexture.width, uv.y * drawingTexture.height);
        }

        return new Vector2(-1, -1);
    }

    private bool IsValidCoordinate(Vector2 coord)
    {
        return coord.x != -1;
    }

    private void UpdateTexture()
    {
        drawingTexture.SetPixels(pixels);
        drawingTexture.Apply(false);
    }
    
    /// <summary>
    /// 清空画布（此操作可被撤销）
    /// </summary>
    public void ClearCanvas()
    {
        // --- 撤销功能 ---
        // 保存清除前的状态，这样清除操作本身也可以被撤销
        SaveHistory();

        var fillColor = Color.white;
        for (var i = 0; i < pixels.Length; i++) pixels[i] = fillColor;
        UpdateTexture();
    }
    
    // --- 撤销功能 --- 
    /// <summary>
    /// 清空画布并重置所有历史记录
    /// </summary>
    public void ClearCanvasAndHistory()
    {
        history.Clear();
        var fillColor = Color.white;
        for (var i = 0; i < pixels.Length; i++) pixels[i] = fillColor;
        UpdateTexture();
    }

    public Texture2D GetDrawingTexture()
    {
        return drawingTexture;
    }
}