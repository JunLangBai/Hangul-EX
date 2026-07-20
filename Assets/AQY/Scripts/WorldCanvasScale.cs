using UnityEngine;

public class WorldCanvasScaler : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    // 当距离相机为0.5米时，希望Canvas保持这个Scale
    [SerializeField] private float referenceDistance = 0.5f;
    [SerializeField] private float referenceScale = 0.0004156749f;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        float distance = Vector3.Distance(transform.position, targetCamera.transform.position);

        float scale = referenceScale * distance / referenceDistance;

        transform.localScale = Vector3.one * scale;
    }
}