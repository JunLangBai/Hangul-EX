using System.Collections;
using UnityEngine;

namespace AZ
{
    public class OverrideAnimationManager : MonoBehaviour
    {
        [Header("核心组件")]
        public Animator characterAnimator;

        private Coroutine idleCoroutine;

        // Awake 里的那些 OverrideController 初始化全删了，用不着

        /// <summary>
        /// 直接播放指定的动画状态
        /// </summary>
        /// <param name="stateName">Animator里State的名字 (注意：是状态机里的State名，不是Trigger名)</param>
        /// <param name="delayBeforeIdle">播放多久后触发 DoIdle (秒)</param>
        public void PlaySpecificAnimation(string stateName, float delayBeforeIdle)
        {
            if (characterAnimator == null) return;

            // 直接播放指定名称的状态
            // 注意：Play传的是状态机里那个方块(State)的名字，而不是连线上的Trigger名字
            characterAnimator.Play(stateName);
        
            // 处理回正逻辑
            if (idleCoroutine != null) StopCoroutine(idleCoroutine);
            idleCoroutine = StartCoroutine(WaitAndTriggerIdle(delayBeforeIdle));
        }

        private IEnumerator WaitAndTriggerIdle(float delay)
        {
            yield return new WaitForSeconds(delay);
            // 这里假设 DoIdle 依然是一个 Trigger 参数，用来切回待机
            characterAnimator.SetTrigger("DoIdle");
            idleCoroutine = null;
        }
    }
}