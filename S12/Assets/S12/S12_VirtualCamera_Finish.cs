using System.Collections.Generic;
using UnityEngine;

// A second, manually projected camera. Its UI wires should sit on the real camera image.
[ExecuteAlways]
[DefaultExecutionOrder(200)]
public sealed class S12_VirtualCamera_Finish : MonoBehaviour
{
    [SerializeField, Range(1f, 179f)] private float fieldOfView = 90f;
    [SerializeField, Min(0.01f)] private float near = 0.3f;
    [SerializeField, Min(0.02f)] private float far = 20f;
    [SerializeField] private MeshFilter[] targets;
    [SerializeField] private S12_CanvasWireGraphic drawingCanvas;
    [SerializeField] private Camera mainCamera;

    public float FieldOfView => fieldOfView;
    public float Near => near;
    public float Far => far;
    public MeshFilter[] Targets => targets;
    public S12_CanvasWireGraphic DrawingCanvas => drawingCanvas;
    public Camera MainCamera => mainCamera;

    public void Configure(float fov, float nearPlane, float farPlane, MeshFilter[] meshTargets,
        S12_CanvasWireGraphic canvas, Camera camera)
    {
        fieldOfView = fov;
        near = nearPlane;
        far = farPlane;
        targets = meshTargets;
        drawingCanvas = canvas;
        mainCamera = camera;
        RefreshCanvas();
    }

    public Matrix4x4 BuildViewMatrix()
    {
        Matrix4x4 inverseRotation = Matrix4x4.Rotate(Quaternion.Inverse(transform.rotation));
        Matrix4x4 inverseTranslation = Matrix4x4.Translate(-transform.position);
        return MultiplyMatrixMatrix(inverseRotation, inverseTranslation);
    }

    public static Matrix4x4 MultiplyMatrixMatrix(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int column = 0; column < 4; column++)
        {
            Vector4 v = b.GetColumn(column);
            result.SetColumn(column, new Vector4(
                a.m00 * v.x + a.m01 * v.y + a.m02 * v.z + a.m03 * v.w,
                a.m10 * v.x + a.m11 * v.y + a.m12 * v.z + a.m13 * v.w,
                a.m20 * v.x + a.m21 * v.y + a.m22 * v.z + a.m23 * v.w,
                a.m30 * v.x + a.m31 * v.y + a.m32 * v.z + a.m33 * v.w));
        }
        return result;
    }

    // Camera space looks down +z. After division by w=z, x and y are NDC coordinates.
    public Matrix4x4 BuildProjectionMatrix(float aspect)
    {
        float scale = 1f / Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
        Matrix4x4 projection = Matrix4x4.zero;
        projection.m00 = scale / aspect;
        projection.m11 = scale;
        projection.m22 = far / (far - near);
        projection.m23 = -near * far / (far - near);
        projection.m32 = 1f;
        return projection;
    }

    public Vector3 ProjectWorldToViewport(Vector3 worldPoint, float aspect)
    {
        Vector3 cameraPoint = BuildViewMatrix().MultiplyPoint3x4(worldPoint);
        if (cameraPoint.z <= 0f) return new Vector3(float.NaN, float.NaN, cameraPoint.z);
        Vector4 clip = BuildProjectionMatrix(aspect) * new Vector4(cameraPoint.x, cameraPoint.y, cameraPoint.z, 1f);
        return new Vector3(0.5f + 0.5f * clip.x / clip.w,
            0.5f + 0.5f * clip.y / clip.w, cameraPoint.z);
    }

    private void LateUpdate() => RefreshCanvas();

    public void RefreshCanvas()
    {
        if (drawingCanvas == null || targets == null || mainCamera == null) return;
        Rect rect = drawingCanvas.rectTransform.rect;
        if (rect.height <= 0f || rect.width <= 0f) return;
        float aspect = mainCamera.aspect;
        Matrix4x4 view = BuildViewMatrix();
        Matrix4x4 projection = BuildProjectionMatrix(aspect);
        List<S12_CanvasWireGraphic.Segment> segments = new List<S12_CanvasWireGraphic.Segment>();

        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            MeshFilter filter = targets[targetIndex];
            if (filter == null || filter.sharedMesh == null || !filter.gameObject.activeInHierarchy) continue;
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Matrix4x4 model = filter.transform.localToWorldMatrix;
            HashSet<ulong> seen = new HashSet<ulong>();
            Color color = WireColor(targetIndex);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                AddEdge(triangles[i], triangles[i + 1]);
                AddEdge(triangles[i + 1], triangles[i + 2]);
                AddEdge(triangles[i + 2], triangles[i]);
            }

            void AddEdge(int ia, int ib)
            {
                int lo = Mathf.Min(ia, ib);
                int hi = Mathf.Max(ia, ib);
                if (!seen.Add(((ulong)(uint)lo << 32) | (uint)hi)) return;
                Vector3 a = view.MultiplyPoint3x4(model.MultiplyPoint3x4(vertices[ia]));
                Vector3 b = view.MultiplyPoint3x4(model.MultiplyPoint3x4(vertices[ib]));
                if (!ClipDepth(ref a, ref b)) return;
                Vector2 pa = PixelPoint(a);
                Vector2 pb = PixelPoint(b);
                segments.Add(new S12_CanvasWireGraphic.Segment(pa, pb, color));
            }

            Vector2 PixelPoint(Vector3 point)
            {
                Vector4 clip = projection * new Vector4(point.x, point.y, point.z, 1f);
                return new Vector2(clip.x / clip.w * rect.width * 0.5f,
                    clip.y / clip.w * rect.height * 0.5f);
            }
        }
        drawingCanvas.SetSegments(segments);
    }

    private bool ClipDepth(ref Vector3 a, ref Vector3 b)
    {
        if ((a.z < near && b.z < near) || (a.z > far && b.z > far)) return false;
        Vector3 start = a;
        Vector3 end = b;
        float dz = end.z - start.z;
        if (Mathf.Abs(dz) < 0.00001f) return start.z >= near && start.z <= far;
        float t0 = Mathf.Max(0f, Mathf.Min((near - start.z) / dz, (far - start.z) / dz));
        float t1 = Mathf.Min(1f, Mathf.Max((near - start.z) / dz, (far - start.z) / dz));
        if (t0 > t1) return false;
        a = Vector3.Lerp(start, end, t0);
        b = Vector3.Lerp(start, end, t1);
        return true;
    }

    private static Color WireColor(int index)
    {
        Color[] colors =
        {
            new Color(0.12f, 1f, 0.93f, 0.9f),
            new Color(1f, 0.76f, 0.3f, 0.9f),
            new Color(1f, 0.45f, 0.65f, 0.9f),
            new Color(0.62f, 0.7f, 1f, 0.9f)
        };
        return colors[index % colors.Length];
    }
}
