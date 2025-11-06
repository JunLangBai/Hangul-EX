using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 【单例版本 - 使用按钮】
/// 负责存储跨场景的游戏设置。
/// (Singleton version using Buttons: Stores game settings across scenes.)
/// </summary>
public class GameSettingsMenu : MonoBehaviour
{
    // --- 单例模式 ---
    public static GameSettingsMenu Instance { get; private set; }
    
    [Header("UI 元素 (UI Elements)")]
    [Tooltip("用于显示当前设置的文本 (Display for current settings)")]
    public TextMeshProUGUI settingsDisplay;

    // --- [新] 模式设置按钮 ---
    [Header("模式设置按钮 (Mode Buttons)")]
    [Tooltip("选择“自动”模式的按钮")]
    public Button modeAutoButton;
    [Tooltip("选择“顺序”模式的按钮")]
    public Button modeOrderButton;
    [Tooltip("选择“逆序”模式的按钮")]
    public Button modeReverseButton;

    // --- [新] 难度设置按钮 ---
    [Header("难度设置按钮 (Difficulty Buttons)")]
    [Tooltip("选择“随机”难度的按钮")]
    public Button difficultyRandomButton;
    [Tooltip("选择“2个数字”的按钮")]
    public Button difficulty2Button;
    [Tooltip("选择“3个数字”的按钮")]
    public Button difficulty3Button;
    [Tooltip("选择“4个数字”的按钮")]
    public Button difficulty4Button;
    [Tooltip("选择“5个数字”的按钮")]
    public Button difficulty5Button;

    [Header("视觉反馈 (Visuals)")]
    [Tooltip("按钮被选中时高亮的颜色")]
    public Color selectedColor = Color.green;
    [Tooltip("按钮未选中时的默认颜色")]
    public Color defaultColor = Color.white;

    // 内部存储 (Internal storage)
    private int selectedDifficulty = 0; // 0=随机, 2=2个, 3=3个...
    private int selectedMode = 0; // 0=自动, 1=顺序, 2=逆序

    private const string SETTING_PREFIX = "手动设置: ";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 核心：使其跨场景
            Debug.Log("GameSettingsMenu Singleton created.");
        }
        else
        {
            Debug.LogWarning("Duplicate GameSettingsMenu found. Destroying new one.");
            Destroy(gameObject); // 销毁重复的实例
        }
    }

    void Start()
    {
        // 1. [新] 绑定所有设置按钮
        BindSettingsButtons();

        // 2. [已移除] Dropdown 相关代码

        // 3. 初始化显示 (Initialize display)
        UpdateDisplay();
        UpdateVisuals(); // [新] 更新按钮颜色
    }

    /// <summary>
    /// [新] 绑定所有按钮的点击事件
    /// </summary>
    void BindSettingsButtons()
    {
        // --- 模式按钮 ---
        modeAutoButton.onClick.AddListener(() => {
            selectedMode = 0;
            UpdateAllUI();
        });
        modeOrderButton.onClick.AddListener(() => {
            selectedMode = 1;
            UpdateAllUI();
        });
        modeReverseButton.onClick.AddListener(() => {
            selectedMode = 2;
            UpdateAllUI();
        });

        // --- 难度按钮 ---
        difficultyRandomButton.onClick.AddListener(() => {
            selectedDifficulty = 0;
            UpdateAllUI();
        });
        difficulty2Button.onClick.AddListener(() => {
            selectedDifficulty = 2;
            UpdateAllUI();
        });
        difficulty3Button.onClick.AddListener(() => {
            selectedDifficulty = 3;
            UpdateAllUI();
        });
        difficulty4Button.onClick.AddListener(() => {
            selectedDifficulty = 4;
            UpdateAllUI();
        });
        difficulty5Button.onClick.AddListener(() => {
            selectedDifficulty = 5;
            UpdateAllUI();
        });
    }

    /// <summary>
    /// [新] 一个辅助方法，用于同时更新文本和按钮颜色
    /// </summary>
    void UpdateAllUI()
    {
        UpdateDisplay();
        UpdateVisuals();
    }

    /// <summary>
    /// [新] 根据当前选择，更新所有按钮的颜色
    /// </summary>
    void UpdateVisuals()
    {
        // 更新模式按钮颜色
        modeAutoButton.GetComponent<Image>().color = (selectedMode == 0) ? selectedColor : defaultColor;
        modeOrderButton.GetComponent<Image>().color = (selectedMode == 1) ? selectedColor : defaultColor;
        modeReverseButton.GetComponent<Image>().color = (selectedMode == 2) ? selectedColor : defaultColor;
        
        // 更新难度按钮颜色
        difficultyRandomButton.GetComponent<Image>().color = (selectedDifficulty == 0) ? selectedColor : defaultColor;
        difficulty2Button.GetComponent<Image>().color = (selectedDifficulty == 2) ? selectedColor : defaultColor;
        difficulty3Button.GetComponent<Image>().color = (selectedDifficulty == 3) ? selectedColor : defaultColor;
        difficulty4Button.GetComponent<Image>().color = (selectedDifficulty == 4) ? selectedColor : defaultColor;
        difficulty5Button.GetComponent<Image>().color = (selectedDifficulty == 5) ? selectedColor : defaultColor;
    }


    /// <summary>
    /// 更新设置显示文本
    /// </summary>
    void UpdateDisplay()
    {
        if (settingsDisplay == null) return;

        string diffText;
        if (selectedDifficulty == 0)
            diffText = "随机难度";
        else
            diffText = $"{selectedDifficulty}个数字";

        string modeText;
        if (selectedMode == 0)
            modeText = "自动模式";
        else if (selectedMode == 1)
            modeText = "固定顺序";
        else
            modeText = "固定逆序";

        settingsDisplay.text = $"{SETTING_PREFIX}{diffText}, {modeText}";
    }

    // --- 【核心】供 GameManager 调用的公共方法 (无需修改) ---

    /// <summary>
    /// MemoryGameManager 调用此方法来获取手动设置的难度。
    /// </summary>
    public int GetManualDifficulty()
    {
        // 验证一下，防止难度按钮把 index=1 (代表"2个数字") 存成 1
        // (在 BindSettingsButtons 中，我们已正确存储 0, 2, 3, 4, 5，所以这里是OK的)
        return selectedDifficulty;
    }

    /// <summary>
    /// MemoryGameManager 调用此方法来获取手动设置的模式。
    /// </summary>
    public int GetManualMode()
    {
        return selectedMode;
    }
}