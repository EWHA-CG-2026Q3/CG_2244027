using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class S13MainSceneBuilder
{
    private const string Root = "Assets/S13";
    private const string SourceScene = Root + "/S13_SunsetPavilion.unity";
    private const string ScenePath = Root + "/S13_Landscape.unity";

    [MenuItem("S13 Main/Build night landscape")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        GameObject pavilion = GameObject.Find("Pavilion");
        GameObject walker = GameObject.Find("SceneWalker");
        Camera playerCamera = Camera.main;
        if (pavilion == null || walker == null || playerCamera == null)
            throw new InvalidOperationException("The original Pavilion scene is incomplete.");

        Light sun = GameObject.Find("Low Amber Sun")?.GetComponent<Light>();
        if (sun == null) throw new InvalidOperationException("Directional light is missing.");
        sun.name = "Night Sun Below Horizon";
        sun.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        sun.color = Color.black;
        sun.intensity = 0f;
        sun.shadows = LightShadows.Soft;
        sun.lightmapBakeType = LightmapBakeType.Realtime;
        RenderSettings.sun = sun;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.13f, 0.16f, 0.25f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 0.4f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.032f;
        RenderSettings.fogColor = new Color(0.055f, 0.075f, 0.13f);
        playerCamera.clearFlags = CameraClearFlags.SolidColor;
        playerCamera.backgroundColor = RenderSettings.fogColor;

        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if (light == sun) continue;
            light.color = new Color(1f, 0.65f, 0.36f);
            light.intensity = light.type == LightType.Spot ? 2f : 1.4f;
            light.shadows = light.type == LightType.Spot ? LightShadows.Soft : LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        GameObject moonObject = new GameObject("Cool Moonlight");
        moonObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        Light moon = moonObject.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.42f, 0.55f, 1f);
        moon.intensity = 0.38f;
        moon.shadows = LightShadows.Soft;
        moon.lightmapBakeType = LightmapBakeType.Realtime;

        GameObject oldTable = GameObject.Find("Table_Wood");
        Vector3 tablePosition = oldTable != null ? oldTable.transform.position : new Vector3(-0.3f, 0.857f, 2.21f);
        if (oldTable != null) UnityEngine.Object.DestroyImmediate(oldTable);
        GameObject tableAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/S02_Table.prefab");
        if (tableAsset == null) throw new InvalidOperationException("S02 table prefab is missing.");
        GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(tableAsset);
        table.name = "S02_MyTable_InPavilion";
        table.transform.position = tablePosition;
        Material wood = Material("S13_Table_Wood", new Color(0.36f, 0.18f, 0.09f), 0f, 0.25f);
        foreach (Renderer renderer in table.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = wood;

        Material metal = Material("S13_Primitive_Bronze", new Color(0.62f, 0.38f, 0.19f), 0.6f, 0.3f);
        Material ceramic = Material("S13_Primitive_Ceramic", new Color(0.85f, 0.75f, 0.56f), 0f, 0.25f);
        Material stone = Material("S13_Primitive_Slate", new Color(0.24f, 0.3f, 0.38f), 0.05f, 0.12f);
        Primitive(PrimitiveType.Cylinder, "Primitive 1 Bronze Vase", tablePosition + new Vector3(-0.68f, 0.98f, 0.28f), new Vector3(0.17f, 0.18f, 0.17f), metal);
        Primitive(PrimitiveType.Sphere, "Primitive 2 Ceramic Orb", tablePosition + new Vector3(0.69f, 0.96f, 0.18f), new Vector3(0.29f, 0.29f, 0.29f), ceramic);
        Primitive(PrimitiveType.Cube, "Primitive 3 Slate Box", tablePosition + new Vector3(0.65f, 0.89f, -0.33f), new Vector3(0.25f, 0.15f, 0.22f), stone);

        GameObject star = new GameObject("My Mesh 1 S03 Star");
        star.transform.position = tablePosition + new Vector3(-0.38f, 1.18f, 0.34f);
        star.transform.localScale = Vector3.one * 0.115f;
        star.AddComponent<S03_CustomPolygonMesh_Star>();
        star.GetComponent<MeshRenderer>().sharedMaterial = Material("S13_Mesh_Star_Gold", new Color(0.93f, 0.65f, 0.22f), 0.45f, 0.42f);

        GameObject diamond = new GameObject("My Mesh 2 S04 Diamond");
        diamond.transform.position = tablePosition + new Vector3(-0.04f, 1.12f, 0.08f);
        diamond.transform.localScale = Vector3.one * 0.24f;
        diamond.AddComponent<S04_CustomDiamondMesh>();
        diamond.GetComponent<MeshRenderer>().sharedMaterial = Material("S13_Mesh_Diamond_Cyan", new Color(0.16f, 0.78f, 0.77f), 0.2f, 0.45f);

        GameObject secondDiamond = new GameObject("My Mesh 3 S10 Rewound Diamond");
        secondDiamond.transform.position = tablePosition + new Vector3(0.34f, 1.15f, 0.32f);
        secondDiamond.transform.localScale = Vector3.one * 0.37f;
        secondDiamond.AddComponent<DiamondMesh_Rewinded>();
        secondDiamond.GetComponent<MeshRenderer>().sharedMaterial = Material("S13_Mesh_Rewound_Coral", new Color(0.96f, 0.32f, 0.32f), 0.1f, 0.35f);

        GameObject shot = new GameObject("ShotCamera");
        Camera shotCamera = shot.AddComponent<Camera>();
        shotCamera.fieldOfView = 59f;
        shotCamera.nearClipPlane = 0.08f;
        shotCamera.farClipPlane = 90f;
        shotCamera.clearFlags = CameraClearFlags.SolidColor;
        shotCamera.backgroundColor = RenderSettings.fogColor;
        shotCamera.transform.position = tablePosition + new Vector3(0f, 1.65f, -3.7f);
        shotCamera.transform.LookAt(tablePosition + new Vector3(0f, 1.05f, 0f));
        shotCamera.enabled = false;

        LightingSettings lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(Root + "/S13_Landscape.lighting");
        if (lighting == null) throw new InvalidOperationException("Lighting settings asset is missing.");
        lighting.name = "S13_Landscape";
        lighting.bakedGI = false;
        lighting.realtimeGI = false;
        Lightmapping.lightingSettings = lighting;
        LightmapSettings.lightmaps = Array.Empty<LightmapData>();
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
        {
            renderer.lightmapIndex = -1;
            renderer.realtimeLightmapIndex = -1;
        }
        EditorUtility.SetDirty(lighting);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath, false);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S13 Night Pavilion";
        AssetDatabase.SaveAssets();
        Debug.Log("S13 regular night scene built: " + ScenePath);
    }

    [MenuItem("S13 Main/Generate lighting")]
    public static void GenerateLighting()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!Lightmapping.Bake()) throw new InvalidOperationException("Generate Lighting failed.");
        LightmapSettings.lightmaps = Array.Empty<LightmapData>();
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
        {
            renderer.lightmapIndex = -1;
            renderer.realtimeLightmapIndex = -1;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("S13 regular Generate Lighting complete.");
    }

    [MenuItem("S13 Main/Verify night landscape")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (GameObject.Find("Pavilion") == null || GameObject.Find("SceneWalker")?.GetComponent<SceneWalker>() == null)
            throw new InvalidOperationException("Pavilion or SceneWalker is missing.");
        if (GameObject.Find("S02_MyTable_InPavilion") == null || GameObject.Find("Table_Wood") != null)
            throw new InvalidOperationException("The Pavilion table was not replaced.");
        if (GameObject.Find("My Mesh 1 S03 Star") == null || GameObject.Find("My Mesh 2 S04 Diamond") == null ||
            GameObject.Find("My Mesh 3 S10 Rewound Diamond") == null)
            throw new InvalidOperationException("Three custom meshes are missing.");
        if (GameObject.Find("ShotCamera")?.GetComponent<Camera>() == null || !RenderSettings.fog)
            throw new InvalidOperationException("Shot camera or fog is missing.");
        if (GameObject.Find("Cool Moonlight")?.GetComponent<Light>() == null)
            throw new InvalidOperationException("Night lighting is incomplete.");
        if (GameObject.Find("Primitive 1 Bronze Vase") == null || GameObject.Find("Primitive 2 Ceramic Orb") == null ||
            GameObject.Find("Primitive 3 Slate Box") == null)
            throw new InvalidOperationException("Three differently materialed primitives are missing.");
        if (GraphicsSettings.defaultRenderPipeline == null && QualitySettings.renderPipeline == null)
            throw new InvalidOperationException("URP pipeline is missing.");
        Debug.Log("S13 regular verified: Pavilion, walker, S02 table, 3 primitives, 3 own meshes, night, fog, shot camera.");
    }

    [MenuItem("S13 Main/Preview ShotCamera")]
    public static void PreviewShotCamera()
    {
        Camera.main.enabled = false;
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S13 Main/Preview Walker Camera")]
    public static void PreviewWalkerCamera()
    {
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = false;
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private static GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        string folder = Root + "/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Materials");
        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }
}
