using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionSetting : MonoBehaviour
{
    // 1. 单例实例
    public  AllSettingCtr allSettingCtr; 
    
    // 2. 公共实例变量（带有默认值）
    //    其他场景将通过 AttentionSetting.Instance.GameDuration 访问
    public int GesturesPerMinute = 20;
    public int TargetCount = 1;
    public float FlashDuration = 1; // 闪烁持续时间;
    

    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }
    
    // 当 Slider 值改变时
    public void OnPerSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        allSettingCtr.attentionGesturesPerMinute = Mathf.RoundToInt(value); 
    }
    
    // 当 Slider 值改变时
    public void OnTargetSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        allSettingCtr.attentionTargetCount = Mathf.RoundToInt(value); 
    }

    public void OnFlashSliderChanged(float value)
    {
        allSettingCtr.attentionFlashDuration = (float)Math.Round(value, 1);
    }
}
