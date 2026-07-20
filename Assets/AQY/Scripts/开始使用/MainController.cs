using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour
{
    
    public void ExitTutorial(GameObject tutorial)
    {
        tutorial.GetComponent<CanvasGroup>().alpha = 0;
        tutorial.GetComponent<CanvasGroup>().blocksRaycasts = false;
        tutorial.GetComponent<CanvasGroup>().interactable = false;
    }

    public void QuitGame()
    {
        // 如果在编辑器中运行，则停止播放
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
