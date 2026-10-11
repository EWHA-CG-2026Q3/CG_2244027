using UnityEditor;
using UnityEngine;

public static class S08GameViewCapture
{
    [MenuItem("S08/Capture Game view screenshot")]
    public static void Capture()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Press Play, then capture the S08 Game view.");
            return;
        }

        const string path = "Assets/S08/Submission/S08_GameView_TransformOrder.png";
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("S08 Game view screenshot requested: " + path);
    }
}
