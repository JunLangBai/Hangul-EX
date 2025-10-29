using System;
using Rokid.UXR.Interaction;
using Rokid.UXR.Module;
using UnityEngine;
using UnityEngine.EventSystems; // 包含 IEventSystemHandler

// 1. 确保继承自 IRayPointerDown 接口
public class PlaneAnchorPlacer : MonoBehaviour, IRayPointerDown
{
    [SerializeField]
    private GameObject anchorPrefab; // 要放置的物体预制件
    

    // 2. 实现接口要求的方法
    public void OnRayPointerDown(PointerEventData eventData)
    {
        // 确保 anchorPrefab 存在
        if (anchorPrefab == null) return;

        // 3. 实例化锚点
        GameObject anchor = GameObject.Instantiate(anchorPrefab);
        anchor.gameObject.SetActive(true);

        // 4. 设置位置和旋转
        // 注意：这里的 eventData.pointerCurrentRaycast 包含了碰撞信息
        anchor.transform.SetPose(
            new Pose(
                eventData.pointerCurrentRaycast.worldPosition, // 碰撞点的世界坐标位置
                eventData.pointerCurrentRaycast.gameObject.transform.rotation // AR 平面的旋转
            )
        );

        // 5. 设置父级 (可以直接设置为这个 AR 平面自身)
        anchor.transform.parent = this.transform; 
        
        // 注意：在这个基于接口的实现中，你不需要像原脚本那样显式检查 ARPlane 组件，
        // 因为 OnRayPointerDown 事件只有在射线击中实现了接口的物体（ARPlane）时才会触发。
    }
}