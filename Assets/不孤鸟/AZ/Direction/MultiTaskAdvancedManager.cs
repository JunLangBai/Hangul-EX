using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Rokid.UXR.Interaction;
using TMPro;

/// <summary>
/// 多任务能力测试 - 进阶难度
/// 在屏幕左右两侧随机显示多个手势图片
/// 玩家需要根据显示的手势做出相应的动作
/// </summary>
public class MultiTaskAdvancedManager : MonoBehaviour
{
    // --- 游戏状态枚举 ---
    private enum GameState
    {
        Ready,      // 准备阶段
        Playing,    // 游戏进行中
        RoundOver,  // 回合结束
        GameOver    // 游戏结束
    }

    [Header("UI 元素")]
    public GameObject startButton;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI feedbackText; // 显示"正确"/"错误"的反馈
    public TextMeshProUGUI progressText; // 显示进度 "5/40"

    [Header("显示区域")]
    public Image leftDisplayImage;  // 左侧显示区域
    public Image rightDisplayImage; // 右侧显示区域

    [Header("手势数据")]
    [Tooltip("所有可用的手势数据")]
    public List<GestureData> allGestures;

    [Header("音频设置")]
    public AudioSource audioSource;
    public AudioClip gameStartSound;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    public AudioClip gestureChangeSound; // 手势切换提示音

    [Header("游戏参数设置")]
    [Tooltip("每回合的持续时间（秒）")]
    public float roundDuration = 5f;
    
    [Tooltip("总回合数")]
    public int totalRounds = 40;
    
    [Tooltip("每侧同时显示的手势数量（1-3）")]
    [Range(1, 3)]
    public int gesturesPerSide = 1;
    
    [Tooltip("正确/错误反馈后的延迟时间")]
    public float feedbackDelay = 1.5f;

    public SettlementScreen settings;

    // --- 游戏状态与数据 ---
    private GameState currentState;
    private float roundTimer;
    
    private int trialsCompleted = 0; // 已完成的回合数
    private int correctCount = 0; // 正确次数
    private int incorrectCount = 0; // 错误次数
    private int missedCount = 0; // 未响应次数
    
    // 当前回合的目标手势
    private List<GestureData> currentLeftGestures = new List<GestureData>();
    private List<GestureData> currentRightGestures = new List<GestureData>();
    
    private bool hasRespondedThisRound = false;
    
    private Coroutine feedbackCoroutine;

