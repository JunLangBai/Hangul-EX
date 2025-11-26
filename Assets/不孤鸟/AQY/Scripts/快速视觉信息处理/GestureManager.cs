using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestureManager : MonoBehaviour
{
    // --- 在Inspector中设置的参数 ---
    [Header("测试配置")] public float testDuration = 60f; // 测试总时长
    public int gesturesPerMinute = 40;
    [Range(1, 3)] public int targetCount = 1; // 目标数量，用于控制难度
    public List<GestureData> allGestures; // 将所有8个GestureData资源拖到这里

    [Header("视觉效果")] [Tooltip("在切换到下一张图片前，图片消失的空白时间（秒）")]
    public float flashDuration = 0.1f; // 闪烁持续时间

    [Header("UI组件")] public GameObject rvpPrefabs;
    
    public SettlementScreen settings;
    
    
    public Transform contentPanel;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI tfHint;
    private Coroutine tfTextCoroutine;

    public Image stimulusImage;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI resultText;
    public GameObject instructionPanel;
    public GameObject gesturePanel;
    public GameObject resultPanel;

    // --- 内部状态变量 ---
    private enum TestState
    {
        Instructions,
        Testing,
        Results
    }

    private float targetProbability = 0.4f; // 控制目标出现概率，例如25%
    
    private TestState currentState;

    private List<GestureData> currentSequence = new List<GestureData>();
    private List<GestureData> targetGestures = new List<GestureData>();
    private int currentStimulusIndex = -1;
    private GestureData currentStimulus => currentSequence[currentStimulusIndex];

    private float stimulusInterval;
    private float testTimer;
    private float reactionTimer;

    // --- 逻辑修改 ---
    private bool responseMadeForCurrentStimulus;

    // 新增一个标志位，专门用于判断“是否做出了正确的反应”
    private bool correctResponseMadeForCurrentStimulus;

    // --- 计分 ---
    private int hits = 0;
    private int misses = 0;
    private int falseAlarms = 0;
    private List<float> reactionTimes = new List<float>();
    
    // --- 新增：用于记录总共呈现了多少个刺激物 ---
    private int stimuliPresentedCount = 0;

    void Start()
    {
        // ... Start() 函数内容不变 ...
        if (AllSettingCtr.Instance != null)
        {
            gesturesPerMinute = AllSettingCtr.Instance.attentionGesturesPerMinute;
            targetCount = AllSettingCtr.Instance.attentionTargetCount;
            flashDuration = AllSettingCtr.Instance.attentionFlashDuration;
        }
        else
        {
            gesturesPerMinute = 40;
            targetCount = 1;
            flashDuration = 0.1f;
        }

        stimulusInterval = 60f / gesturesPerMinute;
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

    // ... StartInstructionPhase(), StartTestPhase(), ShowResults(), Update() 内容不变 ...
    void StartInstructionPhase()
    {
        currentState = TestState.Instructions;
        instructionPanel.SetActive(true);
        timeText.gameObject.SetActive(true);
        gesturePanel.SetActive(false);
        resultPanel.SetActive(false);

        GenerateSequences();

        string targets = "以下图片出现时\n模仿手势:";
        // 清理旧的图标（如果重新开始测试）
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (var target in targetGestures)
        {
            GameObject newUIElement = Instantiate(rvpPrefabs, contentPanel);
            Image image = newUIElement.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = target.gestureImage;
            }
        }

        instructionText.text = targets;

        StartCoroutine(StartCount(5));
        Invoke(nameof(StartTestPhase), 5f);
    }

    void StartTestPhase()
    {
        currentState = TestState.Testing;
        gesturePanel.SetActive(true);
        timeText.gameObject.SetActive(false);
        resultPanel.SetActive(false); // 确保结果面板关闭

        hits = 0;
        misses = 0;
        falseAlarms = 0;
        reactionTimes.Clear();
        testTimer = testDuration;
        currentStimulusIndex = -1; // 重置索引
        stimuliPresentedCount = 0;

        StopAllCoroutines(); // 确保旧的协程已停止
        StartCoroutine(PresentStimuli());
    }

    void ShowResults()
    {
        currentState = TestState.Results;
        instructionPanel.SetActive(false);
        timeText.gameObject.SetActive(false);
        gesturePanel.SetActive(false);
        resultPanel.SetActive(true);
        
        // 正确率 = (总正确数 / 总呈现数) * 100
        // 添加一个检查以防止除以零
        float correctRate = (gesturesPerMinute > 0) ? ((float)hits / gesturesPerMinute) * 100f : 0f;
        
        float avgReactionTime = reactionTimes.Count > 0 ? reactionTimes.Average() : 0;
        var historyCorrect = settings.GetSavedAccuracyForCurrentScene();
        if (historyCorrect == null)
        {
            historyCorrect = correctRate;
        }
        resultText.text = $"测试结束!\n\n正确率:{correctRate:F0}\n平均反应时间: {Mathf.Round(avgReactionTime * 100) / 100}秒\n历史最佳记录:{historyCorrect:F0}";
        settings.SaveLevelAccuracy(correctRate);
    }

    void Update()
    {
        if (currentState == TestState.Testing)
        {
            if (testTimer > 0)
            {
                testTimer -= Time.deltaTime;
                reactionTimer += Time.deltaTime;
            }
            else
            {
                StopAllCoroutines();
                ShowResults();
            }
        }
    }

    // --- 核心逻辑修改 ---
    private void HandleGestureInput(Hand hand, CustomGestureType gestureType)
    {
        if (currentState != TestState.Testing || responseMadeForCurrentStimulus)
        {
            return;
        }

        responseMadeForCurrentStimulus = true; // 锁定，防止对同一刺激进行多次响应

        bool isTarget = IsTarget(currentStimulus);
        bool isCorrectGesture = (currentStimulus.hand == hand && currentStimulus.gestureType == gestureType);

        if (isTarget)
        {
            if (isCorrectGesture)
            {
                hits++;
                reactionTimes.Add(reactionTimer);
                correctResponseMadeForCurrentStimulus = true; // 标记已正确响应
                Debug.Log($"命中! 反应时间: {reactionTimer}");
                ShowTemporaryText("正确！", Color.green);
            }
            // 如果是目标但手势错误 (isCorrectGesture is false)，我们在这里不做任何事。
            // 漏报(Miss)的逻辑将在切换到下一个刺激时处理。
        }
        else // 非目标
        {
            // 对非目标做出了任何手势，都应被视为虚报
            falseAlarms++;
            Debug.Log("虚报!");
            ShowTemporaryText("错误！", Color.red);
        }
    }

    private IEnumerator PresentStimuli()
    {
        float visibleDuration = stimulusInterval - flashDuration;
        if (visibleDuration <= 0)
        {
            Debug.LogError("错误：flashDuration 必须小于 stimulusInterval！");
            visibleDuration = 0.01f;
        }

        // 计算总共要呈现多少个手势
        int totalStimuli = Mathf.FloorToInt(testDuration / stimulusInterval);

        for (int i = 0; i < totalStimuli; i++)
        {
            // --- 计分逻辑修改 ---
            // 在呈现新刺激之前，检查上一个刺激的计分情况
            if (currentStimulusIndex >= 0)
            {
                // 如果上一个刺激是目标，并且直到现在还没有收到“正确”的响应
                if (IsTarget(currentStimulus) && !correctResponseMadeForCurrentStimulus)
                {
                    misses++; // 记为一次“漏报”
                    Debug.Log("漏报!");
                    ShowTemporaryText("漏掉了！", Color.yellow);
                }
            }

            // 检查测试时间是否结束
            if (testTimer <= 0) break;

            // --- 准备并呈现下一个刺激 ---
            currentStimulusIndex = i;
            // 确保索引不会越界
            if (currentStimulusIndex >= currentSequence.Count)
            {
                Debug.LogWarning("手势序列已用完，测试提前结束。");
                break;
            }

            
            stimuliPresentedCount++; // --- 修改：每次呈现新刺激时，计数器加1 ---
            
            stimulusImage.sprite = currentStimulus.gestureImage;
            responseMadeForCurrentStimulus = false;
            correctResponseMadeForCurrentStimulus = false; // 重置正确响应标记
            reactionTimer = 0f;

            stimulusImage.enabled = true;
            yield return new WaitForSeconds(visibleDuration);

            stimulusImage.enabled = false;
            yield return new WaitForSeconds(flashDuration);
        }

        // 循环结束后，手动检查最后一个刺激
        if (currentStimulusIndex >= 0 && IsTarget(currentStimulus) && !correctResponseMadeForCurrentStimulus)
        {
            misses++;
            Debug.Log("漏报 (最后一个)!");
        }

        // 协程自然结束，调用结果展示
        if (currentState == TestState.Testing)
        {
            ShowResults();
        }
    }

    // ... GenerateSequences(), IsTarget(), ShowTemporaryText() 和 ShowTextCoroutine() 不变 ...
    void GenerateSequences()
    {
        // 1. 随机选择一只手作为目标手
        Hand targetHand = (Random.value > 0.5f) ? Hand.Left : Hand.Right;

        // 2. 从所有手势中，根据目标手和数量，选出目标手势
        targetGestures = allGestures
            .Where(g => g.hand == targetHand)
            .OrderBy(x => Random.value)
            .Take(targetCount)
            .ToList();

        // 3. 为了高效生成序列，先筛选出所有的“非目标手势”
        List<GestureData> nonTargetGestures = allGestures.Except(targetGestures).ToList();

        // 清空旧序列
        currentSequence.Clear();
        int totalGesturesInTest = Mathf.CeilToInt(testDuration / stimulusInterval) + 2;

        // --- 核心修改在这里 ---
        for (int i = 0; i < totalGesturesInTest; i++)
        {
            // 根据设定的概率，决定下一个手势是“目标”还是“非目标”
            bool isNextGestureTarget = Random.value < targetProbability;

            if (isNextGestureTarget && targetGestures.Count > 0)
            {
                // 如果是目标，就从目标列表中随机选一个
                currentSequence.Add(targetGestures[Random.Range(0, targetGestures.Count)]);
            }
            else
            {
                // 如果是非目标，或者目标列表为空（以防万一），就从非目标列表中随机选一个
                if (nonTargetGestures.Count > 0)
                {
                    currentSequence.Add(nonTargetGestures[Random.Range(0, nonTargetGestures.Count)]);
                }
                else
                {
                    // 极端情况：如果所有手势都是目标，就只能从目标里选了
                    currentSequence.Add(targetGestures[Random.Range(0, targetGestures.Count)]);
                }
            }
        }
    }

    bool IsTarget(GestureData gesture)
    {
        return targetGestures.Contains(gesture);
    }

    public void ShowTemporaryText(string message, Color textColor)
    {
        if (tfTextCoroutine != null)
        {
            StopCoroutine(tfTextCoroutine);
        }

        tfTextCoroutine = StartCoroutine(ShowTextCoroutine(message, textColor));
    }

    private IEnumerator ShowTextCoroutine(string message, Color textColor)
    {
        tfHint.text = message;
        tfHint.color = textColor;
        yield return new WaitForSeconds(1.2f);
        tfHint.text = string.Empty;
        tfTextCoroutine = null;
    }

    IEnumerator StartCount(int time)
    {
        int remainingTime = time;

        // 循环直到剩余时间为0
        while (remainingTime > 0)
        {
            // 更新UI显示（这里直接显示数字，也可以格式化如"00:05"）
            timeText.text = $"{remainingTime}秒后开始...";

            // 等待1秒（协程暂停1秒后继续执行）
            yield return new WaitForSeconds(1f);

            // 剩余时间减1
            remainingTime--;
        }
    }
}