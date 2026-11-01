using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S16SceneBuilder
{
    private const string Root = "Assets/S16";
    private const string ScenePath = Root + "/S16_Shader_Practice.unity";

    [MenuItem("S16/Build shader practice scene")]
    public static void Build()
    {
        AssetDatabase.Refresh();

        Shader myShader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/S16_MyShader.shader");
        Shader coalShader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/S16_MyCoal.shader");
        Material targetCoal = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Mat_S14_Coal.mat");
        if (myShader == null || coalShader == null || targetCoal == null ||
            !myShader.isSupported || !coalShader.isSupported)
            throw new InvalidOperationException("S16 source shaders or target coal material are missing.");

        Material red = MakeMaterial("Mat_S16_MyShader", myShader);
        Material cyan = MakeMaterial("Mat_S16_MyCoal", coalShader);
        cyan.SetColor("_BaseColor", new Color(0.03f, 0.16f, 0.26f, 1f));
        cyan.SetColor("_EmissionColor", new Color(0.13f, 0.64f, 0.71f, 1f));
        EditorUtility.SetDirty(cyan);

        Material magenta = MakeMaterial("Mat_S16_MyCoal_Magenta", coalShader);
        magenta.SetColor("_BaseColor", new Color(0.22f, 0.03f, 0.28f, 1f));
        magenta.SetColor("_EmissionColor", new Color(0.69f, 0.12f, 0.41f, 1f));
        EditorUtility.SetDirty(magenta);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
        Material floor = MakeMaterial("Mat_S16_Stage", lit);
        floor.SetColor("_BaseColor", new Color(0.075f, 0.105f, 0.155f, 1f));
        floor.SetFloat("_Metallic", 0.32f);
        floor.SetFloat("_Smoothness", 0.36f);
        EditorUtility.SetDirty(floor);

        Material pedestal = MakeMaterial("Mat_S16_Pedestal", lit);
        pedestal.SetColor("_BaseColor", new Color(0.19f, 0.25f, 0.34f, 1f));
        pedestal.SetFloat("_Metallic", 0.55f);
        pedestal.SetFloat("_Smoothness", 0.57f);
        EditorUtility.SetDirty(pedestal);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakePrimitive("Stage", PrimitiveType.Cube, new Vector3(0, -1.82f, 0.3f),
            new Vector3(9.2f, 0.22f, 3f), floor);

        float[] xPositions = { -3.3f, -1.1f, 1.1f, 3.3f };
        foreach (float x in xPositions)
        {
            MakePrimitive("Pedestal_" + x, PrimitiveType.Cylinder,
                new Vector3(x, -1.55f, 0), new Vector3(1.62f, 0.12f, 1.62f), pedestal);
        }

        GameObject coal = MakePrimitive("Target_Coal", PrimitiveType.Sphere,
            new Vector3(-3.3f, -0.3f, 0), Vector3.one * 1.6f, targetCoal);
        coal.transform.rotation = Quaternion.Euler(17f, 26f, -14f);

        MakePrimitive("Sphere_OurShader", PrimitiveType.Sphere,
            new Vector3(-1.1f, -0.3f, 0), Vector3.one * 1.6f, red);

        GameObject star = new GameObject("Star_OurMesh_S03");
        star.transform.position = new Vector3(1.1f, -0.3f, 0);
        star.transform.localScale = Vector3.one * 0.35f;
        star.AddComponent<MeshFilter>();
        star.AddComponent<MeshRenderer>().sharedMaterial = cyan;
        star.AddComponent<S03_CustomPolygonMesh_Star>().RebuildMesh();

        GameObject diamond = new GameObject("Diamond_OurMesh_S04");
        diamond.transform.position = new Vector3(3.3f - 0.7f, -0.3f, -0.7f);
        diamond.transform.localScale = Vector3.one * 1.4f;
        diamond.AddComponent<MeshFilter>();
        diamond.AddComponent<MeshRenderer>().sharedMaterial = magenta;
        diamond.AddComponent<S04_CustomDiamondMesh>().BuildDiamond();

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.9f, 0.8f, 1f);
        light.intensity = 1.6f;
        lightObject.transform.rotation = Quaternion.Euler(35f, -28f, 0);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.065f, 0.105f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 2.55f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 40f;
        cameraObject.transform.position = new Vector3(0, 0.2f, -12f);
        cameraObject.transform.LookAt(new Vector3(0, -0.2f, 0));

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S16 Shader Practice";
        AssetDatabase.SaveAssets();
        Debug.Log("S16 built: target coal, shader sphere, S03 star, and S04 diamond.");
    }

    [MenuItem("S16/Verify shader practice scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check("Target_Coal", "Universal Render Pipeline/Lit");
        Check("Sphere_OurShader", "CG/S16_MyShader");
        Check("Star_OurMesh_S03", "CG/S16_MyCoal");
        Check("Diamond_OurMesh_S04", "CG/S16_MyCoal");
        if (GameObject.Find("Star_OurMesh_S03")?.GetComponent<MeshFilter>()?.sharedMesh == null ||
            GameObject.Find("Diamond_OurMesh_S04")?.GetComponent<MeshFilter>()?.sharedMesh == null)
            throw new InvalidOperationException("One of the two custom meshes is missing.");
        Debug.Log("S16 verified: coal, shader sphere, and both custom meshes use the intended materials.");
    }

    private static void Check(string objectName, string shaderName)
    {
        Renderer renderer = GameObject.Find(objectName)?.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null || renderer.sharedMaterial.shader.name != shaderName)
            throw new InvalidOperationException(objectName + " has an incorrect material.");
    }

    private static Material MakeMaterial(string name, Shader shader)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        return material;
    }

    private static GameObject MakePrimitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
    }
}
