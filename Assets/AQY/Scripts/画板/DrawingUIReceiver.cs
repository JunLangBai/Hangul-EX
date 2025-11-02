using UnityEngine;

/// <summary>
/// UI事件接收器（“电台主持人/DJ”）。
/// 它的唯一工作是：从Unity的UI按钮（OnClick）获取事件，
/// 然后调用 DrawingActions 中的静态 Trigger 方法来“广播”它们。
/// </summary>
public class DrawingUIReceiver : MonoBehaviour
{
    // --- 撤销/清除 ---
    public void HandleUndo() => DrawingActions.TriggerUndo();
    public void HandleClearCanvas() => DrawingActions.TriggerClearCanvas();
    public void HandleClearAll() => DrawingActions.TriggerClearAll();

    // --- 画笔设置 ---
    public void HandleDefaultPen() => DrawingActions.TriggerDefaultPen();
    public void HandleColorChange(int colorIndex) => DrawingActions.TriggerColorChange(colorIndex);
    
    // 这个方法用于接收 笔刷大小(Brush Size) 的 Slider（滑动条）
    public void HandleBrushSizeChange(float size) => DrawingActions.TriggerBrushSizeChange(size);
    
    // 这个方法用于 绘画/橡皮 模式切换按钮 (0=Draw, 1=Erase)
    public void HandleModeChange(int mode) => DrawingActions.TriggerModeChange(mode);
}