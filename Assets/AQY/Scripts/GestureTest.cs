using Rokid.UXR.Interaction;
using TMPro;
using UnityEngine;

public class GestureTest : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void Update()
    {
        var a = IsLeftHandFlatPalmRight();
        if (a)
            text.text = "111";
        else
            text.text = "222";
    }

    /// <summary>
    ///     自定义判断：平举左手且手掌朝右
    /// </summary>
    /// <returns>bool</returns>
    private bool IsLeftHandFlatPalmRight()
    {
        // 1. 检查是否为 'Palm' 基础手势（可选，用于过滤）
        var currentGesType = GesEventInput.Instance.GetGestureType(HandType.LeftHand);
        if (currentGesType != GestureType.Palm) return false;

        // 2. 获取手部关键骨骼点位姿
        var wristPose = GetSkeletonPose(SkeletonIndexFlag.WRIST, HandType.LeftHand);
        // 假设PALM节点提供了手掌的朝向信息
        var palmPose = GetSkeletonPose(SkeletonIndexFlag.PALM, HandType.LeftHand);

        // 3. 自定义判断“掌心朝右”
        // 获取手掌的法线方向 (通常是Pose的Right向量，但具体取决于Rokid SDK如何定义PALM Pose)
        // 这里的 PalmRightDirection 是手掌平面的法线，指向掌心
        var PalmRightDirection = palmPose.rotation * Vector3.right; // 假设朝向是世界坐标

        // 将手掌法线与世界坐标系的右方向 (Vector3.right) 进行点积
        var dotPalmRight = Vector3.Dot(PalmRightDirection.normalized, Vector3.right);

        // 设置阈值：例如 0.85，表示夹角在 30 度以内
        if (dotPalmRight < 0.85f) return false;

        // 4. 自定义判断“平举”
        // 获取手部前向向量（例如：手腕到中指MCP）
        var handForward = (GetSkeletonPose(SkeletonIndexFlag.MIDDLE_FINGER_MCP, HandType.LeftHand).position -
                           wristPose.position).normalized;

        // 平举意味着手部前向向量与水平面（Y轴）的点积要接近 0
        var dotHandForwardY = Vector3.Dot(handForward, Vector3.up);

        // 假设平举的垂直误差在 15 度以内 (cos(15) ≈ 0.96)
        if (Mathf.Abs(dotHandForwardY) > 0.25f) // 0.25f 对应约 75.5 度，即与水平面夹角小于 14.5 度
            return false;

        // 5. 如果以上条件都满足
        return true;
    }

    private Pose GetSkeletonPose(SkeletonIndexFlag index, HandType hand)
    {
        return GesEventInput.Instance.GetSkeletonPose(index, hand);
    }
}