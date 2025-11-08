using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    
    // --- 修改 1: 添加对3D刺激物的引用 ---
    public GameObject stimulus3DObject; // 将您想显示的3D对象拖到这里

    // --- 已删除: 不再需要颜色变量 ---
    // public Color normalColor = Color.gray;
    // public Color stimulusColor = Color.green;
    
    [Header("UI 引用")]
    public TextMeshPro scoreText;
    public Button matchButton;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI feedbackText;

    private List<Trial> trialSequence = new List<Trial>();
    private int currentTrialIndex = 0;
    private int score = 0;
    private bool hasRespondedThisTrial = false;
    
    private int nValue;
    private float stimulusDuration;
    private float interStimulusInterval;
    private int totalTrials;
    private int targetIndexN0;
    private float matchProbability;
    

    void Awake()
    {
        if (NBackSetting.Instance != null)
        {
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
            nValue = 2;
            stimulusDuration = 2.0f;
            interStimulusInterval = 2.5f;
            totalTrials = 40;
            matchProbability = 0.33f;
        }
    }
    
    void Start()
    {
        if (feedbackText != null) feedbackText.text = "↓↓ 向下低头看向平面进行游戏 ↓↓";
        
        // --- 修改 2: 确保3D对象在游戏开始时是隐藏的 ---
        if (stimulus3DObject != null)
        {
            stimulus3DObject.SetActive(false);
        }
        else
        {
            Debug.LogError("刺激物3D对象 (stimulus3DObject) 未在检视器中设置!");
        }

        matchButton.onClick.AddListener(OnMatchButtonPressed);
        UpdateScoreUI();
        
        GenerateTrialSequence();
        StartCoroutine(RunGame());
    }

    // ... (GenerateTrialSequence, GenerateN0Sequence, GenerateN1AndAboveSequence 函数保持不变)
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

    void GenerateN0Sequence()
    {
        trialSequence.Clear();
        System.Random rand = new System.Random();

        Debug.Log($"生成 N=0 序列, 目标索引为: {targetIndexN0}");

        for (int i = 0; i < totalTrials; i++)
        {
            Trial newTrial = new Trial();
            newTrial.positionIndex = rand.Next(0, 9);
            newTrial.isMatch = (newTrial.positionIndex == targetIndexN0);
            trialSequence.Add(newTrial);
        }
    }

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


    IEnumerator RunGame()
    {
        matchButton.interactable = false;
        yield return new WaitForSeconds(2.0f);
        matchButton.interactable = true;

        for (currentTrialIndex = 0; currentTrialIndex < trialSequence.Count; currentTrialIndex++)
        {
            hasRespondedThisTrial = false;
            Trial currentTrial = trialSequence[currentTrialIndex];
            
            // 获取当前试次应该显示刺激物的位置
            GameObject currentCell = gridCells[currentTrial.positionIndex];

            // --- 修改 3: 将3D对象移动到目标位置并显示它 ---
            stimulus3DObject.transform.position = currentCell.transform.position;
            stimulus3DObject.SetActive(true);

            // 等待刺激持续时间
            yield return new WaitForSeconds(stimulusDuration);
            
            // --- 修改 4: 隐藏3D对象 ---
            stimulus3DObject.SetActive(false);
            
            if (currentTrial.isMatch && !hasRespondedThisTrial)
            {
                Debug.Log("Missed! 错过了一个匹配项。");
            }
            
            // 等待试次间隔
            yield return new WaitForSeconds(interStimulusInterval);
        }
        
        Debug.Log("游戏结束！最终得分: " + score);
        if (feedbackText != null) feedbackText.text = $"游戏结束!分数为:{score}\n点击返回按钮回到主界面";
        
        matchButton.interactable = false;
    }
    
    // ... (OnMatchButtonPressed, UpdateScoreUI 函数保持不变)
    public void OnMatchButtonPressed()
    {
        if (hasRespondedThisTrial) return;
        hasRespondedThisTrial = true;
        
        if (currentTrialIndex >= trialSequence.Count) return;

        Trial currentTrial = trialSequence[currentTrialIndex];

        if (currentTrial.isMatch)
        {
            score++;
            buttonText.text = "正确!";
            Debug.Log("Correct! 正确匹配! 得分: " + score);
        }
        else
        {
            buttonText.text = "错误!";
            Debug.Log("Incorrect! 错误匹配! 得分: " + score);
        }
        UpdateScoreUI();
    }
    
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =  currentTrialIndex + "/" + trialSequence.Count;
        }
    }

    // --- 修改 5: SetCellColor 函数不再需要，可以安全删除 ---
    /*
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
    */
}