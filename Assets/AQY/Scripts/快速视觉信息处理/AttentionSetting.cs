using System;
using UnityEngine;
using UnityEngine.UI; // 必须引入 UI 命名空间

public class AttentionSetting : MonoBehaviour
{
    // 1. 获取单例引用
    private AllSettingCtr allSettingCtr; 
    
    // 2. 引入对你的 UI Slider 的引用（需要在 Unity 面板中拖拽赋值）
    [Header("UI 引用")]
    public Slider perSlider;
    public Slider targetSlider;
    public Slider flashSlider;

    // 注意：把原来的 public int GesturesPerMinute = 20 等变量删除了，
    // 因为数据只应该存在于 AllSettingCtr 单例中，避免数据存在两份导致不同步。

    void Awake()
    {
        // 建议在 Awake 中获取引用
        allSettingCtr = AllSettingCtr.Instance;
    }

    void Start()
    {
        // 3. 关键步骤：每次进入这个场景时，读取单例中保存的值，并强行赋给 UI 组件
        // 使用 Start 而不是 Awake，可以确保 AllSettingCtr 已经初始化完毕
        if (allSettingCtr != null)
        {
            if (perSlider != null) 
                perSlider.value = allSettingCtr.attentionGesturesPerMinute;
                
            if (targetSlider != null) 
                targetSlider.value = allSettingCtr.attentionTargetCount;
                
            if (flashSlider != null) 
                flashSlider.value = allSettingCtr.attentionFlashDuration;
        }
    }
    
    // --- 以下是绑在 Slider 的 OnValueChanged 事件上的方法 ---

    public void OnPerSliderChanged(float value)
    {
        if (allSettingCtr != null)
            allSettingCtr.attentionGesturesPerMinute = Mathf.RoundToInt(value); 
    }
    
    public void OnTargetSliderChanged(float value)
    {
        if (allSettingCtr != null)
            allSettingCtr.attentionTargetCount = Mathf.RoundToInt(value); 
    }

    public void OnFlashSliderChanged(float value)
    {
        if (allSettingCtr != null)
            allSettingCtr.attentionFlashDuration = (float)Math.Round(value, 1);
    }
}