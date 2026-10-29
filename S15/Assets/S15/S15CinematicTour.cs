using UnityEngine;

// Only attached temporarily while capturing the submission video.
public sealed class S15CinematicTour : MonoBehaviour
{
    private static readonly Vector3[] Stops =
    {
        new Vector3(0f, 1.75f, -12f),
        new Vector3(0f, 1.75f, -7f),
        new Vector3(0f, 1.75f, -3f),
        new Vector3(0f, 1.75f, -2.4f),
        new Vector3(0f, 1.75f, -1.9f),
        new Vector3(0f, 1.75f, -1.5f)
    };

    private Vector3 center;
    private float startTime;
    private SceneWalker walker;
    private Vector3 nearLookAt;

    private void Start()
    {
        GameObject pavilion = GameObject.Find("Pavilion");
        if (pavilion == null) { enabled = false; return; }
        Renderer[] renderers = pavilion.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        center = bounds.center;
        GameObject table = GameObject.Find("S02_MyTable_InPavilion");
        nearLookAt = table != null ? table.transform.position + new Vector3(0f, 0.5f, -0.7f)
                                   : center + new Vector3(0f, 0.15f, 2.2f);
        walker = GetComponent<SceneWalker>();
        walker.BeginGuidedTour(new Vector3(center.x + Stops[0].x, Stops[0].y, center.z + Stops[0].z));
        startTime = Time.time;
    }

    private void LateUpdate()
    {
        float progress = Mathf.Clamp01((Time.time - startTime) / 40f);
        float position = progress * (Stops.Length - 1);
        int segment = Mathf.Min(Mathf.FloorToInt(position), Stops.Length - 2);
        float blend = Mathf.SmoothStep(0f, 1f, position - segment);
        Vector3 location = Vector3.Lerp(Stops[segment], Stops[segment + 1], blend);
        Vector3 cameraPosition = new Vector3(center.x + location.x, location.y, center.z + location.z);
        Vector3 lookAt = Vector3.Lerp(center + new Vector3(0f, 0.15f, 2.2f),
            nearLookAt, Mathf.SmoothStep(0f, 1f, progress));
        walker.MoveGuidedTour(cameraPosition, lookAt);
    }
}
