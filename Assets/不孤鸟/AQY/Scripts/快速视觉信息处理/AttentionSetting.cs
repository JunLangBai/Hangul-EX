using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionSetting : MonoBehaviour
{
    // 1. 单例实例
    public static AttentionSetting Instance { get; private set; }
    
    // 2. 公共实例变量（带有默认值）
    //    其他场景将通过 AttentionSetting.Instance.GameDuration 访问
    public int GesturesPerMinute = 40;
    public int TargetCount = 1;
    public float FlashDuration = 0.1f; // 闪烁持续时间;
    

    void Awake()
    {
        // --- 单例模式逻辑 ---
        if (Instance != null && Instance != this)
        {
            // 如果一个实例已经存在，并且不是我，
            // 说明我们是从其他场景返回的，销毁这个新创建的重复对象。
            Destroy(gameObject);
            return;
        }

        // 我是第一个，将我设为单例实例
        Instance = this;

        // 关键：使该 GameObject 在加载新场景时“幸存”
        DontDestroyOnLoad(gameObject);
    }
    
    // 当 Slider 值改变时
    public void OnPerSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        GesturesPerMinute = Mathf.RoundToInt(value); 
    }
    
    // 当 Slider 值改变时
    public void OnTargetSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        TargetCount = Mathf.RoundToInt(value); 
    }

    public void OnFlashSliderChanged(float value)
    {
        FlashDuration = (float)Math.Round(value, 1);
    }
}
