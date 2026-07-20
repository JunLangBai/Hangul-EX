using System;
using UnityEngine;
using Rokid.UXR.Interaction;

public class GestureInputController : MonoBehaviour
{
    public static event Action<Hand, CustomGestureType> OnGesturePerformed;

    // --- 新增代码 ---
    // 分别记录左右手上一帧的自定义手势状态
    private CustomGestureType lastLeftGesture = CustomGestureType.None;
    private CustomGestureType lastRightGesture = CustomGestureType.None;
    // --- 新增代码结束 ---

    void Update()
    {
        DetectHandGesture(HandType.LeftHand);
        DetectHandGesture(HandType.RightHand);
    }

    private void DetectHandGesture(HandType handType)
    {
        // 1. 从SDK获取基础手势类型
        GestureType sdkGesture = GesEventInput.Instance.GetGestureType(handType);
        CustomGestureType customGesture = CustomGestureType.None;
        Hand currentHand = (handType == HandType.LeftHand) ? Hand.Left : Hand.Right;

        // --- 逻辑几乎不变，只是将最终的手势赋值给 customGesture ---
        switch (sdkGesture)
        {
            case GestureType.Grip:
                customGesture = CustomGestureType.Grip;
                break;
            case GestureType.Pinch:
                customGesture = CustomGestureType.Pinch;
                break;
            case GestureType.Palm:
                HandOrientation orientation = GesEventInput.Instance.GetHandOrientation(handType);
                if (orientation == HandOrientation.Back)
                {
                    customGesture = CustomGestureType.PalmForward;
                }
                else
                {
                    Pose palmPose = GesEventInput.Instance.GetSkeletonPose(SkeletonIndexFlag.PALM, handType);
                    if (Vector3.Dot(palmPose.up, Vector3.up) > 0.7f)
                    {
                        customGesture = CustomGestureType.PalmUp;
                    }
                    // 如果手掌既不朝前也不朝上，可以根据需要视为None或默认的Palm类型
                }
                break;
            // 当没有检测到特定手势时，sdkGesture会是None，customGesture自然也是None
        }
        
        // --- 核心修改 ---
        // 获取上一帧的手势
        CustomGestureType lastGesture = (currentHand == Hand.Left) ? lastLeftGesture : lastRightGesture;

        // 仅当检测到的手势与上一帧不同时，才触发事件
        if (customGesture != lastGesture)
        {
            // 如果手势从某个具体手势变回了 None，我们不触发事件
            // 我们只关心“做出”手势的那个瞬间
            if (customGesture != CustomGestureType.None)
            {
                OnGesturePerformed?.Invoke(currentHand, customGesture);
            }

            // 更新上一帧的手势状态
            if (currentHand == Hand.Left)
            {
                lastLeftGesture = customGesture;
            }
            else
            {
                lastRightGesture = customGesture;
            }
        }
        // --- 核心修改结束 ---
    }
}