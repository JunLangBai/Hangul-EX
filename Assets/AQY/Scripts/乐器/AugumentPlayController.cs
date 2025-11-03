using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AugumentPlayController : MonoBehaviour
{
    public SpriteRenderer voiceVisable;
    public AudioSource audioSource;
    public List<GameObject> drumstick;

    private void OnTriggerEnter(Collider other)
    {
        // 步骤 1: 获取进入触发器的物体
        GameObject enteringObject = other.gameObject;

        // 步骤 2: 检查进入的物体是否在“白名单”列表中
        if (drumstick.Contains(enteringObject))
        {
            // 如果在列表中，则执行触发逻辑
            voiceVisable.color = Color.green;
            audioSource.Play();
        }
    }
}
