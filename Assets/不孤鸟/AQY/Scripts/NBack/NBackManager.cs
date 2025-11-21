using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;
using System.Linq;


public struct Trial
{
    public int positionIndex;
    public bool isMatch;
}

public class NBackManager : MonoBehaviour
{
    [Header("游戏对象引用")] public GameObject[] gridCells;

    // --- 修改 1: 添加对3D刺激物的引用 ---
    public GameObject stimulus3DObject; // 将您想显示的3D对象拖到这里

    // --- 已删除: 不再需要颜色变量 ---
    // public Color normalColor = Color.gray;
    // public Color stimulusColor = Color.green;

    [Header("UI 引用")] public TextMeshPro scoreText;
    public Button matchButton;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI feedbackText;

    public SettlementScreen settings;

    private readonly List<Trial> trialSequence = new();
    private int currentTrialIndex;
    private int score;
    private bool hasRespondedThisTrial;

    private int nValue;
    private float stimulusDuration;
    private float interStimulusInterval;
    private int totalTrials;
    private int targetIndexN0;
    private float matchProbability;

    private AllSettingCtr allSettingCtr;
    
    private int totalMatchesInSequence;


    private void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;

        if (allSettingCtr != null)
        {
            nValue = allSettingCtr.nValue;
            stimulusDuration = allSettingCtr.stimulusDuration;
            interStimulusInterval = allSettingCtr.interStimulusInterval;
            totalTrials = allSettingCtr.totalTrials;
            targetIndexN0 = allSettingCtr.targetIndexN0;
            matchProbability = allSettingCtr.matchProbability;

            Debug.Log($"设置已加载: N={nValue}, 时长={stimulusDuration}");
        }
        else
        {
            Debug.LogError("NBackSetting 实例未找到! 请确保设置场景已首先加载。");
            nValue = 1;
            stimulusDuration = 2.0f;
            interStimulusInterval = 2.5f;
            totalTrials = 5;
            matchProbability = 0.33f;
        }
    }

    private void Start()
    {
        if (feedbackText != null)
        {
            if (nValue > 0)
                feedbackText.text = $"↓↓ 向下低头看向平面进行游戏 ↓↓\n<size=25>检测出现的方块是否与前{nValue}个方块相同</size>";
            else if (nValue == 0) feedbackText.text = "↓↓ 向下低头看向平面进行游戏 ↓↓\n<size=25>检测出现的方块是在九宫格中心</size>";
        }

        // --- 修改 2: 确保3D对象在游戏开始时是隐藏的 ---
        if (stimulus3DObject != null)
            stimulus3DObject.SetActive(false);
        else
            Debug.LogError("刺激物3D对象 (stimulus3DObject) 未在检视器中设置!");

        matchButton.onClick.AddListener(OnMatchButtonPressed);

        GenerateTrialSequence();
        
        UpdateScoreUI();
        totalMatchesInSequence = trialSequence.Count(trial => trial.isMatch);
        Debug.Log($"序列已生成，共包含 {totalMatchesInSequence} 个匹配项。");
        StartCoroutine(RunGame());
    }

    // ... (GenerateTrialSequence, GenerateN0Sequence, GenerateN1AndAboveSequence 函数保持不变)
    private void GenerateTrialSequence()
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

    private void GenerateN0Sequence()
    {
        trialSequence.Clear();
        var rand = new Random();

        // --- 核心修改 1: 根据概率计算出本次测试应该有多少个匹配项 ---
        var matchesToCreate = Mathf.RoundToInt(totalTrials * matchProbability);

        Debug.Log($"生成 N=0 序列。总试次: {totalTrials}, 目标索引: {targetIndexN0}");
        Debug.Log($"设定概率: {matchProbability:P0}, 将创建 {matchesToCreate} 个匹配项。");


        // --- 步骤 A: 创建一个索引列表，代表所有试次的位置 ---
        var trialIndices = new List<int>();
        for (var i = 0; i < totalTrials; i++) trialIndices.Add(i);

        // --- 步骤 B: 随机打乱这个列表 ---
        for (var i = 0; i < trialIndices.Count - 1; i++)
        {
            var randomIndex = rand.Next(i, trialIndices.Count);
            (trialIndices[i], trialIndices[randomIndex]) = (trialIndices[randomIndex], trialIndices[i]);
        }

        // --- 步骤 C: 将前 `matchesToCreate` 个索引标记为“匹配” ---
        var matchTrialSet = new HashSet<int>();
        for (var i = 0; i < matchesToCreate && i < trialIndices.Count; i++) matchTrialSet.Add(trialIndices[i]);

        // --- 步骤 D: 根据标记生成最终的序列 ---
        for (var i = 0; i < totalTrials; i++)
        {
            var newTrial = new Trial();
            if (matchTrialSet.Contains(i))
            {
                // 这是匹配试次
                newTrial.isMatch = true;
                newTrial.positionIndex = targetIndexN0; // 位置必须是目标位置
            }
            else
            {
                // 这是非匹配试次
                newTrial.isMatch = false;
                // 随机选择一个位置，但必须确保它不是目标位置
                int newPos;
                do
                {
                    newPos = rand.Next(0, 9);
                } while (newPos == targetIndexN0);

                newTrial.positionIndex = newPos;
            }

            trialSequence.Add(newTrial);
        }
    }

    private void GenerateN1AndAboveSequence()
    {
        trialSequence.Clear();
        var rand = new Random();

        // --- 核心修改 1: 根据概率计算出本次测试应该有多少个匹配项 ---
        // 首先，确定有多少个“槽位”是可能成为匹配项的 (必须在第n个之后)
        var potentialMatchSlots = totalTrials - nValue;
        if (potentialMatchSlots < 0) potentialMatchSlots = 0;

        // 根据总槽位和设定的概率，计算出精确的匹配数量
        var matchesToCreate = Mathf.RoundToInt(potentialMatchSlots * matchProbability);

        Debug.Log($"生成 N={nValue} 序列。总试次: {totalTrials}。");
        Debug.Log($"可用匹配槽位: {potentialMatchSlots}, 设定概率: {matchProbability:P0}, 将创建 {matchesToCreate} 个匹配项。");


        // --- 步骤 A: 先生成一个完全不匹配的“基础”序列 ---
        for (var i = 0; i < totalTrials; i++)
        {
            var newTrial = new Trial { isMatch = false };
            int newPos;

            if (i < nValue)
            {
                newPos = rand.Next(0, 9);
            }
            else
            {
                // 确保新生成的位置不等于 n-back 的位置，防止意外匹配
                var previousPos = trialSequence[i - nValue].positionIndex;
                do
                {
                    newPos = rand.Next(0, 9);
                } while (newPos == previousPos);
            }

            newTrial.positionIndex = newPos;
            trialSequence.Add(newTrial);
        }

        // --- 步骤 B: 从所有可能的位置中，随机挑选并强制改为匹配项 ---

        // 找出所有可以成为“匹配项”的索引位置 (i >= nValue)
        var possibleMatchIndices = new List<int>();
        for (var i = nValue; i < totalTrials; i++) possibleMatchIndices.Add(i);

        // 随机打乱这些位置的顺序 (Fisher-Yates shuffle)
        for (var i = 0; i < possibleMatchIndices.Count - 1; i++)
        {
            var randomIndex = rand.Next(i, possibleMatchIndices.Count);
            (possibleMatchIndices[i], possibleMatchIndices[randomIndex]) = (possibleMatchIndices[randomIndex], possibleMatchIndices[i]);
        }

        // --- 步骤 C: 挑选前 `matchesToCreate` 个位置，将它们变为匹配项 ---
        for (var i = 0; i < matchesToCreate && i < possibleMatchIndices.Count; i++)
        {
            var indexToChange = possibleMatchIndices[i];

            // 获取这个试次
            var trialToModify = trialSequence[indexToChange];

            // 将其属性设置为匹配
            trialToModify.isMatch = true;
            // 将其位置设置为与 n-back 前的位置相同
            trialToModify.positionIndex = trialSequence[indexToChange - nValue].positionIndex;

            // 将修改后的试次写回列表
            trialSequence[indexToChange] = trialToModify;
        }
    }

    private IEnumerator RunGame()
    {
        matchButton.interactable = false;
        yield return new WaitForSeconds(2.0f);
        matchButton.interactable = true;

        for (currentTrialIndex = 0; currentTrialIndex < trialSequence.Count; currentTrialIndex++)
        {
            hasRespondedThisTrial = false;
            var currentTrial = trialSequence[currentTrialIndex];

            // 获取当前试次应该显示刺激物的位置
            var currentCell = gridCells[currentTrial.positionIndex];

            // --- 修改 3: 将3D对象移动到目标位置并显示它 ---
            stimulus3DObject.transform.position = currentCell.transform.position;
            stimulus3DObject.SetActive(true);
            UpdateScoreUI();

            // 等待刺激持续时间
            yield return new WaitForSeconds(stimulusDuration);

            // --- 修改 4: 隐藏3D对象 ---
            stimulus3DObject.SetActive(false);

            if (currentTrial.isMatch && !hasRespondedThisTrial) Debug.Log("Missed! 错过了一个匹配项。");

            // 等待试次间隔
            yield return new WaitForSeconds(interStimulusInterval);
        }

        Debug.Log("游戏结束！最终得分: " + score);
        // 计算正确率
        float accuracy = 0f;
        // 为防止除以零，先检查总匹配数是否大于0
        if (totalTrials > 0)
        {
            accuracy = (float)score / totalTrials;
        }

        var historyScore = settings.GetSavedAccuracyForCurrentScene();
        if (historyScore == null)
        {
            historyScore = accuracy;
        }

        if (feedbackText != null)
        {
            feedbackText.text = $"游戏结束!正确率:{accuracy:P0}\n最佳记录:{historyScore:P0}\n点击返回按钮回到主界面";
        }

        settings.SaveLevelAccuracy(accuracy);
        matchButton.interactable = false;
    }

    // ... (OnMatchButtonPressed, UpdateScoreUI 函数保持不变)
    public void OnMatchButtonPressed()
    {
        if (hasRespondedThisTrial) return;
        hasRespondedThisTrial = true;

        if (currentTrialIndex >= trialSequence.Count) return;

        var currentTrial = trialSequence[currentTrialIndex];

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

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = (currentTrialIndex + 1) + "/" + trialSequence.Count;
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