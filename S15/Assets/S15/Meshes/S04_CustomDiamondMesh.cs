using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_CustomDiamondMesh : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private Mesh generatedMesh;

    private void OnEnable()
    {
        BuildDiamond();
    }

    private void OnValidate()
    {
        BuildDiamond();
    }

    public void BuildDiamond()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            return;
        }

        // Cube vertices 0, 1, 5, 4 become the four belt vertices.
        // Two new vertices form the top and bottom tips: 6 vertices total.
        Vector3[] vertices =
        {
            new Vector3(0f, 0f, 0f),       // 0: cube vertex 0
            new Vector3(1f, 0f, 0f),       // 1: cube vertex 1
            new Vector3(1f, 0f, 1f),       // 2: cube vertex 5
            new Vector3(0f, 0f, 1f),       // 3: cube vertex 4
            new Vector3(0.5f, 1f, 0.5f),   // 4: new top vertex
            new Vector3(0.5f, -1f, 0.5f),  // 5: new bottom vertex
        };

        // Clockwise winding when each face is viewed from outside.
        // Four upper faces followed by four lower faces: 8 triangles total.
        int[] triangles =
        {
            4, 1, 0,
            4, 2, 1,
            4, 3, 2,
            4, 0, 3,

            5, 0, 1,
            5, 1, 2,
            5, 2, 3,
            5, 3, 0,
        };

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = "S04_Diamond_6Vertices_8Triangles"
            };
        }
        else
        {
            generatedMesh.Clear();
        }

        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
    }
}
