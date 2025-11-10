using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBackSetting : MonoBehaviour
{

    [Header("N-Back 游戏设置")]
    [Range(0, 5)]
    public int nValue = 2;
    public float stimulusDuration = 2.0f;
    public float interStimulusInterval = 2.5f;
    public int totalTrials = 40;
    
    // N=0 模式的目标
    [Range(0, 8)]
    public int targetIndexN0 = 4;
    
    // --- 在这里添加被遗漏的变量 ---
    [Header("序列生成设置")]
    [Tooltip("在N≥1模式下，一个试验成为'匹配项'的大致概率。")]
    [Range(0.1f, 0.9f)]
    public float matchProbability = 0.33f;

    private bool isOneBlockMode = false;
    
    private AllSettingCtr allSettingCtr;

    private void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }

    public void OneBlockMode(bool b)
    {
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
    }

    public void TotalTrials(float i)
    {
        allSettingCtr.totalTrials = (int)i;
    }
    
    public void Nvalue(float i)
    {
        if(!allSettingCtr.isOneBlockMode)
        {
            allSettingCtr.nValue = (int)i;
        }
    }
}
