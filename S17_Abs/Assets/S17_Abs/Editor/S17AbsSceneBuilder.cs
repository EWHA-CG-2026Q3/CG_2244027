using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S17AbsSceneBuilder
{
    private const string Root = "Assets/S17_Abs";
    private const string ScenePath = Root + "/S17_MyTextureAbs.unity";

    [MenuItem("S17 Abs/Build UV assignment scene")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/S17_MyTextureAbs.shader");
        Texture2D checker = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Checker_4x4.png");
        if (shader == null || !shader.isSupported || shader.name != "CG/S17_MyTextureAbs" || checker == null)
            throw new InvalidOperationException("The S17 shader or checker texture failed to import.");

        TextureImporter importer = AssetImporter.GetAtPath(Root + "/Checker_4x4.png") as TextureImporter;
        if (importer == null) throw new InvalidOperationException("The checker texture importer is missing.");
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        string materialPath = Root + "/Mat_S17_MyTextureAbs.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Mat_S17_MyTextureAbs" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.shader = shader;
        material.SetTexture("_BaseMap", checker);
        EditorUtility.SetDirty(material);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Quad_UV_Crop";
        quad.transform.position = new Vector3(-1.7f, 0f, 0f);
        quad.transform.localScale = new Vector3(2.45f, 2.45f, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = material;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere_UV_Crop";
        sphere.transform.position = new Vector3(1.7f, 0f, 0f);
        sphere.transform.localScale = Vector3.one * 2.45f;
        sphere.GetComponent<Renderer>().sharedMaterial = material;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.09f, 0.14f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 2.3f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 30f;
        cameraObject.transform.position = new Vector3(0f, 0f, -8f);
        cameraObject.transform.LookAt(Vector3.zero);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S17 My Texture Abs";
        AssetDatabase.SaveAssets();
        Debug.Log("S17 Abs built: one custom shader and material on a Quad and Sphere.");
    }

    [MenuItem("S17 Abs/Verify assignment scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Renderer quad = GameObject.Find("Quad_UV_Crop")?.GetComponent<Renderer>();
        Renderer sphere = GameObject.Find("Sphere_UV_Crop")?.GetComponent<Renderer>();
        if (quad == null || sphere == null || quad.sharedMaterial == null ||
            quad.sharedMaterial != sphere.sharedMaterial ||
            quad.sharedMaterial.shader.name != "CG/S17_MyTextureAbs" ||
            quad.sharedMaterial.GetTexture("_BaseMap") == null)
            throw new InvalidOperationException("The S17 scene, shared material, or Base Map is incomplete.");
        Debug.Log("S17 Abs verified: Quad and Sphere share Mat_S17_MyTextureAbs with the checker Base Map.");
    }
}
