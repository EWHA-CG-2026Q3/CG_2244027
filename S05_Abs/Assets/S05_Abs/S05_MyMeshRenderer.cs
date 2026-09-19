using UnityEngine;
using UnityEngine.UI;

public class S05_MyMeshRenderer : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color backgroundColor = new Color(1f, 0f, 0f, 1f);

    private Texture2D canvasTexture;

    private void Start()
    {
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);
        canvasTexture.filterMode = FilterMode.Point;

        FillBackground(backgroundColor);
        canvasTexture.Apply();

        GetComponent<RawImage>().texture = canvasTexture;
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

    private void OnDestroy()
    {
        if (canvasTexture != null)
        {
            Destroy(canvasTexture);
        }
    }
}
