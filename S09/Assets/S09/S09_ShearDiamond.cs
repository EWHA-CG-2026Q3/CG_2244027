using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public sealed class S09_ShearDiamond : MonoBehaviour
{
    public static readonly Vector3 TopVertex = new Vector3(0.5f, 1f, 0.5f);

    [SerializeField] private float shearK = 1.6f;
    private float appliedK = float.NaN;

    public float K => shearK;

    // Matrix4x4 receives its four columns. e2=(0,1,0) becomes (k,1,0).
    public static Matrix4x4 ShearMatrixRaw(float k)
    {
        return new Matrix4x4(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(k, 1f, 0f, 0f),
            new Vector4(0f, 0f, 1f, 0f),
            new Vector4(0f, 0f, 0f, 1f));
    }

    public void SetK(float value)
    {
        shearK = value;
        ApplyShear();
    }

    public void ReapplyShear() => ApplyShear();

    public Vector3 TransformPoint(Vector3 original)
    {
        return ShearMatrixRaw(shearK).MultiplyPoint3x4(original);
    }

    public void LogTopVertex()
    {
        Vector3 result = TransformPoint(TopVertex);
        Debug.Log($"[S09] k={shearK:+0.0;-0.0;0.0} | top (0.5, 1, 0.5) -> " +
                  $"({result.x:0.0}, {result.y:0.0}, {result.z:0.0})", this);
    }

    private void OnEnable() => ApplyShear();

    private void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        bool staleMesh = mesh == null || mesh.vertexCount != 6 ||
                         (mesh.vertices[4] - TransformPoint(TopVertex)).sqrMagnitude > 0.0000001f;
        if (float.IsNaN(appliedK) || !Mathf.Approximately(appliedK, shearK) || staleMesh)
            ApplyShear();
    }

    private void Start()
    {
        if (Application.isPlaying) LogTopVertex();
    }

    private void ApplyShear()
    {
        DiamondMesh diamond = GetComponent<DiamondMesh>();
        if (diamond == null) return;
        Vector3[] source = diamond.BaseVertices;
        Vector3[] result = new Vector3[source.Length];
        Matrix4x4 shear = ShearMatrixRaw(shearK);
        for (int i = 0; i < source.Length; ++i)
            result[i] = shear.MultiplyPoint3x4(source[i]);
        diamond.SetVertices(result);
        appliedK = shearK;
    }
}
