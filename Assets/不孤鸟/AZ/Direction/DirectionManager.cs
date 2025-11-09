using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Rokid.UXR.Interaction; // 1. Rokid SDK 命名空间
using TMPro;

public class DirectionManager : MonoBehaviour
{
    // --- 定义游戏状态 ---
    // 使用枚举来管理游戏流程，比多个布尔值更清晰、更不容易出错
    private enum GameState
    {
        Ready,      // 准备阶段，显示开始按钮
        Playing,    // 游戏进行中，等待玩家输入
        RoundOver,  // 一回合结束，显示反馈（正确/错误），短暂延迟
        GameOver    // 游戏彻底结束
    }

    [Header("UI 元素")]
    public GameObject startButton;
    public TextMeshProUGUI countdownText;
    public AudioSource audioSource;

    [Header("提示图标 (4个)")]
    public Image cueLeftLeft;   // 左侧, 指向左
    public Image cueLeftRight;  // 左侧, 指向右
    public Image cueRightLeft;  // 右侧, 指向左
    public Image cueRightRight; // 右侧, 指向右

    [Header("音频设置")]
    public AudioClip gameStartSound;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    
    [Header("游戏参数设置")]
    public float gameDuration = 10f; // 每回合的持续时间
    public int gameRound = 40; // 总共的回合数
    public bool isRandomMode = false; // 是否是随机（困难）模式
    public float feedbackDelay = 2.0f; // 正确/错误反馈后的延迟时间

    // --- 游戏状态与数据 ---
    private GameState currentState; // 当前游戏状态
    private float roundTimer; // 当前回合的计时器
    
    private int trialsCompleted = 0; // 已完成的回合数 (替换了原先用途混乱的 correctStreak)
    private int score = 0; // 记录正确回答的次数

    // 用于定义当前需要的手
    private enum RequiredHand { None, Left, Right }
    private RequiredHand currentRequiredHand = RequiredHand.None;

    void Start()
    {
        var direction = DirectionSettings.Instance;
        if (direction != null)
        {
           gameDuration =  direction.GameDuration;
           gameRound = (int)direction.GameRound;
           isRandomMode = direction.IsRandomMode;
           
        }

        // 初始化游戏，进入准备状态
        SetState(GameState.Ready);
    }

    void Update()
    {
        // 只在 Playing 状态下才需要每帧更新
        if (currentState == GameState.Playing)
        {
            UpdatePlayingState();
        }
    }
    
    /// <summary>
    /// 状态机核心：管理所有状态的切换
    /// </summary>
    private void SetState(GameState newState)
    {
        currentState = newState;
        
        // 根据新状态执行相应的初始化逻辑
        switch (currentState)
        {
            case GameState.Ready:
                InitializeUI();
                break;
            case GameState.Playing:
                StartNewRound();
                break;
            case GameState.RoundOver:
                // RoundOver 状态的逻辑在协程中处理，这里不需要做什么
                break;
            case GameState.GameOver:
                ShowGameOver();
                break;
        }
    }

    #region 状态处理函数

    /// <summary>
    /// 初始化UI，为游戏准备
    /// </summary>
    private void InitializeUI()
    {
        startButton.SetActive(true);
        countdownText.text = "准备好了吗？";
        HideAllCues();
    }

    /// <summary>
    /// 在 Playing 状态时，每帧执行的逻辑
    /// </summary>
    private void UpdatePlayingState()
    {
        // 更新回合倒计时
        roundTimer -= Time.deltaTime;
        countdownText.text = roundTimer.ToString("F2");

        // 检查玩家输入
        CheckPlayerInput();
        
        // 如果时间到了玩家还未响应，算作错误
        if (roundTimer <= 0)
        {
            roundTimer = 0;
            countdownText.text = "0.00";
            ProcessAnswer(false); // 时间到，处理为错误答案
        }
    }

    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    private void ShowGameOver()
    {
        HideAllCues();
        startButton.SetActive(true);
        // 您可以根据需要显示更详细的结果，例如正确率
        float accuracy = (gameRound > 0) ? ((float)score / trialsCompleted) * 100 : 0;
        countdownText.text = $"测试结束！\n总回合: {trialsCompleted}\n正确率: {accuracy:F1}%";
    }

    #endregion

    #region 游戏流程控制

    /// <summary>
    /// 公共方法：绑定到开始按钮的 onClick 事件
    /// </summary>
    public void OnStartButtonPressed()
    {
        // 重置游戏数据
        trialsCompleted = 0;
        score = 0;
        
        startButton.SetActive(false);

        if (audioSource && gameStartSound)
        {
            audioSource.PlayOneShot(gameStartSound);
        }
        
        // 开始游戏，直接进入 Playing 状态开始第一回合
        SetState(GameState.Playing);
    }

