using UnityEngine;

/// <summary>
/// Draws the same triangle using Unity's GL immediate-mode API.
/// Attach this component to the camera that renders the Game view.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class S06_GLImmediateTriangle : MonoBehaviour
{
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

        GL.Color(new Color(1.00f, 0.28f, 0.38f));
        GL.Vertex3(0.14f, 0.18f, 0f);

        GL.Color(new Color(0.10f, 0.86f, 0.78f));
        GL.Vertex3(0.87f, 0.32f, 0f);

        GL.Color(new Color(0.43f, 0.42f, 1.00f));
        GL.Vertex3(0.58f, 0.88f, 0f);

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
