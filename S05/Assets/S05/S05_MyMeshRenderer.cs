using UnityEngine;
using UnityEngine.UI;

// S05_MeshRenderer
// 강의 템플릿의 줄무늬와 체스판 함수를 완성한 정규 과제입니다.

public class S05_MyMeshRenderer : MonoBehaviour
{
    public enum Pattern { VerticalStripes, Checkerboard }
    [SerializeField] private Pattern pattern = Pattern.VerticalStripes;
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 1f);

    [Header("무늬 실습 (줄무늬·체스판 공용)")]
    [SerializeField] private int patternSize = 16;
    [SerializeField] private Color colorA = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color colorB = new Color(0.3f, 0.5f, 0.8f, 1f);

    private Texture2D canvasTexture;
    private RawImage targetImage;

    void Start()
    {
        targetImage = GetComponent<RawImage>();

        // 1. 빈 캔버스(Texture2D) 생성
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);

        // 2. 픽셀 경계를 흐리지 않게
        canvasTexture.filterMode = FilterMode.Point;

        // 3. 픽셀 채우기 (실습①. 완료 후 아래 줄로 교체해 체스판도 확인해보세요.)
        if (pattern == Pattern.VerticalStripes)
            FillVerticalStripes(patternSize, colorA, colorB);
        else
            FillCheckerboard(patternSize, colorA, colorB);

        // 4. 변경 사항 반영
        canvasTexture.Apply();

        // 5. 화면에 표시
        targetImage.texture = canvasTexture;
    }

    // 참고 예시 ① — 캔버스 전체를 한 가지 색으로 채움 (모든 픽셀이 같은 값, 이미 완성됨)
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

    // 참고 예시 ② — 각 픽셀을 독립적으로 무작위 색으로 채움 (이미 완성됨)
    private void FillRandom()
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                Color randomColor = new Color(Random.value, Random.value, Random.value, 1f);
                canvasTexture.SetPixel(x, y, randomColor);
            }
        }
    }

    // 실습① — 세로 줄무늬. 반복문 구조는 주어져 있습니다. 조건식 한 줄만 채우세요.
    private void FillVerticalStripes(int width, Color colorA, Color colorB)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            // x 구역의 번호가 짝수이면 A, 홀수이면 B를 선택합니다.
            bool isColorA = (x / width) % 2 == 0;

            Color stripeColor = isColorA ? colorA : colorB;
            for (int y = 0; y < canvasHeight; y++)
                canvasTexture.SetPixel(x, y, stripeColor);
        }
    }

    // 실습② — 체스판 무늬. 이번에는 반복문 뼈대만 주어집니다.
    // 위에서 만든 줄무늬 조건을 x, y 둘 다에 적용하는 방식으로 직접 확장해보세요.
    private void FillCheckerboard(int size, Color colorA, Color colorB)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                // 가로와 세로 구역 번호의 합으로 인접한 칸의 색을 번갈아 선택합니다.
                bool isColorA = ((x / size) + (y / size)) % 2 == 0;
                canvasTexture.SetPixel(x, y, isColorA ? colorA : colorB);
            }
        }
    }

    private void OnDestroy()
    {
        if (canvasTexture != null)
        {
            if (Application.isPlaying) Destroy(canvasTexture);
            else DestroyImmediate(canvasTexture);
        }
    }
}