    /// <summary>
    /// 开始一个新回合（生成题目）
    /// </summary>
    private void StartNewRound()
    {
        if (trialsCompleted >= gameRound)
        {
            SetState(GameState.GameOver);
            return;
        }
        
        HideAllCues();
        roundTimer = gameDuration;

        bool isCongruentTrial = !isRandomMode || trialsCompleted < 3;

        // 2. 随机决定图标出现的位置（左侧或右侧）
        bool showOnLeft = Random.Range(0, 2) == 0;
        
        // 3. 决定箭头的指向
        bool arrowPointsLeft;
        if (isCongruentTrial)
        {
            // 在一致性题目中，箭头方向必须与位置相同
            arrowPointsLeft = showOnLeft;
        }
        else
        {
            // 在随机题目中，箭头方向是完全随机的，与位置无关
            arrowPointsLeft = Random.Range(0, 2) == 0;
        }

        // 4. 根据位置和方向，激活对应的UI图标
        if (showOnLeft)
        {
            if (arrowPointsLeft)
                cueLeftLeft.gameObject.SetActive(true); // 左侧，指向左
            else
                cueLeftRight.gameObject.SetActive(true); // 左侧，指向右
        }
        else // 在右侧显示
        {
            if (arrowPointsLeft)
                cueRightLeft.gameObject.SetActive(true); // 右侧，指向左
            else
                cueRightRight.gameObject.SetActive(true); // 右侧，指向右
        }

        // 5. 根据【箭头方向】设置正确答案
        //    注意：正确答案只与箭头指向有关，与它出现的位置无关
        currentRequiredHand = arrowPointsLeft ? RequiredHand.Left : RequiredHand.Right;
    }

    /// <summary>
    /// 检查并处理玩家的手势输入
    /// </summary>
    private void CheckPlayerInput()
    {
        bool leftHandPalm = GesEventInput.Instance.GetGestureType(HandType.LeftHand) == GestureType.Palm;
        bool rightHandPalm = GesEventInput.Instance.GetGestureType(HandType.RightHand) == GestureType.Palm;

        RequiredHand detectedHand = RequiredHand.None;

        if (leftHandPalm && !rightHandPalm)
        {
            detectedHand = RequiredHand.Left;
        }
        else if (rightHandPalm && !leftHandPalm)
        {
            detectedHand = RequiredHand.Right;
        }
        else if (leftHandPalm && rightHandPalm)
        {
            return; // 两只手都举起，忽略
        }
        else
        {
            return; // 没有有效输入
        }

        // 一旦检测到有效输入，立刻处理结果
        ProcessAnswer(detectedHand == currentRequiredHand);
    }

    /// <summary>
    /// 处理玩家的回答（无论正确、错误还是超时）
    /// </summary>
    /// <param name="isCorrect">回答是否正确</param>
    private void ProcessAnswer(bool isCorrect)
    {
        // 进入回合结束状态，这会停止 Update 中的计时器和输入检测
        SetState(GameState.RoundOver);
        
        trialsCompleted++; // 完成的回合数增加

        if (isCorrect)
        {
            score++; // 正确次数增加
            Debug.Log("Correct!");
            countdownText.text = "正确!";
            if (audioSource && correctSound)
            {
                audioSource.PlayOneShot(correctSound);
            }
        }
        else
        {
            Debug.Log("Incorrect!");
            countdownText.text = "错误!";
            if (audioSource && incorrectSound)
            {
                audioSource.PlayOneShot(incorrectSound);
            }
        }
        
        // 启动协程，在短暂延迟后进入下一回合或结束游戏
        StartCoroutine(NextRoundRoutine());
    }

    /// <summary>
    /// 等待指定延迟后，开始下一回合
    /// </summary>
    IEnumerator NextRoundRoutine()
    {
        yield return new WaitForSeconds(feedbackDelay);
        
        // 延迟结束后，切换到 Playing 状态以开始新回合
        SetState(GameState.Playing);
    }
    
    #endregion

    #region 辅助函数

    /// <summary>
    /// 隐藏所有四个提示图标
    /// </summary>
    private void HideAllCues()
    {
        cueLeftLeft.gameObject.SetActive(false);
        cueLeftRight.gameObject.SetActive(false);
        cueRightLeft.gameObject.SetActive(false);
        cueRightRight.gameObject.SetActive(false);
    }
    
    #endregion
}