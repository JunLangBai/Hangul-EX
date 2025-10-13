using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Rokid.UXR.Interaction;
using UnityEngine.UI;

public class BordController : MonoBehaviour
{
    public GameObject mixBord;
    
    public CanvasGroup mixBordGroup;
    public CanvasGroup bookGroup;
    
    // Start is called before the first frame update
    void Start()
    {
        // HideBord();
    }

    public void ShowBord()
    {
        mixBord.SetActive(true);
        mixBordGroup.alpha = 1;
        mixBordGroup.interactable = true;
        mixBordGroup.blocksRaycasts = true;
    }

    public void HideBord()
    {
        mixBord.SetActive(false);
        mixBordGroup.alpha = 0;
        mixBordGroup.interactable = false;
        mixBordGroup.blocksRaycasts = false;
    
        Debug.Log("[DragHandle] HidePanel Complete");
    }

    public void ShowBook()
    {
        mixBord.SetActive(true);
        bookGroup.alpha = 1;
        bookGroup.interactable = true;
        bookGroup.blocksRaycasts = true;
    }

    public void HideBook()
    {
        // mixBord.SetActive(false);
        bookGroup.alpha = 0;
        bookGroup.interactable = false;
        bookGroup.blocksRaycasts = false;
    }
}
