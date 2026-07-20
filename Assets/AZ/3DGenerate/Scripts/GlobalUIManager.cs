using UnityEngine;
using System.Collections.Generic;
using TripoForUnity;
using UnityEngine.UI;

/// <summary>
/// 【新文件】
/// 这是一个“UI总管”（单例），负责集中管理所有UI面板和3D物体的显隐。
/// 任何其他脚本都可以通过 'GlobalUIManager.Instance.HideAll()' 来调用它。
/// </summary>
public class GlobalUIManager : MonoBehaviour
{
    // 1. 设置一个静态的“实例”，让其他脚本可以访问
    public static GlobalUIManager Instance { get; private set; }

    public TripoRuntimeCore TripoRuntimeCore;
    public Slider tripoSlider;

    [Header("要管理的UI (2D)")]
    [Tooltip("拖入所有UI面板 (每个都必须有CanvasGroup)")]
    public List<CanvasGroup> managedUICanvasGroups;

    [Header("要管理的物体 (3D)")]
    [Tooltip("拖入所有3D物体 (例如你的'画笔'模型)")]
    public List<GameObject> managed3DObjects;

    /// <summary>
    /// 脚本唤醒时，注册自己为“实例”
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // (可选) 如果你需要在切换场景时保留它，取消注释下一行
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // 如果场景中已存在一个实例，则销毁这个重复的
            Debug.LogWarning("场景中发现重复的 GlobalUIManager，已销毁。");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 隐藏所有在列表中的UI和3D物体
    /// </summary>
    public void HideAllManagedItems()
    {
        // 1. 隐藏 2D UI (CanvasGroup)
        if (managedUICanvasGroups != null && managedUICanvasGroups.Count > 0)
        {
            foreach (CanvasGroup cg in managedUICanvasGroups) 
            {
                if (cg != null)
                {
                    cg.alpha = 0;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }
        }
        
        // 2. 隐藏 3D 物体 (GameObject)
        if (managed3DObjects != null && managed3DObjects.Count > 0)
        {
            foreach (GameObject obj in managed3DObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
        Debug.Log("GlobalUIManager: 已隐藏所有UI和3D物体。");
    }

    /// <summary>
    /// 显示所有在列表中的UI和3D物体
    /// </summary>
    public void ShowAllManagedItems()
    {
        // 1. 显示 2D UI (CanvasGroup)
        if (managedUICanvasGroups != null && managedUICanvasGroups.Count > 0)
        {
            foreach (CanvasGroup cg in managedUICanvasGroups) 
            {
                if (cg != null)
                {
                    cg.alpha = 1;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }
        
        // 2. 显示 3D 物体 (GameObject)
        if (managed3DObjects != null && managed3DObjects.Count > 0)
        {
            foreach (GameObject obj in managed3DObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
        Debug.Log("GlobalUIManager: 已恢复所有UI和3D物体。");
    }

    public void UpdateSlider()
    {
       tripoSlider.value = TripoRuntimeCore.imageToModelProgress;
    }
}