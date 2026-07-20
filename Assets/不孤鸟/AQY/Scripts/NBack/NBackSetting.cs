using System;
using System.Collections;
using System.Collections.Generic;
using RainbowArt.CleanFlatUI;
using UnityEngine;
using UnityEngine.UI; // 必须引入 UI 命名空间

public class NBackSetting : MonoBehaviour
{
    // 1. 单例引用
    private AllSettingCtr allSettingCtr;

    // 2. 引入对 UI 组件的引用
    [Header("UI 引用")]
    public Switch oneBlockToggle;
    public Slider totalTrialsSlider;
    public Slider nValueSlider;

    // 如果你有其他设置的 UI（比如显示时间等），可以像下面这样继续添加：
    // public Slider stimulusDurationSlider; 
    // public Slider matchProbabilitySlider;
    // ...

    private void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }

    // 3. 关键：场景加载时，读取单例中的值并同步给 UI
    private void Start()
    {
        if (allSettingCtr != null)
        {
            if (oneBlockToggle != null)
                oneBlockToggle.IsOn = allSettingCtr.isOneBlockMode;

            if (totalTrialsSlider != null)
                totalTrialsSlider.value = allSettingCtr.totalTrials;

            if (nValueSlider != null)
                nValueSlider.value = allSettingCtr.nValue;
        }
    }

    // --- 当 Toggle (One Block 模式) 改变时 ---
    public void OneBlockMode(bool b)
    {
        if (allSettingCtr == null) return;

        if (b)
        {
            allSettingCtr.nValue = 0;
            allSettingCtr.isOneBlockMode = true;
        }
        else
        {
            allSettingCtr.nValue = 1;
            allSettingCtr.isOneBlockMode = false;
        }

        // 【体验优化】如果 N-Value 的值被代码强制改变了，我们要让 UI 滑动条也跟着变！
        if (nValueSlider != null)
        {
            nValueSlider.value = allSettingCtr.nValue;
            
            // 可选：如果在 OneBlock 模式下不让玩家修改 N 值，可以直接禁用滑动条
            nValueSlider.interactable = !b; 
        }
    }

    // --- 当 Slider (总尝试次数) 改变时 ---
    public void TotalTrials(float i)
    {
        if (allSettingCtr != null)
        {
            allSettingCtr.totalTrials = Mathf.RoundToInt(i);
        }
    }
    
    // --- 当 Slider (N-Value 值) 改变时 ---
    public void Nvalue(float i)
    {
        if (allSettingCtr == null) return;

        if (!allSettingCtr.isOneBlockMode)
        {
            allSettingCtr.nValue = Mathf.RoundToInt(i);
        }
        else
        {
            // 【防御性逻辑】如果在 OneBlock 模式下玩家强行拖动滑动条
            // 强行把它弹回 0，防止数据错乱
            if (nValueSlider != null && nValueSlider.value != 0)
            {
                nValueSlider.value = 0;
            }
        }
    }
    
    // 如果你在 UI 上有调整 stimulusDuration 等变量的滑动条，仿照下面写：
    /*
    public void OnStimulusDurationChanged(float value)
    {
        if (allSettingCtr != null)
            allSettingCtr.stimulusDuration = (float)Math.Round(value, 1);
    }
    */
}