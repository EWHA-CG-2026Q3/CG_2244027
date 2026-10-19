using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class S11SceneBuilder
{
    private const string Root = "Assets/S11";
    private const string DemoPath = Root + "/Scenes/S11_VirtualCamera_Finish.unity";
    private const string DiamondPath = Root + "/Scenes/S11_DiamondView.unity";

    [MenuItem("S11/Build both assignment scenes")]
    public static void Build()
    {
        EnsureFolder(Root + "/Editor");
        EnsureFolder(Root + "/Materials");
        EnsureFolder(Root + "/Meshes");
        EnsureFolder(Root + "/Scenes");
        BuildDemo();
        BuildDiamondView();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(DemoPath, true),
            new EditorBuildSettingsScene(DiamondPath, true)
        };
        PlayerSettings.productName = "S11 Virtual Camera";
        AssetDatabase.SaveAssets();
        Verify();
        EditorSceneManager.OpenScene(DemoPath);
        Debug.Log("S11 built: demo camera and copied S10 diamond scene have live canvas projection.");
    }

    [MenuItem("S11/Verify both assignment scenes")]
    public static void Verify()
    {
        VerifyDemo();
        VerifyDiamondView();
        Debug.Log("S11 verified: manual matrix multiply, matching cameras, four demo targets, " +
                  "diamond/moon targets, 120-frame moon controller and moving canvas alignment.");
    }

    private static void BuildDemo()
    {
        Material sphereMaterial = MakeMaterial("Sphere_Teal", new Color(0.08f, 0.78f, 0.76f));
        Material cubeMaterial = MakeMaterial("Cube_Orange", new Color(1f, 0.48f, 0.22f));
        Material diamondMaterial = MakeMaterial("Diamond_Pink", new Color(0.95f, 0.3f, 0.61f));
        Material pyramidMaterial = MakeMaterial("Pyramid_Lilac", new Color(0.57f, 0.54f, 1f));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera(new Vector3(2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 2f);
        CreateLight();

        MeshFilter sphere = MakePrimitive("Sphere Target", PrimitiveType.Sphere,
            new Vector3(6f, 0f, 1.35f), Vector3.one * 0.75f, sphereMaterial);
        MeshFilter cube = MakePrimitive("Cube Target", PrimitiveType.Cube,
            new Vector3(6f, 0f, 0.45f), Vector3.one * 0.75f, cubeMaterial);
        GameObject diamond = new GameObject("Diamond Target");
        diamond.transform.position = new Vector3(6f, 0f, -0.45f);
        diamond.transform.localScale = Vector3.one * 0.75f;
        diamond.AddComponent<DiamondMesh_Rewinded>();
        diamond.GetComponent<MeshRenderer>().sharedMaterial = diamondMaterial;
        MeshFilter pyramid = MakeMeshObject("Pyramid Target", CreatePyramidMesh(),
            new Vector3(6f, 0f, -1.35f), pyramidMaterial);
        pyramid.transform.localScale = Vector3.one * 0.75f;

        S11_VirtualCamera_Finish virtualCamera = CreateVirtualCamera(camera,
            new[] { sphere, cube, diamond.GetComponent<MeshFilter>(), pyramid }, 2f,
            new Color(0.027f, 0.058f, 0.1f));
        virtualCamera.RefreshCanvas();
        EditorSceneManager.MarkSceneDirty(scene);
        SaveScene(scene, DemoPath);
    }

    private static void BuildDiamondView()
    {
        // This is a copied S10 scene saved under S11; the original S10 project remains untouched.
        Scene scene = EditorSceneManager.OpenScene(DiamondPath);
        // Rebuilding must not stack multiple canvases onto the copied S10 scene.
        RemoveExistingVirtualCameras();
        Camera camera = Find("Main Camera").GetComponent<Camera>();
        Check(camera != null, "S10 Main Camera must exist in copied scene");
        Vector3 cameraPosition = new Vector3(0.95f, 0.63f, -3f);
        Quaternion cameraRotation = Quaternion.identity;
        camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        camera.orthographic = true;
        camera.orthographicSize = 0.82f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 20f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.045f, 0.085f);

        MeshFilter diamond = Find("Diamond_Mesh").GetComponent<MeshFilter>();
        MeshFilter moon = Find("Diamond_Moon_Mesh").GetComponent<MeshFilter>();
        Check(diamond != null && moon != null, "S10 diamond and moon meshes");
        diamond.GetComponent<S10_DiamondChain_Finish>().Refresh();
        moon.GetComponent<S10_DiamondChain_Finish>().Refresh();
        S11_VirtualCamera_Finish virtualCamera = CreateVirtualCamera(camera,
            new[] { diamond, moon }, 0.82f, camera.backgroundColor);
        virtualCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        virtualCamera.RefreshCanvas();
        EditorSceneManager.MarkSceneDirty(scene);
        SaveScene(scene, DiamondPath);
    }

    private static void VerifyDemo()
    {
        EditorSceneManager.OpenScene(DemoPath);
        S11_VirtualCamera_Finish virtualCamera = Find("VirtualCamera").GetComponent<S11_VirtualCamera_Finish>();
        Check(virtualCamera != null, "demo VirtualCamera component");
        Check(UnityEngine.Object.FindObjectsByType<S11_VirtualCamera_Finish>(FindObjectsSortMode.None).Length == 1,
            "exactly one demo VirtualCamera");
        Check(virtualCamera.transform.position == new Vector3(2f, 0f, 0f), "demo camera position");
        Check(Quaternion.Angle(virtualCamera.transform.rotation, Quaternion.Euler(0f, 90f, 0f)) < 0.001f,
            "demo camera rotation");
        Check(Mathf.Approximately(virtualCamera.Size, 2f) &&
              Mathf.Approximately(virtualCamera.Near, 0.3f) &&
              Mathf.Approximately(virtualCamera.Far, 20f), "demo size and clip planes");
        Check(virtualCamera.Targets.Length == 4, "four targets");
        string[] names = { "Sphere Target", "Cube Target", "Diamond Target", "Pyramid Target" };
        for (int i = 0; i < names.Length; ++i)
            Check(virtualCamera.Targets[i].gameObject.name == names[i], "target " + names[i]);
        CheckCameraMatch(virtualCamera);
        CheckManualMultiply();
        virtualCamera.RefreshCanvas();
        Check(virtualCamera.DrawingCanvas.SegmentCount > 50, "demo canvas draws edges");
        CheckCanvasGeometry(virtualCamera.DrawingCanvas);
        CheckProjection(virtualCamera, true);
    }

    private static void VerifyDiamondView()
    {
        EditorSceneManager.OpenScene(DiamondPath);
        S11_VirtualCamera_Finish virtualCamera = Find("VirtualCamera").GetComponent<S11_VirtualCamera_Finish>();
        Check(virtualCamera != null, "diamond VirtualCamera component");
        Check(UnityEngine.Object.FindObjectsByType<S11_VirtualCamera_Finish>(FindObjectsSortMode.None).Length == 1,
            "exactly one diamond VirtualCamera");
        Check(virtualCamera.Targets.Length == 2, "two diamond targets");
        Check(virtualCamera.Targets[0].gameObject.name == "Diamond_Mesh", "diamond target");
        Check(virtualCamera.Targets[1].gameObject.name == "Diamond_Moon_Mesh", "moon target");
        CheckCameraMatch(virtualCamera);
        Check(Find("Diamond_Mesh").GetComponent<S10_DiamondChain_Finish>() != null,
            "copied S10 diamond chain");
        Check(Find("Diamond_Moon_Mesh").GetComponent<S10_DiamondChain_Finish>() != null,
            "copied S10 moon chain");
        Check(Find("Diamond_Moon_Orbit").GetComponent<Animator>() != null,
            "copied 120-frame moon animation");
        VerifyMovingDiamond(virtualCamera);
    }

    private static void VerifyMovingDiamond(S11_VirtualCamera_Finish virtualCamera)
    {
        Transform orbit = Find("Diamond_Orbit").transform;
        Transform ball = Find("Diamond_Ball").transform;
        Transform moonOrbit = Find("Diamond_Moon_Orbit").transform;
        Quaternion originalOrbit = orbit.localRotation;
        Vector3 originalBallPosition = ball.localPosition;
        Quaternion originalMoonOrbit = moonOrbit.localRotation;
        S10_DiamondChain_Finish diamondChain = Find("Diamond_Mesh").GetComponent<S10_DiamondChain_Finish>();
        S10_DiamondChain_Finish moonChain = Find("Diamond_Moon_Mesh").GetComponent<S10_DiamondChain_Finish>();
        try
        {
            float[] sampleTimes = { 0f, 0.5f, 1f, 1.5f, 2f, 3f };
            foreach (float seconds in sampleTimes)
            {
                orbit.localRotation = Quaternion.Euler(0f, 90f * seconds, 0f);
                ball.localPosition = new Vector3(0f, 0.55f * Mathf.Abs(Mathf.Sin(seconds * Mathf.PI / 0.8f)), 0f);
                moonOrbit.localRotation = Quaternion.Euler(0f, 180f * seconds, 0f);
                diamondChain.Refresh();
                moonChain.Refresh();
                virtualCamera.RefreshCanvas();
                Check(virtualCamera.DrawingCanvas.SegmentCount > 0,
                    "moving diamond canvas at t=" + seconds);
                CheckCanvasGeometry(virtualCamera.DrawingCanvas);
                CheckProjection(virtualCamera, true);
            }
        }
        finally
        {
            orbit.localRotation = originalOrbit;
            ball.localPosition = originalBallPosition;
            moonOrbit.localRotation = originalMoonOrbit;
            diamondChain.Refresh();
            moonChain.Refresh();
            virtualCamera.RefreshCanvas();
        }
    }

    private static void CheckProjection(S11_VirtualCamera_Finish virtualCamera, bool requireInside)
    {
        Camera camera = virtualCamera.MainCamera;
        foreach (MeshFilter filter in virtualCamera.Targets)
        {
            Check(filter != null && filter.sharedMesh != null, "target mesh exists");
            foreach (Vector3 localPoint in filter.sharedMesh.vertices)
            {
                Vector3 worldPoint = filter.transform.TransformPoint(localPoint);
                Vector3 virtualPoint = virtualCamera.ProjectWorldToViewport(worldPoint, camera.aspect);
                Vector3 realPoint = camera.WorldToViewportPoint(worldPoint);
                Check(Vector2.Distance(virtualPoint, realPoint) < 0.0001f,
                    "canvas/Game projection alignment for " + filter.name);
                Check(virtualPoint.z >= virtualCamera.Near && virtualPoint.z <= virtualCamera.Far,
                    "target lies between near/far planes");
                if (requireInside)
                    Check(virtualPoint.x >= 0f && virtualPoint.x <= 1f &&
                          virtualPoint.y >= 0f && virtualPoint.y <= 1f,
                        "target stays inside canvas: " + filter.name);
            }
        }
    }

    private static void CheckCanvasGeometry(S11_CanvasWireGraphic graphic)
    {
        Canvas.ForceUpdateCanvases();
        Mesh generated = graphic.canvasRenderer.GetMesh();
        Check(generated != null && generated.vertexCount > 0,
            "CanvasRenderer receives projected edge geometry");
    }

    private static void CheckCameraMatch(S11_VirtualCamera_Finish virtualCamera)
    {
        Camera camera = virtualCamera.MainCamera;
        Check(camera != null && camera.orthographic, "orthographic Main Camera");
        Check(Vector3.Distance(camera.transform.position, virtualCamera.transform.position) < 0.0001f,
            "matching camera position");
        Check(Quaternion.Angle(camera.transform.rotation, virtualCamera.transform.rotation) < 0.001f,
            "matching camera rotation");
        Check(Mathf.Abs(camera.orthographicSize - virtualCamera.Size) < 0.0001f,
            "matching camera size");
        Check(virtualCamera.DrawingCanvas != null &&
              virtualCamera.DrawingCanvas.canvas.renderMode == RenderMode.ScreenSpaceOverlay,
            "overlay canvas");
    }

    private static void CheckManualMultiply()
    {
        Matrix4x4 A = Matrix4x4.TRS(new Vector3(2f, -1f, 3f),
            Quaternion.Euler(13f, 27f, -9f), new Vector3(1.3f, 0.8f, 2f));
        Matrix4x4 B = Matrix4x4.TRS(new Vector3(-4f, 2f, 1f),
            Quaternion.Euler(-11f, 58f, 23f), new Vector3(0.7f, 1.2f, 1.1f));
        Matrix4x4 expected = A * B;
        Matrix4x4 actual = S11_VirtualCamera_Finish.MultiplyMatrixMatrix(A, B);
        for (int row = 0; row < 4; ++row)
            for (int column = 0; column < 4; ++column)
                Check(Mathf.Abs(expected[row, column] - actual[row, column]) < 0.0001f,
                    "manual matrix multiply element " + row + "," + column);
    }

    private static S11_VirtualCamera_Finish CreateVirtualCamera(Camera camera,
        MeshFilter[] targets, float size, Color background)
    {
        GameObject virtualObject = new GameObject("VirtualCamera");
        virtualObject.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        S11_VirtualCamera_Finish component = virtualObject.AddComponent<S11_VirtualCamera_Finish>();
        GameObject canvasObject = new GameObject("VirtualCamera Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        GameObject drawingObject = new GameObject("Projected Mesh Edges", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(S11_CanvasWireGraphic));
        drawingObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = drawingObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        S11_CanvasWireGraphic graphic = drawingObject.GetComponent<S11_CanvasWireGraphic>();
        graphic.raycastTarget = false;
        component.Configure(size, 0.3f, 20f, targets, graphic, camera);
        camera.backgroundColor = background;
        return component;
    }

    private static Camera CreateCamera(Vector3 position, Quaternion rotation, float size)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 20f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.94f, 0.96f, 1f);
        light.intensity = 1.3f;
        lightObject.transform.rotation = Quaternion.Euler(25f, -120f, 0f);
        RenderSettings.ambientLight = new Color(0.37f, 0.42f, 0.5f);
    }

    private static MeshFilter MakePrimitive(string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj.GetComponent<MeshFilter>();
    }

    private static MeshFilter MakeMeshObject(string name, Mesh mesh, Vector3 position, Material material)
    {
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.position = position;
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj.GetComponent<MeshFilter>();
    }

    private static Mesh CreatePyramidMesh()
    {
        string path = Root + "/Meshes/Pyramid_5Vertices.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = "S11 Pyramid (5 vertices, 6 triangles)" };
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.55f, -0.5f),
            new Vector3(0.5f, -0.55f, -0.5f),
            new Vector3(0.5f, -0.55f, 0.5f),
            new Vector3(-0.5f, -0.55f, 0.5f),
            new Vector3(0f, 0.78f, 0f)
        };
        mesh.triangles = new[]
        {
            4, 1, 0, 4, 2, 1, 4, 3, 2, 4, 0, 3,
            0, 1, 2, 0, 2, 3
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Standard shader missing");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.1f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void RemoveExistingVirtualCameras()
    {
        GameObject oldObject;
        while ((oldObject = GameObject.Find("VirtualCamera")) != null)
            UnityEngine.Object.DestroyImmediate(oldObject);
        while ((oldObject = GameObject.Find("VirtualCamera Canvas")) != null)
            UnityEngine.Object.DestroyImmediate(oldObject);
    }

    private static GameObject Find(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null) throw new InvalidOperationException("S11 missing object: " + name);
        return obj;
    }

    private static void SaveScene(Scene scene, string path)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new InvalidOperationException("Could not save " + path);
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("S11 verification failed: " + label);
    }
}
