using System.Collections;
using System.Collections.Generic;
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

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
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

        // 增加拖动灵敏度倍率
        targetImage.anchoredPosition += (eventData.delta * moveSensitivity) / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 拖动结束
    }
}
