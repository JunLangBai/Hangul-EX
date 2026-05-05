using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 多任务进阶难度设置脚本
/// 用于在设置场景中调整游戏参数
/// </summary>
public class MultiTaskAdvancedSettings : MonoBehaviour
{
    private AllSettingCtr allSettingCtr;

    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }

    /// <summary>
    /// 当回合时长滑块值改变时调用
    /// </summary>
    /// <param name="value">滑块值（秒）</param>
    public void OnRoundDurationSliderChanged(float value)
    {
        if (allSettingCtr != null)
        {
            allSettingCtr.multiTaskRoundDuration = value;
            Debug.Log($"多任务进阶 - 回合时长设置为: {value}秒");
        }
    }

    /// <summary>
    /// 当总回合数滑块值改变时调用
    /// </summary>
    /// <param name="value">滑块值（回合数）</param>
    public void OnTotalRoundsSliderChanged(float value)
    {
        if (allSettingCtr != null)
        {
            allSettingCtr.multiTaskTotalRounds = Mathf.RoundToInt(value);
            Debug.Log($"多任务进阶 - 总回合数设置为: {Mathf.RoundToInt(value)}");
        }
    }

    /// <summary>
    /// 当每侧手势数量滑块值改变时调用
    /// </summary>
    /// <param name="value">滑块值（1-3）</param>
    public void OnGesturesPerSideSliderChanged(float value)
    {
        if (allSettingCtr != null)
        {
            allSettingCtr.multiTaskGesturesPerSide = Mathf.RoundToInt(value);
            Debug.Log($"多任务进阶 - 每侧手势数量设置为: {Mathf.RoundToInt(value)}");
        }
    }

    /// <summary>
    /// 当反馈延迟滑块值改变时调用
    /// </summary>
    /// <param name="value">滑块值（秒）</param>
    public void OnFeedbackDelaySliderChanged(float value)
    {
        if (allSettingCtr != null)
        {
            allSettingCtr.multiTaskFeedbackDelay = value;
            Debug.Log($"多任务进阶 - 反馈延迟设置为: {value}秒");
        }
    }
}
