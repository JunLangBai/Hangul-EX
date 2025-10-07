using Rokid.UXR.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider), typeof(MeshFilter), typeof(MeshRenderer))]
public class GroundPainterContinuous : MonoBehaviour,
    IRayBeginDrag, IRayDragToTarget, IRayEndDrag
{
    [Header("笔刷设置")]
    public Texture2D brushTex;          // 圆形白色笔刷贴图，背景透明
    public float brushSize = 0.05f;     // 笔刷大小
    public Color brushColor = Color.red;

    [Header("材质设置")]
    public Material targetMaterial;     // 使用的材质，Shader 要有 _DrawTex 属性

    private RenderTexture drawTex;
    private Material paintMat;
    private bool isDrawing = false;

    private Vector3 lastWorldPoint;     // 上一次世界坐标，用于连续插值

    void Start()
    {
        // 创建绘制 RenderTexture
        drawTex = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
        drawTex.wrapMode = TextureWrapMode.Clamp;
        targetMaterial.SetTexture("_DrawTex", drawTex);

        // 创建 Shader 材质
        paintMat = new Material(Shader.Find("Hidden/BrushBlit"));

        // 清空画布
        ClearTexture();
    }

    private void ClearTexture()
    {
        RenderTexture.active = drawTex;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
    }

    void Update()
    {
        // 每帧绘制连续线条
        if (isDrawing)
        {
            Vector3 currentPoint = lastWorldPoint; // 默认用上一帧
            // 在实际使用中，lastWorldPoint 会在 OnRayDragToTarget 更新
            // 如果你有手势系统提供实时位置，可以在这里替换 currentPoint

            // 计算 UV 并绘制线
            Vector2 uvFrom = WorldPosToUV(lastWorldPoint);
            Vector2 uvTo = WorldPosToUV(currentPoint);
            DrawLine(uvFrom, uvTo);

            lastWorldPoint = currentPoint;
        }
    }

    public void OnRayBeginDrag(PointerEventData eventData)
    {
        isDrawing = true;
        lastWorldPoint = eventData.pointerCurrentRaycast.worldPosition;
        DrawAt(WorldPosToUV(lastWorldPoint));
    }

    public void OnRayDragToTarget(Vector3 targetPoint)
    {
        // 只更新最新世界坐标，不直接绘制
        lastWorldPoint = targetPoint;
    }

    public void OnRayEndDrag(PointerEventData eventData)
    {
        isDrawing = false;
    }

    // 绘制单个笔刷点
    private void DrawAt(Vector2 uv)
    {
        paintMat.SetTexture("_BrushTex", brushTex);
        paintMat.SetColor("_Color", brushColor);
        paintMat.SetVector("_Coord", new Vector4(uv.x, uv.y, brushSize, brushSize));

        RenderTexture temp = RenderTexture.GetTemporary(drawTex.width, drawTex.height, 0);
        Graphics.Blit(drawTex, temp);
        Graphics.Blit(temp, drawTex, paintMat);
        RenderTexture.ReleaseTemporary(temp);
    }

    // 绘制连续线条
    private void DrawLine(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0) return;

        int steps = Mathf.CeilToInt(dist / (brushSize * 0.25f));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 uvStep = from + dir * (i / (float)steps);
            DrawAt(uvStep);
        }
    }

    // 世界坐标 → UV
    private Vector2 WorldPosToUV(Vector3 worldPos)
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return Vector2.zero;

        Mesh mesh = mf.sharedMesh;
        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];

            Vector2 uv0 = uvs[tris[i]];
            Vector2 uv1 = uvs[tris[i + 1]];
            Vector2 uv2 = uvs[tris[i + 2]];

            Vector3 bary = Barycentric(localPos, v0, v1, v2);
            if (bary.x >= 0 && bary.y >= 0 && bary.z >= 0)
            {
                return uv0 * bary.x + uv1 * bary.y + uv2 * bary.z;
            }
        }

        return Vector2.zero;
    }

    private Vector3 Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) < 1e-6f) return new Vector3(-1, -1, -1);
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        return new Vector3(u, v, w);
    }
}
