using System;
using Rokid.UXR.Interaction;
using UnityEngine;

public class CustomHandGestureDetector : MonoBehaviour
{
    
    
    

    void Update()
    {
        // 确保 GesEventInput 实例存在
        if (GesEventInput.Instance == null)
        {
            Debug.LogError("GesEventInput.Instance is null. Cannot perform gesture detection.");
            return;
        }

        
    }

    
}