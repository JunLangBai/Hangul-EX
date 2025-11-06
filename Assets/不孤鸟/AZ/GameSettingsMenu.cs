using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

/// <summary>
/// [新版本] 负责管理游戏设置菜单 (使用独立的按钮平铺选项)
/// </summary>
public class GameSettingsManager : MonoBehaviour
{
    [Header("核心引用 (Core References)")]
    [Tooltip("拖入场景中的 MemoryGameManager 脚本")]
    public MemoryGameManager gameManager;

    [Header("菜单 UI 元素 (Menu UI)")]
    [Tooltip("“开始自定义游戏”按钮")]
    public Button startCustomGameButton;
    
    [Tooltip("“开始常规游戏”按钮")]
    public Button startNormalGameButton;
    
    [Tooltip("设置菜单的父物体 (Panel)")]
    public GameObject settingsMenuPanel;
    
    [Tooltip("主游戏界面的父物体 (Panel)")]
    public GameObject gamePanel;
    
    [Header("自定义设置 - 模式 (Custom Mode)")]
    public Button modeOrderButton; // "顺序" 按钮
    public Button modeReverseButton; // "逆序" 按钮

    [Header("自定义设置 - 难度 (Custom Difficulty)")]
    public Button difficulty2Button; // "2个数字" 按钮
    public Button difficulty3Button; // "3个数字" 按钮
    public Button difficulty4Button; // "4个数字" 按钮
    public Button difficulty5Button; // "5个数字" 按钮

    [Header("视觉反馈 (Visuals)")]
    [Tooltip("按钮被选中时高亮的颜色")]
    public Color selectedColor = Color.green;
    [Tooltip("按钮未选中时的默认颜色")]
    public Color defaultColor = Color.white;
    
    // 用 List 来管理按钮组，方便切换颜色
    private List<Button> modeButtons;
    private List<Button> difficultyButtons;

    // --- 内部设置状态 ---
    private bool currentIsOrder = true; // 默认"顺序"
    private int currentDifficulty = 2; // 默认"2个数字"


    void Start()
    {
        // 1. 检查引用
        if (gameManager == null)
        {
            Debug.LogError("GameSettingsManager: MemoryGameManager 未指定！");
            return;
        }

        // 2. 绑定 "开始游戏" 按钮
        if (startCustomGameButton != null)
        {
            startCustomGameButton.onClick.AddListener(OnStartCustomGameClicked);
        }
        
        if (startNormalGameButton != null)
        {
            startNormalGameButton.onClick.AddListener(OnStartNormalGameClicked);
        }

        // 3. [新] 绑定所有设置按钮
        BindSettingsButtons();

        // 4. 初始化 UI 状态
        if (settingsMenuPanel != null) settingsMenuPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
        
        // 5. [新] 初始化默认选项和视觉效果
        UpdateVisuals();
    }

    /// <summary>
    /// [新] 绑定所有设置按钮的点击事件
    /// </summary>
    void BindSettingsButtons()
    {
        // --- 模式按钮 ---
        modeButtons = new List<Button> { modeOrderButton, modeReverseButton };
        
        modeOrderButton.onClick.AddListener(() => {
            currentIsOrder = true;
            UpdateVisuals();
        });
        
        modeReverseButton.onClick.AddListener(() => {
            currentIsOrder = false;
            UpdateVisuals();
        });

        // --- 难度按钮 ---
        difficultyButtons = new List<Button> { difficulty2Button, difficulty3Button, difficulty4Button, difficulty5Button };

        difficulty2Button.onClick.AddListener(() => {
            currentDifficulty = 2;
            UpdateVisuals();
        });
        
        difficulty3Button.onClick.AddListener(() => {
            currentDifficulty = 3;
            UpdateVisuals();
        });

        difficulty4Button.onClick.AddListener(() => {
            currentDifficulty = 4;
            UpdateVisuals();
        });

        difficulty5Button.onClick.AddListener(() => {
            currentDifficulty = 5;
            UpdateVisuals();
        });
    }

    /// <summary>
    /// [新] 根据当前选择，更新所有按钮的颜色
    /// </summary>
    void UpdateVisuals()
    {
        // 更新模式按钮颜色
        modeOrderButton.GetComponent<Image>().color = currentIsOrder ? selectedColor : defaultColor;
        modeReverseButton.GetComponent<Image>().color = !currentIsOrder ? selectedColor : defaultColor;
        
        // 更新难度按钮颜色
        difficulty2Button.GetComponent<Image>().color = (currentDifficulty == 2) ? selectedColor : defaultColor;
        difficulty3Button.GetComponent<Image>().color = (currentDifficulty == 3) ? selectedColor : defaultColor;
        difficulty4Button.GetComponent<Image>().color = (currentDifficulty == 4) ? selectedColor : defaultColor;
        difficulty5Button.GetComponent<Image>().color = (currentDifficulty == 5) ? selectedColor : defaultColor;
    }


    /// <summary>
    /// 当点击“开始自定义游戏”按钮时调用
    /// </summary>
    void OnStartCustomGameClicked()
    {
        // 1. 从内部变量读取设置
        Debug.Log($"[SettingsManager] 启动自定义游戏。难度: {currentDifficulty}, 模式: {(currentIsOrder ? "顺序" : "逆序")}");

        // 2. 隐藏菜单, 显示游戏
        ShowGamePanel();
        
        // 3. 调用 MemoryGameManager 的方法
        gameManager.StartCustomGame(currentDifficulty, currentIsOrder);
    }

    /// <summary>
    /// 当点击“开始常规游戏”按钮时调用
    /// </summary>
    void OnStartNormalGameClicked()
    {
        Debug.Log("[SettingsManager] 启动常规游戏。");
        ShowGamePanel();
        
        // 调用常规游戏启动（保留3次答对换模式的逻辑）
        gameManager.StartGame(); 
    }

    /// <summary>
    /// 切换UI面板
    /// </summary>
    void ShowGamePanel()
    {
        if (settingsMenuPanel != null) settingsMenuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
    }
}