using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBackSetting : MonoBehaviour
{
    // 1. 创建一个公共的、静态的实例，以便任何脚本都可以访问它
    public static NBackSetting Instance;

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
    
    // 2. Awake方法，用于实现单例模式
    private void Awake()
    {
        // 如果已经有一个实例存在了，就销毁这个新的，保证唯一性
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; // 退出，不执行后面的代码
        }

        // 如果还没有实例，就将自己设为实例
        Instance = this;
        
        // 3. 最关键的一行：告诉Unity不要销毁这个GameObject
        DontDestroyOnLoad(this.gameObject);
    }

    public void OneBlockMode(bool b)
    {
        if (b)
        {
            nValue = 0;
            isOneBlockMode = true;
        }
        else
        {
            nValue = 1;
            isOneBlockMode = false;
        }
    }

    public void TotalTrials(float i)
    {
        totalTrials = (int)i;
    }
    
    public void Nvalue(float i)
    {
        if(!isOneBlockMode)
        {
            nValue = (int)i;
        }
    }
}
