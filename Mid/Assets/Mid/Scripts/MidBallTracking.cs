using UnityEngine;

/// <summary>Mouse -> bounded world plane -> ball -> independent yaw and pitch. Body never rotates.</summary>
public sealed class MidBallTracking : MonoBehaviour
{
    public Camera ViewCamera;
    public Transform Ball;
    public Transform Neck; // Kept for the original single-doll scene.
    [System.Serializable]
    public class Spectator
    {
        public Transform Neck;
        public float FollowSpeed = 5f;
        [System.NonSerialized] public float Yaw;
        [System.NonSerialized] public float Pitch;
    }
    public Spectator[] Spectators = new Spectator[0];
    public float MaxUpPitch = 35f;
    public float MaxDownPitch = 30f;
    public Vector3 ResetBallPosition = new Vector3(0, 3.2f, -3f);
    public float MaxYaw = 48f;
    public float BallFollowSpeed = 8f;
    public float HeadFollowSpeed = 5f;
    public Vector2 HorizontalBounds = new Vector2(-4.5f, 4.5f);
    public Vector2 VerticalBounds = new Vector2(0.55f, 6.8f);
    public float BallPlaneZ = -3.0f;
    public bool AutoDemo;
    public float CurrentYaw { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public string DisplayTitle = "LITTLE SPECTATORS";
    public string DisplaySubtitle = "Nine little friends. A world of curiosity.";
    private GUIStyle title, subtitle, hint;

    void Start() { ResetPose(); }

    public void ResetPose()
    {
        TargetPosition = new Vector3(ResetBallPosition.x, ResetBallPosition.y, BallPlaneZ);
        Ball.position = TargetPosition;
        CurrentYaw = 0;
        if (Neck) Neck.localRotation = Quaternion.identity;
        foreach (var spectator in Spectators)
        {
            spectator.Yaw = spectator.Pitch = 0;
            if (spectator.Neck) spectator.Neck.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) AutoDemo = !AutoDemo;
        if (Input.GetKeyDown(KeyCode.R)) ResetPose();
        if (AutoDemo)
            SetWorldTarget(new Vector3(Mathf.Sin(Time.time * 0.8f) * 4f,
                3.5f + Mathf.Sin(Time.time * 1.1f) * 2.6f, BallPlaneZ));
        else if (Application.isFocused)
            MoveCursor(Input.mousePosition);
        Step(Time.deltaTime);
    }

    // False means the pointer left the Game view: hold the previous target.
    public bool MoveCursor(Vector2 screenPosition)
    {
        if (!ViewCamera.pixelRect.Contains(screenPosition)) return false;
        Ray ray = ViewCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, BallPlaneZ));
        if (!plane.Raycast(ray, out float distance)) return false;
        SetWorldTarget(ray.GetPoint(distance));
        return true;
    }

    public void SetWorldTarget(Vector3 point)
    {
        TargetPosition = new Vector3(Mathf.Clamp(point.x, HorizontalBounds.x, HorizontalBounds.y),
            Mathf.Clamp(point.y, VerticalBounds.x, VerticalBounds.y), BallPlaneZ);
    }

    public static Matrix4x4 YawMatrix(float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.m00 = c; matrix.m02 = s;
        matrix.m20 = -s; matrix.m22 = c;
        return matrix;
    }

    public void Step(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        Ball.position = Vector3.Lerp(Ball.position, TargetPosition,
            1f - Mathf.Exp(-BallFollowSpeed * deltaTime));
        if (Spectators.Length == 0 && Neck)
            Spectators = new[] { new Spectator { Neck = Neck, FollowSpeed = HeadFollowSpeed } };
        foreach (var spectator in Spectators)
        {
            if (!spectator.Neck) continue;
            Vector3 direction = spectator.Neck.parent.InverseTransformDirection(Ball.position - spectator.Neck.position);
            float horizontal = new Vector2(direction.x, direction.z).magnitude;
            float yaw = Mathf.Clamp(Mathf.Atan2(-direction.x, -direction.z) * Mathf.Rad2Deg, -MaxYaw, MaxYaw);
            float pitch = Mathf.Clamp(Mathf.Atan2(direction.y, horizontal) * Mathf.Rad2Deg, -MaxDownPitch, MaxUpPitch);
            float blend = 1f - Mathf.Exp(-spectator.FollowSpeed * deltaTime);
            spectator.Yaw = Mathf.Lerp(spectator.Yaw, yaw, blend);
            spectator.Pitch = Mathf.Lerp(spectator.Pitch, pitch, blend);
            // Local X tilt followed by parent Y turn: diagonals work without roll.
            spectator.Neck.localRotation = (YawMatrix(spectator.Yaw) * PitchMatrix(spectator.Pitch)).rotation;
        }
        if (Spectators.Length > 0) CurrentYaw = Spectators[0].Yaw;
    }

    public static Matrix4x4 PitchMatrix(float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        Matrix4x4 matrix = Matrix4x4.identity;
        matrix.m11 = c; matrix.m12 = -s;
        matrix.m21 = s; matrix.m22 = c;
        return matrix;
    }

    void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            hint = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(0.18f, 0.28f, 0.23f);
            subtitle.normal.textColor = new Color(0.36f, 0.43f, 0.36f);
            hint.normal.textColor = title.normal.textColor;
        }
        GUI.Label(new Rect(28, 20, 600, 40), DisplayTitle, title);
        GUI.Label(new Rect(30, 57, 600, 25), DisplaySubtitle, subtitle);
        GUI.Label(new Rect(0, Screen.height - 44, Screen.width, 30),
            (AutoDemo ? "AUTO DEMO" : "MOVE YOUR MOUSE") + "    /    SPACE: auto    /    R: reset", hint);
    }
}
