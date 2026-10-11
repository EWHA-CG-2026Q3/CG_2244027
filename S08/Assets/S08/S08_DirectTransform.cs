using UnityEngine;

// Applies the two operations directly to a cube's original vertex positions.
// The GameObject itself stays at its reference origin in the scene.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class S08_DirectTransform : MonoBehaviour
{
    public enum DemoMode
    {
        TranslateThenScale,
        ScaleThenTranslate
    }

    [SerializeField] private DemoMode mode;
    [SerializeField] private Vector3 translation = new Vector3(0.9f, 0f, 0f);
    [SerializeField] private Vector3 scale = Vector3.one * 1.8f;
    [SerializeField, HideInInspector] private Mesh sourceMesh;

    private Mesh generatedMesh;
    private DemoMode appliedMode;
    private Vector3 appliedTranslation;
    private Vector3 appliedScale;

    public DemoMode Mode => mode;
    public Vector3 Translation => translation;
    public Vector3 Scale => scale;

    public Vector3 TransformPoint(Vector3 original)
    {
        switch (mode)
        {
            case DemoMode.TranslateThenScale:
                return Vector3.Scale(original + translation, scale);
            case DemoMode.ScaleThenTranslate:
                return Vector3.Scale(original, scale) + translation;
            default:
                return original;
        }
    }

    public void Configure(DemoMode selectedMode, Vector3 move, Vector3 resize)
    {
        mode = selectedMode;
        translation = move;
        scale = resize;
        RebuildMesh();
    }

    private void OnEnable() => RebuildMesh();

    private void Update()
    {
        if (generatedMesh == null || mode != appliedMode ||
            translation != appliedTranslation || scale != appliedScale)
            RebuildMesh();
    }

    private void OnDisable()
    {
        if (generatedMesh == null) return;
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh == generatedMesh)
            filter.sharedMesh = sourceMesh;
        if (Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }

    private void RebuildMesh()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter == null) return;
        if (sourceMesh == null && filter.sharedMesh != generatedMesh)
            sourceMesh = filter.sharedMesh;
        if (sourceMesh == null) return;

        if (generatedMesh == null)
        {
            generatedMesh = Instantiate(sourceMesh);
            generatedMesh.name = "S08 direct-transform result";
            generatedMesh.hideFlags = HideFlags.DontSave;
        }

        Vector3[] vertices = sourceMesh.vertices;
        for (int i = 0; i < vertices.Length; ++i)
            vertices[i] = TransformPoint(vertices[i]);
        generatedMesh.vertices = vertices;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        filter.sharedMesh = generatedMesh;
        appliedMode = mode;
        appliedTranslation = translation;
        appliedScale = scale;
    }
}
