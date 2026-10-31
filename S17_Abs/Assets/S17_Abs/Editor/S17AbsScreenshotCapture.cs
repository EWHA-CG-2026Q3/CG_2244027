using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class S17AbsScreenshotCapture
{
    private const string ScenePath = "Assets/S17_Abs/S17_MyTextureAbs.unity";
    private const string PendingKey = "S17.Abs.ScreenshotPending";
    private static double startedAt;
    private static bool capturing;
    private static bool requested;
    private static string outputPath;

    static S17AbsScreenshotCapture()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Update;
    }

    [MenuItem("S17 Abs/Capture Game view PNG")]
    public static void Capture()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before capturing the S17 Game view.");
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings", "S17_MyTextureAbs_GameView.png"));
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
        if (EditorApplication.timeSinceStartup - startedAt < 1.5d) return;
        if (!requested)
        {
            ScreenCapture.CaptureScreenshot(outputPath);
            requested = true;
            return;
        }
        if (!File.Exists(outputPath)) return;
        capturing = false;
        Debug.Log("S17 Abs Game view screenshot: " + outputPath);
        EditorApplication.isPlaying = false;
    }
}
