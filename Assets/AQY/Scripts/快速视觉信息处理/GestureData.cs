using UnityEngine;

// 定义我们自己的手势枚举，方便与SDK解耦和扩展
public enum CustomGestureType { None, Grip, Pinch, PalmForward, PalmUp }
// 定义左右手枚举
public enum Hand { Left, Right }

[CreateAssetMenu(fileName = "NewGesture", menuName = "AttentionTest/GestureData")]
public class GestureData : ScriptableObject
{
    public string gestureName;
    public Sprite gestureImage;
    public Hand hand;
    public CustomGestureType gestureType;
}