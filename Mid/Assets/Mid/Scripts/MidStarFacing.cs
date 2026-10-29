using UnityEngine;

// Keep the star's little face readable while its root follows the existing mouse target.
public sealed class MidStarFacing : MonoBehaviour
{
    public Camera ViewCamera;
    void LateUpdate(){if(ViewCamera)transform.rotation=ViewCamera.transform.rotation;}
}
