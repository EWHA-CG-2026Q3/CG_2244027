using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class S05ProjectTools
{
    [MenuItem("S05/Rebuild assignment scenes")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory("Assets/S05/Scenes");
        System.IO.Directory.CreateDirectory("Assets/S05/Screenshots");
        AssetDatabase.Refresh();
        CreateScene(S05_MyMeshRenderer.Pattern.VerticalStripes, "S05_VerticalStripes");
        CreateScene(S05_MyMeshRenderer.Pattern.Checkerboard, "S05_Checkerboard");
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/S05/Scenes/S05_VerticalStripes.unity", true),
            new EditorBuildSettingsScene("Assets/S05/Scenes/S05_Checkerboard.unity", true)
        };
        EditorSceneManager.OpenScene("Assets/S05/Scenes/S05_VerticalStripes.unity");
        AssetDatabase.SaveAssets();
        VerifyPatterns();
    }

    private static void CreateScene(S05_MyMeshRenderer.Pattern pattern, string name)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var image = new GameObject("Pattern Canvas", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter), typeof(S05_MyMeshRenderer));
        image.transform.SetParent(canvas.transform, false);
        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, 20f);
        rect.offsetMax = new Vector2(-20f, -20f);
        var fitter = image.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;
        var serialized = new SerializedObject(image.GetComponent<S05_MyMeshRenderer>());
        serialized.FindProperty("pattern").enumValueIndex = (int)pattern;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, "Assets/S05/Scenes/" + name + ".unity");
    }

    public static void VerifyPatterns()
    {
        foreach (var pattern in new[] { S05_MyMeshRenderer.Pattern.VerticalStripes, S05_MyMeshRenderer.Pattern.Checkerboard })
        {
            var obj = new GameObject("Verification", typeof(RectTransform), typeof(RawImage), typeof(S05_MyMeshRenderer));
            var renderer = obj.GetComponent<S05_MyMeshRenderer>();
            var serialized = new SerializedObject(renderer);
            serialized.FindProperty("pattern").enumValueIndex = (int)pattern;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            typeof(S05_MyMeshRenderer).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(renderer, null);
            var texture = (Texture2D)obj.GetComponent<RawImage>().texture;
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    bool white = pattern == S05_MyMeshRenderer.Pattern.VerticalStripes ? (x / 16) % 2 == 0 : ((x / 16) + (y / 16)) % 2 == 0;
                    var expected = (Color32)(white ? Color.white : new Color(0.3f, 0.5f, 0.8f, 1f));
                    var actual = (Color32)texture.GetPixel(x, y);
                    if (Mathf.Abs(actual.r - expected.r) > 1 || Mathf.Abs(actual.g - expected.g) > 1 || Mathf.Abs(actual.b - expected.b) > 1 || actual.a != expected.a)
                        throw new System.Exception("Incorrect pixel: " + pattern + " at " + x + "," + y);
                }
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(texture);
            Debug.Log("S05 verified all 65536 texels: " + pattern);
        }
    }
}
