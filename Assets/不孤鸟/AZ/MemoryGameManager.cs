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
    public ASRManager asrManager;
    private AudioSource audioSource;
    
    [Header("UI 元素 (UI Elements)")]
    [Tooltip("“下一轮”按钮 (原 'startGameButton')")]
    public Button nextRoundButton; // 已重命名，原 'startGameButton'
    [Tooltip("用于显示模式、反馈 ('正确', '错误') 的文本框 (Feedback text)")]
    public TextMeshProUGUI feedbackText;
    
    [Header("游戏按钮 (0-9) (Number Buttons)")]
    public Button[] numberButtons;

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

    // --- [修改] ---
    // GameSettingsManager 会在游戏开始时调用它
    // 我们不需要在这里自动绑定它，而是让 GameSettingsManager 来控制
    private bool isNormalModeGame = true; // 标记当前是否为“常规模式”

    void Start()
    {
        // 1. 获取组件
        audioSource = GetComponent<AudioSource>();

        // 2. 检查引用
        if (asrManager == null) Debug.LogError("ASRManager 未在 Inspector 中指定！");
        if (numberButtons.Length != 10 || digitAudioClips.Length != 10) Debug.LogError("数字按钮或音频剪辑必须正好为10个！");

        // 3. 绑定 UI 事件
        // [修改] "开始游戏"按钮(现在是"下一轮"按钮)的逻辑被移到了 ProcessAnswer 中
        // 我们希望这个按钮只在答题结束后才可用
        if (nextRoundButton == null) Debug.LogError("NextRoundButton (原 startGameButton) 未指定!");
        
        // [修改] 我们让 "下一轮" 按钮在常规模式下调用 StartGame()
        // 在自定义模式下，它会做什么？... 让我们重新思考一下。
        // 更好的逻辑：
        // 1. GameSettingsManager 调用 StartGame() 或 StartCustomGame()
        // 2. 游戏运行 -> ProcessAnswer()
        // 3. ProcessAnswer() 启用 "nextRoundButton"
        // 4. "nextRoundButton" 被点击时，需要知道是继续 "常规模式" 还是 "自定义模式"
        
        // 让我们简化一下：
        // 1. "nextRoundButton" (原 startGameButton) 只在 *常规模式* 下使用。
        // 2. 在 *自定义模式* 下，答完一题后，你需要返回菜单或重新开始自定义游戏。
        // 
        // 为了实现你的要求，我们假设 "下一轮" 按钮总是启动 *常规模式*
        // 因此，我们将它重命名为 nextRoundButton，并在 Start() 中绑定它
        
        nextRoundButton.onClick.AddListener(StartGame); // 这个按钮现在总是启动“常规模式”

        // 循环绑定10个数字按钮
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int number = i; 
            numberButtons[i].onClick.AddListener(() => OnNumberButtonPressed(number));
        }

        // 4. 订阅 ASR 事件
        asrManager.OnASRResultReady += OnASRResultReceived;

        // 5. 初始化
        feedbackText.text = "请从主菜单开始游戏";
        SetInputActive(false); 
        nextRoundButton.interactable = false; // 游戏开始前（在菜单时）禁用
        // GameSettingsManager 会在切换面板时启用它（如果它绑定的是 StartNormalGame）
        // 让我们在 Start() 中禁用它，然后在 ShowGamePanel() (在 SettingsManager 中) 再启用它。
        
        // *** 重要 ***
        // 你需要修改 GameSettingsManager.cs 中的 ShowGamePanel() 方法
        // 添加: gameManager.nextRoundButton.interactable = true;
        // 并且在 GameSettingsManager 的 Start() 中设置:
        // gameManager.nextRoundButton.interactable = false;
        //
        // 为了简单起见，我将假设 "nextRoundButton" 在游戏面板上，并且 *总是* 可见的。
        // 让我们修改 GameSettingsManager 的逻辑。

        // --- 让我们采用更简单的逻辑 ---
        // 1. 你原来的 'startGameButton' 现在改名为 'nextRoundButton'。
        // 2. 它在 Start() 中绑定到 StartGame()。
        // 3. 它只在 ProcessAnswer() (回答完毕) 和 Start() (游戏初始) 时启用。
        // 4. GameSettingsManager 不再需要 'startNormalGameButton'，它只需要 'startCustomGameButton'。
        // 5. 你原来的 'startGameButton' (现在是 'nextRoundButton') 就是用来玩“常规模式”的。

        // --- 好的，我们回到上面的代码，它基本是正确的 ---
        // Start()
        // ... (绑定 nextRoundButton.onClick.AddListener(StartGame)) ...
        // feedbackText.text = "请点击开始常规游戏";
        // SetInputActive(false); 
        // nextRoundButton.interactable = true; // 初始状态，允许开始常规游戏
        
        // (在 Start() 中...)
        feedbackText.text = "请点击'开始游戏'或从菜单自定义";
        SetInputActive(false); 
        nextRoundButton.interactable = true; // 初始状态，允许开始
    }

    /// <summary>
    /// [修改] "常规模式" 的入口点
    /// (由 'nextRoundButton' 或 'startNormalGameButton' 调用)
    /// </summary>
    public void StartGame()
    {
        if (gameInProgress) return;
        
        isNormalModeGame = true; // 标记为常规模式

        // --- 核心逻辑：决定本轮模式 ---
        if (consecutiveCorrectAnswers >= 3)
        {
            isOrderMode = false;
            consecutiveCorrectAnswers = 0; 
        }
        else
        {
            isOrderMode = true;
        }
        
        // 随机难度
        int difficulty = Random.Range(2, 6); // (2, 3, 4, 5)

        // 调用核心准备方法
        PrepareAndStartGame(difficulty, isOrderMode);
    }

    /// <summary>
    /// [新增] "自定义模式" 的入口点
    /// (由 GameSettingsManager 调用)
    /// </summary>
    /// <param name="difficulty">指定的难度 (e.g., 2, 3, 4, 5)</param>
    /// <param name="isOrder">指定的模式 (true=顺序)</param>
    public void StartCustomGame(int difficulty, bool isOrder)
    {
        if (gameInProgress) return;
        
        isNormalModeGame = false; // 标记为自定义模式
        consecutiveCorrectAnswers = 0; // 自定义模式不计入连续答对

        // 使用传入的参数
        PrepareAndStartGame(difficulty, isOrder);
    }

    /// <summary>
    /// [新增] 真正准备并开始游戏的核心方法
    /// </summary>
    private void PrepareAndStartGame(int difficulty, bool isOrder)
    {
        gameInProgress = true;
        nextRoundButton.interactable = false; // 游戏开始，禁用"下一轮"

        // 启动休息计时器（如果尚未启动）
        if (restTimerCoroutine == null)
        {
            restTimerCoroutine = StartCoroutine(RestTimerCoroutine());
            Debug.Log("[GameManager] 休息计时器已启动。");
        }

        SetInputActive(false); // 播放时禁用所有输入
        
        currentSequence.Clear();
        playerInputString = "";

        // 设置模式和UI
        this.isOrderMode = isOrder; // 保存当前模式
        feedbackText.text = isOrderMode ? "模式: 顺序" : "模式: 逆序";
        Debug.Log($"[GameManager] 新回合开始。难度: {difficulty}, 模式: {(isOrderMode ? "顺序" : "逆序")}");

        // 3. 生成题目序列
        for (int i = 0; i < difficulty; i++)
        {
            currentSequence.Add(Random.Range(0, 10)); // 0-9
        }
        
        // 4. 生成答案字符串
        currentAnswerString = GenerateAnswerString();
        Debug.Log($"[GameManager] 题目已生成 (Question): {string.Join(",", currentSequence)}");
        Debug.Log($"[GameManager] 正确答案 (Answer): {currentAnswerString}");

        // 5. 开始播放
        StartCoroutine(PlaySequence());
    }


    IEnumerator PlaySequence()
    {
        // 1. 播放模式提示音
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

        // 2. 提示“请仔细听”
        feedbackText.text = "请仔细听...";
        yield return new WaitForSeconds(0.5f); // 准备时间

        // 3. 播放数字序列
        foreach (int num in currentSequence)
        {
            audioSource.PlayOneShot(digitAudioClips[num]);
            yield return new WaitForSeconds(digitAudioClips[num].length + 0.2f);
        }

        // 4. 播放完毕
        feedbackText.text = "请回答 (按键或语音)";
        SetInputActive(true); // 允许玩家输入
        gameInProgress = false; 
    }

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
        string cleanedResult = CleanASRString(rawASRResult);
        Debug.Log($"[GameManager] 清理后结果 (Cleaned): {cleanedResult}");

        bool isCorrect = (cleanedResult == currentAnswerString);
        string feedback = isCorrect ? "正确！" : $"错误。识别到: {cleanedResult}。正确答案是: {currentAnswerString}";
        
        ProcessAnswer(isCorrect, feedback);
    }

    /// <summary>
    /// [修改] 统一处理答案
    /// </summary>
    private void ProcessAnswer(bool isCorrect, string feedbackMessage)
    {
        feedbackText.text = feedbackMessage;
        SetInputActive(false); // 答题完毕，禁用输入
        
        // [修改] 只有在常规模式下才启用 "下一轮" 按钮
        // 在自定义模式下，玩家需要返回主菜单
        if (isNormalModeGame)
        {
            nextRoundButton.interactable = true; // 允许开始下一轮（常规）
        }
        else
        {
            feedbackText.text += "\n(自定义模式结束，请返回菜单)";
            // 此时，GameSettingsManager 应该提供一个"返回"按钮
        }


        if (isCorrect)
        {
            Debug.Log("[GameManager] 回答正确。");
            if (audioClipCorrect != null) audioSource.PlayOneShot(audioClipCorrect);
            
            // [修改] 只有在“常规模式”下才计数
            if (isNormalModeGame && isOrderMode)
            {
                consecutiveCorrectAnswers++;
            }
        }
        else
        {
            Debug.Log("[GameManager] 回答错误。");
            if (audioClipWrong != null) audioSource.PlayOneShot(audioClipWrong);
            
            // 答错，重置计数（仅在常规模式下有意义）
            if (isNormalModeGame)
            {
                consecutiveCorrectAnswers = 0;
            }
        }
        
        Debug.Log($"[GameManager] 连续答对次数: {consecutiveCorrectAnswers}");
    }

    
    // --- 辅助方法 (无需修改) ---

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