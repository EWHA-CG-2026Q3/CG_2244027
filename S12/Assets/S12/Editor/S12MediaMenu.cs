using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class S12MediaMenu
{
    private const string PendingKey = "S12CapturePending";

    static S12MediaMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("S12/Capture aspect-1 Game screenshot")]
    private static void Screenshot()
    {
        SessionState.SetInt(PendingKey, 1);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("S12/Record StillLife Game frames")]
    private static void Video()
    {
        SessionState.SetInt(PendingKey, 2);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        int mode = SessionState.GetInt(PendingKey, 0);
        SessionState.SetInt(PendingKey, 0);
        if (mode == 0) return;
        GameObject camera = GameObject.Find("VirtualCamera");
        if (camera == null)
        {
            Debug.LogError("S12 capture: VirtualCamera not found.");
            EditorApplication.isPlaying = false;
            return;
        }
        S12_SubmissionRecorder recorder = camera.AddComponent<S12_SubmissionRecorder>();
        recorder.captureVideo = mode == 2;
    }
}
