using System; // 这是使用 'Action' 的必要条件
using UnityEngine;

/// <summary>
/// 静态事件中心（“广播电台”）。
/// 它定义了所有UI可以触发的“节目”（事件）。
/// </summary>
public static class DrawingActions
{
    // --- 定义事件（节目列表） ---

    // 'Action' 是一个委托（delegate），代表一个不返回任何东西的方法。
    // 'static' 意味着它属于这个类，而不是类的某个实例。
    // 'event' 是一种安全的机制，只允许外部通过 '+= 和 '-=' 来订阅/取消订阅。
    public static event Action OnUndoClicked;
    public static event Action OnClearCanvasClicked;
    public static event Action OnClearAllClicked;
    public static event Action OnDefaultPenClicked;

    // 这些 'Action<T>' 代表“带参数的节目”
    public static event Action<int> OnColorChanged;
    public static event Action<float> OnBrushSizeChanged;
    public static event Action<int> OnModeChanged;


    // --- 定义触发方法（DJ的“喊话”按钮） ---

    // 任何脚本都可以调用这些静态方法来“广播”一个事件。
    // '?.Invoke()' 是一个安全调用：
    // 它会检查 "是否有人(收音机)正在收听这个节目？"
    // 如果是，就播放它 (Invoke)。
    // 如果不是 (null)，就什么也不做（也不会报错）。
    
    public static void TriggerUndo() => OnUndoClicked?.Invoke();
    public static void TriggerClearCanvas() => OnClearCanvasClicked?.Invoke();
    public static void TriggerClearAll() => OnClearAllClicked?.Invoke();
    public static void TriggerDefaultPen() => OnDefaultPenClicked?.Invoke();

    public static void TriggerColorChange(int colorIndex) => OnColorChanged?.Invoke(colorIndex);
    public static void TriggerBrushSizeChange(float size) => OnBrushSizeChanged?.Invoke(size);
    public static void TriggerModeChange(int mode) => OnModeChanged?.Invoke(mode);
}