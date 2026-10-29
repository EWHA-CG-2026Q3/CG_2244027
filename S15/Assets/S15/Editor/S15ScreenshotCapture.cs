using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class S15ScreenshotCapture
{
    private const string ScenePath = "Assets/S15/S15_Landscape.unity";
    private const string PendingKey = "S15.ShotPending";
    private static double startedAt;
    private static bool capturing;
    private static bool requested;
    private static string outputPath;

    static S15ScreenshotCapture()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Update;
    }

    [MenuItem("S15/Capture ShotCamera Game view PNG")]
    public static void Capture()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before taking the S15 screenshot.");

        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);

        Camera walker = GameObject.Find("SceneWalker")?.GetComponentInChildren<Camera>();
        Camera shot = GameObject.Find("ShotCamera")?.GetComponent<Camera>();
        if (walker == null || shot == null)
            throw new InvalidOperationException("S15 cameras are missing.");

        walker.enabled = false;
        shot.enabled = true;
        outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings", "S15_GameView_ShotCamera.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        if (File.Exists(outputPath)) File.Delete(outputPath);
        startedAt = EditorApplication.timeSinceStartup;
        capturing = true;
        requested = false;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private static void Update()
    {
        if (!capturing || !EditorApplication.isPlaying) return;
        double elapsed = EditorApplication.timeSinceStartup - startedAt;
        if (elapsed < 4d) return;

        if (!requested)
        {
            ScreenCapture.CaptureScreenshot(outputPath);
            requested = true;
            return;
        }
        if (!File.Exists(outputPath)) return;
        capturing = false;
        Debug.Log("S15 ShotCamera Game view screenshot: " + outputPath);
        EditorApplication.isPlaying = false;
    }
}
