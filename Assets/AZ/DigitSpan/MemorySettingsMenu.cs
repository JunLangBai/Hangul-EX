using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemorySettingsMenu : MonoBehaviour
{
    // --- 单例引用 ---
    private AllSettingCtr allSettingCtr;

    [Header("UI 元素 (UI Elements)")]
    public TextMeshProUGUI settingsDisplay;

    // --- [模式] 设置按钮 ---
    [Header("模式设置按钮 (Mode Buttons)")]
    public ToggleGroup toggleGroup;
    [Tooltip("确保顺序是：0=自动, 1=顺序, 2=逆序")]
    public List<Toggle> toggles;

    // --- [难度] 设置滑动条 ---
    [Header("难度设置滑动条 (Difficulty Slider)")]
    [Tooltip("选择难度的滑动条 (0=随机, 1=2个, ..., 4=5个)")]
    public Slider difficultySlider;

    [Header("视觉反馈 (Visuals for Buttons)")]
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;

    private const string SETTING_PREFIX = "当前记忆设置: ";

    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
       
        // 确保所有 Toggle 都被正确分配到同一个 ToggleGroup，从而实现自动单选
        if (toggleGroup != null)
        {
            foreach (Toggle toggle in toggles)
            {
                toggle.group = toggleGroup;
            }
        }
    }

    void Start()
    {
        if (allSettingCtr != null)
        {
            // 1. 同步单例数据到 Slider (难度)
            if (difficultySlider != null)
            {
                // 反向映射：难度(0, 2, 3, 4, 5) -> 滑动条(0, 1, 2, 3, 4)
                if (allSettingCtr.memoryDifficulty == 0)
                    difficultySlider.value = 0;
                else
                    difficultySlider.value = allSettingCtr.memoryDifficulty - 1;

                // 绑定滑动条事件
                difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
            }

            // 2. 同步单例数据到 Toggle (模式)
            int savedMode = allSettingCtr.memoryMode;
            if (savedMode >= 0 && savedMode < toggles.Count)
            {
                // 这会自动触发对应 Toggle 的 OnValueChanged 
                toggles[savedMode].isOn = true;
            }

            // 3. 动态绑定 Toggle 事件 (无需在 Inspector 面板里手动连线)
            for (int i = 0; i < toggles.Count; i++)
            {
                int modeIndex = i; // 必须使用局部变量传递给闭包
                toggles[i].onValueChanged.AddListener((isOn) => 
                {
                    if (isOn) 
                    {
                        UpdateMode(modeIndex);
                    }
                });
            }

            // 4. 初始化文字显示
            UpdateSettingsDisplay();
        }
    }

    /// <summary>
    /// 当难度滑动条的值发生变化时调用
    /// </summary>
    public void OnDifficultyChanged(float value)
    {
        int sliderValue = (int)value;
        
        // 正向映射：滑动条(0, 1, 2, 3, 4) -> 难度(0, 2, 3, 4, 5)
        if (sliderValue == 0)
            allSettingCtr.memoryDifficulty = 0; // "随机"
        else
            allSettingCtr.memoryDifficulty = sliderValue + 1; 

        UpdateSettingsDisplay();
    }
    
    /// <summary>
    /// 当 Toggle 模式改变时调用
    /// </summary>
    public void UpdateMode(int modeIndex)
    {
        allSettingCtr.memoryMode = modeIndex;
        UpdateSettingsDisplay();
    }

    /// <summary>
    /// 更新底部 TextMeshPro 的文字显示 (可选功能，如果需要的话)
    /// </summary>
    private void UpdateSettingsDisplay()
    {
        if (settingsDisplay != null)
        {
            string modeStr = allSettingCtr.memoryMode == 0 ? "自动" : (allSettingCtr.memoryMode == 1 ? "顺序" : "逆序");
            string diffStr = allSettingCtr.memoryDifficulty == 0 ? "随机" : $"{allSettingCtr.memoryDifficulty}个";
            
            settingsDisplay.text = $"{SETTING_PREFIX}\n模式: {modeStr} | 难度: {diffStr}";
        }
    }
}