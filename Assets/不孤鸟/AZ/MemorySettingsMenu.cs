using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 【单例版本 - 混合模式】
/// 负责存储跨场景的游戏设置。
/// (Singleton version using a mix of Buttons and a Slider: Stores game settings across scenes.)
/// </summary>
public class MemorySettingsMenu : MonoBehaviour
{
    // --- 单例模式 ---
    public static MemorySettingsMenu Instance { get; private set; }
    
    [Header("UI 元素 (UI Elements)")]
    [Tooltip("用于显示当前设置的文本 (Display for current settings)")]
    public TextMeshProUGUI settingsDisplay;

    // --- [模式] 设置按钮 ---
    [Header("模式设置按钮 (Mode Buttons)")]
    // 在 Inspector 窗口中将你的 Toggle Group 拖拽到这里
    public ToggleGroup toggleGroup;

    // 在 Inspector 窗口中将你的所有 Toggle 拖拽到这里
    public List<Toggle> toggles;

    // --- [难度] 设置滑动条 ---
    [Header("难度设置滑动条 (Difficulty Slider)")]
    [Tooltip("选择难度的滑动条 (0=随机, 1=2个, ..., 4=5个)")]
    public Slider difficultySlider;

    [Header("视觉反馈 (Visuals for Buttons)")]
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 遍历所有 Toggle，并将它们分配给同一个 ToggleGroup
        foreach (Toggle toggle in toggles)
        {
            toggle.group = toggleGroup;
        }

        // 示例：通过代码设置默认选中的 Toggle (例如第一个)
        if (toggles.Count > 0)
        {
            toggles[0].isOn = true;
        }
    }

    void Start()
    {
        
        // 1. 绑定UI事件
        BindDifficultySlider();

        // 2. 根据UI初始值，初始化显示
        OnDifficultyChanged(difficultySlider.value); // 同步滑动条的初始位置
    }

    

    /// <summary>
    /// 绑定难度滑动条的值变化事件
    /// </summary>
    void BindDifficultySlider()
    {
        if (difficultySlider != null)
        {
            difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }

    /// <summary>
    /// 当难度滑动条的值发生变化时调用
    /// </summary>
    void OnDifficultyChanged(float value)
    {
        int sliderValue = (int)value;
        
        // 将滑动条的值 (0, 1, 2, 3, 4) 映射到实际的难度值 (0, 2, 3, 4, 5)
        if (sliderValue == 0)
        {
            selectedDifficulty = 0; // "随机"
        }
        else
        {
            selectedDifficulty = sliderValue + 1; // 1->2, 2->3, ...
        }
        
    }
    
    /// <summary>
    /// 更新设置显示文本
    /// </summary>
    public void UpdateMode(float value)
    {
        if (value <= 2 && value >= 0)
        {
            selectedMode = (int)value;
        }
        else
        {
            selectedMode = 0;
        }
    }

    private void OnToggleChanged(Toggle changedToggle, bool isOn)
    {
        if (isOn)
        {
            // 取消其他所有Toggle的选中状态
            foreach (Toggle toggle in toggles)
            {
                if (toggle != changedToggle && toggle.isOn)
                {
                    toggle.isOn = false;
                }
            }
        }
    }
    
    // --- 【核心】供 GameManager 调用的公共方法 (无需修改) ---

    public int GetManualDifficulty()
    {
        return selectedDifficulty;
    }

    public int GetManualMode()
    {
        return selectedMode;
    }
}