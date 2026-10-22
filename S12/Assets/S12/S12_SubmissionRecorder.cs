using System.Collections;
using System.IO;
using UnityEngine;

// Temporary Play-mode-only capture component; never saved into a scene.
public sealed class S12_SubmissionRecorder : MonoBehaviour
{
    public bool captureVideo;

    private IEnumerator Start()
    {
        string recordings = Path.GetFullPath(Path.Combine(Application.dataPath, "../Recordings"));
        Directory.CreateDirectory(recordings);
        Time.captureFramerate = 15;
        int count = captureVideo ? 180 : 1;
        string frames = Path.Combine(recordings, "S12_StillLife_Frames");
        if (captureVideo) Directory.CreateDirectory(frames);

        // Let the Game view and Canvas finish their first frame.
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null)
            {
                Debug.LogError("S12 capture failed: no Game view texture.");
                break;
            }
            byte[] bytes = captureVideo ? screenshot.EncodeToJPG(85) : screenshot.EncodeToPNG();
            string path = captureVideo
                ? Path.Combine(frames, "frame" + i.ToString("D4") + ".jpg")
                : Path.Combine(recordings, "S12_Aspect1_GameView.png");
            File.WriteAllBytes(path, bytes);
            Destroy(screenshot);
        }
        Time.captureFramerate = 0;
        Debug.Log(captureVideo ? "S12 video frames captured: " + count : "S12 aspect screenshot captured.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
