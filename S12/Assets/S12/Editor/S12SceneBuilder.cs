using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class S12SceneBuilder
{
    private const string Root = "Assets/S12";
    private const string Demo = Root + "/Scenes/S12_VirtualCamera_Finish.unity";
    private const string StillLife = Root + "/Scenes/S12_StillLife.unity";

    [MenuItem("S12/Build assignment scenes")]
    public static void Build()
    {
        Ensure("Scenes"); Ensure("Materials"); Ensure("Meshes"); Ensure("Prefabs");
        BuildDemo();
        BuildStillLife();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Demo, true),
            new EditorBuildSettingsScene(StillLife, true)
        };
        PlayerSettings.productName = "S12 Perspective Virtual Camera";
        AssetDatabase.SaveAssets();
        Verify();
        EditorSceneManager.OpenScene(StillLife);
        Debug.Log("S12 scenes built and verified.");
    }

    [MenuItem("S12/Verify assignment scenes")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(Demo);
        S12_VirtualCamera_Finish demo = GetVirtual();
        Require(demo.transform.position == new Vector3(2f, 0f, 0f), "demo position");
        Require(Quaternion.Angle(demo.transform.rotation, Quaternion.Euler(0f, 90f, 0f)) < 0.01f, "demo rotation");
        Require(demo.FieldOfView == 90f && demo.Near == 0.3f && demo.Far == 20f, "demo camera settings");
        Require(demo.Targets.Length == 2 && demo.Targets[0].name == "Ball" &&
            demo.Targets[1].name == "Ball_Far", "demo targets");
        VerifyProjection(demo, 90f);
        VerifyProjection(demo, 60f); // Temporarily test both FOVs, then restore 90.
        EditorSceneManager.OpenScene(StillLife);
        S12_VirtualCamera_Finish still = GetVirtual();
        Require(still.Targets.Length >= 8, "all table pieces and meshes are targets");
        Require(still.MainCamera.transform.parent == still.transform, "Main Camera is a child");
        Require(still.MainCamera.transform.localPosition == Vector3.zero &&
            still.MainCamera.transform.localRotation == Quaternion.identity, "child camera local transform");
        Require(GameObject.Find("S02_Table") != null, "S02 table prefab");
        Require(GameObject.Find("S03_Star") != null && GameObject.Find("S04_Diamond") != null &&
            GameObject.Find("Custom_Pyramid") != null, "three handmade meshes");
        Require(still.GetComponent<S12_CameraDolly>() != null, "moving virtual camera");
        VerifyProjection(still, 45f);
        Debug.Log("S12 verification passed: 90/60 degree camera agreement, table + 3 handmade meshes, child camera and moving view.");
    }

    private static void BuildDemo()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = MakeCamera(new Vector3(2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 90f);
        MeshFilter ball = Primitive("Ball", PrimitiveType.Sphere,
            new Vector3(5f, 0.42f, -0.8f), Vector3.one * 0.95f, Mat("Ball_Teal", new Color(0.06f, 0.82f, 0.81f)));
        MeshFilter farBall = Primitive("Ball_Far", PrimitiveType.Sphere,
            new Vector3(9f, -0.2f, 1.3f), Vector3.one * 1.2f, Mat("Ball_Far_Orange", new Color(1f, 0.45f, 0.18f)));
        Light();
        MakeVirtual(camera, new[] { ball, farBall }, 90f);
        EditorSceneManager.SaveScene(scene, Demo);
    }

    private static void BuildStillLife()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Vector3 cameraPosition = new Vector3(0f, 1.55f, -4f);
        Quaternion cameraRotation = Quaternion.LookRotation(new Vector3(0f, 0.65f, 1.8f) - cameraPosition);
        Camera camera = MakeCamera(cameraPosition, cameraRotation, 45f);
        Light();

        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/S02_Table.prefab");
        Require(tablePrefab != null, "copied S02 prefab exists");
        GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
        table.name = "S02_Table";
        table.transform.position = new Vector3(0f, -0.25f, 1.9f);
        Material tableMaterial = Mat("S02_Table_Wood", new Color(0.39f, 0.23f, 0.13f));
        foreach (MeshRenderer renderer in table.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterial = tableMaterial;

        MeshFilter star = Custom("S03_Star", StarMesh(), new Vector3(-0.72f, 0.62f, 1.45f),
            new Vector3(0.72f, 0.72f, 0.72f), Mat("Star_Gold", new Color(1f, 0.72f, 0.12f)));
        star.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        MeshFilter diamond = Custom("S04_Diamond", DiamondMesh(), new Vector3(0.55f, 0.87f, 2.05f),
            Vector3.one * 0.57f, Mat("Diamond_Rose", new Color(0.95f, 0.22f, 0.53f)));
        MeshFilter pyramid = Custom("Custom_Pyramid", PyramidMesh(), new Vector3(1.65f, 0.03f, 3.85f),
            Vector3.one * 0.95f, Mat("Pyramid_Lilac", new Color(0.46f, 0.43f, 1f)));
        Primitive("Near_Cube", PrimitiveType.Cube, new Vector3(-1.85f, -0.05f, 0.3f),
            Vector3.one * 0.65f, Mat("Near_Cube_Cyan", new Color(0.06f, 0.78f, 0.93f)));
        Primitive("Far_Ball", PrimitiveType.Sphere, new Vector3(-1.55f, 0.2f, 4.4f),
            Vector3.one * 0.55f, Mat("Far_Ball_Mint", new Color(0.29f, 0.9f, 0.59f)));

        List<MeshFilter> targets = new List<MeshFilter>();
        targets.AddRange(table.GetComponentsInChildren<MeshFilter>());
        targets.Add(star); targets.Add(diamond); targets.Add(pyramid);
        targets.Add(GameObject.Find("Near_Cube").GetComponent<MeshFilter>());
        targets.Add(GameObject.Find("Far_Ball").GetComponent<MeshFilter>());
        S12_VirtualCamera_Finish virtualCamera = MakeVirtual(camera, targets.ToArray(), 45f);
        camera.transform.SetParent(virtualCamera.transform, true);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        virtualCamera.gameObject.AddComponent<S12_CameraDolly>();
        EditorSceneManager.SaveScene(scene, StillLife);
    }

    private static S12_VirtualCamera_Finish MakeVirtual(Camera camera, MeshFilter[] targets, float fov)
    {
        GameObject virtualObject = new GameObject("VirtualCamera");
        virtualObject.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        S12_VirtualCamera_Finish virtualCamera = virtualObject.AddComponent<S12_VirtualCamera_Finish>();
        GameObject canvasObject = new GameObject("VirtualCamera Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        GameObject wireObject = new GameObject("Projected Mesh Edges", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(S12_CanvasWireGraphic));
        wireObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = wireObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        S12_CanvasWireGraphic graphic = wireObject.GetComponent<S12_CanvasWireGraphic>();
        graphic.raycastTarget = false;
        virtualCamera.Configure(fov, 0.3f, 20f, targets, graphic, camera);
        return virtualCamera;
    }

    private static Camera MakeCamera(Vector3 position, Quaternion rotation, float fov)
    {
        GameObject obj = new GameObject("Main Camera");
        obj.tag = "MainCamera";
        Camera camera = obj.AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.orthographic = false;
        camera.fieldOfView = fov;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 20f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.026f, 0.045f, 0.087f);
        obj.AddComponent<AudioListener>();
        return camera;
    }

    private static void Light()
    {
        GameObject obj = new GameObject("Key Light");
        UnityEngine.Light light = obj.AddComponent<UnityEngine.Light>();
        light.type = LightType.Directional;
        light.intensity = 1.45f;
        light.color = new Color(1f, 0.93f, 0.84f);
        light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.55f);
    }

    private static MeshFilter Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.GetComponent<MeshFilter>();
    }

    private static MeshFilter Custom(string name, Mesh mesh, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj.GetComponent<MeshFilter>();
    }

    private static Mesh StarMesh()
    {
        Vector3[] vertices = new Vector3[11];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < 10; i++)
        {
            float angle = (90f + i * 36f) * Mathf.Deg2Rad;
            float r = i % 2 == 0 ? 0.62f : 0.27f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
        }
        int[] triangles = new int[30];
        for (int i = 0; i < 10; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % 10 + 1;
        }
        return SaveMesh("S03_Star_10Vertices", vertices, triangles);
    }

    private static Mesh DiamondMesh()
    {
        Vector3[] v =
        {
            new Vector3(0f, 0.7f, 0f), new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0.5f), new Vector3(0f, -0.65f, 0f)
        };
        int[] t = { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4,
            5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
        return SaveMesh("S04_Diamond_6Vertices_8Triangles", v, t);
    }

    private static Mesh PyramidMesh()
    {
        Vector3[] v =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0f, 0.65f, 0f)
        };
        int[] t = { 4, 1, 0, 4, 2, 1, 4, 3, 2, 4, 0, 3, 0, 1, 2, 0, 2, 3 };
        return SaveMesh("Custom_Pyramid_5Vertices", v, t);
    }

    private static Mesh SaveMesh(string name, Vector3[] vertices, int[] triangles)
    {
        string path = Root + "/Meshes/" + name + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static Material Mat(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void VerifyProjection(S12_VirtualCamera_Finish virtualCamera, float fov)
    {
        Camera camera = virtualCamera.MainCamera;
        Require(!camera.orthographic, "perspective Main Camera");
        Require(Vector3.Distance(camera.transform.position, virtualCamera.transform.position) < 0.0001f,
            "matching positions");
        Require(Quaternion.Angle(camera.transform.rotation, virtualCamera.transform.rotation) < 0.001f,
            "matching rotations");
        // Test the 60-degree variation without changing serialized assignment values.
        float oldCameraFov = camera.fieldOfView;
        var field = typeof(S12_VirtualCamera_Finish).GetField("fieldOfView",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float oldVirtualFov = (float)field.GetValue(virtualCamera);
        camera.fieldOfView = fov;
        field.SetValue(virtualCamera, fov);
        try
        {
            foreach (MeshFilter target in virtualCamera.Targets)
            {
                Require(target != null && target.sharedMesh != null, "target mesh");
                foreach (Vector3 vertex in target.sharedMesh.vertices)
                {
                    Vector3 world = target.transform.TransformPoint(vertex);
                    Vector3 actual = camera.WorldToViewportPoint(world);
                    Vector3 expected = virtualCamera.ProjectWorldToViewport(world, camera.aspect);
                    Require(Vector2.Distance(actual, expected) < 0.0001f,
                        "canvas/Game projection for " + target.name + " at FOV " + fov);
                    Require(expected.z >= virtualCamera.Near && expected.z <= virtualCamera.Far,
                        "target lies between clip planes");
                }
            }
            Canvas.ForceUpdateCanvases();
            virtualCamera.RefreshCanvas();
            Require(virtualCamera.DrawingCanvas.SegmentCount > 0, "visible wire segments");
        }
        finally
        {
            camera.fieldOfView = oldCameraFov;
            field.SetValue(virtualCamera, oldVirtualFov);
            virtualCamera.RefreshCanvas();
        }
    }

    private static S12_VirtualCamera_Finish GetVirtual()
    {
        GameObject obj = GameObject.Find("VirtualCamera");
        Require(obj != null, "VirtualCamera object");
        S12_VirtualCamera_Finish result = obj.GetComponent<S12_VirtualCamera_Finish>();
        Require(result != null, "VirtualCamera script");
        return result;
    }

    private static void Ensure(string subfolder)
    {
        if (!AssetDatabase.IsValidFolder(Root + "/" + subfolder))
            AssetDatabase.CreateFolder(Root, subfolder);
    }

    private static void Require(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException("S12 verification failed: " + name);
    }
}
