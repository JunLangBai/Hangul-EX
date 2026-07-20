using RainbowArt.CleanFlatUI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class DirectionSettings : MonoBehaviour
{
    private AllSettingCtr allSettingCtr;
    // // 2. 公共实例变量（带有默认值）
    // //    其他场景将通过 DirectionSettings.Instance.GameDuration 访问
    // public float GameDuration { get; private set; } = 10f;
    // public bool IsRandomMode  = false;
    // public float GameRound { get; private set; } = 40f;

    public Switch IsRandomMode;
    // 2. 引入对 UI 组件的引用（包括两个 Slider 和一个 Toggle）
    [Header("UI 引用")]
    public Slider timeSlider;
    public Slider groundSlider; // 对应 Round

    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
    }

    // 3. 关键：场景加载时，读取单例中的值并同步给 UI
    void Start()
    {
        if (allSettingCtr != null)
        {
            if (timeSlider != null)
                timeSlider.value = allSettingCtr.directionGameDuration;

            if (IsRandomMode != null)
                IsRandomMode.IsOn = allSettingCtr.directionIsRandomMode;

            if (groundSlider != null)
                groundSlider.value = allSettingCtr.directionGameRounds;
        }
    }
    

    // 当 Slider 值改变时
    public void OnTimeSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        allSettingCtr. directionGameDuration = Mathf.RoundToInt(value); 
    }

    // 当 Toggle 值改变时
    public void OnRandomToggleChanged(bool value)
    {
        // 只更新内存中的实例变量
        allSettingCtr. directionIsRandomMode = value;
    }
    
    // 当 Slider 值改变时
    public void OnGroundSliderChanged(float value)
    {
        // 只更新内存中的实例变量
        allSettingCtr. directionGameRounds = Mathf.RoundToInt(value); 
    }
    
}