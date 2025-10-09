using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Rokid.UXR.Interaction;
using UnityEngine.UI;

public class BordController : MonoBehaviour
{
    public GameObject mixBord;
    
    // Start is called before the first frame update
    void Start()
    {
        HideBord();
    }

    public void ShowBord()
    {
        mixBord.SetActive(true);
    }

    public void HideBord()
    {
        mixBord.SetActive(false);
    
        Debug.Log("[DragHandle] HidePanel Complete");
    }
}
