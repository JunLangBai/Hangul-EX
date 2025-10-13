using UnityEngine;
using UnityEngine.EventSystems;

public class DragBorldHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("要移动的目标图片")]
    public RectTransform targetImage;

    [Header("拖动灵敏度（越大越快）")]
    [Range(0.1f, 10f)]
    public float moveSensitivity = 2f;  // 拖动倍率，默认2倍

    private Canvas canvas;
    private RectTransform canvasRectTransform; // 用于缓存Canvas的RectTransform

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // 获取并缓存Canvas的RectTransform
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
        
        if (targetImage == null)
        {
            targetImage = transform.parent.GetComponent<RectTransform>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 拖动开始（可添加音效或视觉反馈）
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetImage == null || canvas == null) return;

        // 1. 计算理论上的新位置
        // 首先，像原来一样根据鼠标（或触摸）的移动来计算图片的新位置
        targetImage.anchoredPosition += (eventData.delta * moveSensitivity) / canvas.scaleFactor;

        // 2. 计算边界
        // 为了防止图片移出Canvas，我们需要计算出它的中心点（pivot）可以移动的x和y的最小和最大值
        
        // Canvas的半宽和半高
        float canvasHalfWidth = canvasRectTransform.rect.width / 2f;
        float canvasHalfHeight = canvasRectTransform.rect.height / 2f;

        // 目标的半宽和半高（这里要考虑图片的轴心点 pivot）
        float targetHalfWidth = targetImage.rect.width * (1f - targetImage.pivot.x);
        float targetHalfWidth_rev = targetImage.rect.width * targetImage.pivot.x;

        float targetHalfHeight = targetImage.rect.height * (1f - targetImage.pivot.y);
        float targetHalfHeight_rev = targetImage.rect.height * targetImage.pivot.y;

        // 根据Canvas的尺寸和目标图片的尺寸，计算出目标图片中心点可以移动的范围
        float minX = -canvasHalfWidth + targetHalfWidth_rev;
        float maxX = canvasHalfWidth - targetHalfWidth;
        float minY = -canvasHalfHeight + targetHalfHeight_rev;
        float maxY = canvasHalfHeight - targetHalfHeight;

        // 3. 将位置限制在边界内
        // 获取当前的位置
        Vector2 clampedPosition = targetImage.anchoredPosition;
        // 使用Mathf.Clamp函数将x和y坐标分别限制在计算出的最小和最大值之间
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);

        // 4. 更新到限制后的安全位置
        targetImage.anchoredPosition = clampedPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 拖动结束
    }
}