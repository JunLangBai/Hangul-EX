using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class MemoryGameManager : MonoBehaviour
{
    [Header("游戏核心引用 (Core References)")]
    [Tooltip("将场景中的 ASRManager 脚本组件拖到这里")]
    public ASRManager asrManager;

    // [已修改] 不再需要 public 引用，将自动查找单例
    private AllSettingCtr allSettingCtr; 
    
    [Tooltip("用于播放0-9数字和提示音的 AudioSource")]
    private AudioSource audioSource;
    
    [Header("UI 元素 (UI Elements)")]
    public Button startGameButton;
    [Tooltip("用于显示模式、反馈 ('正确', '错误') 的文本框")]
    public TextMeshProUGUI feedbackText;
    
    [Header("游戏按钮 (0-9) (Number Buttons)")]
    public Button[] numberButtons;

    // --- [新] 计数器逻辑移到此处 ---
    [Header("做题计数器 (Counter)")]
    [Tooltip("用于显示累计做了多少题的文本")]
    public TextMeshProUGUI counterDisplay; // 【新增】拖入你的TMP文本

    private int completedRoundsCounter = 0; // 【新增】用于计数的变量
    // --- [新结束] ---

    [Header("数字音频 (0-9) (Digit Audio)")]
    public AudioClip[] digitAudioClips;

    [Header("游戏提示音频 (Gameplay Audio Cues)")]
    public AudioClip audioClipOrder;
    public AudioClip audioClipReverse;
    public AudioClip audioClipCorrect;
    public AudioClip audioClipWrong;
    public AudioClip audioClipRest;
    
    // --- 内部游戏状态 (Internal Game State) ---
    private bool isOrderMode = true; 
    private int consecutiveCorrectAnswers = 0; 
    private Coroutine restTimerCoroutine = null; 

    private List<int> currentSequence = new List<int>(); 
    private string currentAnswerString; 
    private string playerInputString = ""; 
    private bool gameInProgress = false; 

    private readonly Regex punctuationRegex = new Regex("[,.，。？！ ]");
    private readonly string[] chineseNumbers = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    
    private readonly Dictionary<char, char> koreanToChineseMap = new Dictionary<char, char>()
    {
        {'영', '零'}, {'공', '零'}, {'령', '零'}, // 0
        {'일', '一'},                             // 1
        {'이', '二'},                             // 2
        {'삼', '三'}, {'선', '三'},               // 3 (包含你遇到的音译 "선")
        {'사', '四'}, {'시', '四'},               // 4 (包含你遇到的音译 "시")
        {'오', '五'}, {'우', '五'},                             // 5
        {'육', '六'}, {'륙', '六'},               // 6
        {'칠', '七'},                             // 7
        {'팔', '八'},                             // 8
        {'구', '九'}, {'주', '九'}                              // 9
    };

    void Start()
    {
        // 1. 获取组件
        audioSource = GetComponent<AudioSource>();

        // 2. [已修改] 查找单例
        allSettingCtr = AllSettingCtr.Instance; 
        
        // 3. 检查引用
        if (asrManager == null) Debug.LogError("ASRManager 未在 Inspector 中指定！");
        if (numberButtons.Length != 10 || digitAudioClips.Length != 10) Debug.LogError("数字按钮或音频剪辑必须正好为10个！");
        if (allSettingCtr == null) Debug.LogWarning("GameSettingsMenu.Instance 未找到，将使用自动模式。");
        if (counterDisplay == null) Debug.LogWarning("CounterDisplay (计数器文本) 未在 Inspector 中指定！");

        // 4. 绑定 UI 事件
        startGameButton.onClick.AddListener(StartGame);

        // 循环绑定10个数字按钮
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int number = i; 
            numberButtons[i].onClick.AddListener(() => OnNumberButtonPressed(number));
        }

        // 5. 订阅 ASR 事件
        asrManager.OnASRResultReady += OnASRResultReceived;

        // 6. 初始化
        feedbackText.text = "请点击开始游戏";
        SetInputActive(false); 
        startGameButton.interactable = true; // 初始状态，允许开始
        UpdateCounterDisplay(); // [新增] 初始化计数器显示为0
    }


    /// <summary>
    /// 开始新一轮游戏 (Start a new game round)
    /// </summary>
    void StartGame()
    {
        if (gameInProgress) return;
        gameInProgress = true;
        startGameButton.interactable = false; 

        if (restTimerCoroutine == null)
        {
            restTimerCoroutine = StartCoroutine(RestTimerCoroutine());
            Debug.Log("[GameManager] 休息计时器已启动。");
        }

        SetInputActive(false); 
        
        currentSequence.Clear();
        playerInputString = "";

        // --- 【修改】从设置菜单单例读取 ---
        int manualDifficulty = 0;
        int manualMode = 0; // 0=Auto, 1=Order, 2=Reverse

        if (allSettingCtr != null) // 检查是否成功找到了单例
        {
            manualDifficulty = allSettingCtr.memoryDifficulty;
            manualMode = allSettingCtr.memoryMode;
        }

        // --- 决定模式 ---
        if (manualMode == 1) // 1 = 固定顺序
        {
            isOrderMode = true;
            Debug.Log("[GameManager] 模式: 固定顺序 (手动)");
        }
        else if (manualMode == 2) // 2 = 固定逆序
        {
            isOrderMode = false;
            Debug.Log("[GameManager] 模式: 固定逆序 (手动)");
        }
        else // 0 = 自动模式
        {
            Debug.Log("[GameManager] 模式: 自动 (3次答对切换)");
            if (consecutiveCorrectAnswers >= 3)
            {
                isOrderMode = false;
                consecutiveCorrectAnswers = 0; 
            }
            else
            {
                isOrderMode = true;
            }
        }
        
        feedbackText.text = isOrderMode ? "模式: 顺序" : "模式: 逆序";
        
        // --- 决定难度 ---
        int difficulty;
        if (manualDifficulty > 0) // 0 = 随机
        {
            difficulty = manualDifficulty;
            Debug.Log($"[GameManager] 难度: {difficulty} (手动)");
        }
        else
        {
            difficulty = Random.Range(2, 6); // (2, 3, 4, 5)
            Debug.Log($"[GameManager] 难度: {difficulty} (随机)");
        }

        // ... (生成题目和答案的逻辑不变) ...
        for (int i = 0; i < difficulty; i++)
        {
            currentSequence.Add(Random.Range(0, 10));
        }
        
        currentAnswerString = GenerateAnswerString();
        Debug.Log($"[GameManager] 题目已生成 (Question): {string.Join(",", currentSequence)}");
        Debug.Log($"[GameManager] 正确答案 (Answer): {currentAnswerString}");

        StartCoroutine(PlaySequence());
    }

    /// <summary>
    /// 协程：播放模式提示音，然后播放数字序列
    /// </summary>
    IEnumerator PlaySequence()
    {
        AudioClip modeClip = isOrderMode ? audioClipOrder : audioClipReverse;
        if (modeClip != null)
        {
            audioSource.PlayOneShot(modeClip);
            yield return new WaitForSeconds(modeClip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f); 
        }

        feedbackText.text = "请仔细听...";
        yield return new WaitForSeconds(0.5f); 

        foreach (int num in currentSequence)
        {
            audioSource.PlayOneShot(digitAudioClips[num]);
            yield return new WaitForSeconds(digitAudioClips[num].length + 0.2f);
        }

        feedbackText.text = "请回答 (按键或语音)";
        SetInputActive(true); 
        gameInProgress = false; 
    }

    /// <summary>
    /// 协程：5分钟休息提醒
    /// </summary>
    IEnumerator RestTimerCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(300f); // 5分钟
            Debug.Log("[GameManager] 5分钟提醒：请休息。");
            if (audioClipRest != null)
            {
                audioSource.PlayOneShot(audioClipRest);
            }
        }
    }


    void OnNumberButtonPressed(int number)
    {
        if (gameInProgress) return; 
        if (string.IsNullOrEmpty(currentAnswerString)) return; 

        playerInputString += NumberToChinese(number);
        feedbackText.text = "已输入: " + playerInputString;

        if (playerInputString.Length == currentAnswerString.Length)
        {
            CheckButtonAnswer();
        }
    }

    void CheckButtonAnswer()
    {
        bool isCorrect = (playerInputString == currentAnswerString);
        string feedback = isCorrect ? "正确！" : $"错误。正确答案是: {currentAnswerString}";
        ProcessAnswer(isCorrect, feedback);
        playerInputString = ""; 
    }

    void OnASRResultReceived(string rawASRResult)
    {
        if (gameInProgress) return; 
        if (string.IsNullOrEmpty(currentAnswerString)) return; 

        Debug.Log($"[GameManager] 收到 ASR 原始结果 (Raw ASR): {rawASRResult}");
        
        // 1. 去除标点符号和空格
        string cleanedResult = CleanASRString(rawASRResult);
        
        // 2. 将可能存在的阿拉伯数字统一转换为中文数字
        cleanedResult = ConvertArabicToChinese(cleanedResult);

        // 3. 【新增】将可能存在的韩文统一转换为中文数字
        cleanedResult = ConvertKoreanToChinese(cleanedResult);
        
        Debug.Log($"[GameManager] 清理与转换后最终结果 (Final Result): {cleanedResult}");

        // 4. 比对
        bool isCorrect = (cleanedResult == currentAnswerString);
        string feedback = isCorrect ? "正确！" : $"错误。识别到: {cleanedResult}。正确答案是: {currentAnswerString}";
        
        ProcessAnswer(isCorrect, feedback);
    }

    /// <summary>
    /// 【新增】将字符串中的阿拉伯数字 (0-9) 转换为对应的中文数字 (零-九)
    /// </summary>
    string ConvertArabicToChinese(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        
        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            // 判断当前字符是否为阿拉伯数字
            if (char.IsDigit(c))
            {
                // 将字符 '0'-'9' 转换为整数 0-9
                int digit = c - '0';
                // 使用类中已经定义好的 chineseNumbers 数组进行映射
                sb.Append(chineseNumbers[digit]); 
            }
            else
            {
                // 如果原本就是中文（如"一"）或其他字符，原样保留
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    
    /// <summary>
    /// 【新增】将字符串中的韩文数字/音译转换为对应的中文数字
    /// </summary>
    string ConvertKoreanToChinese(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        
        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            // 如果字典里包含这个韩文字符，就转换成对应的中文数字
            if (koreanToChineseMap.ContainsKey(c))
            {
                sb.Append(koreanToChineseMap[c]);
            }
            else
            {
                // 如果不是韩文，原样保留（比如已经是中文了）
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    
    /// <summary>
    /// [已修改] 统一处理答案对错的逻辑
    /// </summary>
    private void ProcessAnswer(bool isCorrect, string feedbackMessage)
    {
        feedbackText.text = feedbackMessage;
        SetInputActive(false); 
        startGameButton.interactable = true; 

        if (isCorrect)
        {
            Debug.Log("[GameManager] 回答正确。");
            if (audioClipCorrect != null) audioSource.PlayOneShot(audioClipCorrect);
            if (isOrderMode) consecutiveCorrectAnswers++;
        }
        else
        {
            Debug.Log("[GameManager] 回答错误。");
            if (audioClipWrong != null) audioSource.PlayOneShot(audioClipWrong);
            consecutiveCorrectAnswers = 0;
        }
        
        Debug.Log($"[GameManager] 连续答对次数: {consecutiveCorrectAnswers}");

        // --- 【修改】调用内部计数器 ---
        completedRoundsCounter++;
        UpdateCounterDisplay();
        // --- 【修改结束】 ---
    }

    // --- [新] 计数器更新方法 ---
    /// <summary>
    /// [新] 更新计数器显示的文本
    /// </summary>
    private void UpdateCounterDisplay()
    {
        if (counterDisplay != null)
        {
            counterDisplay.text = $"你已经做了 {completedRoundsCounter} 道题！";
        }
    }

    
    // --- 辅助方法 (Helper Methods) ---

    void SetInputActive(bool isActive)
    {
        foreach (var btn in numberButtons)
        {
            btn.interactable = isActive;
        }
        asrManager.SetRecordButtonActive(isActive);
    }

    string GenerateAnswerString()
    {
        StringBuilder sb = new StringBuilder();
        if (isOrderMode)
        {
            foreach (int num in currentSequence) sb.Append(NumberToChinese(num));
        }
        else
        {
            for (int i = currentSequence.Count - 1; i >= 0; i--) sb.Append(NumberToChinese(currentSequence[i]));
        }
        return sb.ToString();
    }

    string CleanASRString(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return punctuationRegex.Replace(raw, "");
    }

    string NumberToChinese(int num)
    {
        if (num < 0 || num > 9) return "";
        return chineseNumbers[num];
    }

    private void OnDestroy()
    {
        if (asrManager != null)
        {
            asrManager.OnASRResultReady -= OnASRResultReceived;
        }

        if (restTimerCoroutine != null)
        {
            StopCoroutine(restTimerCoroutine);
        }
    }
}