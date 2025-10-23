using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // 引入 DOTween 命名空间

public class TutorialUIManager : MonoBehaviour
{
    [Tooltip("场景中所有需要切换的图片的 Canvas Group 组件")]
    public CanvasGroup[] imageGroups;

    [Tooltip("渐变持续时间")]
    public float fadeDuration = 0.5f;

    [Tooltip("下一张按钮 (可选)")]
    public Button nextButton;
    
    [Tooltip("上一张按钮 (可选)")]
    public Button prevButton;

    private int currentIndex = 0; // 当前显示的图片索引
    private bool isFading = false; // 渐变锁，防止重复点击

    void Start()
    {
        // 确保数组中有图片
        if (imageGroups == null || imageGroups.Length == 0)
        {
            Debug.LogError("没有图片Canvas Group被分配到 UIManager 脚本!");
            return;
        }

        // 绑定按钮点击事件
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => ChangeImage(1));
        }
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(() => ChangeImage(-1));
        }

        // 初始化：确保第一张图片显示，其余隐藏
        for (int i = 0; i < imageGroups.Length; i++)
        {
            imageGroups[i].alpha = (i == currentIndex) ? 1f : 0f;
            // 确保只有当前图片可以被点击交互（如果图片上有交互组件）
            imageGroups[i].blocksRaycasts = (i == currentIndex);
        }
    }

    /// <summary>
    /// 切换图片的主逻辑
    /// </summary>
    /// <param name="direction">1为下一张，-1为上一张</param>
    public void ChangeImage(int direction)
    {
        if (isFading || imageGroups.Length < 2)
        {
            return; // 正在渐变或图片数量不足
        }

        isFading = true;

        // 当前图片
        CanvasGroup currentGroup = imageGroups[currentIndex];

        // 计算下一张图片的新索引 (使用取模运算实现循环)
        int newIndex = currentIndex + direction;
        if (newIndex >= imageGroups.Length)
        {
            newIndex = 0; // 循环到第一张 (下一张)
        }
        else if (newIndex < 0)
        {
            newIndex = imageGroups.Length - 1; // 循环到最后一张 (上一张)
        }

        // 下一张图片
        CanvasGroup nextGroup = imageGroups[newIndex];

        // 1. 渐隐当前图片 (Alpha: 1 -> 0)
        currentGroup.DOFade(0f, fadeDuration)
                    .SetEase(Ease.OutCubic); // 使用一个缓动效果，让渐变更自然
        
        // 2. 渐显下一张图片 (Alpha: 0 -> 1)
        // 启用下一张图片的交互，并在渐变完成后执行回调
        nextGroup.blocksRaycasts = true;
        nextGroup.DOFade(1f, fadeDuration)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(() =>
                 {
                     // 渐变完成后，禁用旧图片的交互
                     currentGroup.blocksRaycasts = false;
                     
                     // 更新当前索引，解除渐变锁
                     currentIndex = newIndex;
                     isFading = false;
                 });
    }
}