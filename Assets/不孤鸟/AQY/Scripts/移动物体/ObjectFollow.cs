using Rokid.UXR.Interaction;
using UnityEngine;

namespace Rokid.UXR.Utility
{
    [ExecuteAlways]
    public class ObjectFollow : MonoBehaviour
    {
        public enum FollowType
        {
            RotationAndPosition, // 跟随相机的位置和旋转
            PositionOnly,        // 仅跟随相机的位置
            RotationOnly         // 仅跟随相机的旋转
        }

        [SerializeField, Tooltip("跟随相机的方式")]
        private FollowType followType = FollowType.RotationAndPosition;

        [Header("跟随偏移设置")]
        [SerializeField, Tooltip("相对于相机位置的目标偏移量")]
        private Vector3 offsetPosition = new Vector3(0, 0, 1.5f); // 默认给一个Z值，让物体在前方
        [SerializeField, Tooltip("相对于相机旋转的目标偏移量")]
        private Quaternion offsetRotation = Quaternion.identity;

        [Header("旋转轴锁定")]
        [SerializeField, Tooltip("跟随相机旋转时，锁定X轴")]
        private bool lockRotX = false;
        [SerializeField, Tooltip("跟随相机旋转时，锁定Y轴")]
        private bool lockRotY = false;
        [SerializeField, Tooltip("跟随相机旋转时，锁定Z轴")]
        private bool lockRotZ = false;

        [Header("平滑跟随设置")]
        [SerializeField, Tooltip("触发跟随的角度阈值。当相机偏离物体的角度超过此值时，物体才开始移动。")]
        private float followThresholdAngle = 20.0f;
        [SerializeField, Tooltip("物体移动到目标位置时的速度。")]
        private float followSpeed = 4.0f;

        [Header("FOV 中心点适配")]
        [SerializeField, Tooltip("根据FOV调整相机中心点")]
        private bool adjustCenterByFov = true;

        private Vector3 oriOffsetPosition = Vector3.zero;
        private bool isUpdate;

        private void Start()
        {
            if (isUpdate == false)
            {
                oriOffsetPosition = offsetPosition;
                AdjustCenterByCameraFov(adjustCenterByFov);
            }
        }

        private void LateUpdate()
        {
            if (MainCameraCache.mainCamera == null) return;
            Transform cameraTransform = MainCameraCache.mainCamera.transform;

            // --- 核心逻辑改动 ---

            // 1. 计算出基于当前相机朝向的“理想目标位置”。
            Vector3 targetPosition;
            Quaternion targetRotation;
            CalculateTargetPose(cameraTransform, out targetPosition, out targetRotation);

            // 2. 计算相机正前方与“物体当前位置”的方向向量之间的夹角。
            Vector3 directionToObject = this.transform.position - cameraTransform.position;
            // 如果距离过近，则直接移动到目标位置，避免方向向量计算错误
            if (directionToObject.sqrMagnitude < 0.001f)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }
            float angle = Vector3.Angle(cameraTransform.forward, directionToObject.normalized);

            // 3. 只有当夹角大于我们设定的阈值时，才执行移动。
            if (angle > followThresholdAngle)
            {
                // 使用 Lerp/Slerp 平滑地将物体移动到“理想目标位置”
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
            // 4. 如果夹角在阈值内，则不执行任何操作。物体会保持在当前的世界坐标位置。
        }

        /// <summary>
        /// 根据当前相机状态，计算出物体应该在的目标位置和旋转
        /// </summary>
        private void CalculateTargetPose(Transform cameraTransform, out Vector3 position, out Quaternion rotation)
        {
            switch (followType)
            {
                case FollowType.RotationAndPosition:
                    position = cameraTransform.TransformPoint(offsetPosition);
                    Vector3 cameraEuler = (offsetRotation * cameraTransform.rotation).eulerAngles;
                    rotation = Quaternion.Euler(
                        lockRotX ? transform.eulerAngles.x : cameraEuler.x,
                        lockRotY ? transform.eulerAngles.y : cameraEuler.y,
                        lockRotZ ? transform.eulerAngles.z : cameraEuler.z
                    );
                    break;
                case FollowType.PositionOnly:
                    position = cameraTransform.position + offsetPosition;
                    rotation = this.transform.rotation; // 保持自身旋转
                    break;
                case FollowType.RotationOnly:
                    position = this.transform.position; // 保持自身位置
                    Vector3 cameraEuler1 = (offsetRotation * cameraTransform.rotation).eulerAngles;
                    rotation = Quaternion.Euler(
                        lockRotX ? transform.eulerAngles.x : cameraEuler1.x,
                        lockRotY ? transform.eulerAngles.y : cameraEuler1.y,
                        lockRotZ ? transform.eulerAngles.z : cameraEuler1.z
                    );
                    break;
                default:
                    position = this.transform.position;
                    rotation = this.transform.rotation;
                    break;
            }
        }


        public void AdjustCenterByCameraFov(bool adjustCenter, bool useLeftEyeFov = true)
        {
            this.adjustCenterByFov = adjustCenter;
            if (adjustCenter)
            {
                Vector3 center = Utils.GetCameraCenter(oriOffsetPosition.z, useLeftEyeFov);
                offsetPosition = center + new Vector3(oriOffsetPosition.x, oriOffsetPosition.y, 0);
            }
            else
            {
                offsetPosition = oriOffsetPosition;
            }
        }

        public void UpdateOffsetPosition(Vector3 newOffsetPosition, bool shouldAdjustCenter)
        {
            isUpdate = true;
            this.offsetPosition = this.oriOffsetPosition = newOffsetPosition;
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