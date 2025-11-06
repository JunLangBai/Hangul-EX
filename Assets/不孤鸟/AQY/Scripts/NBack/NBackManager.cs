using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct Trial
{
    public int positionIndex;
    public bool isMatch;
}

public class NBackManager : MonoBehaviour
{
    [Header("游戏对象引用")]
    public GameObject[] gridCells;
    public Color normalColor = Color.gray;
    public Color stimulusColor = Color.green;
    
    [Header("UI 引用")]
    public Text scoreText;
    public Button matchButton;
    public Text feedbackText; // 用于显示游戏结束等信息

    private List<Trial> trialSequence = new List<Trial>();
    private int currentTrialIndex = 0;
    private int score = 0;
    private bool hasRespondedThisTrial = false;
    // 创建私有变量来存储从 GameSettings 读取的值
    private int nValue;
    private float stimulusDuration;
    private float interStimulusInterval;
    private int totalTrials;
    private int targetIndexN0;
    private float matchProbability;
    

    // 在 Start 或 Awake 中读取设置
    void Awake()
    {
        // 检查 GameSettings 实例是否存在
        if (NBackSetting.Instance != null)
        {
            // 从单例中加载所有设置
            nValue = NBackSetting.Instance.nValue;
            stimulusDuration = NBackSetting.Instance.stimulusDuration;
            interStimulusInterval = NBackSetting.Instance.interStimulusInterval;
            totalTrials = NBackSetting.Instance.totalTrials;
            targetIndexN0 = NBackSetting.Instance.targetIndexN0;
            matchProbability = NBackSetting.Instance.matchProbability;
            
            Debug.Log($"设置已加载: N={nValue}, 时长={stimulusDuration}");
        }
        else
        {
            Debug.LogError("NBackSetting 实例未找到! 请确保设置场景已首先加载。");
            // 在这里可以设置一些默认值以防万一
            nValue = 2;
            stimulusDuration = 2.0f;
            interStimulusInterval = 2.5f;
            totalTrials = 40;
            matchProbability = 0.33f;
        }
    }
    
    void Start()
    {
        if (feedbackText != null) feedbackText.text = "";
        
        foreach (var cell in gridCells)
        {
            SetCellColor(cell, normalColor);
        }

        matchButton.onClick.AddListener(OnMatchButtonPressed);
        UpdateScoreUI();
        
        GenerateTrialSequence();
        StartCoroutine(RunGame());
    }

    // 1. 根据N值分发任务序列的生成
    void GenerateTrialSequence()
    {
        switch (nValue)
        {
            case 0:
                GenerateN0Sequence();
                break;
            default: // N >= 1
                GenerateN1AndAboveSequence();
                break;
        }
    }

    // 为 N=0 (Go/No-Go) 生成序列
    void GenerateN0Sequence()
    {
        trialSequence.Clear();
        System.Random rand = new System.Random();

        Debug.Log($"生成 N=0 序列, 目标索引为: {targetIndexN0}");

        for (int i = 0; i < totalTrials; i++)
        {
            Trial newTrial = new Trial();
            newTrial.positionIndex = rand.Next(0, 9);
            // 匹配条件：当前亮起的方块是否是预设的目标方块
            newTrial.isMatch = (newTrial.positionIndex == targetIndexN0);
            trialSequence.Add(newTrial);
        }
    }

    // 为 N >= 1 生成序列 (原始逻辑)
    void GenerateN1AndAboveSequence()
    {
        trialSequence.Clear();
        System.Random rand = new System.Random();
        
        Debug.Log($"生成 N={nValue} 序列");

        for (int i = 0; i < totalTrials; i++)
        {
            Trial newTrial = new Trial();
            
            if (i >= nValue && Random.value < matchProbability)
            {
                newTrial.isMatch = true;
                newTrial.positionIndex = trialSequence[i - nValue].positionIndex;
            }
            else
            {
                newTrial.isMatch = false;
                int newPos = rand.Next(0, 9);
                
                if (i >= nValue && newPos == trialSequence[i - nValue].positionIndex)
                {
                    newPos = (newPos + 1) % 9;
                }
                newTrial.positionIndex = newPos;
            }
            trialSequence.Add(newTrial);
        }
    }

    // 2. 运行游戏循环的协程 (基本不变)
    IEnumerator RunGame()
    {
        matchButton.interactable = false;
        yield return new WaitForSeconds(2.0f);
        matchButton.interactable = true;

        for (currentTrialIndex = 0; currentTrialIndex < trialSequence.Count; currentTrialIndex++)
        {
            hasRespondedThisTrial = false;
            Trial currentTrial = trialSequence[currentTrialIndex];
            
            GameObject currentCell = gridCells[currentTrial.positionIndex];
            SetCellColor(currentCell, stimulusColor);

            yield return new WaitForSeconds(stimulusDuration);
            
            SetCellColor(currentCell, normalColor);
            
            if (currentTrial.isMatch && !hasRespondedThisTrial)
            {
                Debug.Log("Missed! 错过了一个匹配项。");
            }
            
            yield return new WaitForSeconds(interStimulusInterval);
        }
        
        Debug.Log("游戏结束！最终得分: " + score);
        if (feedbackText != null) feedbackText.text = "游戏结束!\n最终得分: " + score;
        matchButton.interactable = false;
    }

    // 3. 当玩家按下 "Match" 按钮时调用
    // 这个函数无需修改，因为isMatch标志在生成时已经正确设置了
    public void OnMatchButtonPressed()
    {
        if (hasRespondedThisTrial) return;
        hasRespondedThisTrial = true;
        
        // 检查索引是否有效，防止在协程间隙或结束后按键导致错误
        if (currentTrialIndex >= trialSequence.Count) return;

        Trial currentTrial = trialSequence[currentTrialIndex];

        if (currentTrial.isMatch)
        {
            score++;
            Debug.Log("Correct! 正确匹配! 得分: " + score);
        }
        else
        {
            score--;
            Debug.Log("Incorrect! 错误匹配! 得分: " + score);
        }
        UpdateScoreUI();
    }
    
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    void SetCellColor(GameObject cell, Color color)
    {
        Image image = cell.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            return;
        }
        
        Renderer renderer = cell.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}