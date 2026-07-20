using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmotionSetting : MonoBehaviour
{
    // 1. 单例实例
    private  AllSettingCtr allSettingCtr; 
    
    // 2. 引入对 UI Slider 的引用
    [Header("UI 引用")]
    public Slider countSlider;
    public Slider displaySlider;
    
    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }

    // 3. 关键：场景加载时，读取单例中的值并更新给 UI
    void Start()
    {
        if (allSettingCtr != null)
        {
            if (countSlider != null)
                countSlider.value = allSettingCtr.emotionCount;

            if (displaySlider != null)
                displaySlider.value = allSettingCtr.emotionDisplayTime;
        }
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
