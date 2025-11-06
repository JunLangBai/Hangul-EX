using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 【无需修改】
/// 一个可选的设置菜单，允许康复师手动覆盖下一轮的难度和模式。
/// (An optional settings menu that allows a therapist to manually override the next round's difficulty and mode.)
/// </summary>
public class MemoryManager : MonoBehaviour
{
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

    void Start()
    {
        // 1. 初始化 Dropdowns (Initialize Dropdowns)
        // (确保在 Unity 编辑器中已设置好选项)
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            selectedDifficulty = difficultyDropdown.value;
        }

        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
            selectedMode = modeDropdown.value;
        }
        
        // 2. 绑定应用按钮 (Bind Apply Button)
        if (applyButton != null)
        {
            // 在这个演示中，我们假设 "Apply" 只是为了更新显示文本
            // 真正的 "Apply" 逻辑在 MemoryGameManager 中通过 Get...() 方法实现
            applyButton.onClick.AddListener(UpdateDisplay);
        }

        // 3. 初始化显示 (Initialize display)
        UpdateDisplay();
    }

    // --- Dropdown 事件监听 ---

    void OnDifficultyChanged(int index)
    {
        // Dropdown index (0="随机", 1="2个", 2="3个", 3="4个", 4="5个")
        // 我们将其转换为数字 (0=随机, 2, 3, 4, 5)
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
        // Dropdown index (0="自动", 1="顺序", 2="逆序")
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
    /// <returns>0=随机, 2-5=指定数量</returns>
    public int GetManualDifficulty()
    {
        return selectedDifficulty;
    }

    /// <summary>
    /// MemoryGameManager 调用此方法来获取手动设置的模式。
    /// </summary>
    /// <returns>0=自动, 1=顺序 (Order), 2=逆序 (Reverse)</returns>
    public int GetManualMode()
    {
        return selectedMode;
    }
}