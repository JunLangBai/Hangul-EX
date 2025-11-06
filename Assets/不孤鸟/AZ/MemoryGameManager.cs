using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 游戏的核心逻辑管理器 (Game's core logic manager)
/// 负责：出题、播放音频、接收按键输入、接收ASR结果、判断对错
/// (Handles: creating questions, playing audio, receiving button input, receiving ASR results, judging correctness)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MemoryGameManager : MonoBehaviour
{
    [Header("游戏核心引用 (Core References)")]
    [Tooltip("将场景中的 ASRManager 脚本组件拖到这里 (Drag the ASRManager script component here)")]
    public ASRManager asrManager;
    
    [Tooltip("用于播放0-9数字音频的 AudioSource 组件 (AudioSource for playing 0-9 digits)")]
    private AudioSource audioSource;
    
    [Header("UI 元素 (UI Elements)")]
    public Button startGameButton;
    [Tooltip("“顺序”模式按钮 (Order Mode Button)")]
    public Button orderModeButton;
    [Tooltip("“逆序”模式按钮 (Reverse Mode Button)")]
    public Button reverseModeButton;
    [Tooltip("用于显示 '正确' 或 '错误，答案是...' 的文本框 (Feedback text)")]
    public TextMeshProUGUI feedbackText;
    
    [Header("游戏按钮 (0-9) (Number Buttons)")]
    [Tooltip("请按 0 到 9 的顺序拖入10个按钮 (Drag 10 buttons here, in 0-9 order)")]
    public Button[] numberButtons;

    [Header("音频资源 (0-9) (Audio Clips)")]
    [Tooltip("请按 0 到 9 的顺序拖入10个音频文件 (Drag 10 audio clips here, in 0-9 order)")]
    public AudioClip[] digitAudioClips;
    
    // --- 内部游戏状态 (Internal Game State) ---
    private bool isOrderMode = true;
    private List<int> currentSequence = new List<int>(); // 当前的题目序列 (Current question sequence), e.g., [1, 5, 2]
    private string currentAnswerString; // 当前的正确答案 (Current correct answer), e.g., "一五二"
    private string playerInputString = ""; // 玩家通过按键输入的字符串 (Player's input string via buttons)
    private bool gameInProgress = false; // 标记游戏是否正在进行 (Is game in progress?)

    // 用于清理 ASR 结果中的标点符号 (Regex for cleaning ASR punctuation)
    private readonly Regex punctuationRegex = new Regex("[,.，。？！ ]");
    // 数字到汉字的映射 (Map for numbers to Chinese characters)
    private readonly string[] chineseNumbers = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

    void Start()
    {
        // 1. 获取必要的组件 (Get required components)
        audioSource = GetComponent<AudioSource>();

        // 2. 检查引用是否完整 (Check references)
        if (asrManager == null)
        {
            Debug.LogError("ASRManager 未在 Inspector 中指定！");
            return;
        }
        if (numberButtons.Length != 10 || digitAudioClips.Length != 10)
        {
            Debug.LogError("数字按钮或音频剪辑必须正好为10个！");
            return;
        }

        // 3. 绑定 UI 事件 (Bind UI events)
        startGameButton.onClick.AddListener(StartGame);
        orderModeButton.onClick.AddListener(() => SetMode(true));
        reverseModeButton.onClick.AddListener(() => SetMode(false));

        // 循环绑定10个数字按钮的点击事件 (Loop to bind all 10 number buttons)
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int number = i; // 必须在闭包中捕获变量 (Must capture variable in closure)
            numberButtons[i].onClick.AddListener(() => OnNumberButtonPressed(number));
        }

        // 4. 订阅 ASRManager 的结果事件 (Subscribe to ASRManager's event)
        // 当 ASRManager 收到结果时，它会调用 OnASRResultReceived 方法
        asrManager.OnASRResultReady += OnASRResultReceived;

        // 初始化 (Initialize)
        SetMode(true);
        feedbackText.text = "请点击开始游戏";
        SetInputActive(false); // 游戏未开始时禁用输入 (Disable input before game starts)
    }

    /// <summary>
    /// 设置游戏模式（顺序/逆序） (Set game mode)
    /// </summary>
    void SetMode(bool isOrder)
    {
        if (gameInProgress) return; // 游戏中不允许切换模式 (Don't allow switching mode mid-game)
        
        isOrderMode = isOrder;
        feedbackText.text = isOrderMode ? "模式: 顺序" : "模式: 逆序";
        
        // 更新按钮视觉效果 (Update button visuals)
        orderModeButton.GetComponent<Image>().color = isOrderMode ? Color.green : Color.white;
        reverseModeButton.GetComponent<Image>().color = isOrderMode ? Color.white : Color.green;
    }

    /// <summary>
    /// 开始新一轮游戏 (Start a new game round)
    /// </summary>
    void StartGame()
    {
        if (gameInProgress) return;
        gameInProgress = true;

        feedbackText.text = "请仔细听...";
        SetInputActive(false); // 播放时禁用所有输入 (Disable input during playback)
        
        // 1. 清理上一轮状态 (Clear previous state)
        currentSequence.Clear();
        playerInputString = "";

        // 2. 随机难度 (2-5个数字) (Randomize difficulty)
        int difficulty = Random.Range(2, 6); // (2, 3, 4, 5)

        // 3. 随机生成题目序列 (Generate random sequence)
        for (int i = 0; i < difficulty; i++)
        {
            currentSequence.Add(Random.Range(0, 10)); // 0-9
        }
        
        // 4. 根据模式生成正确答案字符串 (Generate answer string based on mode)
        currentAnswerString = GenerateAnswerString();
        Debug.Log($"[GameManager] 题目已生成 (Question): {string.Join(",", currentSequence)}");
        Debug.Log($"[GameManager] 正确答案 (Answer): {currentAnswerString}");

        // 5. 开始播放音频序列 (Play audio sequence)
        StartCoroutine(PlaySequence());
    }

    /// <summary>
    /// 协程：按顺序播放音频 (Coroutine: Play sequence)
    /// </summary>
    IEnumerator PlaySequence()
    {
        yield return new WaitForSeconds(0.5f); // 准备时间 (Prep time)

        foreach (int num in currentSequence)
        {
            audioSource.PlayOneShot(digitAudioClips[num]);
            // 等待当前音频播放完毕 + 0.2秒间隔 (Wait for clip to finish + 0.2s gap)
            yield return new WaitForSeconds(digitAudioClips[num].length + 0.2f);
        }

        // 播放完毕 (Playback finished)
        feedbackText.text = "请回答 (按键或语音)";
        SetInputActive(true); // 允许玩家输入 (Allow player input)
        gameInProgress = false; 
    }

    /// <summary>
    /// 当玩家按下 0-9 按钮时调用 (Called when 0-9 button is pressed)
    /// </summary>
    void OnNumberButtonPressed(int number)
    {
        if (!gameInProgress) // 只有在播放完毕后才接收按键 (Only accept input after playback)
        {
            playerInputString += NumberToChinese(number);
            feedbackText.text = "已输入: " + playerInputString;

            // 检查玩家输入是否已达到答案长度 (Check if input length matches answer length)
            if (playerInputString.Length == currentAnswerString.Length)
            {
                CheckButtonAnswer();
            }
        }
    }

    /// <summary>
    /// 检查按键输入的答案 (Check button-based answer)
    /// </summary>
    void CheckButtonAnswer()
    {
        if (playerInputString == currentAnswerString)
        {
            feedbackText.text = "正确！";
        }
        else
        {
            feedbackText.text = $"错误。正确答案是: {currentAnswerString}";
        }
        
        playerInputString = ""; 
        SetInputActive(false); // 答题完毕，禁用输入 (Answered, disable input)
    }

    /// <summary>
    /// 当 ASRManager 广播 ASR 结果时调用 (Called when ASRManager broadcasts a result)
    /// </summary>
    void OnASRResultReceived(string rawASRResult)
    {
        if (string.IsNullOrEmpty(currentAnswerString)) return; // 游戏还没出题 (Game hasn't started)

        Debug.Log($"[GameManager] 收到 ASR 原始结果 (Raw ASR): {rawASRResult}");
        
        // 1. 清理标点符号 (Clean punctuation)
        string cleanedResult = CleanASRString(rawASRResult);
        Debug.Log($"[GameManager] 清理后结果 (Cleaned): {cleanedResult}");

        // 2. 判断对错 (Check answer)
        if (cleanedResult == currentAnswerString)
        {
            feedbackText.text = "正确！";
        }
        else
        {
            feedbackText.text = $"错误。识别到: {cleanedResult}。正确答案是: {currentAnswerString}";
        }
        
        SetInputActive(false); // 答题完毕 (Answered)
    }
    
    // --- 辅助方法 (Helper Methods) ---

    /// <summary>
    /// 激活或禁用所有输入按钮 (Enable/Disable all input)
    /// </summary>
    void SetInputActive(bool isActive)
    {
        // 激活/禁用 0-9 按钮 (0-9 buttons)
        foreach (var btn in numberButtons)
        {
            btn.interactable = isActive;
        }
        
        // 激活/禁用录音按钮 (Record button)
        asrManager.SetRecordButtonActive(isActive);
    }

    /// <summary>
    /// 根据当前模式和序列生成答案字符串 (Generate answer string)
    /// </summary>
    string GenerateAnswerString()
    {
        StringBuilder sb = new StringBuilder();
        if (isOrderMode)
        {
            // 顺序 (Order)
            foreach (int num in currentSequence)
            {
                sb.Append(NumberToChinese(num));
            }
        }
        else
        {
            // 逆序 (Reverse)
            for (int i = currentSequence.Count - 1; i >= 0; i--)
            {
                sb.Append(NumberToChinese(currentSequence[i]));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 辅助：将 ASR 结果中的标点符号移除 (Helper: Clean ASR string)
    /// </summary>
    string CleanASRString(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return punctuationRegex.Replace(raw, "");
    }

    /// <summary>
    /// 辅助：将 0-9 整数转换为汉字 (Helper: Convert int 0-9 to Chinese char)
    /// </summary>
    string NumberToChinese(int num)
    {
        if (num < 0 || num > 9) return "";
        return chineseNumbers[num];
    }

    // 确保在对象销毁时取消订阅事件 (Unsubscribe on destroy)
    private void OnDestroy()
    {
        if (asrManager != null)
        {
            asrManager.OnASRResultReady -= OnASRResultReceived;
        }
    }
}