using UnityEngine;

// Only attached temporarily while capturing the submission video.
public sealed class S13CinematicTour : MonoBehaviour
{
    private static readonly Vector3[] Stops =
    {
        new Vector3(0f, 1.75f, -12f),
        new Vector3(0f, 1.75f, -7.5f),
        new Vector3(4.1f, 1.85f, -6.7f),
        new Vector3(7.2f, 2.0f, -1.4f),
        new Vector3(6.7f, 2.1f, 4.5f),
        new Vector3(0.8f, 2.05f, 8.7f)
    };

    private Vector3 center;
    private float startTime;

    private void Start()
    {
        GameObject pavilion = GameObject.Find("Pavilion");
        if (pavilion == null) { enabled = false; return; }
        Renderer[] renderers = pavilion.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        center = bounds.center;
        startTime = Time.time;
    }

    private void LateUpdate()
    {
        float progress = Mathf.Clamp01((Time.time - startTime) / 40f);
        float position = progress * (Stops.Length - 1);
        int segment = Mathf.Min(Mathf.FloorToInt(position), Stops.Length - 2);
        float blend = Mathf.SmoothStep(0f, 1f, position - segment);
        Vector3 location = Vector3.Lerp(Stops[segment], Stops[segment + 1], blend);
        transform.position = new Vector3(center.x + location.x, location.y, center.z + location.z);
        Vector3 lookAt = center + new Vector3(0f, 0.15f, 0f);
        transform.rotation = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
    }
}
