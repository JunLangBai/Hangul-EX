using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionSetting : MonoBehaviour
{
    // 1. 单例实例
    private  AllSettingCtr allSettingCtr; 
    
    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }
    
    // 当 Slider 值改变时
    public void OnCountSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        allSettingCtr.emotionCount = Mathf.RoundToInt(value); 
    }
    
    // 当 Slider 值改变时
    public void OnDisplaySliderChanged(float value)
    {
        // 只更新内存中的实例变量
        allSettingCtr.emotionDisplayTime = (float)Math.Round(value, 1);
    }
}
