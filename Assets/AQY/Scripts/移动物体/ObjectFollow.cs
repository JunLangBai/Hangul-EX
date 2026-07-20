using Rokid.UXR.Interaction;
using UnityEngine;

namespace Rokid.UXR.Utility
{
    // [ExecuteAlways] // 建议在真机调试时去掉ExecuteAlways，除非你需要编辑器内实时预览，否则容易造成编辑器卡顿或误操作
    public class ObjectFollow : MonoBehaviour
    {
        public enum FollowType
        {
            RotationAndPosition,
            PositionOnly,
            RotationOnly
        }

        [SerializeField, Tooltip("跟随相机的方式")]
        private FollowType followType = FollowType.RotationAndPosition;

        [Header("跟随偏移设置")]
        [SerializeField, Tooltip("相对于相机位置的目标偏移量")]
        public Vector3 offsetPosition = new Vector3(0, 0, 1.5f);
        [SerializeField, Tooltip("相对于相机旋转的目标偏移量")]
        private Quaternion offsetRotation = Quaternion.identity;

        [Header("旋转轴锁定")]
        [SerializeField] private bool lockRotX = false;
        [SerializeField] private bool lockRotY = false;
        [SerializeField] private bool lockRotZ = false;

        [Header("平滑跟随设置")]
        [SerializeField, Tooltip("触发跟随的角度阈值(死区)。超过此角度开始跟随。")]
        private float followThresholdAngle = 35.0f;
        
        [SerializeField, Tooltip("停止跟随的角度阈值。当角度小于此值时停止跟随(回到中心)。")]
        private float stopFollowThresholdAngle = 1.0f; // 新增：回到中心才停止

        [SerializeField, Tooltip("触发跟随的距离阈值。")]
        private float followDistanceThreshold = 0.2f; // 新增：距离变化也会触发跟随

        [SerializeField, Tooltip("物体移动速度")]
        private float followSpeed = 5.0f;

        [Header("FOV 中心点适配")]
        [SerializeField] private bool adjustCenterByFov = true;

        private Vector3 oriOffsetPosition = Vector3.zero;
        private bool isUpdate;
        
        // 核心状态标记：当前是否正在跟随归位中
        private bool isFollowing = false; 

        private void Start()
        {
            if (!isUpdate)
            {
                oriOffsetPosition = offsetPosition;
                AdjustCenterByCameraFov(adjustCenterByFov);
            }
        }

        private void LateUpdate()
        {
            if (MainCameraCache.mainCamera == null) return;
            Transform cameraTransform = MainCameraCache.mainCamera.transform;

            // 1. 计算理想的目标位置和旋转 (如果不加延迟，物体应该在哪)
            Vector3 targetPos;
            Quaternion targetRot;
            CalculateTargetPose(cameraTransform, out targetPos, out targetRot);

            // 2. 计算当前误差 (角度 和 距离)
            float angleDiff = 0f;
            float distDiff = 0f;

            // 计算角度差 (基于物体当前位置 vs 相机正前方)
            Vector3 directionToCurrent = transform.position - cameraTransform.position;
            if (directionToCurrent.sqrMagnitude > 0.001f)
            {
                // 计算 "相机正方向" 和 "相机到物体方向" 的夹角
                // 注意：这里的目标方向应该是 理想位置的方向，或者是相机Forward，取决于你的需求。
                // 为了简单，我们计算 "物体当前位置" 和 "理想位置" 相对于相机的夹角差距，或者直接用相机Forward
                angleDiff = Vector3.Angle(cameraTransform.forward, directionToCurrent.normalized);
            }

            // 计算距离差 (当前位置 vs 理想位置)
            distDiff = Vector3.Distance(transform.position, targetPos);

            // 3. 状态机逻辑 (核心修复)
            if (!isFollowing)
            {
                // 如果目前是静止的，检查是否超出阈值（死区）
                if (angleDiff > followThresholdAngle || distDiff > followDistanceThreshold)
                {
                    isFollowing = true;
                }
            }
            else
            {
                // 如果正在跟随，检查是否已经足够接近目标（归位）
                // 这里使用较小的阈值，确保物体回到中心附近才停止
                if (angleDiff < stopFollowThresholdAngle && distDiff < 0.05f)
                {
                    isFollowing = false;
                }
            }

            // 4. 执行移动
            if (isFollowing)
            {
                float t = Time.deltaTime * followSpeed;
                
                // 针对位置的处理
                transform.position = Vector3.Lerp(transform.position, targetPos, t);

                // 针对旋转的处理
                // 如果是PositionOnly，我们通常不希望Lerp旋转，但在CalculateTargetPose里已经处理了targetRot为自身
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            }
        }

        private void CalculateTargetPose(Transform cameraTransform, out Vector3 position, out Quaternion rotation)
        {
            // 计算位置
            switch (followType)
            {
                case FollowType.RotationAndPosition:
                case FollowType.PositionOnly: 
                    // 修复：PositionOnly 也应该是相对于相机的局部坐标，否则物体会固定在世界原点附近
                    position = cameraTransform.TransformPoint(offsetPosition);
                    break;
                case FollowType.RotationOnly:
                    position = this.transform.position; // 保持原地
                    break;
                default:
                    position = this.transform.position;
                    break;
            }

            // 计算旋转
            switch (followType)
            {
                case FollowType.RotationAndPosition:
                case FollowType.RotationOnly:
                    // 基础目标旋转
                    Quaternion baseRot = cameraTransform.rotation * offsetRotation;
                    Vector3 targetEuler = baseRot.eulerAngles;

                    // 处理轴锁定
                    rotation = Quaternion.Euler(
                        lockRotX ? transform.eulerAngles.x : targetEuler.x,
                        lockRotY ? transform.eulerAngles.y : targetEuler.y,
                        lockRotZ ? transform.eulerAngles.z : targetEuler.z
                    );
                    break;
                case FollowType.PositionOnly:
                    rotation = transform.rotation; // 保持自身当前旋转，不跟随相机转动
                    break;
                default:
                    rotation = transform.rotation;
                    break;
            }
        }

        public void AdjustCenterByCameraFov(bool adjustCenter, bool useLeftEyeFov = true)
        {
            this.adjustCenterByFov = adjustCenter;
            if (adjustCenter)
            {
                // 确保 Rokid SDK 的 Utils 类可用，如果报错请确保引入了正确的命名空间
                // 如果在编辑器模式下 Utils 报错，可以加个 try-catch 或者预编译宏
                try {
                    Vector3 center = Utils.GetCameraCenter(oriOffsetPosition.z, useLeftEyeFov);
                    offsetPosition = center + new Vector3(oriOffsetPosition.x, oriOffsetPosition.y, 0);
                } catch {}
            }
            else
            {
                offsetPosition = oriOffsetPosition;
            }
        }

        public void UpdateOffsetPosition(Vector3 newOffsetPosition, bool shouldAdjustCenter)
        {
            isUpdate = true;
            this.oriOffsetPosition = newOffsetPosition;
            if (Utils.IsAndroidPlatform())
            {
                AdjustCenterByCameraFov(shouldAdjustCenter);
            }
            else
            {
                this.offsetPosition = newOffsetPosition;
            }
        }
    }
}