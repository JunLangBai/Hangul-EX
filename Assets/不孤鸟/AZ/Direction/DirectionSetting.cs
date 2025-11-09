using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class DirectionSettings : MonoBehaviour
{
    // 1. 单例实例
    public static DirectionSettings Instance { get; private set; }
    
    // 2. 公共实例变量（带有默认值）
    //    其他场景将通过 DirectionSettings.Instance.GameDuration 访问
    public float GameDuration { get; private set; } = 10f;
    public bool IsRandomMode  = false;
    public float GameRound { get; private set; } = 40f;

    void Awake()
    {
        // --- 单例模式逻辑 ---
        if (Instance != null && Instance != this)
        {
            // 如果一个实例已经存在，并且不是我，
            // 说明我们是从其他场景返回的，销毁这个新创建的重复对象。
            Destroy(gameObject);
            return;
        }

        // 我是第一个，将我设为单例实例
        Instance = this;

        // 关键：使该 GameObject 在加载新场景时“幸存”
        DontDestroyOnLoad(gameObject);
    }
    

    // 当 Slider 值改变时
    public void OnTimeSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        GameDuration = Mathf.RoundToInt(value); 
    }

    // 当 Toggle 值改变时
    public void OnRandomToggleChanged(bool value)
    {
        // 只更新内存中的实例变量
        IsRandomMode = value;
    }
    
    // 当 Slider 值改变时
    public void OnGroundSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        GameRound = Mathf.RoundToInt(value); 
    }
    
}