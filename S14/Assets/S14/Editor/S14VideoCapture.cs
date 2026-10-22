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
public static class S14VideoCapture
{
    private const string ScenePath = "Assets/S14/S14_Landscape.unity";
    private const string PendingKey = "S14.RecordGameViewPending";

    static S14VideoCapture()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("S14/Record 40-second Game view walk")]
    public static void Record()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before starting the S14 recording.");

        if (SceneManager.GetActiveScene().path != ScenePath)
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
        Camera camera = Camera.main;
        if (walker == null || camera == null)
            throw new InvalidOperationException("S14 SceneWalker or Main Camera is missing.");

        walker.GetComponent<SceneWalker>().enabled = false;
        camera.gameObject.AddComponent<S14CinematicTour>();

        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
        Directory.CreateDirectory(directory);
        RecorderControllerSettings settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.FrameRate = 24f;
        settings.CapFrameRate = true;
        settings.ExitPlayMode = true;
        settings.SetRecordModeToTimeInterval(0f, 40f);

        MovieRecorderSettings movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "S14 Game view submission video";
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
        movie.OutputFile = Path.Combine(directory, "S14_GameView_40s");
        settings.AddRecorderSettings(movie);

        RecorderController recorder = new RecorderController(settings);
        recorder.PrepareRecording();
        if (!recorder.StartRecording())
            throw new InvalidOperationException("Unity Recorder could not start the Game view capture.");
        Debug.Log("S14 Game view video recording: " + movie.OutputFile + ".mp4");
    }
}
