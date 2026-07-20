using UnityEngine;

public class ConstantScreenSize3D : MonoBehaviour
{
    public Camera cam;
    public Transform child;
    
    [Tooltip("在多远的距离下是物体的原始大小")]
    public float referenceDistance = 0.3f;

    private Vector3 referenceScale;
    private Vector3 referenceLocalPos; // 增加：记录初始的局部坐标

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        // 记录子物体的初始缩放和初始位置
        referenceScale = child.localScale;
        referenceLocalPos = child.localPosition; 
    }

    void LateUpdate()
    {
        // 优化：计算父物体（锚点）到相机的距离，而不是子物体。
        // 为了防止物体在屏幕边缘时因为球面距离导致的大小变形，
        // 我们使用物体在相机前方（Z轴）的投影距离（Plane Distance）。
        Vector3 toObject = transform.position - cam.transform.position;
        float distance = Vector3.Dot(toObject, cam.transform.forward);

        // 如果物体在相机背后，不进行处理
        if (distance <= 0) return;

        // 计算缩放比例
        float scale = distance / referenceDistance;

        // 【关键修复 1】：缩放物体的大小
        child.localScale = referenceScale * scale;

        // 【关键修复 2】：同步缩放局部坐标（让它在屏幕上的相对位置也保持不变）
        child.localPosition = referenceLocalPos * scale;
    }
}