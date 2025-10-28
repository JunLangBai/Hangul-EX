using Rokid.UXR.Interaction;
using Rokid.UXR.Module;
using TMPro;
using UnityEngine;

public class GradTest : MonoBehaviour
{
    public TextMeshPro promptGrad;
    public TextMeshPro testTxt;

    public GameObject prefab;

    // 确保 ARPlaneManager 实例存在 (检查 null 是关键!)
    private ARPlaneManager _planeManager;

    private bool isObject;
    private bool isPinching;
    private GameObject newObject;
    private Vector3 pinchVector3;

    private void Awake()
    {
        // 尝试获取 ARPlaneManager 实例
        _planeManager = ARPlaneManager.Instance;
        if (_planeManager == null)
        {
            Debug.LogError(
                "ARPlaneManager.Instance is NULL. Make sure the component exists in the scene and is initialized.");
            enabled = false; // 如果Manager不存在，禁用此脚本
            return;
        }

        // 在 Awake 中订阅事件，确保在 Manager 激活时就开始接收
        ARPlaneManager.OnPlaneAdded += HandlePlaneAdded;
        ARPlaneManager.OnPlaneUpdated += HandlePlaneUpdated;
        ARPlaneManager.OnPlaneRemoved += HandlePlaneRemoved;
    }

    private void Start()
    {
        // 检查实例是否成功获取
        if (_planeManager != null)
            // 启动平面检测流程
            StartGradHandle(promptGrad.gameObject);
    }

    private void Update()
    {
        GetGesture();
        if (isObject)
            StartMoveObj();
    }

    // 在脚本禁用或销毁时，一定要取消监听！
    private void OnDisable()
    {
        Debug.Log("停止监听平面事件。");
        ARPlaneManager.OnPlaneAdded -= HandlePlaneAdded;
        ARPlaneManager.OnPlaneUpdated -= HandlePlaneUpdated;
        ARPlaneManager.OnPlaneRemoved -= HandlePlaneRemoved;
    }

    private void OnDestroy()
    {
        // 在销毁时关闭平面追踪器
        if (_planeManager != null) _planeManager.ClosePlaneTracker();
    }

    public void StartGradHandle(GameObject obj)
    {
        obj.SetActive(true);

        ARPlaneManager.Instance.OpenPlaneTracker();
    }

    public void StopGradHandle(GameObject obj)
    {
        ARPlaneManager.Instance.ClosePlaneTracker();
    }


    // 处理新发现的平面
    private void HandlePlaneAdded(ARPlane plane)
    {
        Debug.Log($"发现新平面! ID: {plane.boundedPlane.planeHandle}");
        // testTxt.text = $"发现新平面! ID: {plane.boundedPlane.planeHandle}";


        // 获取平面的详细信息
        var bp = plane.boundedPlane;


        // 筛选 4: 检查所有条件
        if (!isObject)
        {
            Debug.Log("[合格] 发现新平面");

            // 实例化物体
            InstantiatePlaneVisual(bp.pose.position);
        }
        else
        {
            Debug.Log("[过滤] 平面不合格。");
        }

        // 实际应用中：
        // 1. 你会创建一个新的GameObject来代表这个平面。
        // 2. 你会使用 plane.boundedPlane.boundary3D 的数据来创建一个Mesh（网格）。
        // 3. 你会把这个GameObject的位置和旋转设置为 plane.boundedPlane.pose。
        // 4. 你可能会把 plane.boundedPlane.planeHandle 作为key，GameObject作为value存入一个字典（Dictionary），方便以后更新。
    }


    // 处理已更新的平面
    private void HandlePlaneUpdated(ARPlane plane)
    {
        // 实际应用中：
        // 1. 你会从字典中根据 plane.boundedPlane.planeHandle 找到对应的GameObject。
        // 2. 更新这个GameObject的pose。
        // 3. 重新生成或更新它的Mesh来匹配新的 boundary3D。
    }

    // 处理已移除的平面
    private void HandlePlaneRemoved(ARPlane plane)
    {
        Debug.Log($"平面消失了! ID: {plane.boundedPlane.planeHandle}");

        // SECURE:
        // 1. 从字典中根据 plane.boundedPlane.planeHandle 找到对应的GameObject。
        // 2. 销毁（Destroy）这个GameObject。
        // 3. 从字典中移除这个条目。
    }


    private void InstantiatePlaneVisual(Vector3 point)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is not assigned in the Inspector!");
            return;
        }

        var v = new Vector3(point.x, point.y, point.z).normalized;
        testTxt.text = $"生成物体了{v}";

        // 生成物体
        newObject = Instantiate(prefab, v, Quaternion.identity);

        // 确保主摄像机存在
        if (Camera.main != null)
        {
            // 计算从物体到摄像机的水平方向
            var directionToCamera = Camera.main.transform.position - newObject.transform.position;
            directionToCamera.y = 0; // 锁定Y轴，只进行水平旋转

            if (directionToCamera != Vector3.zero)
            {
                // 让物体朝向摄像机
                newObject.transform.rotation = Quaternion.LookRotation(-directionToCamera);
                isObject = true;
                StopGradHandle(promptGrad.gameObject);
            }
        }
    }

    public void StartMoveObj()
    {
        if (isPinching)
        {
            newObject.transform.position = pinchVector3;
            testTxt.text = "正在移动物体...";
        }
    }

    public void GetGesture()
    {
        var type = GesEventInput.Instance.GetGestureType(HandType.RightHand);
        if (type == GestureType.Pinch)
        {
            // 2. **获取食指指尖 (INDEX_FINGER_TIP) 的骨骼点位姿 (Pose)**
            var indexTipPose = GesEventInput.Instance.GetSkeletonPose(
                SkeletonIndexFlag.INDEX_FINGER_TIP,
                HandType.RightHand
            );

            // 3. **提取位置信息**
            pinchVector3 = indexTipPose.position;
            isPinching = true;
            
        }
        if (type == GestureType.OpenPinch) isPinching = false;
    }
}