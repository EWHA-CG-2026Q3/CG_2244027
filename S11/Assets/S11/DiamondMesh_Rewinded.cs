using UnityEngine;

// A six-vertex bipyramid centred at the origin. The chain component supplies
// world-space vertices, so this GameObject deliberately stays parentless.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class DiamondMesh_Rewinded : MonoBehaviour
{
    private static readonly Vector3[] OriginalVertices =
    {
        new Vector3(0f, -0.5f, 0f),
        new Vector3(-0.5f, 0f, -0.5f),
        new Vector3(0.5f, 0f, -0.5f),
        new Vector3(0.5f, 0f, 0.5f),
        new Vector3(-0.5f, 0f, 0.5f),
        new Vector3(0f, 0.5f, 0f)
    };

    private static readonly int[] Triangles =
    {
        0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1,
        5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4
    };

    private Mesh generatedMesh;
    public Vector3[] BaseVertices => OriginalVertices;

    private void OnEnable()
    {
        generatedMesh = new Mesh { name = "S10 Rewound Diamond" };
        generatedMesh.hideFlags = HideFlags.DontSave;
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        SetVertices(OriginalVertices);
        S10_DiamondChain_Finish chain = GetComponent<S10_DiamondChain_Finish>();
        if (chain != null) chain.Refresh();
    }

    private void OnDisable()
    {
        if (generatedMesh == null) return;
        if (GetComponent<MeshFilter>().sharedMesh == generatedMesh)
            GetComponent<MeshFilter>().sharedMesh = null;
        if (Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }

    public void SetVertices(Vector3[] vertices)
    {
        if (generatedMesh == null || vertices == null || vertices.Length != OriginalVertices.Length) return;
        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = Triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
    }
}
