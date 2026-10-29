using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class S15SceneBuilder
{
    private const string Root = "Assets/S15";
    private const string ScenePath = Root + "/S15_Landscape.unity";
    private const string TextureRoot = "Assets/ThirdParty/Textures/";

    [MenuItem("S15/Build flowing landscape")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Shader flowShader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/S15_SG_Flow.shadergraph");
        if (flowShader == null || flowShader.isSupported == false)
            throw new InvalidOperationException("S15_SG_Flow Shader Graph did not import as a supported shader.");

        Texture2D water = Texture("Water_Y2K_Seamless.png");
        Texture2D lava = Texture("Lava_Seamless.png");

        Material tableFlow = FlowMaterial("Mat_S15_SG_Flow", flowShader, water, water,
            new Vector2(0f, 0.025f), new Vector2(0.015f, -0.018f), new Vector2(0.45f, 0.45f), 0.25f, 0.35f);
        GameObject table = GameObject.Find("S02_MyTable_InPavilion");
        if (table == null) throw new InvalidOperationException("The S02 table is missing.");
        Renderer tabletop = Array.Find(table.GetComponentsInChildren<Renderer>(true), r => r.name == "Tabletop");
        if (tabletop == null) throw new InvalidOperationException("The table top is missing.");
        tabletop.sharedMaterial = tableFlow;

        // Practice 6 keeps the table effect. The assignment's additional flow is
        // a separate, clearly visible pool on the pavilion floor.
        Material poolFlow = FlowMaterial("Mat_S15_SG_Flow_FloorPool", flowShader, water, water,
            new Vector2(0.045f, 0.015f), new Vector2(-0.025f, 0.035f),
            new Vector2(0.45f, 0.45f), 0.42f, 1.8f);
        BuildFloorPool(table.transform.position, poolFlow);

        Material lavaFlow = FlowMaterial("Mat_S15_SG_Flow_Lava", flowShader, lava, lava,
            new Vector2(0.04f, 0.025f), new Vector2(-0.03f, 0.06f), new Vector2(1.5f, 1.5f), 0.35f, 1.2f);
        GameObject bowl = GameObject.Find("FireBowl");
        if (bowl == null) throw new InvalidOperationException("The S14 fire bowl is missing.");
        Transform oldLava = bowl.transform.Find("Lava_Flow_Quad");
        if (oldLava != null) UnityEngine.Object.DestroyImmediate(oldLava.gameObject);
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Lava_Flow_Quad";
        quad.transform.SetParent(bowl.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0.069f, 0f);
        quad.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        quad.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = lavaFlow;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

        ParticleSystem smoke = bowl.transform.Find("PS_Smoke")?.GetComponent<ParticleSystem>();
        if (smoke == null || !smoke.colorOverLifetime.enabled || !smoke.sizeOverLifetime.enabled)
            throw new InvalidOperationException("The completed S14 smoke effect is missing.");

        Camera shot = GameObject.Find("ShotCamera")?.GetComponent<Camera>();
        Camera walker = GameObject.Find("SceneWalker")?.GetComponentInChildren<Camera>();
        if (shot == null || walker == null) throw new InvalidOperationException("The cameras are missing.");
        shot.transform.position = table.transform.position + new Vector3(0f, 2.15f, -4.55f);
        shot.transform.LookAt(table.transform.position + new Vector3(0f, 0.42f, -0.62f));
        shot.enabled = false;
        walker.enabled = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S15 Flowing Pavilion Pool";
        AssetDatabase.SaveAssets();
        Debug.Log("S15 built: subtle practice flow on the table, a separate flowing floor pool, and fire-bowl lava.");
    }

    [MenuItem("S15/Preview ShotCamera")]
    public static void PreviewShot()
    {
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = false;
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S15/Preview Walker Camera")]
    public static void PreviewWalker()
    {
        GameObject.Find("ShotCamera").GetComponent<Camera>().enabled = false;
        GameObject.Find("SceneWalker").GetComponentInChildren<Camera>().enabled = true;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem("S15/Verify scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject bowl = GameObject.Find("FireBowl");
        GameObject table = GameObject.Find("S02_MyTable_InPavilion");
        GameObject pool = GameObject.Find("Water_Basin_Floor");
        if (bowl == null || table == null || pool == null ||
            bowl.transform.Find("Lava_Flow_Quad") == null || pool.transform.Find("Water_Flow_Plane") == null)
            throw new InvalidOperationException("The table, floor pool, or lava flow is missing.");
        Renderer tabletop = Array.Find(table.GetComponentsInChildren<Renderer>(true), r => r.name == "Tabletop");
        Material tableMaterial = tabletop?.sharedMaterial;
        Material lavaMaterial = bowl.transform.Find("Lava_Flow_Quad").GetComponent<Renderer>().sharedMaterial;
        Material poolMaterial = pool.transform.Find("Water_Flow_Plane").GetComponent<Renderer>().sharedMaterial;
        if (tableMaterial == null || lavaMaterial == null || poolMaterial == null ||
            tableMaterial.shader != lavaMaterial.shader || tableMaterial.shader != poolMaterial.shader ||
            tableMaterial.GetTexture("_MainTex1") == null || tableMaterial.GetTexture("_MainTex2") == null ||
            lavaMaterial.GetTexture("_MainTex1") == null || poolMaterial.GetTexture("_MainTex1") == null)
            throw new InvalidOperationException("The two-layer flow materials are incomplete.");
        ParticleSystem smoke = bowl.transform.Find("PS_Smoke")?.GetComponent<ParticleSystem>();
        if (smoke == null || !smoke.colorOverLifetime.enabled || !smoke.sizeOverLifetime.enabled)
            throw new InvalidOperationException("The smoke is incomplete.");
        Debug.Log("S15 verified: smoke, subtle table practice flow, separate floor pool flow, lava flow, shared Shader Graph.");
    }

    private static void BuildFloorPool(Vector3 tablePosition, Material flow)
    {
        GameObject oldPool = GameObject.Find("Water_Basin_Floor");
        if (oldPool != null) UnityEngine.Object.DestroyImmediate(oldPool);

        GameObject pool = new GameObject("Water_Basin_Floor");
        pool.transform.position = tablePosition + new Vector3(0f, 0.025f, -1.7f);
        Material rim = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Mat_S14_Primitive_Stone.mat");
        if (rim == null) throw new InvalidOperationException("The stone rim material is missing.");

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water_Flow_Plane";
        water.transform.SetParent(pool.transform, false);
        water.transform.localPosition = new Vector3(0f, 0.045f, 0f);
        water.transform.localScale = new Vector3(0.136f, 1f, 0.082f);
        water.GetComponent<Renderer>().sharedMaterial = flow;
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());

        PoolRim(pool.transform, "Rim_Front", new Vector3(0f, 0.05f, -0.44f), new Vector3(1.52f, 0.1f, 0.08f), rim);
        PoolRim(pool.transform, "Rim_Back", new Vector3(0f, 0.05f, 0.44f), new Vector3(1.52f, 0.1f, 0.08f), rim);
        PoolRim(pool.transform, "Rim_Left", new Vector3(-0.72f, 0.05f, 0f), new Vector3(0.08f, 0.1f, 0.8f), rim);
        PoolRim(pool.transform, "Rim_Right", new Vector3(0.72f, 0.05f, 0f), new Vector3(0.08f, 0.1f, 0.8f), rim);
    }

    private static void PoolRim(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rim.name = name;
        rim.transform.SetParent(parent, false);
        rim.transform.localPosition = position;
        rim.transform.localScale = scale;
        rim.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Texture2D Texture(string name)
    {
        string path = TextureRoot + name;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture missing: " + path);
        if (importer.wrapMode != TextureWrapMode.Repeat || importer.maxTextureSize != 1024 ||
            importer.textureCompression != TextureImporterCompression.Uncompressed || importer.filterMode != FilterMode.Trilinear)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material FlowMaterial(string name, Shader shader, Texture2D tex1, Texture2D tex2,
        Vector2 flow1, Vector2 flow2, Vector2 tiling, float blend, float glow)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetTexture("_MainTex1", tex1);
        material.SetTexture("_MainTex2", tex2);
        material.SetVector("_Flow1", flow1);
        material.SetVector("_Flow2", flow2);
        material.SetVector("_Tiling", tiling);
        material.SetFloat("_Blend", blend);
        material.SetFloat("_Glow", glow);
        EditorUtility.SetDirty(material);
        return material;
    }
}
