using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Star : MonoBehaviour
{
    private const int BoundaryVertexCount = 10;

    [Header("Star Shape")]
    [Min(0.1f)] public float outerRadius = 2.4f;
    [Min(0.1f)] public float innerRadius = 1.15f;

    [SerializeField, HideInInspector]
    private Mesh generatedMesh;

    private void OnEnable()
    {
        RebuildMesh();
    }

    private void OnValidate()
    {
        RebuildMesh();
    }

    public void RebuildMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            return;
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = "S03_CustomPolygon_10Vertex_Star"
            };
        }
        else
        {
            generatedMesh.Clear();
        }

        // Vertex 0 is the center. Vertices 1-10 trace the star clockwise.
        Vector3[] vertices = new Vector3[BoundaryVertexCount + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[BoundaryVertexCount * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < BoundaryVertexCount; i++)
        {
            float angle = (90f - i * 36f) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, y, 0f);
            uv[i + 1] = new Vector2(
                x / (outerRadius * 2f) + 0.5f,
                y / (outerRadius * 2f) + 0.5f
            );

            int next = (i + 1) % BoundaryVertexCount;
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = next + 1;
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
    }
}
