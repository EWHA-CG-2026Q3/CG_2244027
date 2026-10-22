using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class S14SceneBuilder
{
    private const string Root = "Assets/S14";
    private const string ScenePath = Root + "/S14_Landscape.unity";
    private const string TextureRoot = "Assets/ThirdParty/Pavilion/Textures/";

    [MenuItem("S14/Complete charcoal, bloom and smoke")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject table = GameObject.Find("S02_MyTable_InPavilion");
        if (table == null || GameObject.Find("SceneWalker") == null)
            throw new InvalidOperationException("The S13 landscape source is incomplete.");

        Vector3 tablePosition = table.transform.position;
        Texture2D metalTexture = RequiredTexture("Lamp.png");
        Texture2D woodTexture = RequiredTexture("Wood_a.png");
        Texture2D stoneTexture = RequiredTexture("Plane_Ground02.png");

        AssignMaterial("Primitive 1 Bronze Vase", Lit("Mat_S14_Primitive_Metal", new Color(0.72f, 0.8f, 0.75f), metalTexture, 0.55f, 0.27f));
        AssignMaterial("Primitive 2 Ceramic Orb", Lit("Mat_S14_Primitive_Wood", Color.white, woodTexture, 0f, 0.2f));
        AssignMaterial("Primitive 3 Slate Box", Lit("Mat_S14_Primitive_Stone", Color.white, stoneTexture, 0.05f, 0.12f));

        Material star = Lit("Mat_S14_Star_Emissive", new Color(0.95f, 0.55f, 0.12f), null, 0.25f, 0.3f);
        star.EnableKeyword("_EMISSION");
        star.SetColor("_EmissionColor", new Color(4f, 1.7f, 0.12f, 1f));
        star.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(star);
        AssignMaterial("My Mesh 1 S03 Star", star);
        AssignMaterial("My Mesh 2 S04 Diamond", Lit("Mat_S14_Diamond_Jade", new Color(0.12f, 0.68f, 0.56f), null, 0.25f, 0.42f));
        AssignMaterial("My Mesh 3 S10 Rewound Diamond", Lit("Mat_S14_Diamond_Ruby", new Color(0.78f, 0.12f, 0.17f), null, 0.1f, 0.36f));

        GameObject previousBowl = GameObject.Find("FireBowl");
        if (previousBowl != null) UnityEngine.Object.DestroyImmediate(previousBowl);

        GameObject bowl = new GameObject("FireBowl");
        bowl.transform.position = tablePosition + new Vector3(0f, 0.875f, -0.34f);
        GameObject bowlBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bowlBody.name = "FireBowl_Body";
        bowlBody.transform.SetParent(bowl.transform, false);
        bowlBody.transform.localScale = new Vector3(0.32f, 0.055f, 0.32f);
        bowlBody.GetComponent<Renderer>().sharedMaterial = Lit("Mat_S14_FireBowl", new Color(0.77f, 0.82f, 0.8f), metalTexture, 0.52f, 0.26f);

        Texture2D coalTexture = GeneratedTexture("T_S14_CoalCracks", true);
        Material coal = Lit("Mat_S14_Coal", new Color(0.17f, 0.12f, 0.1f), coalTexture, 0f, 0.1f);
        coal.SetTexture("_EmissionMap", coalTexture);
        coal.SetColor("_EmissionColor", new Color(5f, 1.5f, 0.14f, 1f));
        coal.EnableKeyword("_EMISSION");
        coal.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(coal);

        Vector3[] coalPositions =
        {
            new Vector3(-0.12f, 0.085f, -0.04f),
            new Vector3(0.10f, 0.09f, -0.07f),
            new Vector3(-0.02f, 0.105f, 0.10f),
            new Vector3(0.13f, 0.07f, 0.11f),
            new Vector3(-0.14f, 0.08f, 0.10f)
        };
        for (int i = 0; i < coalPositions.Length; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            piece.name = "Coal_" + (i + 1);
            piece.transform.SetParent(bowl.transform, true);
            piece.transform.position = bowl.transform.position + coalPositions[i];
            piece.transform.rotation = Quaternion.Euler(i * 23f, i * 41f, i * 17f);
            piece.transform.localScale = new Vector3(0.12f + i % 2 * 0.02f, 0.09f, 0.11f + i % 3 * 0.01f);
            piece.GetComponent<Renderer>().sharedMaterial = coal;
        }

        GameObject fireLightObject = new GameObject("FireBowl_PointLight");
        fireLightObject.transform.SetParent(bowl.transform, true);
        fireLightObject.transform.position = bowl.transform.position + new Vector3(0f, 0.17f, 0f);
        Light fireLight = fireLightObject.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.4f, 0.1f);
        fireLight.intensity = 2.7f;
        fireLight.range = 2.4f;
        fireLight.shadows = LightShadows.Soft;
        fireLight.lightmapBakeType = LightmapBakeType.Realtime;

        BuildSmoke(bowl);
        BuildBloom();

        Camera walkerCamera = GameObject.Find("SceneWalker").GetComponentInChildren<Camera>();
        Camera shotCamera = GameObject.Find("ShotCamera").GetComponent<Camera>();
        walkerCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        shotCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        shotCamera.transform.position = tablePosition + new Vector3(0f, 1.62f, -3.4f);
        shotCamera.transform.LookAt(tablePosition + new Vector3(0f, 1.02f, 0f));
        shotCamera.enabled = false;
        walkerCamera.enabled = true;

        LightingSettings lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(Root + "/S14_Landscape.lighting");
        if (lighting == null) throw new InvalidOperationException("Lighting settings are missing.");
        lighting.name = "S14_Landscape";
        Lightmapping.lightingSettings = lighting;
        EditorUtility.SetDirty(lighting);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath, false);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S14 Fire and Smoke Pavilion";
        AssetDatabase.SaveAssets();
        Debug.Log("S14 landscape built: textured primitives, emissive meshes and coal, point light, Bloom, smoke.");
    }

    [MenuItem("S14/Generate lighting")]
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
        Debug.Log("S14 Generate Lighting complete.");
    }

    [MenuItem("S14/Preview ShotCamera")]
    public static void PreviewShotCamera()
    {
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = false;
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S14/Preview Walker Camera")]
    public static void PreviewWalkerCamera()
    {
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = false;
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S14/Verify scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject bowl = GameObject.Find("FireBowl");
        if (bowl == null || bowl.transform.Find("PS_Smoke")?.GetComponent<ParticleSystem>() == null ||
            bowl.transform.Find("FireBowl_PointLight")?.GetComponent<Light>() == null)
            throw new InvalidOperationException("The fire bowl, smoke or point light is missing.");
        Volume s14Volume = GameObject.Find("S14_GlobalVolume")?.GetComponent<Volume>();
        if (s14Volume?.sharedProfile?.TryGet(out Bloom bloom) != true ||
            !bloom.active || bloom.intensity.value < 1f)
            throw new InvalidOperationException("Bloom is missing or disabled.");
        foreach (string name in new[] { "Primitive 1 Bronze Vase", "Primitive 2 Ceramic Orb", "Primitive 3 Slate Box" })
        {
            Renderer renderer = GameObject.Find(name)?.GetComponent<Renderer>();
            if (renderer?.sharedMaterial?.GetTexture("_BaseMap") == null)
                throw new InvalidOperationException(name + " has no texture.");
        }
        Renderer star = GameObject.Find("My Mesh 1 S03 Star")?.GetComponent<Renderer>();
        if (star?.sharedMaterial == null || !star.sharedMaterial.IsKeywordEnabled("_EMISSION"))
            throw new InvalidOperationException("The custom star is not emissive.");
        if (Camera.main?.GetUniversalAdditionalCameraData().renderPostProcessing != true)
            throw new InvalidOperationException("Camera post-processing is disabled.");
        Debug.Log("S14 verified: FireBowl, coal, smoke, light, Bloom, 3 textured primitives, 3 own meshes, emissive star.");
    }

    private static void BuildSmoke(GameObject bowl)
    {
        GameObject smokeObject = new GameObject("PS_Smoke");
        smokeObject.transform.SetParent(bowl.transform, true);
        smokeObject.transform.position = bowl.transform.position + new Vector3(0f, 0.13f, 0f);
        smokeObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = smoke.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.48f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startColor = new Color(0.62f, 0.53f, 0.45f, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        ParticleSystem.EmissionModule emission = smoke.emission;
        emission.rateOverTime = 10f;
        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.01f;

        ParticleSystem.VelocityOverLifetimeModule velocity = smoke.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.z = new ParticleSystem.MinMaxCurve(0.3f);

        ParticleSystem.NoiseModule noise = smoke.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.72f, 0.38f, 0.2f), 0f),
                new GradientColorKey(new Color(0.48f, 0.43f, 0.4f), 0.45f),
                new GradientColorKey(new Color(0.32f, 0.34f, 0.4f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.36f, 0.2f),
                new GradientAlphaKey(0.22f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        ParticleSystem.ColorOverLifetimeModule color = smoke.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule size = smoke.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f), new Keyframe(0.45f, 1f), new Keyframe(1f, 1.55f)));

        ParticleSystemRenderer renderer = smoke.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 1f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        Texture2D smokeTexture = GeneratedTexture("T_S14_SmokeSoft", false);
        string materialPath = Root + "/Materials/Mat_S14_Smoke.mat";
        Material smokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (smokeMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (shader == null) throw new InvalidOperationException("URP Particles/Lit is unavailable.");
            smokeMaterial = new Material(shader) { name = "Mat_S14_Smoke" };
            AssetDatabase.CreateAsset(smokeMaterial, materialPath);
        }
        smokeMaterial.SetTexture("_BaseMap", smokeTexture);
        smokeMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.62f));
        smokeMaterial.SetFloat("_Surface", 1f);
        smokeMaterial.SetFloat("_Blend", 0f);
        smokeMaterial.SetFloat("_Smoothness", 0f);
        smokeMaterial.SetFloat("_PreserveSpecularLighting", 0f);
        smokeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        smokeMaterial.SetOverrideTag("RenderType", "Transparent");
        smokeMaterial.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(smokeMaterial);
        renderer.sharedMaterial = smokeMaterial;
    }

    private static void BuildBloom()
    {
        GameObject previous = GameObject.Find("S14_GlobalVolume");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        string folder = Root + "/Profiles";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Profiles");
        string path = folder + "/S14_Bloom.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.components.RemoveAll(component => component == null);
        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
        if (!AssetDatabase.Contains(bloom)) AssetDatabase.AddObjectToAsset(bloom, profile);
        bloom.active = true;
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.5f);
        bloom.scatter.Override(0.75f);
        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(bloom);

        GameObject volumeObject = new GameObject("S14_GlobalVolume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private static Material Lit(string name, Color color, Texture2D texture, float metallic, float smoothness)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D RequiredTexture(string name)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + name);
        if (texture == null) throw new InvalidOperationException("Missing third-party texture: " + name);
        return texture;
    }

    private static void AssignMaterial(string objectName, Material material)
    {
        Renderer renderer = GameObject.Find(objectName)?.GetComponent<Renderer>();
        if (renderer == null) throw new InvalidOperationException("Missing scene object: " + objectName);
        renderer.sharedMaterial = material;
    }

    private static Texture2D GeneratedTexture(string name, bool coal)
    {
        string folder = Root + "/Textures";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root, "Textures");
        string path = folder + "/" + name + ".asset";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                if (coal)
                {
                    float wave = Mathf.Sin(42f * u + 4f * Mathf.Sin(17f * v)) *
                                 Mathf.Sin(35f * v + 5f * Mathf.Sin(19f * u));
                    float cracks = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(wave) * 5f), 3f);
                    pixels[y * size + x] = Color.Lerp(
                        new Color(0.008f, 0.006f, 0.004f, 1f),
                        new Color(1f, 0.34f + 0.24f * cracks, 0.015f, 1f), cracks);
                }
                else
                {
                    float dx = (u - 0.5f) * 2f;
                    float dy = (v - 0.5f) * 2f;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }
}
