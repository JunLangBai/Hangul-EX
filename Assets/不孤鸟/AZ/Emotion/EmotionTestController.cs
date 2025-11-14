using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmotionTestController : MonoBehaviour
{
    [Header("测试设置")]
    public int totalQuestions = 10; // 外部定义的题目数量
    public float displayTime = 3.0f; // 外部定义的情绪显示时间

    [Tooltip("用户回答后，到下一题开始前的等待时间")]
    public float delayBetweenQuestions = 1.5f;
    
    [Header("模型与动画")]
    public GameObject characterModel; // 拖入您的模型对象
    public Animator modelAnimator;

    [Header("UI 元素")]
    public Button[] emotionButtons; // 拖入四个情绪按钮
    
    public GameObject startPanel;
    public GameObject gamePanel;
    public GameObject overPanel;
    
    public TextMeshProUGUI resultText;
    
    private AllSettingCtr allSettingCtr;
    
    private int correct;
    private int incorrect;
    
    private int currentQuestionIndex = 0;
    private string currentEmotion;
    private List<string> emotions = new List<string> { "smiling", "sad", "angry", "Fear" }; // 与您的动画状态名称对应

    void Start()
    {
        allSettingCtr = AllSettingCtr.Instance;
        if (allSettingCtr != null)
        {
            totalQuestions = allSettingCtr.emotionCount;
            displayTime = allSettingCtr.emotionDisplayTime;
        }
        else
        {
            totalQuestions = 20; 
            displayTime = 3.0f;
        }

        startPanel.SetActive(true);
        gamePanel.SetActive(false);
        overPanel.SetActive(false);
        characterModel.SetActive(false);
    }

    public void StartGame()
    {
        
        startPanel.SetActive(false);
        gamePanel.SetActive(true);
        overPanel.SetActive(false);
        characterModel.SetActive(true);
        
        // 禁用按钮，防止重复作答
        SetButtonsInteractable(false);
        // 【修改点】
        StartCoroutine(NextQuestionRoutine());
    }

    // 开始新一轮题目
    void StartNewQuestion()
    {
        if (currentQuestionIndex < totalQuestions)
        {
            currentQuestionIndex++;
            StartCoroutine(ShowEmotion());
        }
        else
        {
            Debug.Log("测试完成!");
            
            characterModel.SetActive(false);
            startPanel.SetActive(false);
            gamePanel.SetActive(false);
            overPanel.SetActive(true);

            resultText.text = $"测试结束!请点击返回按钮\n\n正确: {correct}\n错误: {incorrect}";
            // 在这里可以添加测试结束的逻辑，例如显示最终得分
        }
    }

    // 显示情绪的协程
    IEnumerator ShowEmotion()
    {
        // 随机选择一个情绪
        currentEmotion = emotions[Random.Range(0, emotions.Count)];

        // 激活模型并播放动画
        characterModel.SetActive(true);
        modelAnimator.Play(currentEmotion);

        // 等待指定时间
        yield return new WaitForSeconds(displayTime);

        modelAnimator.SetTrigger("DoIdle");
        
        yield return new WaitForSeconds(0.5f);
        // // 隐藏模型 
        // characterModel.SetActive(false);
        
        // 启用按钮，让用户作答
        SetButtonsInteractable(true);
    }

    // 用户选择答案
    public void OnEmotionSelected(string selectedEmotion)
    {
        if (selectedEmotion == currentEmotion)
        {
            Debug.Log("回答正确!");
            correct++;
        }
        else
        {
            Debug.Log("回答错误!");
            incorrect++;
        }

        SetButtonsInteractable(false);

        // 【修改点】
        StartCoroutine(NextQuestionRoutine());
    }
    
    IEnumerator NextQuestionRoutine()
    {
        yield return new WaitForSeconds(delayBetweenQuestions);
        StartNewQuestion();
    }

    // 设置按钮的可交互状态
    void SetButtonsInteractable(bool isInteractable)
    {
        foreach (Button button in emotionButtons)
        {
            button.interactable = isInteractable;
        }
    }
}