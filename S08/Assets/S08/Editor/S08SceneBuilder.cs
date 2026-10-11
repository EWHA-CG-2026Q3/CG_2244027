using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S08SceneBuilder
{
    private const string Root = "Assets/S08";
    private const string ScenePath = Root + "/Scenes/S08_TransformOrder.unity";
    private static readonly Vector3 Move = new Vector3(0.9f, 0f, 0f);
    private static readonly Vector3 Resize = Vector3.one * 1.8f;

    [MenuItem("S08/Build comparison scene")]
    public static void Build()
    {
        EnsureFolder(Root + "/Scenes");
        EnsureFolder(Root + "/Materials");
        EnsureFolder(Root + "/Submission");

        Material cyan = MakeMaterial("TranslateThenScale_Cyan", new Color(0.06f, 0.83f, 0.93f));
        Material coral = MakeMaterial("ScaleThenTranslate_Coral", new Color(1f, 0.37f, 0.43f));
        Material panel = MakeMaterial("Panel_Navy", new Color(0.05f, 0.09f, 0.16f));
        Material guide = MakeMaterial("Reference_Gold", new Color(1f, 0.78f, 0.35f));
        Material separator = MakeMaterial("Separator_Blue", new Color(0.16f, 0.25f, 0.36f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndLight();
        MakePrimitive("Left comparison panel", PrimitiveType.Cube, new Vector3(-2.12f, 0f, 1.5f),
            new Vector3(4.05f, 3.35f, 0.04f), panel);
        MakePrimitive("Right comparison panel", PrimitiveType.Cube, new Vector3(2.12f, 0f, 1.5f),
            new Vector3(4.05f, 3.35f, 0.04f), panel);
        MakePrimitive("Centre divider", PrimitiveType.Cube, new Vector3(0f, 0f, 1f),
            new Vector3(0.025f, 3.15f, 0.04f), separator);

        S08_DirectTransform first = CreateExample("TranslateThenScale", -3.25f,
            S08_DirectTransform.DemoMode.TranslateThenScale, cyan, guide);
        S08_DirectTransform second = CreateExample("ScaleThenTranslate", 0.8f,
            S08_DirectTransform.DemoMode.ScaleThenTranslate, coral, guide);

        Selection.activeGameObject = first.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save S08 comparison scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S08 Transform Order Comparison";
        AssetDatabase.SaveAssets();
        Verify();
        Debug.Log("S08 scene built: both transform orders are visible together.");
    }

    [MenuItem("S08/Verify comparison scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath);
        S08_DirectTransform first = GameObject.Find("TranslateThenScale").GetComponent<S08_DirectTransform>();
        S08_DirectTransform second = GameObject.Find("ScaleThenTranslate").GetComponent<S08_DirectTransform>();
        Check(first.Mode == S08_DirectTransform.DemoMode.TranslateThenScale, "first mode");
        Check(second.Mode == S08_DirectTransform.DemoMode.ScaleThenTranslate, "second mode");
        Check(first.Translation == second.Translation && first.Scale == second.Scale, "identical operands");
        Check(Mathf.Abs(first.TransformPoint(Vector3.zero).x - 1.62f) < 0.0001f,
            "translation then scale result");
        Check(Mathf.Abs(second.TransformPoint(Vector3.zero).x - 0.9f) < 0.0001f,
            "scale then translation result");
        Debug.Log("S08 verified: T then S moves origin 1.62; S then T moves origin 0.9; both use T=0.9 and S=1.8.");
    }

    private static S08_DirectTransform CreateExample(string name, float originX,
        S08_DirectTransform.DemoMode mode, Material resultMaterial, Material guideMaterial)
    {
        Vector3 origin = new Vector3(originX, 0.15f, 0f);
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = origin;
        cube.GetComponent<MeshRenderer>().sharedMaterial = resultMaterial;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        S08_DirectTransform demo = cube.AddComponent<S08_DirectTransform>();
        demo.Configure(mode, Move, Resize);

        float displacement = demo.TransformPoint(Vector3.zero).x;
        float guideY = -1.28f;
        MakePrimitive(name + " origin marker", PrimitiveType.Sphere,
            new Vector3(originX, guideY, -0.1f), Vector3.one * 0.14f, guideMaterial);
        MakePrimitive(name + " result marker", PrimitiveType.Sphere,
            new Vector3(originX + displacement, guideY, -0.1f), Vector3.one * 0.14f, resultMaterial);
        MakePrimitive(name + " translation guide", PrimitiveType.Cube,
            new Vector3(originX + displacement * 0.5f, guideY, 0f),
            new Vector3(displacement, 0.025f, 0.025f), guideMaterial);
        return demo;
    }

    private static Transform MakePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.transform;
    }

    private static void CreateCameraAndLight()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2.45f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.033f, 0.061f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 30f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.transform.rotation = Quaternion.identity;
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.92f, 0.96f, 1f);
        light.intensity = 1.25f;
        lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.34f, 0.38f, 0.46f);
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("No compatible material shader found.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.12f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("S08 verification failed: " + description);
    }
}
