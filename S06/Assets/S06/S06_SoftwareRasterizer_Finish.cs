using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lecture-style software rasterizer: visit each texel, decide whether its
/// center lies inside the triangle, and display the result in a RawImage.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class S06_SoftwareRasterizer_Finish : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;

    // Changed from the lecture's A(128,200), B(60,60), C(200,60).
    // All three edges now have different lengths and the base is tilted.
    [SerializeField] private Vector2 vertexA = new Vector2(29f, 50f);
    [SerializeField] private Vector2 vertexB = new Vector2(222f, 77f);
    [SerializeField] private Vector2 vertexC = new Vector2(148f, 226f);

    // Changed from the lecture's orange fill.
    [SerializeField] private Color fillColor = new Color(1f, 0.27f, 0.40f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.025f, 0.035f, 0.075f, 1f);

    private Texture2D canvasTexture;

    private void OnEnable()
    {
        DrawCanvas();
    }

    private void OnDisable()
    {
        if (canvasTexture != null)
        {
            DestroyImmediate(canvasTexture);
            canvasTexture = null;
        }
    }

    private void DrawCanvas()
    {
        RawImage targetImage = GetComponent<RawImage>();
        if (targetImage == null)
        {
            return;
        }

        if (canvasTexture != null)
        {
            DestroyImmediate(canvasTexture);
        }

        canvasTexture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false)
        {
            name = "S06_Barycentric_Pixel_Canvas",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        FillBackground(backgroundColor);
        DrawTriangle(vertexA, vertexB, vertexC, fillColor);
        canvasTexture.Apply(false, false);
        targetImage.texture = canvasTexture;
    }

    private void FillBackground(Color color)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                canvasTexture.SetPixel(x, y, color);
            }
        }
    }

    private void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                Vector2 pixelCenter = new Vector2(x + 0.5f, y + 0.5f);
                if (IsInsideTriangle(pixelCenter, a, b, c))
                {
                    canvasTexture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float denom = a.x * (b.y - c.y)
                    + b.x * (c.y - a.y)
                    + c.x * (a.y - b.y);

        if (Mathf.Approximately(denom, 0f))
        {
            return false;
        }

        float w1 = (p.x * (b.y - c.y)
                  + b.x * (c.y - p.y)
                  + c.x * (p.y - b.y)) / denom;

        float w2 = (a.x * (p.y - c.y)
                  + p.x * (c.y - a.y)
                  + c.x * (a.y - p.y)) / denom;

        float w3 = 1f - w1 - w2;
        return w1 >= 0f && w2 >= 0f && w3 >= 0f;
    }
}
