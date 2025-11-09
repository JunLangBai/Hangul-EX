using System;
using UnityEngine;
using Rokid.UXR.Interaction; // 引入Rokid SDK的命名空间

public class GestureInputController : MonoBehaviour
{
    // 定义一个事件，当检测到手势时触发
    public static event Action<Hand, CustomGestureType> OnGesturePerformed;

    void Update()
    {
        // 分别检测左右手
        DetectHandGesture(HandType.LeftHand);
        DetectHandGesture(HandType.RightHand);
    }

    private void DetectHandGesture(HandType handType)
    {
        // 1. 从SDK获取基础手势类型
        GestureType sdkGesture = GesEventInput.Instance.GetGestureType(handType);
        CustomGestureType customGesture = CustomGestureType.None;

        Hand currentHand = (handType == HandType.LeftHand) ? Hand.Left : Hand.Right;

        switch (sdkGesture)
        {
            case GestureType.Grip:
                customGesture = CustomGestureType.Grip;
                break;
            case GestureType.Pinch:
                customGesture = CustomGestureType.Pinch;
                break;
            case GestureType.Palm:
                // 2. 如果是手掌，需要进一步判断朝向
                HandOrientation orientation = GesEventInput.Instance.GetHandOrientation(handType);
                if (orientation == HandOrientation.Back)
                {
                    customGesture = CustomGestureType.PalmForward;
                }
                else // 默认为Palm时是朝向用户
                {
                    // 3. 自定义逻辑判断“手掌朝上”
                    // 这里需要获取骨骼点信息
                    Pose palmPose = GesEventInput.Instance.GetSkeletonPose(SkeletonIndexFlag.PALM, handType);
                    // palmPose.up 指向掌心方向的向量
                    if (Vector3.Dot(palmPose.up, Vector3.up) > 0.7f) // 向量点积判断是否朝上
                    {
                        customGesture = CustomGestureType.PalmUp;
                    }
                }
                break;
        }

        // 4. 如果检测到了一个有效手势，就触发事件
        if (customGesture != CustomGestureType.None)
        {
            // 使用 ?.Invoke() 安全地触发事件
            OnGesturePerformed?.Invoke(currentHand, customGesture);
        }
    }
}