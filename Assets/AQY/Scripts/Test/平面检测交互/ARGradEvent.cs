using System;
using Rokid.UXR.Interaction;
using Rokid.UXR.Module;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


public class ARGradEvent : MonoBehaviour
{
    //事件系统
    public enum CurrentStatus
    {
        Start, 
        ScanGrad,
        FixedTransform,
        Use
    }
    
    private CurrentStatus _currentState;
    public CurrentStatus CurrentState
    {
        get { return _currentState; }
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnEnumChanged(_currentState);
            }
        }
    }

    private void OnEnumChanged(CurrentStatus currentState)
    {
        CurrentState = currentState;
    }
    
    public event Action OnStart ;
    public event Action OnScanGrad;
    public event Action OnFixedTransform;
    public event Action OnUse;

    public void OnStateChanged()
    {
        if (CurrentState == CurrentStatus.Start)
        {
            Debug.Log("Game Over State Detected!");
            OnStart?.Invoke();
        }

        if (CurrentState == CurrentStatus.ScanGrad)
        {
            OnScanGrad?.Invoke();
        }

        if (CurrentState == CurrentStatus.FixedTransform)
        {
            OnFixedTransform?.Invoke();
        }

        if (CurrentState == CurrentStatus.Use)
        {
            OnUse?.Invoke();
        }
    }
    
    public void TriggerEnum(CurrentStatus currentState)
    {
        CurrentState = currentState;
    }
    
}