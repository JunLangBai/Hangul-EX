using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 【单例版本】
/// 负责存储跨场景的游戏设置。
/// (Singleton version: Stores game settings across scenes.)
/// </summary>
public class GameSettingsMenu : MonoBehaviour
{
    // --- [新] 单例模式 ---
    public static GameSettingsMenu Instance { get; private set; }
    // --- [新结束] ---

    [Header("UI 元素 (UI Elements)")]
    [Tooltip("用于选择难度的 Dropdown (Difficulty Dropdown)")]
    public TMP_Dropdown difficultyDropdown; // (0=随机, 1=2个, 2=3个, 3=4个, 4=5个)
    
    [Tooltip("用于选择模式的 Dropdown (Mode Dropdown)")]
    public TMP_Dropdown modeDropdown; // (0=自动, 1=顺序, 2=逆序)

    [Tooltip("用于应用设置的按钮 (Apply Settings Button)")]
    public Button applyButton;

    [Tooltip("用于显示当前设置的文本 (Display for current settings)")]
    public TextMeshProUGUI settingsDisplay;

    // 内部存储 (Internal storage)
    private int selectedDifficulty = 0; // 0 = 随机 (Random)
    private int selectedMode = 0; // 0 = 自动 (Auto)

    private const string SETTING_PREFIX = "手动设置: ";

    // --- [新] Awake 用于单例和跨场景 ---
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
    // --- [新结束] ---

    void Start()
    {
        // 1. 初始化 Dropdowns (Initialize Dropdowns)
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            // 确保我们读取的是当前值（在场景加载时）
            OnDifficultyChanged(difficultyDropdown.value); 
        }

        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
            // 确保我们读取的是当前值
            OnModeChanged(modeDropdown.value);
        }
        
        // 2. 绑定应用按钮 (Bind Apply Button)
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(UpdateDisplay);
        }

        // 3. 初始化显示 (Initialize display)
        UpdateDisplay();
        
        // 4. [已移除] 计数器相关代码
    }

    // --- Dropdown 事件监听 ---

    void OnDifficultyChanged(int index)
    {
        if (index == 0)
        {
            selectedDifficulty = 0; // 0 代表随机
        }
        else
        {
            selectedDifficulty = index + 1; // 1->2, 2->3, 3->4, 4->5
        }
        UpdateDisplay();
    }

    void OnModeChanged(int index)
    {
        selectedMode = index;
        UpdateDisplay();
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

    // --- 【核心】供 GameManager 调用的公共方法 ---

    /// <summary>
    /// MemoryGameManager 调用此方法来获取手动设置的难度。
    /// </summary>
    public int GetManualDifficulty()
    {
        return selectedDifficulty;
    }

    /// <summary>
    /// MemoryGameManager 调用此方法来获取手动设置的模式。
    /// </summary>
    public int GetManualMode()
    {
        return selectedMode;
    }
    
    // --- [已移除] 计数器相关方法 ---
}