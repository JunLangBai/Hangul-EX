using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // 确保这个物体上一定有Image组件，可以接收UI事件
public class CanvasDragHandle : MonoBehaviour
{
    
    [Header("UI 组件（Inspector 绑定）")]
    public CanvasGroup panelCanvasGroup;
    public Button toggleButton; // 请确保这个按钮不是 panel 的子对象（或至少在外面可以点击）

    [Header("动画设置")]
    public float duration = 0.35f;
    public Ease easeType = Ease.OutCubic;

    [Tooltip("如果你希望隐藏时彻底 SetActive(false)，开启此项（注意：按钮不能在面板内）。")]
    public bool useSetActiveWhenHidden = false;

    private bool isShown = false;

    private void Awake()
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogError("[DragHandle] panelCanvasGroup 未绑定！(Inspector)");
            enabled = false;
            return;
        }

        if (toggleButton == null)
        {
            Debug.LogWarning("[DragHandle] toggleButton 未绑定，请在 Inspector 绑定。");
        }
        else
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }

        // 初始隐藏（不把 panel 彻底 SetActive(false)，以免按钮也被禁用）
        panelCanvasGroup.gameObject.SetActive(true);
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        isShown = false;

        Debug.Log($"[DragHandle] Awake: panelActive={panelCanvasGroup.gameObject.activeSelf}, alpha={panelCanvasGroup.alpha}, isShown={isShown}");
    }

    private void TogglePanel()
    {
        Debug.Log($"[DragHandle] TogglePanel called. isShown={isShown}, panelActive={panelCanvasGroup.gameObject.activeSelf}, alpha={panelCanvasGroup.alpha}");
        if (isShown) HidePanel();
        else ShowPanel();

        // 保持原来的翻转逻辑（也可以改成在动画完成时设置）
        isShown = !isShown;
    }

    private void ShowPanel()
    {
        panelCanvasGroup.DOKill();
        if (!panelCanvasGroup.gameObject.activeSelf) panelCanvasGroup.gameObject.SetActive(true);
        // 确保从 0 开始动画
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.DOFade(1f, duration)
            .SetEase(easeType)
            .OnStart(() =>
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
                Debug.Log("[DragHandle] ShowPanel OnStart");
            })
            .OnComplete(() => Debug.Log("[DragHandle] ShowPanel OnComplete"));
    }

    private void HidePanel()
    {
        panelCanvasGroup.DOKill();
        panelCanvasGroup.DOFade(0f, duration)
            .SetEase(easeType)
            .OnStart(() => Debug.Log("[DragHandle] HidePanel OnStart"))
            .OnComplete(() =>
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
                if (useSetActiveWhenHidden)
                {
                    panelCanvasGroup.gameObject.SetActive(false);
                    Debug.Log("[DragHandle] HidePanel SetActive(false)");
                }
                Debug.Log("[DragHandle] HidePanel OnComplete");
            });
    }
    
}