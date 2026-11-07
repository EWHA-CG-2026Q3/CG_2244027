using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S17SceneBuilder
{
    private const string Root = "Assets/S17";
    private const string FinalScene = Root + "/S17_D3.unity";
    private const string B2Scene = Root + "/S17_Mission_B2.unity";
    private const string QuarterScene = Root + "/S17_Mission_FullInQuarter.unity";

    [MenuItem("S17/Build three practice scenes")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Texture2D checker = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Checker_4x4.png");
        if (checker == null) throw new InvalidOperationException("The S17 checker texture is missing.");

        TextureImporter importer = AssetImporter.GetAtPath(Root + "/Checker_4x4.png") as TextureImporter;
        if (importer == null) throw new InvalidOperationException("The S17 texture importer is missing.");
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        Material d3 = MakeMaterial("S17_MyTexture", "Mat_S17_MyTexture", checker);
        Material b2 = MakeMaterial("S17_Mission_B2", "Mat_S17_Mission_B2", checker);
        Material quarter = MakeMaterial("S17_Mission_FullInQuarter", "Mat_S17_Mission_FullInQuarter", checker);

        BuildScene(B2Scene, b2);
        BuildScene(QuarterScene, quarter);
        BuildScene(FinalScene, d3);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(FinalScene, true),
            new EditorBuildSettingsScene(B2Scene, true),
            new EditorBuildSettingsScene(QuarterScene, true)
        };
        PlayerSettings.productName = "S17 D3 Texture Practice";
        AssetDatabase.SaveAssets();
        Debug.Log("S17 built: D3 final scene plus B2 and whole-image-in-quarter practice scenes.");
    }

    [MenuItem("S17/Verify three practice scenes")]
    public static void Verify()
    {
        CheckScene(B2Scene, "CG/S17_Mission_B2");
        CheckScene(QuarterScene, "CG/S17_Mission_FullInQuarter");
        CheckScene(FinalScene, "CG/S17_MyTexture");
        Debug.Log("S17 verified: all three scenes contain a Quad, Sphere, and S03 star with the intended material.");
    }

    private static Material MakeMaterial(string shaderFileName, string materialName, Texture2D checker)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/" + shaderFileName + ".shader");
        if (shader == null || !shader.isSupported)
            throw new InvalidOperationException(shaderFileName + " failed to import.");
        string path = Root + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetTexture("_BaseMap", checker);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildScene(string path, Material material)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Quad";
        quad.transform.position = new Vector3(-2.65f, 0f, 0f);
        quad.transform.localScale = new Vector3(1.85f, 1.85f, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = material;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere";
        sphere.transform.position = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 1.85f;
        sphere.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        sphere.GetComponent<Renderer>().sharedMaterial = material;

        GameObject star = new GameObject("S03_CustomStar");
        star.transform.position = new Vector3(2.65f, 0f, 0f);
        star.transform.localScale = Vector3.one * 0.39f;
        star.AddComponent<MeshFilter>();
        star.AddComponent<MeshRenderer>().sharedMaterial = material;
        star.AddComponent<S03_CustomPolygonMesh_Star>().RebuildMesh();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.075f, 0.12f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 2.12f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 30f;
        cameraObject.transform.position = new Vector3(0f, 0f, -8f);
        cameraObject.transform.LookAt(Vector3.zero);

        EditorSceneManager.SaveScene(scene, path);
    }

    private static void CheckScene(string path, string shaderName)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Material shared = null;
        foreach (string name in new[] { "Quad", "Sphere", "S03_CustomStar" })
        {
            Renderer renderer = GameObject.Find(name)?.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null ||
                renderer.sharedMaterial.shader.name != shaderName ||
                renderer.sharedMaterial.GetTexture("_BaseMap") == null)
                throw new InvalidOperationException(path + " has an incorrect object or material: " + name);
            if (shared == null) shared = renderer.sharedMaterial;
            else if (shared != renderer.sharedMaterial)
                throw new InvalidOperationException(path + " does not share one material.");
        }
        if (GameObject.Find("S03_CustomStar")?.GetComponent<MeshFilter>()?.sharedMesh == null)
            throw new InvalidOperationException(path + " does not contain a generated star mesh.");
    }
}
