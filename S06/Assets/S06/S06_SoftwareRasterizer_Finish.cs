using UnityEngine;

/// <summary>
/// Draws an asymmetrical triangle by testing every pixel against its three edges.
/// The vertices use normalized coordinates, with (0, 0) at the bottom-left.
/// </summary>
[ExecuteAlways]
public class S06_SoftwareRasterizer_Finish : MonoBehaviour
{
    private const int TextureWidth = 960;
    private const int TextureHeight = 540;

    // Deliberately unequal edges: this is not the template's regular triangle.
    private static readonly Vector2 VertexA = new Vector2(0.14f, 0.18f);
    private static readonly Vector2 VertexB = new Vector2(0.87f, 0.32f);
    private static readonly Vector2 VertexC = new Vector2(0.58f, 0.88f);

    private static readonly Color ColorA = new Color(1.00f, 0.28f, 0.38f);
    private static readonly Color ColorB = new Color(0.10f, 0.86f, 0.78f);
    private static readonly Color ColorC = new Color(0.43f, 0.42f, 1.00f);
    private static readonly Color Background = new Color(0.025f, 0.035f, 0.075f);

    private Texture2D rasterizedImage;

    private void OnEnable()
    {
        Rasterize();
    }

    private void OnDisable()
    {
        if (rasterizedImage != null)
        {
            DestroyImmediate(rasterizedImage);
            rasterizedImage = null;
        }
    }

    private void OnGUI()
    {
        if (rasterizedImage == null)
        {
            Rasterize();
        }

        if (rasterizedImage != null)
        {
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                rasterizedImage,
                ScaleMode.StretchToFill
            );
        }
    }

    private void Rasterize()
    {
        if (rasterizedImage != null)
        {
            DestroyImmediate(rasterizedImage);
        }

        rasterizedImage = new Texture2D(
            TextureWidth,
            TextureHeight,
            TextureFormat.RGBA32,
            false
        )
        {
            name = "S06_CPU_Rasterized_Triangle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[TextureWidth * TextureHeight];
        float totalArea = Edge(VertexA, VertexB, VertexC);

        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / TextureWidth,
                    (y + 0.5f) / TextureHeight
                );

                float weightA = Edge(VertexB, VertexC, point) / totalArea;
                float weightB = Edge(VertexC, VertexA, point) / totalArea;
                float weightC = Edge(VertexA, VertexB, point) / totalArea;

                bool isInside = weightA >= 0f && weightB >= 0f && weightC >= 0f;
                pixels[y * TextureWidth + x] = isInside
                    ? weightA * ColorA + weightB * ColorB + weightC * ColorC
                    : Background;
            }
        }

        rasterizedImage.SetPixels(pixels);
        rasterizedImage.Apply(false, false);
    }

    private static float Edge(Vector2 start, Vector2 end, Vector2 point)
    {
        return (end.x - start.x) * (point.y - start.y)
             - (end.y - start.y) * (point.x - start.x);
    }
}