    void Start()
    {
        // 从全局设置加载参数
        var settingsCtr = AllSettingCtr.Instance;
        if (settingsCtr != null)
        {
            roundDuration = settingsCtr.multiTaskRoundDuration;
            totalRounds = settingsCtr.multiTaskTotalRounds;
            gesturesPerSide = settingsCtr.multiTaskGesturesPerSide;
            feedbackDelay = settingsCtr.multiTaskFeedbackDelay;
            
            Debug.Log($"多任务进阶设置已加载: 回合时长={roundDuration}秒, 总回合={totalRounds}, 每侧手势={gesturesPerSide}, 反馈延迟={feedbackDelay}秒");
        }
        else
        {
            Debug.LogWarning("AllSettingCtr.Instance 未找到，使用默认设置");
        }

        SetState(GameState.Ready);
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdatePlayingState();
        }
    }

    /// <summary>
    /// 状态机：管理游戏状态切换
    /// </summary>
    private void SetState(GameState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case GameState.Ready:
                InitializeUI();
                break;
            case GameState.Playing:
                StartNewRound();
                break;
            case GameState.RoundOver:
                // 在协程中处理
                break;
            case GameState.GameOver:
                ShowGameOver();
                break;
        }
    }

    #region 状态处理函数

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        startButton.SetActive(true);
        countdownText.text = "准备好了吗？\n根据屏幕显示的手势\n做出相应的动作";
        
        if (feedbackText != null)
            feedbackText.text = string.Empty;
        
        if (progressText != null)
            progressText.text = string.Empty;
        
        HideAllGestures();
    }

    /// <summary>
    /// Playing 状态的每帧更新
    /// </summary>
    private void UpdatePlayingState()
    {
        roundTimer -= Time.deltaTime;
        countdownText.text = roundTimer.ToString("F2");

        // 检查玩家输入
        CheckPlayerInput();
        
        // 时间到了还未响应，算作未响应
        if (roundTimer <= 0)
        {
            roundTimer = 0;
            countdownText.text = "0.00";
            
            if (!hasRespondedThisRound)
            {
                ProcessAnswer(false, false); // 未响应
            }
        }
    }

    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    private void ShowGameOver()
    {
        HideAllGestures();
        startButton.SetActive(true);
        
        float accuracy = (trialsCompleted > 0) ? ((float)correctCount / trialsCompleted) * 100 : 0;
        
        var historyScore = settings.GetSavedAccuracyForCurrentScene();
        if (historyScore == null)
        {
            historyScore = accuracy;
        }
        
        countdownText.text = $"测试结束！\n" +
                            $"总回合: {trialsCompleted}\n" +
                            $"正确: {correctCount}\n" +
                            $"错误: {incorrectCount}\n" +
                            $"未响应: {missedCount}\n" +
                            $"正确率: {accuracy:F1}%\n" +
                            $"最佳记录: {historyScore:F1}%";
        
        settings.SaveLevelAccuracy(accuracy);
        
        if (feedbackText != null)
            feedbackText.text = string.Empty;
        
        if (progressText != null)
            progressText.text = string.Empty;
    }

    #endregion

    #region 游戏流程控制

    /// <summary>
    /// 开始按钮点击事件
    /// </summary>
    public void OnStartButtonPressed()
    {
        // 重置游戏数据
        trialsCompleted = 0;
        correctCount = 0;
        incorrectCount = 0;
        missedCount = 0;
        
        startButton.SetActive(false);

        if (audioSource && gameStartSound)
        {
            audioSource.PlayOneShot(gameStartSound);
        }
        
        SetState(GameState.Playing);
    }

    /// <summary>
    /// 开始新回合
    /// </summary>
    private void StartNewRound()
    {
        if (trialsCompleted >= totalRounds)
        {
            SetState(GameState.GameOver);
            return;
        }
        
        HideAllGestures();
        roundTimer = roundDuration;
        hasRespondedThisRound = false;
        
        // 生成随机手势
        GenerateRandomGestures();
        
        // 显示手势
        DisplayGestures();
        
        // 播放手势切换提示音
        if (audioSource && gestureChangeSound)
        {
            audioSource.PlayOneShot(gestureChangeSound);
        }
        
        // 更新进度显示
        if (progressText != null)
        {
            progressText.text = $"{trialsCompleted + 1}/{totalRounds}";
        }
        
        Debug.Log($"回合 {trialsCompleted + 1}: 左侧 {currentLeftGestures.Count} 个手势, 右侧 {currentRightGestures.Count} 个手势");
    }

    /// <summary>
    /// 生成随机手势组合
    /// </summary>
    private void GenerateRandomGestures()
    {
        currentLeftGestures.Clear();
        currentRightGestures.Clear();
        
        if (allGestures == null || allGestures.Count == 0)
        {
            Debug.LogError("没有可用的手势数据！");
            return;
        }
        
        // 为左侧生成手势
        for (int i = 0; i < gesturesPerSide; i++)
        {
            GestureData randomGesture = allGestures[Random.Range(0, allGestures.Count)];
            currentLeftGestures.Add(randomGesture);
        }
        
        // 为右侧生成手势
        for (int i = 0; i < gesturesPerSide; i++)
        {
            GestureData randomGesture = allGestures[Random.Range(0, allGestures.Count)];
            currentRightGestures.Add(randomGesture);
        }
    }

    /// <summary>
    /// 显示手势图片
    /// 如果有多个手势，这里简化为只显示第一个
    /// 可以根据需要扩展为显示多个手势的布局
    /// </summary>
    private void DisplayGestures()
    {
        // 显示左侧手势
        if (currentLeftGestures.Count > 0 && leftDisplayImage != null)
        {
            leftDisplayImage.sprite = currentLeftGestures[0].gestureImage;
            leftDisplayImage.gameObject.SetActive(true);
        }
        
        // 显示右侧手势
        if (currentRightGestures.Count > 0 && rightDisplayImage != null)
        {
            rightDisplayImage.sprite = currentRightGestures[0].gestureImage;
            rightDisplayImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 检查玩家输入
    /// </summary>
    private void CheckPlayerInput()
    {
        if (hasRespondedThisRound)
            return;
        
        // 检测左手手势
        GestureType leftGestureType = GesEventInput.Instance.GetGestureType(HandType.LeftHand);
        CustomGestureType leftCustomGesture = ConvertToCustomGesture(leftGestureType, HandType.LeftHand);
        
        // 检测右手手势
        GestureType rightGestureType = GesEventInput.Instance.GetGestureType(HandType.RightHand);
        CustomGestureType rightCustomGesture = ConvertToCustomGesture(rightGestureType, HandType.RightHand);
        
        // 检查是否有有效输入
        bool leftHandActive = leftCustomGesture != CustomGestureType.None;
        bool rightHandActive = rightCustomGesture != CustomGestureType.None;
        
        if (!leftHandActive && !rightHandActive)
            return; // 没有输入
        
        // 判断是否正确
        bool isCorrect = CheckIfCorrect(leftCustomGesture, rightCustomGesture);
        
        ProcessAnswer(true, isCorrect);
    }

    /// <summary>
    /// 将SDK手势转换为自定义手势类型
    /// </summary>
    private CustomGestureType ConvertToCustomGesture(GestureType sdkGesture, HandType handType)
    {
        switch (sdkGesture)
        {
            case GestureType.Grip:
                return CustomGestureType.Grip;
            case GestureType.Pinch:
                return CustomGestureType.Pinch;
            case GestureType.Palm:
                HandOrientation orientation = GesEventInput.Instance.GetHandOrientation(handType);
                if (orientation == HandOrientation.Back)
                {
                    return CustomGestureType.PalmForward;
                }
                else
                {
                    Pose palmPose = GesEventInput.Instance.GetSkeletonPose(SkeletonIndexFlag.PALM, handType);
                    if (Vector3.Dot(palmPose.up, Vector3.up) > 0.7f)
                    {
                        return CustomGestureType.PalmUp;
                    }
                }
                break;
        }
        return CustomGestureType.None;
    }

    /// <summary>
    /// 检查玩家的手势是否正确
    /// </summary>
    private bool CheckIfCorrect(CustomGestureType leftGesture, CustomGestureType rightGesture)
    {
        bool leftCorrect = false;
        bool rightCorrect = false;
        
        // 检查左手
        if (leftGesture != CustomGestureType.None)
        {
            foreach (var gesture in currentLeftGestures)
            {
                if (gesture.hand == Hand.Left && gesture.gestureType == leftGesture)
                {
                    leftCorrect = true;
                    break;
                }
            }
        }
        else
        {
            // 如果左手没有输入，检查左侧是否需要左手手势
            bool needsLeftHand = false;
            foreach (var gesture in currentLeftGestures)
            {
                if (gesture.hand == Hand.Left)
                {
                    needsLeftHand = true;
                    break;
                }
            }
            leftCorrect = !needsLeftHand; // 如果不需要左手，则左手正确
        }
        
        // 检查右手
        if (rightGesture != CustomGestureType.None)
        {
            foreach (var gesture in currentRightGestures)
            {
                if (gesture.hand == Hand.Right && gesture.gestureType == rightGesture)
                {
                    rightCorrect = true;
                    break;
                }
            }
        }
        else
        {
            // 如果右手没有输入，检查右侧是否需要右手手势
            bool needsRightHand = false;
            foreach (var gesture in currentRightGestures)
            {
                if (gesture.hand == Hand.Right)
                {
                    needsRightHand = true;
                    break;
                }
            }
            rightCorrect = !needsRightHand; // 如果不需要右手，则右手正确
        }
        
        return leftCorrect && rightCorrect;
    }

    /// <summary>
    /// 处理玩家的回答
    /// </summary>
    /// <param name="hasResponded">是否有响应</param>
    /// <param name="isCorrect">是否正确</param>
    private void ProcessAnswer(bool hasResponded, bool isCorrect)
    {
        SetState(GameState.RoundOver);
        
        hasRespondedThisRound = true;
        trialsCompleted++;

        if (!hasResponded)
        {
            // 未响应
            missedCount++;
            Debug.Log("未响应!");
            ShowFeedback("未响应", Color.yellow);
        }
        else if (isCorrect)
        {
            // 正确
            correctCount++;
            Debug.Log("正确!");
            ShowFeedback("正确!", Color.green);
            
            if (audioSource && correctSound)
            {
                audioSource.PlayOneShot(correctSound);
            }
        }
        else
        {
            // 错误
            incorrectCount++;
            Debug.Log("错误!");
            ShowFeedback("错误!", Color.red);
            
            if (audioSource && incorrectSound)
            {
                audioSource.PlayOneShot(incorrectSound);
            }
        }
        
        StartCoroutine(NextRoundRoutine());
    }

    /// <summary>
    /// 等待延迟后进入下一回合
    /// </summary>
    IEnumerator NextRoundRoutine()
    {
        yield return new WaitForSeconds(feedbackDelay);
        SetState(GameState.Playing);
    }

    #endregion

    #region 辅助函数

    /// <summary>
    /// 隐藏所有手势显示
    /// </summary>
    private void HideAllGestures()
    {
        if (leftDisplayImage != null)
            leftDisplayImage.gameObject.SetActive(false);
        
        if (rightDisplayImage != null)
            rightDisplayImage.gameObject.SetActive(false);
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
        yield return new WaitForSeconds(feedbackDelay);
        feedbackText.text = string.Empty;
        feedbackCoroutine = null;
    }

    #endregion
}
