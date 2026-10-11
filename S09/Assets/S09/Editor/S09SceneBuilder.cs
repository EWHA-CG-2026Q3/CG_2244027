using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class S09SceneBuilder
{
    private const string Root = "Assets/S09";
    private const string ScenePath = Root + "/Scenes/S09_DiamondShear.unity";
    private const float StudentK = (7f + 1f) / 5f; // CG_2244027

    [MenuItem("S09/Build diamond shear scene")]
    public static void Build()
    {
        EnsureFolder(Root + "/Scenes");
        EnsureFolder(Root + "/Materials");
        EnsureFolder(Root + "/Submission");

        Material original = MakeMaterial("Original_Translucent", new Color(0.84f, 0.9f, 1f, 0.28f), true);
        Material positive = MakeMaterial("PositiveK_Orange", new Color(1f, 0.43f, 0.25f), false);
        Material negative = MakeMaterial("NegativeK_Cyan", new Color(0.12f, 0.82f, 0.98f), false);
        Material panel = MakeMaterial("Panel_Navy", new Color(0.055f, 0.092f, 0.15f), false);
        Material pedestal = MakeMaterial("Pedestal_Slate", new Color(0.12f, 0.2f, 0.29f), false);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndLight();
        MakePrimitive("Positive k backdrop", PrimitiveType.Cube, new Vector3(-2f, 0f, 1.7f),
            new Vector3(3.75f, 3.45f, 0.05f), panel);
        MakePrimitive("Negative k backdrop", PrimitiveType.Cube, new Vector3(2f, 0f, 1.7f),
            new Vector3(3.75f, 3.45f, 0.05f), panel);
        MakePrimitive("Left pedestal", PrimitiveType.Cylinder, new Vector3(-1.55f, -1.03f, 0f),
            new Vector3(0.83f, 0.055f, 0.75f), pedestal);
        MakePrimitive("Right pedestal", PrimitiveType.Cylinder, new Vector3(1.95f, -1.03f, 0f),
            new Vector3(0.83f, 0.055f, 0.75f), pedestal);

        MakeDiamond("Original +k", new Vector3(-1.95f, 0f, 0f), original, null);
        MakeDiamond("Original -k", new Vector3(1.55f, 0f, 0f), original, null);
        S09_ShearDiamond plus = MakeDiamond("Sheared +1.6", new Vector3(-1.95f, 0f, 0f), positive, StudentK);
        MakeDiamond("Sheared -1.6", new Vector3(1.55f, 0f, 0f), negative, -StudentK);

        Selection.activeGameObject = plus.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save the S09 diamond scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S09 Diamond Shear Comparison";
        AssetDatabase.SaveAssets();
        Verify();
        Debug.Log("S09 scene built: originals coincide with the +1.6 and -1.6 sheared diamonds.");
    }

    [MenuItem("S09/Verify shear scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath);
        S09_ShearDiamond plus = FindShear("Sheared +1.6");
        S09_ShearDiamond minus = FindShear("Sheared -1.6");
        Check(Mathf.Abs(plus.K - StudentK) < 0.0001f, "positive student k");
        Check(Mathf.Abs(minus.K + StudentK) < 0.0001f, "negative student k");
        Check(plus.transform.position == GameObject.Find("Original +k").transform.position,
            "positive original is at same position");
        Check(minus.transform.position == GameObject.Find("Original -k").transform.position,
            "negative original is at same position");
        Check(GameObject.Find("Original +k").GetComponent<S09_ShearDiamond>() == null,
            "positive original has DiamondMesh only");
        Check(GameObject.Find("Original -k").GetComponent<S09_ShearDiamond>() == null,
            "negative original has DiamondMesh only");
        Check(plus.GetComponent<MeshRenderer>().sharedMaterial !=
              GameObject.Find("Original +k").GetComponent<MeshRenderer>().sharedMaterial,
            "original and sheared material differ");
        Check(Approximately(plus.TransformPoint(Vector3.up), new Vector3(1.6f, 1f, 0f)),
            "e2 maps to (k,1,0)");
        Check(Approximately(plus.TransformPoint(S09_ShearDiamond.TopVertex), new Vector3(2.1f, 1f, 0.5f)),
            "positive top result");
        Check(Approximately(minus.TransformPoint(S09_ShearDiamond.TopVertex), new Vector3(-1.1f, 1f, 0.5f)),
            "negative top result");
        Check(Approximately(plus.GetComponent<MeshFilter>().sharedMesh.vertices[4],
            new Vector3(2.1f, 1f, 0.5f)), "positive visible mesh top");
        Check(Approximately(minus.GetComponent<MeshFilter>().sharedMesh.vertices[4],
            new Vector3(-1.1f, 1f, 0.5f)), "negative visible mesh top");
        foreach (Vector3 point in plus.GetComponent<DiamondMesh>().BaseVertices)
            if (Mathf.Abs(point.y) < 0.0001f)
                Check(Approximately(plus.TransformPoint(point), point), "middle vertices remain fixed");
        Debug.Log("S09 verified: e2=(1.6,1,0); top +k=(2.1,1,0.5), top -k=(-1.1,1,0.5); middle y=0 stays fixed.");
    }

    [MenuItem("S09/Select positive shear")]
    public static void SelectPositive() => Selection.activeGameObject = GameObject.Find("Sheared +1.6");

    [MenuItem("S09/Select negative shear")]
    public static void SelectNegative() => Selection.activeGameObject = GameObject.Find("Sheared -1.6");

    [MenuItem("S09/Log both top vertices")]
    public static void LogBothTops()
    {
        FindShear("Sheared +1.6").LogTopVertex();
        FindShear("Sheared -1.6").LogTopVertex();
        EditorApplication.ExecuteMenuItem("Window/General/Console");
    }

    private static S09_ShearDiamond FindShear(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null) throw new InvalidOperationException("Missing " + name);
        S09_ShearDiamond component = obj.GetComponent<S09_ShearDiamond>();
        if (component == null) throw new InvalidOperationException("Missing shear component on " + name);
        return component;
    }

    private static S09_ShearDiamond MakeDiamond(string name, Vector3 position,
        Material material, float? k)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * 0.8f;
        obj.AddComponent<DiamondMesh>();
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!k.HasValue) return null;
        S09_ShearDiamond shear = obj.AddComponent<S09_ShearDiamond>();
        shear.SetK(k.Value);
        return shear;
    }

    private static void CreateCameraAndLight()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2.25f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.017f, 0.032f, 0.059f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 30f;
        cameraObject.transform.position = new Vector3(0f, 0.85f, -8f);
        cameraObject.transform.LookAt(new Vector3(0f, 0f, 0f));
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.94f, 0.96f, 1f);
        light.intensity = 1.3f;
        lightObject.transform.rotation = Quaternion.Euler(36f, -35f, 0f);
        RenderSettings.ambientLight = new Color(0.31f, 0.38f, 0.48f);
    }

    private static void MakePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
    }

    private static Material MakeMaterial(string name, Color color, bool transparent)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Standard shader not found.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.11f);
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

    private static bool Approximately(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 0.0000001f;

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("S09 verification failed: " + label);
    }
}
