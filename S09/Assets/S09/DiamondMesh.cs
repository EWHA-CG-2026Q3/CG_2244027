using UnityEngine;

// The same six-vertex, eight-triangle diamond used as the unmodified reference.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class DiamondMesh : MonoBehaviour
{
    private static readonly Vector3[] OriginalVertices =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(1f, 0f, 0f),
        new Vector3(1f, 0f, 1f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0.5f, 1f, 0.5f),
        new Vector3(0.5f, -1f, 0.5f)
    };

    private static readonly int[] Triangles =
    {
        4, 1, 0, 4, 2, 1, 4, 3, 2, 4, 0, 3,
        5, 0, 1, 5, 1, 2, 5, 2, 3, 5, 3, 0
    };

    private Mesh generatedMesh;
    public Vector3[] BaseVertices => OriginalVertices;

    private void OnEnable()
    {
        generatedMesh = new Mesh { name = "S09 Diamond (6 vertices, 8 triangles)" };
        generatedMesh.hideFlags = HideFlags.DontSave;
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        SetVertices(OriginalVertices);
        S09_ShearDiamond shear = GetComponent<S09_ShearDiamond>();
        if (shear != null) shear.ReapplyShear();
    }

    private void OnDisable()
    {
        if (generatedMesh == null) return;
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh == generatedMesh)
            filter.sharedMesh = null;
        if (Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }

    public void SetVertices(Vector3[] vertices)
    {
        if (generatedMesh == null || vertices == null || vertices.Length != OriginalVertices.Length)
            return;
        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = Triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
    }
}
