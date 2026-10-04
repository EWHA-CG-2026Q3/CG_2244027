using UnityEngine;
using UnityEngine.UI;

// Smaller z means a fragment is closer to the camera.
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class S07_DepthTest2 : MonoBehaviour
{
    private const int CanvasSize = 256;
    private static readonly Color Background = new Color(0.025f, 0.035f, 0.075f, 1f);

    // The pink triangle becomes farther from left to right.
    private static readonly Vector3 PinkA = new Vector3(36f, 42f, 0.10f);
    private static readonly Vector3 PinkB = new Vector3(224f, 76f, 0.90f);
    private static readonly Vector3 PinkC = new Vector3(118f, 226f, 0.50f);

    // The cyan triangle becomes closer from left to right.
    private static readonly Vector3 CyanA = new Vector3(42f, 220f, 0.90f);
    private static readonly Vector3 CyanB = new Vector3(220f, 212f, 0.10f);
    private static readonly Vector3 CyanC = new Vector3(138f, 24f, 0.50f);

    private static readonly Color Pink = new Color(1f, 0.26f, 0.42f, 1f);
    private static readonly Color Cyan = new Color(0.12f, 0.83f, 1f, 1f);
    private Texture2D canvas;

    private void OnEnable() { Render(); }

    private void OnDisable()
    {
        GetComponent<RawImage>().texture = null;
        if (canvas == null) return;
        if (Application.isPlaying) Destroy(canvas);
        else DestroyImmediate(canvas);
        canvas = null;
    }

    private void Render()
    {
        if (canvas != null) DestroyImmediate(canvas);
        canvas = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false);
        canvas.name = "S07_Depth_Buffer_Result";
        canvas.filterMode = FilterMode.Point;
        canvas.wrapMode = TextureWrapMode.Clamp;
        canvas.hideFlags = HideFlags.HideAndDontSave;

        float[] depth = new float[CanvasSize * CanvasSize];
        for (int i = 0; i < depth.Length; i++) depth[i] = float.PositiveInfinity;
        for (int y = 0; y < CanvasSize; y++)
            for (int x = 0; x < CanvasSize; x++) canvas.SetPixel(x, y, Background);

        DrawTriangle(PinkA, PinkB, PinkC, Pink, depth);
        DrawTriangle(CyanA, CyanB, CyanC, Cyan, depth);
        canvas.Apply(false, false);
        GetComponent<RawImage>().texture = canvas;
    }

    private void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, float[] depth)
    {
        float area = Cross(b - a, c - a);
        if (Mathf.Approximately(area, 0f)) return;

        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)));
        int maxX = Mathf.Min(CanvasSize - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)));
        int maxY = Mathf.Min(CanvasSize - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)));
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Vector3 p3 = new Vector3(p.x, p.y, 0f);
                float wa = Cross(b - p3, c - p3) / area;
                float wb = Cross(c - p3, a - p3) / area;
                float wc = 1f - wa - wb;
                if (wa < 0f || wb < 0f || wc < 0f) continue;

                // TODO 1: interpolate z from the three vertex depths.
                float z = wa * a.z + wb * b.z + wc * c.z;
                int index = y * CanvasSize + x;
                // TODO 2: update color and depth only if this fragment is closer.
                if (z >= depth[index]) continue;
                depth[index] = z;
                canvas.SetPixel(x, y, color);
            }
    }

    private static float Cross(Vector3 u, Vector3 v) { return u.x * v.y - u.y * v.x; }
}
