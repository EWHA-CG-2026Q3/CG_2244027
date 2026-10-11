using UnityEngine;

// Emulates a parent hierarchy by multiplying each root Transform's local TRS.
// Both diamond meshes remain at the top level of the Hierarchy.
[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh_Rewinded))]
public sealed class S10_DiamondChain_Finish : MonoBehaviour
{
    [SerializeField] private Transform[] chain;
    public Transform[] Chain => chain;

    public void SetChain(Transform[] parentsFirst)
    {
        chain = parentsFirst;
        ApplyChain();
    }

    private void LateUpdate() => ApplyChain();

    public Matrix4x4 ComposeMatrix()
    {
        Matrix4x4 composed = Matrix4x4.identity;
        if (chain == null) return composed;
        foreach (Transform step in chain)
        {
            if (step == null) continue;
            composed *= Matrix4x4.TRS(step.localPosition, step.localRotation, step.localScale);
        }
        return composed;
    }

    private void ApplyChain()
    {
        DiamondMesh_Rewinded shape = GetComponent<DiamondMesh_Rewinded>();
        if (shape == null || chain == null || chain.Length == 0) return;
        Matrix4x4 world = ComposeMatrix();
        Vector3[] source = shape.BaseVertices;
        Vector3[] transformed = new Vector3[source.Length];
        for (int i = 0; i < source.Length; ++i)
            transformed[i] = world.MultiplyPoint3x4(source[i]);
        shape.SetVertices(transformed);
    }
}
