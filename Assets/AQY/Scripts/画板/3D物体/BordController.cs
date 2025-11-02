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
        HideBook();
    }

    public void ShowBord()
    {
        if (mixBordGroup != null)
        {
            mixBordGroup.alpha = 1;
            mixBordGroup.interactable = true;
            mixBordGroup.blocksRaycasts = true;
        }
    }

    public void HideBord()
    {
        if (mixBordGroup != null)
        {
            mixBordGroup.alpha = 0;
            mixBordGroup.interactable = false;
            mixBordGroup.blocksRaycasts = false;
        }

        Debug.Log("[DragHandle] HidePanel Complete");
    }

    public void ShowBook()
    {
        if (mixBordGroup != null)
        {
            mixBord.SetActive(true);
        }

        if (bookGroup != null)
        {
            bookGroup.alpha = 1;
            bookGroup.interactable = true;
            bookGroup.blocksRaycasts = true;
        }

    }

    public void HideBook()
    {
        if (bookGroup != null)
        {
            // mixBord.SetActive(false);
            bookGroup.alpha = 0;
            bookGroup.interactable = false;
            bookGroup.blocksRaycasts = false;
        }
    }

    public void HideCanvas()
    {
        if (mixBord != null)
        {
            mixBord.SetActive(false);
        }
    }

    public void ShowCanvas()
    {
        if (mixBord != null)
        {
            mixBord.SetActive(true);
        }
    }
}
