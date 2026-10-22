using UnityEngine;

// Moves the virtual camera and its child Main Camera together for the submission video.
public sealed class S12_CameraDolly : MonoBehaviour
{
    [SerializeField] private Vector3 localAxis = Vector3.forward;
    [SerializeField, Min(0f)] private float distance = 1.0f;
    [SerializeField, Min(0.01f)] private float period = 8f;
    private Vector3 initialPosition;

    private void Awake() => initialPosition = transform.position;

    private void Update()
    {
        float displacement = Mathf.Sin(Time.time * Mathf.PI * 2f / period) * distance;
        transform.position = initialPosition + transform.rotation * localAxis.normalized * displacement;
    }
}
