using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class S13SceneBuilder
{
    private const string Root = "Assets/S13_Abs";
    private const string ScenePath = Root + "/S13_SunsetPavilion.unity";

    [MenuItem("S13/Build sunset pavilion")]
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject pavilion = GameObject.Find("Pavilion");
        GameObject walker = GameObject.Find("SceneWalker");
        GameObject ground = GameObject.Find("Plane") ?? GameObject.Find("Sunset Ground");
        GameObject sunObject = GameObject.Find("Directional Light") ?? GameObject.Find("Low Amber Sun");
        if (pavilion == null || walker == null || ground == null || sunObject == null)
            throw new InvalidOperationException("The Pavilion teaching scene is incomplete.");

        EnsureFolder(Root + "/Materials");
        Material groundMaterial = GetOrMakeMaterial("Evening_Sand", new Color(0.48f, 0.35f, 0.31f));
        Material pathMaterial = GetOrMakeMaterial("Copper_Path", new Color(0.72f, 0.49f, 0.36f));
        Material borderMaterial = GetOrMakeMaterial("Dusk_Border", new Color(0.24f, 0.25f, 0.38f));

        Bounds bounds = GetBounds(pavilion);
        ground.transform.position = new Vector3(bounds.center.x, -0.035f, bounds.center.z);
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        ground.name = "Sunset Ground";

        GameObject previous = GameObject.Find("Sunset Walkway");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        GameObject path = new GameObject("Sunset Walkway");
        float approach = bounds.min.z - 0.6f;
        for (int i = 0; i < 9; ++i)
        {
            float z = approach - i * 0.72f;
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Copper stepping stone " + (i + 1);
            stone.transform.SetParent(path.transform);
            stone.transform.position = new Vector3(bounds.center.x + Mathf.Sin(i * 1.7f) * 0.11f, 0.005f, z);
            stone.transform.localScale = new Vector3(1.15f, 0.055f, 0.55f);
            stone.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 ? borderMaterial : pathMaterial;
        }

        Light sun = sunObject.GetComponent<Light>();
        sunObject.name = "Low Amber Sun";
        sun.transform.rotation = Quaternion.Euler(18f, 226f, 0f);
        sun.color = new Color(1f, 0.62f, 0.39f);
        sun.intensity = 1.55f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.82f;
        sun.lightmapBakeType = LightmapBakeType.Mixed;
        RenderSettings.sun = sun;

        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light == sun) continue;
            light.color = new Color(1f, 0.68f, 0.41f);
            light.intensity = light.type == LightType.Spot ? 0.65f : 0.42f;
            light.range = Mathf.Max(light.range, 5f);
            light.shadows = LightShadows.Soft;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.47f, 0.67f);
        RenderSettings.ambientEquatorColor = new Color(0.76f, 0.48f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.32f);
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.75f, 0.50f, 0.51f);
        RenderSettings.fogStartDistance = 9f;
        RenderSettings.fogEndDistance = 52f;
        Material sky = GetOrMakeSky();
        RenderSettings.skybox = sky;

        walker.transform.position = new Vector3(bounds.center.x, 1.05f, approach - 5.9f);
        Camera camera = Camera.main;
        if (camera == null || camera.transform.parent != walker.transform)
            throw new InvalidOperationException("SceneWalker must own Main Camera.");
        camera.transform.localPosition = new Vector3(0f, 0.69f, 0f);
        camera.transform.localRotation = Quaternion.identity;
        camera.fieldOfView = 63f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 90f;
        Vector3 focus = bounds.center + Vector3.up * 0.55f;
        walker.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(focus - camera.transform.position, Vector3.up));
        camera.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        SceneWalker control = walker.GetComponent<SceneWalker>();
        control.cameraTransform = camera.transform;
        control.moveSpeed = 2.15f;
        control.runMultiplier = 1.8f;

        LightingSettings lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(Root + "/S13_Landscape.lighting");
        if (lighting == null) throw new InvalidOperationException("Lighting settings missing.");
        lighting.name = "S13_SunsetLighting";
        lighting.bakedGI = true;
        lighting.realtimeGI = false;
        lighting.lightmapResolution = 12f;
        Lightmapping.lightingSettings = lighting;
        LightmapSettings.lightmaps = Array.Empty<LightmapData>();
        LightmapSettings.lightProbes = null;
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

        PlayerSettings.productName = "S13 Sunset Pavilion";
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorUtility.SetDirty(lighting);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"S13 sunset scene built. Pavilion world bounds: {bounds}");
    }

    [MenuItem("S13/Generate sunset lighting")]
    public static void Bake()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool success = Lightmapping.Bake();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        if (!success) throw new InvalidOperationException("Unity Generate Lighting failed.");
        Debug.Log("S13 Generate Lighting complete. Lightmaps: " + LightmapSettings.lightmaps.Length);
    }

    [MenuItem("S13/Verify assignment scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject pavilion = GameObject.Find("Pavilion");
        GameObject walker = GameObject.Find("SceneWalker");
        Camera camera = Camera.main;
        Light sun = GameObject.Find("Low Amber Sun")?.GetComponent<Light>();
        if (pavilion == null || pavilion.GetComponentsInChildren<Renderer>().Length < 3)
            throw new InvalidOperationException("Pavilion model is missing.");
        if (walker == null || walker.GetComponent<SceneWalker>() == null ||
            walker.GetComponent<CharacterController>() == null || camera == null ||
            camera.transform.parent != walker.transform)
            throw new InvalidOperationException("SceneWalker or player camera is incomplete.");
        if (sun == null || sun.shadows == LightShadows.None || sun.color.r <= sun.color.b)
            throw new InvalidOperationException("Sunset sun or shadows are missing.");
        if (!RenderSettings.fog || RenderSettings.ambientMode != AmbientMode.Trilight)
            throw new InvalidOperationException("Sunset fog or ambient light is missing.");
        if (GraphicsSettings.defaultRenderPipeline == null && QualitySettings.renderPipeline == null)
            throw new InvalidOperationException("URP is not assigned.");
        int urpMaterials = 0;
        foreach (Renderer renderer in pavilion.GetComponentsInChildren<Renderer>())
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && material.shader.name.Contains("Universal Render Pipeline")) urpMaterials++;
        if (urpMaterials < 3) throw new InvalidOperationException("Pavilion materials were not converted to URP.");
        Debug.Log($"S13 verified: Pavilion, SceneWalker, URP materials ({urpMaterials}), sunset sun, ambient, fog, shadows; baked lightmaps: {LightmapSettings.lightmaps.Length}.");
    }

    private static Bounds GetBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("Pavilion has no renderers.");
        Bounds result = renderers[0].bounds;
        foreach (Renderer renderer in renderers) result.Encapsulate(renderer.bounds);
        return result;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
    }

    private static Material GetOrMakeMaterial(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.16f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrMakeSky()
    {
        string path = Root + "/Materials/Apricot_Sky.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null) throw new InvalidOperationException("Procedural sky shader missing.");
            sky = new Material(shader) { name = "Apricot_Sky" };
            AssetDatabase.CreateAsset(sky, path);
        }
        sky.SetColor("_SkyTint", new Color(0.49f, 0.42f, 0.63f));
        sky.SetColor("_GroundColor", new Color(0.66f, 0.42f, 0.44f));
        sky.SetFloat("_AtmosphereThickness", 1.15f);
        sky.SetFloat("_SunSize", 0.055f);
        sky.SetFloat("_Exposure", 1.1f);
        EditorUtility.SetDirty(sky);
        return sky;
    }
}
