using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Rokid.UXR.Interaction; // 1. Rokid SDK 命名空间
using TMPro;

public class DirectionManager : MonoBehaviour
{
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

    [Header("游戏状态")]
    private bool isPlaying = false;
    private float gameTimer;
    private bool forceRandom;

    public float sceneTime;
    public bool isTime;
    //预防第一次进场景就计时
    private bool isGameStart = false;
    
    private int correctStreak = 0; // 连续答对的次数

    // 用于定义当前需要的手
    private enum RequiredHand { None, Left, Right }
    private RequiredHand currentRequiredHand = RequiredHand.None;

    // 用于防止一帧内多次检测
    private bool inputReceivedThisTurn = false;

    void Start()
    {
        var direction = DirectionSettings.Instance;
        // 从设置中加载时间
        if (direction != null)
        {
            gameTimer = direction.GameDuration;
            forceRandom = direction.IsRandomMode;
        }
        else
        {
            gameTimer = 10;
            forceRandom =  false;
        }
        InitializeUI();
    }

    void InitializeUI()
    {
        startButton.SetActive(true);
        countdownText.text = "准备好了吗？";
        sceneTime = gameTimer;
        HideAllCues();
    }

    // Update 每帧调用
    void Update()
    {
        if (isPlaying) return;

        // 游戏倒计时
        if (sceneTime > 0 && !inputReceivedThisTurn && isGameStart)
        {
            sceneTime -= Time.deltaTime;
            countdownText.text  =  sceneTime.ToString("F2");
        }
        else if (sceneTime <= 0)
        {
            sceneTime = 0;
            isTime = true;
        }
        
        else if (isPlaying)
        {
            EndGame();
        }

        if (isTime)
        {
            EndGame();
        }

        // 检查玩家输入
        if (!inputReceivedThisTurn && !isTime)
        {
            CheckPlayerInput();
        }
    }

    // --- 在这里，就是你要的函数 ---
    /// <summary>
    /// 绑定到开始按钮的 onClick 事件
    /// </summary>
    public void OnStartButtonPressed()
    {
        StartGame();
    }
    // ---------------------------------

    void StartGame()
    {
        inputReceivedThisTurn = true; // 立即生成第一个提示
        startButton.SetActive(false);
        isGameStart = true;
        correctStreak = 0;

        if (audioSource && gameStartSound)
        {
            audioSource.PlayOneShot(gameStartSound);
        }

        GenerateNextTrial();
    }

    void EndGame()
    {
        isPlaying = false;
        HideAllCues();
        startButton.SetActive(true);
        countdownText.text = "时间到";
        currentRequiredHand = RequiredHand.None;
    }

    /// <summary>
    /// 2. 隐藏所有四个图标
    /// </summary>
    void HideAllCues()
    {
        cueLeftLeft.gameObject.SetActive(false);
        cueLeftRight.gameObject.SetActive(false);
        cueRightLeft.gameObject.SetActive(false);
        cueRightRight.gameObject.SetActive(false);
    }

    /// <summary>
    /// 3. 生成下一个挑战，直接激活四个Image中的一个
    /// </summary>
    void GenerateNextTrial()
    {
        HideAllCues(); // 先隐藏全部
        inputReceivedThisTurn = false; // 允许玩家输入
        isTime = false;
        sceneTime = gameTimer;

        
        bool isEasyMode = (correctStreak < 3) && !forceRandom;

        bool showOnLeft = (Random.Range(0, 2) == 0); // 决定在左侧还是右侧显示

        if (isEasyMode)
        {
            // 简单模式：方向和位置一致
            if (showOnLeft)
            {
                // 左侧显示向左箭头
                cueLeftLeft.gameObject.SetActive(true);
                currentRequiredHand = RequiredHand.Left;
            }
            else
            {
                // 右侧显示向右箭头
                cueRightRight.gameObject.SetActive(true);
                currentRequiredHand = RequiredHand.Right;
            }
        }
        else
        {
            // 随机（困难）模式：方向和位置可能不一致
            bool arrowPointsLeft = (Random.Range(0, 2) == 0); // 决定箭头指向左还是右

            if (showOnLeft)
            {
                if (arrowPointsLeft)
                {
                    // 左侧，向左 (一致)
                    cueLeftLeft.gameObject.SetActive(true);
                    currentRequiredHand = RequiredHand.Left;
                }
                else
                {
                    // 左侧，向右 (冲突)
                    cueLeftRight.gameObject.SetActive(true);
                    currentRequiredHand = RequiredHand.Right;
                }
            }
            else // showOnRight
            {
                if (arrowPointsLeft)
                {
                    // 右侧，向左 (冲突)
                    cueRightLeft.gameObject.SetActive(true);
                    currentRequiredHand = RequiredHand.Left;
                }
                else
                {
                    // 右侧，向右 (一致)
                    cueRightRight.gameObject.SetActive(true);
                    currentRequiredHand = RequiredHand.Right;
                }
            }
        }
    }


    /// <summary>
    /// 检查玩家的手势输入
    /// </summary>
    void CheckPlayerInput()
    {
        // 严格使用文档提供的 GetGestureType 接口, HandType 枚举, GestureType.Palm 枚举
        bool leftHandPalm = GesEventInput.Instance.GetGestureType(HandType.LeftHand) == GestureType.Palm;
        bool rightHandPalm = GesEventInput.Instance.GetGestureType(HandType.RightHand) == GestureType.Palm;

        RequiredHand detectedHand = RequiredHand.None;

        if (leftHandPalm && !rightHandPalm)
        {
            // 只举了左手 (手掌)
            detectedHand = RequiredHand.Left;
        }
        else if (rightHandPalm && !leftHandPalm)
        {
            // 只举了右手 (手掌)
            detectedHand = RequiredHand.Right;
        }
        else if (leftHandPalm && rightHandPalm)
        {
            // 两只手都举了，无效输入，忽略
            return;
        }
        else
        {
            // 没有举手，等待输入
            return;
        }

        // --- 一旦检测到有效输入 (只举了左手或右手) ---
        inputReceivedThisTurn = true;

        if (detectedHand == currentRequiredHand)
        {
            OnCorrectAnswer();
        }
        else
        {
            OnIncorrectAnswer();
        }

        StartCoroutine(NextTrialWithDelay(0.5f));
    }

    void OnCorrectAnswer()
    {
        Debug.Log("Correct!");
        correctStreak++;
        if (audioSource && correctSound)
        {
            audioSource.PlayOneShot(correctSound);
        }
    }

    void OnIncorrectAnswer()
    {
        Debug.Log("Incorrect!");
        correctStreak = 0;
        if (audioSource && incorrectSound)
        {
            audioSource.PlayOneShot(incorrectSound);
        }
    }

    IEnumerator NextTrialWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isPlaying)
        {
            GenerateNextTrial();
        }
    }
}