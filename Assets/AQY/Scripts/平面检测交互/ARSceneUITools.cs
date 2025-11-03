using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARSceneUITools : MonoBehaviour
{
    public ARObjectList ARObj;
    
    public List<CanvasGroup> ARObjCanvasGroups;
    
    //事件系统
    private ARGradEvent gameManager;
    //手势模块
    private ARSceneManager sceneManager;
    
    //场景生成物体tag
    public string interactableObjectTag = "ARObj";


    private void Start()
    {
        gameManager = FindObjectOfType<ARGradEvent>();
        sceneManager = FindObjectOfType<ARSceneManager>();
        ChangeSceneUI(0);
    }

    public void ChangeObj(int i)
    {
        ChangeSceneUI(i+1);
        ChangeSceneObject(ARObj.objectList[i]);
    }

    public void ChangeSceneUI(int i)
    {
        // 1. 安全检查：确保列表已分配
        if (ARObjCanvasGroups == null)
        {
            Debug.LogError("ChangeSceneUI: ARObjCanvasGroups 列表未在 Inspector 中分配！");
            return;
        }

        // 2. 安全检查：确保索引在有效范围内
        if (i < 0 || i >= ARObjCanvasGroups.Count)
        {
            Debug.LogError($"ChangeSceneUI: 传入的索引 {i} 超出范围。列表大小为 {ARObjCanvasGroups.Count}。");
            return;
        }

        // 3. 遍历列表中的所有 CanvasGroup
        for (int j = 0; j < ARObjCanvasGroups.Count; j++)
        {
            CanvasGroup currentGroup = ARObjCanvasGroups[j];
            
            // 安全检查：跳过列表中未分配的空元素
            if (currentGroup == null)
            {
                Debug.LogWarning($"ChangeSceneUI: ARObjCanvasGroups[{j}] 的元素为 null。已跳过。");
                continue;
            }

            // 4. 检查当前循环索引 j 是否等于我们想要的索引 i
            if (j == i)
            {
                // 这是我们想要显示的UI
                currentGroup.alpha = 1f;            // 设为不透明
                currentGroup.interactable = true;   // 允许交互 (如按钮点击)
                currentGroup.blocksRaycasts = true; // 阻挡射线 (允许点击)
            }
            else
            {
                // 这是我们想要隐藏的其他UI
                currentGroup.alpha = 0f;              // 设为完全透明
                currentGroup.interactable = false;    // 禁止交互
                currentGroup.blocksRaycasts = false;  // 不阻挡射线 (允许穿透点击)
            }
        }
    }

    public void ChangeSceneObject(GameObject newPrefab)
    {
        // 1. 检查新预制体是否有效
        if (newPrefab == null)
        {
            Debug.LogError("ChangeSceneObject: 传入的 newPrefab 为 null！");
            return;
        }
        
        // 2. 检查Tag是否设置
        if (string.IsNullOrEmpty(interactableObjectTag))
        {
            Debug.LogError("ChangeSceneObject: interactableObjectTag 字段未在 Inspector 中设置！");
            return;
        }

        // 3. 确定新物体的位置和旋转
        // 我们需要一个基准点。我们优先使用 SceneObj 引用，
        // 但如果它为 null，我们就查找场景中*任何*一个带Tag的物体作为基准。
        
        GameObject transformSource = null;
        
        if (sceneManager.SceneObj != null)
        {
            transformSource = sceneManager.SceneObj;
        }
        else
        {
            Debug.LogWarning("ChangeSceneObject: SceneObj 引用为 null。正在尝试通过Tag查找基准物体...");
            transformSource = GameObject.FindGameObjectWithTag(interactableObjectTag);
        }

        // 4. 如果连Tag都找不到，我们就无法替换
        if (transformSource == null)
        {
            Debug.LogError($"ChangeSceneObject: 无法找到任何物体作为变换基准 (SceneObj is null AND Tag '{interactableObjectTag}' 未找到)。无法替换。");
            return;
        }

        // 5. 保存变换信息
        Vector3 currentPosition = transformSource.transform.position;
        Quaternion currentRotation = transformSource.transform.rotation;
        Transform currentParent = transformSource.transform.parent;

        // 6. 启动协程执行“销毁->等待->创建”的原子操作
        StartCoroutine(Co_ClearAllAndReplace(newPrefab, currentPosition, currentRotation, currentParent));
    }
    
    // ▼▼▼ 【添加这个新协程】 ▼▼▼
    /// <summary>
    /// 协程：先销毁所有带Tag的物体，等待一帧，然后创建新物体。
    /// </summary>
    private IEnumerator Co_ClearAllAndReplace(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        // 1. 查找所有带Tag的物体
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag(interactableObjectTag);

        if (allObjects.Length > 0)
        {
            Debug.Log($"[Co_ClearAllAndReplace] 正在销毁 {allObjects.Length} 个带 '{interactableObjectTag}' Tag 的物体...");
            foreach (GameObject obj in allObjects)
            {
                Destroy(obj);
            }
        }
        else
        {
            Debug.LogWarning($"[Co_ClearAllAndReplace] 没有找到带 '{interactableObjectTag}' Tag 的物体去销毁。");
        }
        
        // 2. (关键) 等待一帧
        // Destroy() 不是立即执行的，它会在当前帧的末尾执行。
        // 我们等待下一帧，确保场景是干净的。
        yield return null; 

        // 3. 创建新物体
        Debug.Log($"[Co_ClearAllAndReplace] 正在 {position} 位置创建新物体: {prefab.name}");
        sceneManager.SceneObj = Instantiate(prefab, position, rotation);
        
        // 4. (关键) 确保新物体也有这个Tag！
        sceneManager.SceneObj.tag = interactableObjectTag;

        // 5. 恢复父节点（如果之前有的话）
        if (parent != null)
        {
            sceneManager.SceneObj.transform.SetParent(parent);
        }
        
        Debug.Log("[Co_ClearAllAndReplace] 替换完成。");
        
        gameManager.TriggerEnum(ARGradEvent.CurrentStatus.FixedTransform);
        gameManager.OnStateChanged();
    }
}
