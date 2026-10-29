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

        Texture2D water1 = Texture("Water_Current_1.png");
        Texture2D water2 = Texture("Water_Current_2.png");
        Texture2D lava = Texture("Lava_Seamless.png");

        Material tableFlow = FlowMaterial("Mat_S15_SG_Flow", flowShader, water1, water2,
            new Vector2(0f, 0.05f), new Vector2(0.02f, 0.1f), new Vector2(1f, 1f), 0.5f, 1.8f);
        GameObject table = GameObject.Find("S02_MyTable_InPavilion");
        if (table == null) throw new InvalidOperationException("The S02 table is missing.");
        Renderer tabletop = Array.Find(table.GetComponentsInChildren<Renderer>(true), r => r.name == "Tabletop");
        if (tabletop == null) throw new InvalidOperationException("The table top is missing.");
        tabletop.sharedMaterial = tableFlow;

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
        shot.enabled = false;
        walker.enabled = true;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S15 Flowing Water and Lava Pavilion";
        AssetDatabase.SaveAssets();
        Debug.Log("S15 built: two-layer flowing water on the S02 table and a second seamless lava flow in the fire bowl.");
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
        if (bowl == null || table == null || bowl.transform.Find("Lava_Flow_Quad") == null)
            throw new InvalidOperationException("The table or second flow is missing.");
        Renderer tabletop = Array.Find(table.GetComponentsInChildren<Renderer>(true), r => r.name == "Tabletop");
        Material tableMaterial = tabletop?.sharedMaterial;
        Material lavaMaterial = bowl.transform.Find("Lava_Flow_Quad").GetComponent<Renderer>().sharedMaterial;
        if (tableMaterial == null || lavaMaterial == null || tableMaterial.shader != lavaMaterial.shader ||
            tableMaterial.GetTexture("_MainTex1") == null || tableMaterial.GetTexture("_MainTex2") == null ||
            lavaMaterial.GetTexture("_MainTex1") == null)
            throw new InvalidOperationException("The two-layer flow materials are incomplete.");
        ParticleSystem smoke = bowl.transform.Find("PS_Smoke")?.GetComponent<ParticleSystem>();
        if (smoke == null || !smoke.colorOverLifetime.enabled || !smoke.sizeOverLifetime.enabled)
            throw new InvalidOperationException("The smoke is incomplete.");
        Debug.Log("S15 verified: smoke, two-layer table flow, seamless lava flow, shared S15 Shader Graph.");
    }

    private static Texture2D Texture(string name)
    {
        string path = TextureRoot + name;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture missing: " + path);
        if (importer.wrapMode != TextureWrapMode.Repeat || importer.maxTextureSize != 1024)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = 1024;
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
