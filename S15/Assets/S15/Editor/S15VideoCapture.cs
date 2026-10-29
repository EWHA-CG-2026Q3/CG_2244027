using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class S15VideoCapture
{
    private const string ScenePath = "Assets/S15/S15_Landscape.unity";
    private const string PendingKey = "S15.RecordGameViewPending";

    static S15VideoCapture()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("S15/Record 40-second Game view walk")]
    public static void Record()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before starting the S15 recording.");

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        EditorApplication.delayCall += StartCapture;
    }

    private static void StartCapture()
    {
        if (!EditorApplication.isPlaying) return;
        GameObject walker = GameObject.Find("SceneWalker");
        Camera camera = walker?.GetComponentInChildren<Camera>(true);
        if (walker == null || camera == null)
            throw new InvalidOperationException("S15 SceneWalker or Main Camera is missing.");

        if (walker.GetComponent<SceneWalker>() == null)
            throw new InvalidOperationException("The active camera must belong to SceneWalker.");
        Camera shot = GameObject.Find("ShotCamera")?.GetComponent<Camera>();
        if (shot != null) shot.enabled = false;
        camera.enabled = true;
        walker.AddComponent<S15CinematicTour>();

        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
        Directory.CreateDirectory(directory);
        RecorderControllerSettings settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.FrameRate = 24f;
        settings.CapFrameRate = true;
        settings.ExitPlayMode = true;
        settings.SetRecordModeToTimeInterval(0f, 40f);

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "S15 Game view submission video";
        movie.Enabled = true;
        movie.CaptureAudio = false;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1280,
            OutputHeight = 720
        };
        movie.OutputFile = Path.Combine(directory, "S15_GameView_40s_Walker");
        settings.AddRecorderSettings(movie);

        RecorderController recorder = new RecorderController(settings);
        recorder.PrepareRecording();
        if (!recorder.StartRecording())
            throw new InvalidOperationException("Unity Recorder could not start the Game view capture.");
        Debug.Log("S15 Game view video recording: " + movie.OutputFile + ".mp4");
    }
}
