using System;
using RainbowArt.CleanFlatUI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class DistanceToggleConnector : MonoBehaviour
{
    private void Start()
    {
        // 直接调用单例修改数据
        if (ChangeDistance.Instance != null)
        {
            this.gameObject.GetComponent<Switch>().IsOn = ChangeDistance.Instance.distance;
        }
    }

    // 把这个方法绑定到 Toggle 的 OnValueChanged 面板事件上
    public void ChangeDistanceSetting(bool isOn)
    {
        // 直接调用单例修改数据
        if (ChangeDistance.Instance != null)
        {
            ChangeDistance.Instance.distance = isOn;
            ChangeDistance.Instance.DistanceChange();
        }
    }
}