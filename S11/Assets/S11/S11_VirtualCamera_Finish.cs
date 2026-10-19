using System.Collections.Generic;
using UnityEngine;

// Projects mesh edges onto a real Unity UI Canvas using a manually built view matrix.
[ExecuteAlways]
[DefaultExecutionOrder(200)]
public sealed class S11_VirtualCamera_Finish : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float size = 2f;
    [SerializeField, Min(0.01f)] private float near = 0.3f;
    [SerializeField, Min(0.02f)] private float far = 20f;
    [SerializeField] private MeshFilter[] targets;
    [SerializeField] private S11_CanvasWireGraphic drawingCanvas;
    [SerializeField] private Camera mainCamera;

    public float Size => size;
    public float Near => near;
    public float Far => far;
    public MeshFilter[] Targets => targets;
    public S11_CanvasWireGraphic DrawingCanvas => drawingCanvas;
    public Camera MainCamera => mainCamera;

    public void Configure(float viewSize, float nearPlane, float farPlane,
        MeshFilter[] drawingTargets, S11_CanvasWireGraphic canvas, Camera camera)
    {
        size = viewSize;
        near = nearPlane;
        far = farPlane;
        targets = drawingTargets;
        drawingCanvas = canvas;
        mainCamera = camera;
        RefreshCanvas();
    }

    // Multiply one column of B at a time; no Matrix4x4 * Matrix4x4 is used.
    public static Matrix4x4 MultiplyMatrixMatrix(Matrix4x4 A, Matrix4x4 B)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int column = 0; column < 4; ++column)
        {
            Vector4 b = B.GetColumn(column);
            result.SetColumn(column, new Vector4(
                A.m00 * b.x + A.m01 * b.y + A.m02 * b.z + A.m03 * b.w,
                A.m10 * b.x + A.m11 * b.y + A.m12 * b.z + A.m13 * b.w,
                A.m20 * b.x + A.m21 * b.y + A.m22 * b.z + A.m23 * b.w,
                A.m30 * b.x + A.m31 * b.y + A.m32 * b.z + A.m33 * b.w));
        }
        return result;
    }

    public Matrix4x4 BuildViewMatrix()
    {
        Matrix4x4 Rinv = Matrix4x4.Rotate(Quaternion.Inverse(transform.rotation));
        Matrix4x4 Tinv = Matrix4x4.Translate(-transform.position);
        return MultiplyMatrixMatrix(Rinv, Tinv);
    }

    public Vector3 ProjectWorldToViewport(Vector3 worldPoint, float aspect)
    {
        Vector3 cameraPoint = BuildViewMatrix().MultiplyPoint3x4(worldPoint);
        return new Vector3(
            0.5f + cameraPoint.x / (2f * size * aspect),
            0.5f + cameraPoint.y / (2f * size),
            cameraPoint.z);
    }

    private void LateUpdate() => RefreshCanvas();

    public void RefreshCanvas()
    {
        if (drawingCanvas == null || targets == null || size <= 0f) return;
        float canvasHeight = drawingCanvas.rectTransform.rect.height;
        if (canvasHeight <= 0f) canvasHeight = Mathf.Max(1f, Screen.height);
        float pixelsPerWorldUnit = canvasHeight / (2f * size);
        Matrix4x4 view = BuildViewMatrix();
        List<S11_CanvasWireGraphic.Segment> segments = new List<S11_CanvasWireGraphic.Segment>();

        for (int targetIndex = 0; targetIndex < targets.Length; ++targetIndex)
        {
            MeshFilter filter = targets[targetIndex];
            if (filter == null || filter.sharedMesh == null || !filter.gameObject.activeInHierarchy) continue;
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Matrix4x4 model = filter.transform.localToWorldMatrix;
            Color color = WireColor(targetIndex);
            HashSet<ulong> seenEdges = new HashSet<ulong>();
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                AddEdge(triangles[i], triangles[i + 1]);
                AddEdge(triangles[i + 1], triangles[i + 2]);
                AddEdge(triangles[i + 2], triangles[i]);
            }

            void AddEdge(int indexA, int indexB)
            {
                int low = Mathf.Min(indexA, indexB);
                int high = Mathf.Max(indexA, indexB);
                ulong key = ((ulong)(uint)low << 32) | (uint)high;
                if (!seenEdges.Add(key)) return;
                Vector3 a = view.MultiplyPoint3x4(model.MultiplyPoint3x4(vertices[indexA]));
                Vector3 b = view.MultiplyPoint3x4(model.MultiplyPoint3x4(vertices[indexB]));
                if (a.z < near || b.z < near || a.z > far || b.z > far) return;
                segments.Add(new S11_CanvasWireGraphic.Segment(
                    new Vector2(a.x, a.y) * pixelsPerWorldUnit,
                    new Vector2(b.x, b.y) * pixelsPerWorldUnit,
                    color));
            }
        }
        drawingCanvas.SetSegments(segments);
    }

    private static Color WireColor(int targetIndex)
    {
        Color[] palette =
        {
            new Color(0.18f, 1f, 0.94f, 0.82f),
            new Color(1f, 0.82f, 0.42f, 0.82f),
            new Color(1f, 0.48f, 0.72f, 0.88f),
            new Color(0.63f, 0.7f, 1f, 0.88f)
        };
        return palette[targetIndex % palette.Length];
    }
}
