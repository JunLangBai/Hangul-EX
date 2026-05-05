using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手势跟随训练管理器
/// GesturePanel中显示目标手势，用户需要模仿该手势
/// 每隔一段时间手势会自动切换到下一个
/// </summary>
public class GestureFollowManager : MonoBehaviour
{
    [Header("测试配置")]
    [Tooltip("测试总时长（秒）")]
    public float testDuration = 60f;
    
    [Tooltip("每个手势显示的时长（秒）")]
    public float gestureDisplayDuration = 3f;
    
    [Tooltip("所有可用的手势数据")]
    public List<GestureData> allGestures;

    [Header("音频设置")]
    public AudioSource audioSource;
    public AudioClip correctSound; // 正确时的声音
    public AudioClip incorrectSound; // 错误时的声音
    public AudioClip gestureChangeSound; // 手势切换时的提示音

    [Header("UI组件")]
    public GameObject instructionPanel;
    public GameObject gesturePanel;
    public GameObject resultPanel;
    
    public Image targetGestureImage; // GesturePanel中显示目标手势的Image
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI feedbackText; // 显示"正确"/"错误"的反馈文本
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI progressText; // 显示进度 "5/20"
    
    public SettlementScreen settings;

    // --- 内部状态变量 ---
    private enum TestState
    {
        Instructions,
        Testing,
        Results
    }

    private TestState currentState;
    private List<GestureData> gestureSequence = new List<GestureData>(); // 本次测试的手势序列
    private int currentGestureIndex = -1;
    private GestureData currentTargetGesture => gestureSequence[currentGestureIndex];
    
    private float testTimer;
    private float gestureTimer; // 当前手势的计时器
    private bool hasRespondedToCurrentGesture; // 是否已对当前手势做出响应
    
    // --- 计分 ---
    private int correctCount = 0; // 正确次数
    private int incorrectCount = 0; // 错误次数
    private int missedCount = 0; // 未响应次数
    private int totalGestures = 0; // 总手势数
    private List<float> reactionTimes = new List<float>();
    
    private Coroutine feedbackCoroutine;

    void Start()
    {
        // 从全局设置中加载参数（如果有）
        if (AllSettingCtr.Instance != null)
        {
            // 可以根据需要从AllSettingCtr加载参数
            // gestureDisplayDuration = AllSettingCtr.Instance.gestureFollowDuration;
        }

        StartInstructionPhase();
    }

    void OnEnable()
    {
        GestureInputController.OnGesturePerformed += HandleGestureInput;
    }

    void OnDisable()
    {
        GestureInputController.OnGesturePerformed -= HandleGestureInput;
    }

    void StartInstructionPhase()
    {
        currentState = TestState.Instructions;
        instructionPanel.SetActive(true);
        gesturePanel.SetActive(false);
        resultPanel.SetActive(false);

        GenerateGestureSequence();

        instructionText.text = "请根据屏幕中显示的手势\n做出相应的动作\n\n准备开始...";
        
        StartCoroutine(StartCountdown(5));
        Invoke(nameof(StartTestPhase), 5f);
    }

    void StartTestPhase()
    {
        currentState = TestState.Testing;
        instructionPanel.SetActive(false);
        gesturePanel.SetActive(true);
        resultPanel.SetActive(false);

        // 重置计分
        correctCount = 0;
        incorrectCount = 0;
        missedCount = 0;
        totalGestures = gestureSequence.Count;
        reactionTimes.Clear();
        
        testTimer = testDuration;
        currentGestureIndex = -1;

        StopAllCoroutines();
        StartCoroutine(PresentGestures());
    }

    void ShowResults()
    {
        currentState = TestState.Results;
        instructionPanel.SetActive(false);
        gesturePanel.SetActive(false);
        resultPanel.SetActive(true);

        // 计算正确率
        float accuracy = (totalGestures > 0) ? ((float)correctCount / totalGestures) * 100f : 0f;
        float avgReactionTime = reactionTimes.Count > 0 ? reactionTimes.Average() : 0f;
        
        var historyAccuracy = settings.GetSavedAccuracyForCurrentScene();
        if (historyAccuracy == null)
        {
            historyAccuracy = accuracy;
        }

        resultText.text = $"测试结束!\n\n" +
                         $"正确: {correctCount}\n" +
                         $"错误: {incorrectCount}\n" +
                         $"未响应: {missedCount}\n" +
                         $"正确率: {accuracy:F1}%\n" +
                         $"平均反应时间: {avgReactionTime:F2}秒\n" +
                         $"历史最佳: {historyAccuracy:F1}%";
        
        settings.SaveLevelAccuracy(accuracy);
    }

