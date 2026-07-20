using System.Collections;
using System.Collections.Generic;
using Rokid.UXR.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeDistance : MonoBehaviour
{
    public static ChangeDistance Instance;
    
    public bool distance;
    
    private void Awake()
    {
        // 如果一个实例已经存在，并且不是当前这个，就销毁当前这个，保证唯一性
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        // 如果还没有实例，就将自己设为实例
        Instance = this;

        // 3. 关键：让这个 GameObject 在加载新场景时不会被销毁
        DontDestroyOnLoad(this.gameObject);
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "3DGenerate" &&  scene.name != "NBack")
        {
            DistanceChange();
        }
    }

    public void DistanceChange()
    {
        if (!distance)
        {
            GameObject point = GameObject.Find("PointableUI");
            if (point.GetComponent<ObjectFollow>() != null)
            {
                point.GetComponent<ObjectFollow>().offsetPosition = new Vector3(0, 0, 1.5f);
            }
            else
            {
                point.transform.position = new Vector3(0, 0, 1.5f);
            }
            
        }
        else
        {
            GameObject point = GameObject.Find("PointableUI");
            if (point.GetComponent<ObjectFollow>() != null)
            {
                point.GetComponent<ObjectFollow>().offsetPosition = new Vector3(0, 0, 0.5f);
            }
            else
            {
                point.transform.position = new Vector3(0, 0, 0.5f);
            }
            
        }
    }
}
