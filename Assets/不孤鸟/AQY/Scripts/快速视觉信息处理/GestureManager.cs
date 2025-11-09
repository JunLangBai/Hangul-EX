using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestureManager : MonoBehaviour
{
    // --- 在Inspector中设置的参数 ---
    [Header("测试配置")]
    public float testDuration = 60f; // 测试总时长
    public int gesturesPerMinute = 40;
    [Range(1, 3)]
    public int targetCount = 1; // 目标数量，用于控制难度
    public List<GestureData> allGestures; // 将所有8个GestureData资源拖到这里

    [Header("视觉效果")]
    [Tooltip("在切换到下一张图片前，图片消失的空白时间（秒）")]
    public float flashDuration = 0.1f; // 闪烁持续时间

    
    [Header("UI组件")]
    public Image stimulusImage;     // 用于显示手势图片的UI Image组件。
    public TextMeshProUGUI instructionText;    // 用于在指令阶段显示提示文字的UI Text组件。
    public TextMeshProUGUI resultText;         // 用于在结果阶段显示分数的UI Text组件。
    public GameObject instructionPanel; // 指令阶段的UI面板。
    public GameObject gesturePanel;     // 测试进行中显示手势图片的UI面板。
    public GameObject resultPanel;      // 展示最终结果的UI面板。

    // --- 内部状态变量 ---
    private enum TestState { Instructions, Testing, Results }
    private TestState currentState;
    
    private List<GestureData> currentSequence = new List<GestureData>();
    private List<GestureData> targetGestures = new List<GestureData>();
    private int currentStimulusIndex = -1;
    private GestureData currentStimulus => currentSequence[currentStimulusIndex];

    private float stimulusInterval;
    private float testTimer;
    private float reactionTimer;
    private bool responseMadeForCurrentStimulus;

    // --- 计分 ---
    private int hits = 0;
    private int misses = 0;
    private int falseAlarms = 0;
    private List<float> reactionTimes = new List<float>();

    void Start()
    {
        AttentionSetting attentionSetting = AttentionSetting.Instance;
        if (attentionSetting != null)
        {
            gesturesPerMinute = attentionSetting.GesturesPerMinute;
            targetCount = attentionSetting.TargetCount;
            flashDuration = attentionSetting.FlashDuration;
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
        // 订阅手势输入事件
        GestureInputController.OnGesturePerformed += HandleGestureInput;
    }

    void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        GestureInputController.OnGesturePerformed -= HandleGestureInput;
    }

    // 阶段1：指令阶段
    void StartInstructionPhase()
    {
        currentState = TestState.Instructions;
        instructionPanel.SetActive(true);
        gesturePanel.SetActive(false);
        resultPanel.SetActive(false);
        
        GenerateSequences();

        // TODO: 在UI上优雅地展示 targetGestures
        string targets = "以下手势图片出现时模仿手势: \n\n";
        foreach(var target in targetGestures)
        {
            targets += $"{target.gestureName}\n";
        }
        instructionText.text = targets;

        // 示例：5秒后自动开始测试
        Invoke(nameof(StartTestPhase), 5f);
    }
    
    // 阶段2：测试阶段
    void StartTestPhase()
    {
        currentState = TestState.Testing;
        gesturePanel.SetActive(true);

        hits = 0;
        misses = 0;
        falseAlarms = 0;
        reactionTimes.Clear();
        testTimer = testDuration;

        StartCoroutine(PresentStimuli());
    }

    // 阶段3：结果阶段
    void ShowResults()
    {
        currentState = TestState.Results;
        instructionPanel.SetActive(false);
        gesturePanel.SetActive(false);
        resultPanel.SetActive(true);

        float avgReactionTime = reactionTimes.Count > 0 ? reactionTimes.Average() : 0;
        resultText.text = $"测试结束!\n\n命中: {hits}\n漏报: {misses}\n虚报: {falseAlarms}\n平均反应时间: {avgReactionTime:F2}秒";
    }

    void Update()
    {
        if (currentState == TestState.Testing)
        {
            testTimer -= Time.deltaTime;
            reactionTimer += Time.deltaTime;
            if (testTimer <= 0)
            {
                StopAllCoroutines();
                ShowResults();
            }
        }
    }
    
    // 核心逻辑：处理用户手势输入
    private void HandleGestureInput(Hand hand, CustomGestureType gestureType)
    {
        if (currentState != TestState.Testing || responseMadeForCurrentStimulus)
        {
            return; // 如果不在测试中，或已对当前刺激做出反应，则忽略
        }

        responseMadeForCurrentStimulus = true; // 标记已反应

        bool isTarget = IsTarget(currentStimulus);
        bool isCorrect = (currentStimulus.hand == hand && currentStimulus.gestureType == gestureType);

        if (isTarget)
        {
            if (isCorrect)
            {
                hits++;
                reactionTimes.Add(reactionTimer);
                Debug.Log($"命中! 反应时间: {reactionTimer}");
            }
            // 如果是目标但做错了，也算作漏报（在切换刺激时处理）
        }
        else // 不是目标
        {
            if (isCorrect) // 即使不是目标，但模仿了，就是虚报
            {
                 falseAlarms++;
                 Debug.Log("虚报!");
            }
        }
    }

    //// <summary>
    /// 这是一个协程，用于按设定的时间间隔，依次呈现手势刺激序列。
    /// 新版本加入了闪烁效果。
    /// </summary>
    private IEnumerator PresentStimuli()
    {
        // --- 闪烁逻辑修改 ---
        // 1. 计算出图片真正需要显示的时间。
        // 总间隔时间 = 图片显示时间 + 闪烁空白时间
        float visibleDuration = stimulusInterval - flashDuration;

        // 2. 安全检查：确保闪烁时间不会比总的间隔时间还长。
        if (visibleDuration <= 0)
        {
            Debug.LogError("错误：闪烁时间 (flashDuration) 必须小于总的刺激间隔时间 (stimulusInterval)！");
            // 在出错时提供一个极短的显示时间以避免死循环。
            visibleDuration = 0.01f; 
        }
        
        // 遍历本次测试生成的完整手势序列。
        for (int i = 0; i < currentSequence.Count; i++)
        {
            // 在呈现一个新的刺激之前，先检查上一个刺激的结果。
            if (currentStimulusIndex >= 0)
            {
                // 如果上一个刺激是目标，并且用户直到现在还没有做出任何反应...
                if (IsTarget(currentStimulus) && !responseMadeForCurrentStimulus)
                {
                    misses++; // ...则记为一次“漏报”。
                    Debug.Log("漏报!");
                }
            }

            // --- 准备并呈现下一个刺激 ---
            currentStimulusIndex = i; // 更新当前刺激的索引。
            stimulusImage.sprite = currentStimulus.gestureImage; // 在UI上更新手势图片。
            responseMadeForCurrentStimulus = false; // 重置反应标记。
            reactionTimer = 0f; // 重置反应时间计时器。

            // --- 闪烁逻辑修改 ---
            // 3. 让图片显示出来
            stimulusImage.enabled = true;

            // 4. 等待“图片显示时间”
            yield return new WaitForSeconds(visibleDuration);

            // 5. 时间到后，隐藏图片，开始“闪烁空白期”
            stimulusImage.enabled = false;
            
            // 6. 等待“闪烁空白时间”
            yield return new WaitForSeconds(flashDuration);
        }
    }
    // --- 辅助函数 ---
    void GenerateSequences()
    {
        // 1. 确定本次测试用左手还是右手
        Hand targetHand = (Random.value > 0.5f) ? Hand.Left : Hand.Right;
        
        // 2. 从该手中随机挑选目标手势
        targetGestures = allGestures
            .Where(g => g.hand == targetHand)
            .OrderBy(x => Random.value)
            .Take(targetCount)
            .ToList();

        // 3. 生成完整的伪随机测试序列
        currentSequence.Clear();
        int totalGesturesInTest = (int)(testDuration / stimulusInterval);
        for (int i = 0; i < totalGesturesInTest; i++)
        {
            currentSequence.Add(allGestures[Random.Range(0, allGestures.Count)]);
        }
    }

    bool IsTarget(GestureData gesture)
    {
        return targetGestures.Contains(gesture);
    }
}