    void Update()
    {
        if (currentState == TestState.Testing)
        {
            if (testTimer > 0)
            {
                testTimer -= Time.deltaTime;
                gestureTimer += Time.deltaTime;
            }
            else
            {
                StopAllCoroutines();
                ShowResults();
            }
        }
    }

    /// <summary>
    /// 生成手势序列
    /// </summary>
    void GenerateGestureSequence()
    {
        gestureSequence.Clear();
        
        // 计算总共需要多少个手势
        int totalGesturesNeeded = Mathf.CeilToInt(testDuration / gestureDisplayDuration);
        
        // 随机生成手势序列
        for (int i = 0; i < totalGesturesNeeded; i++)
        {
            if (allGestures.Count > 0)
            {
                GestureData randomGesture = allGestures[Random.Range(0, allGestures.Count)];
                gestureSequence.Add(randomGesture);
            }
        }
        
        Debug.Log($"生成了 {gestureSequence.Count} 个手势的序列");
    }

    /// <summary>
    /// 呈现手势序列
    /// </summary>
    IEnumerator PresentGestures()
    {
        for (int i = 0; i < gestureSequence.Count; i++)
        {
            // 检查测试时间是否结束
            if (testTimer <= 0) break;

            // 检查上一个手势是否未响应
            if (currentGestureIndex >= 0 && !hasRespondedToCurrentGesture)
            {
                missedCount++;
                Debug.Log("未响应!");
                ShowFeedback("未响应", Color.yellow);
            }

            // 切换到下一个手势
            currentGestureIndex = i;
            hasRespondedToCurrentGesture = false;
            gestureTimer = 0f;

            // 显示目标手势
            targetGestureImage.sprite = currentTargetGesture.gestureImage;
            
            // 播放手势切换提示音
            if (audioSource != null && gestureChangeSound != null)
            {
                audioSource.PlayOneShot(gestureChangeSound);
            }

            // 更新进度显示
            if (progressText != null)
            {
                progressText.text = $"{currentGestureIndex + 1}/{gestureSequence.Count}";
            }

            Debug.Log($"显示手势 {currentGestureIndex + 1}: {currentTargetGesture.gestureName}");

            // 等待指定时长
            yield return new WaitForSeconds(gestureDisplayDuration);
        }

        // 检查最后一个手势
        if (currentGestureIndex >= 0 && !hasRespondedToCurrentGesture)
        {
            missedCount++;
            Debug.Log("未响应 (最后一个)!");
        }

        // 测试结束
        if (currentState == TestState.Testing)
        {
            ShowResults();
        }
    }

    /// <summary>
    /// 处理手势输入
    /// </summary>
    private void HandleGestureInput(Hand hand, CustomGestureType gestureType)
    {
        if (currentState != TestState.Testing || hasRespondedToCurrentGesture)
        {
            return;
        }

        if (currentGestureIndex < 0 || currentGestureIndex >= gestureSequence.Count)
        {
            return;
        }

        hasRespondedToCurrentGesture = true; // 锁定，防止重复响应

        // 检查手势是否正确
        bool isCorrectHand = (currentTargetGesture.hand == hand);
        bool isCorrectGesture = (currentTargetGesture.gestureType == gestureType);
        bool isCorrect = isCorrectHand && isCorrectGesture;

        if (isCorrect)
        {
            correctCount++;
            reactionTimes.Add(gestureTimer);
            Debug.Log($"正确! 反应时间: {gestureTimer:F2}秒");
            ShowFeedback("正确!", Color.green);
            
            // 播放正确音效
            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
        }
        else
        {
            incorrectCount++;
            Debug.Log($"错误! 目标: {currentTargetGesture.hand} {currentTargetGesture.gestureType}, 输入: {hand} {gestureType}");
            ShowFeedback("错误!", Color.red);
            
            // 播放错误音效
            if (audioSource != null && incorrectSound != null)
            {
                audioSource.PlayOneShot(incorrectSound);
            }
        }
    }

    /// <summary>
    /// 显示临时反馈文本
    /// </summary>
    void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(ShowFeedbackCoroutine(message, color));
    }

    IEnumerator ShowFeedbackCoroutine(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
        yield return new WaitForSeconds(1.0f);
        feedbackText.text = string.Empty;
        feedbackCoroutine = null;
    }

    /// <summary>
    /// 倒计时协程
    /// </summary>
    IEnumerator StartCountdown(int seconds)
    {
        int remainingTime = seconds;

        while (remainingTime > 0)
        {
            if (timeText != null)
            {
                timeText.text = $"{remainingTime}秒后开始...";
            }

            yield return new WaitForSeconds(1f);
            remainingTime--;
        }

        if (timeText != null)
        {
            timeText.text = string.Empty;
        }
    }
}
