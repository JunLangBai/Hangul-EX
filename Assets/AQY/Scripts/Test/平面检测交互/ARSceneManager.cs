using System.Collections;
using System.Collections.Generic;
using Rokid.UXR.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARPlane = Rokid.UXR.Module.ARPlane;
using ARPlaneManager = Rokid.UXR.Module.ARPlaneManager;

public class ARSceneManager : MonoBehaviour
{
    //提醒扫描平面
    public GameObject promptGrad;
    //测试用tmp
    public TextMeshPro testTxt;

    //要生成的物体
    public GameObject generatePrefab;

    // 目标手
    public HandType targetHand = HandType.RightHand; // 可以根据需要选择 LeftHand 或 RightHand
    
    
    // 手掌朝上状态的标志
    private bool isPalmUpActive = false;
    //手掌朝前的标志
    private bool isStopGestureActive = false;
    
    //UI
    private GameObject pointableUI;
    // 用于存储食指指尖的位置
    private Vector3 indexHandPosition;
    
    // 手掌朝前角度阈值：判断手掌的向上方向与世界Vector3.up的接近程度
    public float angleThreshold = 45f;
    
    
    // 确保 ARPlaneManager 实例存在 (检查 null 是关键!)
    private ARPlaneManager _planeManager;
    
    //物体是否生成
    private bool isObjectSet = false;
    //是否定位过物体
    private bool isPositioning = false;
    //是否完成物体固定流程
    private bool isFinishFix =false;
    
    //是否为捏合手势
    private bool isPinching;
    private GameObject SceneObj;
    private Vector3 pinchVector3;
    
    //事件系统
    private ARGradEvent gameManager;
    
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
        
