using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class DirectionSettings : MonoBehaviour
{
    private AllSettingCtr allSettingCtr;
    // 2. 公共实例变量（带有默认值）
    //    其他场景将通过 DirectionSettings.Instance.GameDuration 访问
    public float GameDuration { get; private set; } = 10f;
    public bool IsRandomMode  = false;
    public float GameRound { get; private set; } = 40f;

    void Awake()
    {
        allSettingCtr = AllSettingCtr.Instance;
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