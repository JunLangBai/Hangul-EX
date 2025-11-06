using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasFader : MonoBehaviour
{
    private float duration = 0.3f;
    
    // CanvasGroup组件的引用
    private CanvasGroup canvasGroup;

    // 当前正在运行的渐变协程
    private Coroutine currentFadeCoroutine;

    void Awake()
    {
        // 获取挂载在同一个游戏对象上的CanvasGroup组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasFader requires a CanvasGroup component on the same GameObject.");
        }
    }

    private void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 公共方法，用于启动渐隐或渐显效果
    /// </summary>
    /// <param name="fadeIn">传入true为渐显，false为渐隐</param>
    /// <param name="duration">渐变过程的持续时间（秒）</param>
    public void Fade(bool fadeIn)
    {
        // 如果有正在运行的渐变，先停止它
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        // 根据传入的布尔值确定目标Alpha值
        float targetAlpha = fadeIn ? 1f : 0f;

        // 启动新的渐变协程
        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    /// <summary>
    /// 执行渐变效果的协程
    /// </summary>
    /// <param name="targetAlpha">目标Alpha值 (0 或 1)</param>
    /// <param name="duration">渐变持续时间</param>
    /// <returns></returns>
    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        // 记录开始时的Alpha值
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        // 在持续时间内，平滑地改变Alpha值
        while (time < duration)
        {
            // 使用Lerp进行线性插值，实现平滑过渡
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            
            // 时间累加
            time += Time.deltaTime;
            
            // 等待下一帧
            yield return null;
        }

        // 确保最终Alpha值精确地设置为目标值
        canvasGroup.alpha = targetAlpha;

        // 渐变完成后，可以根据需要设置interactable和blocksRaycasts属性
        // 渐显时，允许交互
        if (targetAlpha == 1f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        // 渐隐后，禁止交互
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 协程结束
        currentFadeCoroutine = null;
    }
}
