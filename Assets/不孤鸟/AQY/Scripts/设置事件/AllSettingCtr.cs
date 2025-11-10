using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllSettingCtr : MonoBehaviour
{
    // 1. 实现单例模式所需的唯一的静态实例
    public static AllSettingCtr Instance { get; private set; }

    // --- N-Back 设置 ---
    [Header("N-Back 设置")]
    public int nValue = 2;
    public float stimulusDuration = 2.0f;
    public float interStimulusInterval = 2.5f;
    public int totalTrials = 40;
    public int targetIndexN0 = 4;
    public float matchProbability = 0.33f;
    public bool isOneBlockMode = false;

    // --- 记忆游戏设置 ---
    [Header("记忆游戏设置")]
    public int memoryDifficulty = 0; // 0=随机, 2=2个, 3=3个...
    public int memoryMode = 0;       // 0=自动, 1=顺序, 2=逆序

    // --- 方向游戏设置 ---
    [Header("方向游戏设置")]
    public float directionGameDuration = 10f;
    public bool directionIsRandomMode = false;
    public float directionGameRounds = 40f;

    // --- 注意力游戏设置 ---
    [Header("注意力游戏设置")]
    public int attentionGesturesPerMinute = 20;
    public int attentionTargetCount = 1;
    public float attentionFlashDuration = 1.0f;

    // 2. Awake 方法，用于实现单例模式
    private void Awake()
    {
        // 如果一个实例已经存在，并且不是当前这个，就销毁当前这个，保证唯一性
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        // 如果还没有实例，就将自己设为实例
        Instance = this;

        // 3. 关键：让这个 GameObject 在加载新场景时不会被销毁
        DontDestroyOnLoad(this.gameObject);
    }
}
