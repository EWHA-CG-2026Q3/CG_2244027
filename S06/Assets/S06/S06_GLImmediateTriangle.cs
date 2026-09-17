using UnityEngine;

/// <summary>
/// Draws a triangle using Unity's GL immediate-mode API.
/// Attach this component to the camera that renders the Game view.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class S06_GLImmediateTriangle : MonoBehaviour
{
    [SerializeField] private Vector3 vertexA = new Vector3(0.113f, 0.195f, 0f);
    [SerializeField] private Vector3 vertexB = new Vector3(0.867f, 0.301f, 0f);
    [SerializeField] private Vector3 vertexC = new Vector3(0.578f, 0.883f, 0f);
    [SerializeField] private Color triangleColor = new Color(0.16f, 0.83f, 1f, 1f);

    private Material glMaterial;

    private void OnEnable()
    {
        EnsureMaterial();
    }

    private void OnDisable()
    {
        if (glMaterial != null)
        {
            DestroyImmediate(glMaterial);
            glMaterial = null;
        }
    }

    private void OnPostRender()
    {
        EnsureMaterial();
        if (glMaterial == null)
        {
            return;
        }

        glMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.TRIANGLES);
        GL.Color(triangleColor);
        GL.Vertex3(vertexA.x, vertexA.y, vertexA.z);
        GL.Vertex3(vertexB.x, vertexB.y, vertexB.z);
        GL.Vertex3(vertexC.x, vertexC.y, vertexC.z);
        GL.End();
        GL.PopMatrix();
    }

    private void EnsureMaterial()
    {
        if (glMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            Debug.LogError("Unity's Hidden/Internal-Colored shader was not found.", this);
            return;
        }

        glMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        glMaterial.SetInt("_ZWrite", 0);
    }
}