        pointableUI = GameObject.Find("PointableUI");
    }

    private void Start()
    {
        gameManager = FindObjectOfType<ARGradEvent>();

        if (gameManager != null)
        {
            gameManager.OnStart += Introduce;
            gameManager.OnScanGrad += StartGradHandle;
            gameManager.OnFixedTransform += StartFixedTransform;
            gameManager.OnUse += StartUse;
        }

        // 检查实例是否成功获取
        if (_planeManager != null)
        {
            gameManager.TriggerEnum(ARGradEvent.CurrentStatus.Start);
            gameManager.OnStateChanged();
        }
        
        pointableUI.SetActive(false);
    }
    
    private void Update()
    {
        if (gameManager.CurrentState == ARGradEvent.CurrentStatus.FixedTransform)
        {
            GetGesture();
            if (isObjectSet)
            {
                StartMoveObj();
            }

            CheckStopGesture();
        }

        if (gameManager.CurrentState == ARGradEvent.CurrentStatus.Use)
        {
            testTxt.text = "正在使用";
            CheckPalmUpGesture();
        }
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

    //介绍事件
    public void Introduce()
    {
        //韩易橙的介绍流程
        
        //完成后跳转
        gameManager.TriggerEnum(ARGradEvent.CurrentStatus.ScanGrad);
        gameManager.OnStateChanged();
        
    }

    //开始扫描事件
    public void StartGradHandle()
    {
        promptGrad.SetActive(true);
        ARPlaneManager.Instance.OpenPlaneTracker();
    }

    public void StopGradHandle()
    {
        ARPlaneManager.Instance.ClosePlaneTracker();
    }

    public void StartFixedTransform()
    {
        //写开始固定物体需要的流程
        
        
    }

    public void StartUse()
    {
        //写开始使用后应该有的事件
        
        //写需要从使用中跳转到其他场景的逻辑
    }

    // 处理新发现的平面
    private void HandlePlaneAdded(ARPlane plane)
    {
        Debug.Log($"发现新平面! ID: {plane.boundedPlane.planeHandle}");
        // testTxt.text = $"发现新平面! ID: {plane.boundedPlane.planeHandle}";


        // 获取平面的详细信息
        var bp = plane.boundedPlane;


        // 筛选 4: 检查所有条件
        if (!isObjectSet)
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
        if (generatePrefab == null)
        {
            Debug.LogError("Prefab is not assigned in the Inspector!");
            return;
        }

        var v = new Vector3(point.x, point.y, point.z).normalized;
        testTxt.text = $"生成物体了{v}";

        // 生成物体
        SceneObj = Instantiate(generatePrefab, v, Quaternion.identity);

        // 确保主摄像机存在
        if (Camera.main != null)
        {
            // 计算从物体到摄像机的水平方向
            var directionToCamera = Camera.main.transform.position - SceneObj.transform.position;
            directionToCamera.y = 0; // 锁定Y轴，只进行水平旋转

            if (directionToCamera != Vector3.zero)
            {
                // 让物体朝向摄像机
                SceneObj.transform.rotation = Quaternion.LookRotation(-directionToCamera);
                isObjectSet = true;
                StopGradHandle();
                
                // --- 关键修复：添加以下代码 ---

                // 1. 隐藏扫描提示
                if (promptGrad != null)
                {
                    promptGrad.SetActive(false);
                }

                // 2. 切换到“固定/移动”状态
                if (gameManager != null)
                {
                    gameManager.TriggerEnum(ARGradEvent.CurrentStatus.FixedTransform);
                    gameManager.OnStateChanged();
                }
            }
        }
    }

    public void StartMoveObj()
    {
        if (isPinching)
        {
            SceneObj.transform.position = pinchVector3;
            testTxt.text = "正在移动物体...";
        }
    }

    public void GetGesture()
    {
        var type = GesEventInput.Instance.GetGestureType(targetHand);
        if (type == GestureType.Pinch )
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

        if (type == GestureType.OpenPinch)
        {
            isPinching = false;
            isPositioning = true;
        }
    }
    
    /// <summary>
    /// 检测手掌朝上（Palm Up）手势的逻辑
    /// </summary>
    private void CheckPalmUpGesture()
    {
        // 1. 获取当前手势类型和掌心朝向
        GestureType currentGesture = GesEventInput.Instance.GetGestureType(targetHand);
        HandOrientation currentOrientation = GesEventInput.Instance.GetHandOrientation(targetHand);

        // --- 激活/保持逻辑 ---
        if (currentGesture == GestureType.Palm && currentOrientation == HandOrientation.Palm)
        {
            if (!isPalmUpActive)
            {
                isPalmUpActive = true;
                Debug.Log($"[{targetHand}] ✋ 手掌朝上 **激活**！");
                // 在此处添加手势激活时的逻辑（如：显示UI、进入特定模式）
                pointableUI.SetActive(true);
            }
            // 保持激活状态，可以持续执行一些逻辑
            // Debug.Log($"[{targetHand}] ✋ 手掌朝上 **保持**。");
            
            // 3. 获取 PALM (手心) 节点的骨骼点位姿 (Pose)
            Pose palmPose = GesEventInput.Instance.GetSkeletonPose(
                SkeletonIndexFlag.PALM,
                targetHand);
                
            // 4. 提取位置信息
            indexHandPosition = palmPose.position;
            
            pointableUI.transform.position = indexHandPosition;
        }
        // --- 取消/禁用逻辑 ---
        else if (isPalmUpActive)
        {
            // 取消条件：变为握拳 (Grip) 或 掌心朝向远离使用者 (Back)
            if (currentGesture == GestureType.Grip)
            {
                isPalmUpActive = false;
                Debug.Log($"[{targetHand}] ✊ 握拳 **取消** 手掌朝上状态。");
                // 在此处添加手势取消时的逻辑
            }
            else if (currentOrientation == HandOrientation.Back)
            {
                isPalmUpActive = false;
                Debug.Log($"[{targetHand}] 👈 掌心反转 **取消** 手掌朝上状态。");
                // 在此处添加手势取消时的逻辑
            }
        }
    }
    
    /// <summary>
    /// 检测“手掌朝前，五指朝上树立”（停止）手势。
    /// </summary>
    private void CheckStopGesture()
    {
        // 1. 初筛：手势类型为 Palm 且掌心朝向远离使用者 (Back)
        GestureType currentGesture = GesEventInput.Instance.GetGestureType(targetHand);
        HandOrientation currentOrientation = GesEventInput.Instance.GetHandOrientation(targetHand);

        if (currentGesture == GestureType.Palm && currentOrientation == HandOrientation.Back)
        {
            // 2. 精细判断：手掌是否“树立”
            Pose palmPose = GesEventInput.Instance.GetSkeletonPose(SkeletonIndexFlag.PALM, targetHand);
            
            // 获取手掌的向上向量（通常与中指方向平行，可以近似认为是手背朝前的姿势）
            // 注意：具体哪个轴是“向上”取决于 Rokid 骨骼点的坐标系定义。
            // 这里我们假设手掌的局部 Z 轴是手背法线（朝前/后），Y 轴是向上/下。
            Vector3 palmUpVector = palmPose.rotation * Vector3.up; 
            
            // 检查 palmUpVector 与世界坐标系 Up (Vector3.up) 的夹角
            float angleToUp = Vector3.Angle(palmUpVector, Vector3.up);

            if (angleToUp < angleThreshold)
            {
                // 3. 满足所有条件：手掌朝前，五指朝上树立
                if (!isStopGestureActive)
                {
                    isStopGestureActive = true;
                    Debug.Log($"[{targetHand}] ✋ **停止手势激活!** 手掌朝前，五指朝上。");
                    // 在此处添加手势激活逻辑（如：暂停游戏、显示菜单）
                    if (gameManager.CurrentState == ARGradEvent.CurrentStatus.FixedTransform && isPositioning)
                    {
                        testTxt.text = "物体固定！";
                        isFinishFix = true;
                        //完成固定后跳转
                        gameManager.TriggerEnum(ARGradEvent.CurrentStatus.Use);
                        gameManager.OnStateChanged();
                    }
                }
                return; // 保持激活状态
            }
        }

        // 4. 取消/禁用逻辑
        if (isStopGestureActive)
        {
            isStopGestureActive = false;
            Debug.Log($"[{targetHand}] ✋ 停止手势 **取消**。");
            // 在此处添加手势取消逻辑
        }
    }
}
