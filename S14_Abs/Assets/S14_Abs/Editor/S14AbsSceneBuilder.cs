using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S14AbsSceneBuilder
{
    private const string Root = "Assets/S14_Abs";
    private const string ScenePath = Root + "/S14_Abs_Landscape.unity";

    [MenuItem("S14 Abs/Build lantern and sparks")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject table = GameObject.Find("S02_MyTable_InPavilion");
        GameObject walker = GameObject.Find("SceneWalker");
        if (table == null || walker == null)
            throw new InvalidOperationException("The S13 pavilion source is incomplete.");

        GameObject oldBowl = GameObject.Find("FireBowl");
        if (oldBowl != null) UnityEngine.Object.DestroyImmediate(oldBowl);
        GameObject oldLantern = GameObject.Find("S14Abs_Lantern");
        if (oldLantern != null) UnityEngine.Object.DestroyImmediate(oldLantern);

        Vector3 tablePosition = table.transform.position;
        GameObject lantern = new GameObject("S14Abs_Lantern");
        lantern.transform.position = tablePosition + new Vector3(0f, 1.01f, -0.34f);
        Texture2D metalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/ThirdParty/Pavilion/Textures/Lamp.png");
        if (metalTexture == null) throw new InvalidOperationException("Pavilion metal texture is missing.");
        Material metal = Lit("Mat_S14Abs_LanternMetal", new Color(0.65f, 0.78f, 0.8f), metalTexture, 0.75f);
        Material glow = Lit("Mat_S14Abs_LanternGlow", new Color(1f, 0.72f, 0.21f), null, 0f);
        glow.EnableKeyword("_EMISSION");
        glow.SetColor("_EmissionColor", new Color(5f, 2.4f, 0.24f, 1f));
        glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(glow);

        Primitive(lantern, PrimitiveType.Cylinder, "Lantern_Base", new Vector3(0f, -0.16f, 0f),
            new Vector3(0.20f, 0.025f, 0.20f), metal);
        Primitive(lantern, PrimitiveType.Cylinder, "Lantern_Roof", new Vector3(0f, 0.26f, 0f),
            new Vector3(0.23f, 0.035f, 0.23f), metal);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.14f : 0.14f;
            float z = i < 2 ? -0.14f : 0.14f;
            Primitive(lantern, PrimitiveType.Cube, "Lantern_Frame_" + i,
                new Vector3(x, 0.055f, z), new Vector3(0.025f, 0.39f, 0.025f), metal);
        }
        Primitive(lantern, PrimitiveType.Sphere, "Lantern_Glow", new Vector3(0f, 0.05f, 0f),
            new Vector3(0.18f, 0.18f, 0.18f), glow);

        GameObject lightObject = new GameObject("Lantern_PointLight");
        lightObject.transform.SetParent(lantern.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.25f);
        light.intensity = 2.8f;
        light.range = 2.7f;
        light.shadows = LightShadows.Soft;
        light.lightmapBakeType = LightmapBakeType.Realtime;

        BuildSparks(lantern);
        GameObject volumeObject = GameObject.Find("S14Abs_GlobalVolume") ?? GameObject.Find("S14_GlobalVolume");
        Volume volume = volumeObject?.GetComponent<Volume>();
        if (volume == null || volume.sharedProfile == null || !volume.sharedProfile.TryGet(out Bloom bloom))
            throw new InvalidOperationException("The source Bloom profile is missing.");
        volume.gameObject.name = "S14Abs_GlobalVolume";
        bloom.active = true;
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.2f);
        EditorUtility.SetDirty(bloom);

        Camera mainCamera = walker.GetComponentInChildren<Camera>();
        Camera shotCamera = GameObject.Find("ShotCamera")?.GetComponent<Camera>();
        if (mainCamera == null || shotCamera == null)
            throw new InvalidOperationException("Pavilion cameras are missing.");
        mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        shotCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        shotCamera.transform.position = tablePosition + new Vector3(0f, 1.62f, -3.4f);
        shotCamera.transform.LookAt(tablePosition + new Vector3(0f, 1.04f, 0f));
        shotCamera.enabled = false;
        mainCamera.enabled = true;

        LightingSettings lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(Root + "/S14_Abs_Landscape.lighting");
        if (lighting == null) throw new InvalidOperationException("Lighting settings are missing.");
        lighting.name = "S14_Abs_Landscape";
        Lightmapping.lightingSettings = lighting;
        EditorUtility.SetDirty(lighting);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath, false);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S14 Abs Lantern Pavilion";
        AssetDatabase.SaveAssets();
        Debug.Log("S14 Abs lantern built: textured metal, emissive core, point light, Bloom and sparks.");
    }

    [MenuItem("S14 Abs/Generate lighting")]
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
        Debug.Log("S14 Abs Generate Lighting complete.");
    }

    [MenuItem("S14 Abs/Preview ShotCamera")]
    public static void PreviewShotCamera()
    {
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = false;
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S14 Abs/Preview Walker Camera")]
    public static void PreviewWalkerCamera()
    {
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = false;
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S14 Abs/Verify scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject lantern = GameObject.Find("S14Abs_Lantern");
        if (lantern == null || GameObject.Find("FireBowl") != null ||
            lantern.transform.Find("Lantern_PointLight")?.GetComponent<Light>() == null ||
            lantern.transform.Find("PS_Sparks")?.GetComponent<ParticleSystem>() == null)
            throw new InvalidOperationException("Lantern, light or sparks are missing, or the fire bowl remains.");
        Renderer glow = lantern.transform.Find("Lantern_Glow")?.GetComponent<Renderer>();
        if (glow?.sharedMaterial?.IsKeywordEnabled("_EMISSION") != true)
            throw new InvalidOperationException("Lantern is not emissive.");
        Volume volume = GameObject.Find("S14Abs_GlobalVolume")?.GetComponent<Volume>();
        if (volume?.sharedProfile?.TryGet(out Bloom bloom) != true || !bloom.active || bloom.intensity.value < 1f)
            throw new InvalidOperationException("Bloom is missing.");
        Debug.Log("S14 Abs verified: metal lantern, emissive core, point light, sparks and Bloom.");
    }

    private static GameObject Primitive(GameObject parent, PrimitiveType type, string name,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = localScale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static Material Lit(string name, Color color, Texture2D texture, float metallic)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", 0.4f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildSparks(GameObject lantern)
    {
        GameObject particleObject = new GameObject("PS_Sparks");
        particleObject.transform.SetParent(lantern.transform, false);
        particleObject.transform.localPosition = new Vector3(0.15f, 0.27f, 0f);
        particleObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 4f;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.15f);
        main.startColor = new Color(1f, 0.68f, 0.25f, 0.8f);
        main.maxParticles = 100;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 32f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 0.09f;
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.09f;
        noise.frequency = 0.8f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.82f, 0.42f), 0f),
                    new GradientColorKey(new Color(1f, 0.32f, 0.08f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        Texture2D softTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/T_S14_SmokeSoft.asset");
        if (softTexture == null) throw new InvalidOperationException("Soft particle texture is missing.");
        string path = Root + "/Materials/Mat_S14Abs_Sparks.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) throw new InvalidOperationException("URP particle shader is missing.");
            material = new Material(shader) { name = "Mat_S14Abs_Sparks" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", softTexture);
        material.SetColor("_BaseColor", new Color(1f, 0.75f, 0.3f, 1f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.sharedMaterial = material;
    }
}
