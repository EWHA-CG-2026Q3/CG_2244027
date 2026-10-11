using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class S10SceneBuilder
{
    private const string Root = "Assets/S10";
    private const string ScenePath = Root + "/Scenes/S10_BouncingBallAnimation.unity";
    private static readonly Color Navy = new Color(0.025f, 0.045f, 0.085f);
    private static readonly Color Turquoise = new Color(0.11f, 0.88f, 0.89f);
    private static readonly Color Coral = new Color(1f, 0.36f, 0.39f);
    private static readonly Color MoonColor = new Color(1f, 0.88f, 0.49f);

    [MenuItem("S10/Build assignment scene")]
    public static void Build()
    {
        EnsureFolder(Root + "/Animations");
        EnsureFolder(Root + "/Materials");
        EnsureFolder(Root + "/Scenes");

        Material ballMaterial = MakeMaterial("Ball_Turquoise", Turquoise);
        Material diamondMaterial = MakeMaterial("Diamond_Coral", Coral);
        Material moonMaterial = MakeMaterial("Moon_Gold", MoonColor);
        Material floorMaterial = MakeMaterial("Floor_Navy", new Color(0.08f, 0.13f, 0.19f));

        AnimationClip orbitClip = MakeClip("Ball_Orbit_4s", "localEulerAnglesRaw.y", 4f,
            new Keyframe(0f, 0f), new Keyframe(4f, 360f));
        AnimationClip bounceClip = MakeClip("Ball_Bounce_0p8s", "m_LocalPosition.y", 0.8f,
            new Keyframe(0f, 0f), new Keyframe(0.4f, 0.55f), new Keyframe(0.8f, 0f));
        AnimationClip moonClip = MakeClip("Moon_Orbit_120frames", "localEulerAnglesRaw.y", 2f,
            new Keyframe(0f, 0f), new Keyframe(2f, 360f));
        AnimatorController orbitController = MakeController("Ball_Orbit", orbitClip);
        AnimatorController bounceController = MakeController("Ball_Bounce", bounceClip);
        AnimatorController moonController = MakeController("Moon_Orbit", moonClip);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCameraAndLight();
        CreateStage("Ball_Pedestal", -0.95f, Turquoise, floorMaterial);
        CreateStage("Diamond_Pedestal", 0.95f, Coral, floorMaterial);

        // Real Unity parent hierarchy: the two course animations animate
        // Ball_Orbit and Ball_Sphere; the moon has its own 120-frame clip.
        Transform ballStage = MakeNode("BallStage", null, new Vector3(-0.95f, 0.55f, 0f));
        Transform ballOrbit = MakeNode("Ball_Orbit", ballStage, Vector3.zero);
        Transform ballOffset = MakeNode("Ball_Offset", ballOrbit, new Vector3(0.42f, 0f, 0f));
        Transform ballSphere = MakeNode("Ball_Sphere", ballOffset, Vector3.zero, Vector3.one * 0.1f);
        MakePrimitive("Ball_Mesh", PrimitiveType.Sphere, ballSphere, Vector3.zero, Vector3.one, ballMaterial);
        AddAnimator(ballOrbit, orbitController);
        AddAnimator(ballSphere, bounceController);

        Transform ballMoonOrbit = MakeNode("Moon_Orbit", ballSphere, Vector3.zero);
        Transform ballMoonOffset = MakeNode("Moon_Offset", ballMoonOrbit, new Vector3(1.5f, 0f, 0f));
        Transform ballMoonSphere = MakeNode("Moon_Sphere", ballMoonOffset, Vector3.zero, Vector3.one * 0.5f);
        MakePrimitive("Moon_Mesh", PrimitiveType.Sphere, ballMoonSphere, Vector3.zero, Vector3.one, moonMaterial);
        AddAnimator(ballMoonOrbit, moonController);

        // The diamond's five elements intentionally all have no parent.
        // S10_DiamondChain_Finish reconstructs the equivalent world transform.
        Transform diamondStage = MakeNode("DiamondStage", null, new Vector3(0.95f, 0.55f, 0f));
        Transform diamondOrbit = MakeNode("Diamond_Orbit", null, Vector3.zero);
        Transform diamondOffset = MakeNode("Diamond_Offset", null, new Vector3(0.42f, 0f, 0f));
        Transform diamondBall = MakeNode("Diamond_Ball", null, Vector3.zero, Vector3.one * 0.1f);
        Transform diamondMesh = MakeDiamond("Diamond_Mesh", diamondMaterial);
        diamondMesh.GetComponent<S10_DiamondChain_Finish>().SetChain(new[]
        {
            diamondStage, diamondOrbit, diamondOffset, diamondBall
        });
        AddAnimator(diamondOrbit, orbitController);
        AddAnimator(diamondBall, bounceController);

        Transform diamondMoonOrbit = MakeNode("Diamond_Moon_Orbit", null, Vector3.zero);
        Transform diamondMoonOffset = MakeNode("Diamond_Moon_Offset", null, new Vector3(1.5f, 0f, 0f));
        Transform diamondMoonSphere = MakeNode("Diamond_Moon_Sphere", null, Vector3.zero, Vector3.one * 0.5f);
        Transform diamondMoonMesh = MakeDiamond("Diamond_Moon_Mesh", moonMaterial);
        diamondMoonMesh.GetComponent<S10_DiamondChain_Finish>().SetChain(new[]
        {
            diamondStage, diamondOrbit, diamondOffset, diamondBall,
            diamondMoonOrbit, diamondMoonOffset, diamondMoonSphere
        });
        AddAnimator(diamondMoonOrbit, moonController);

        Selection.activeGameObject = ballStage.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save the S10 scene.");
        AssetDatabase.ImportAsset(ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "S10 Bouncing Ball and Diamond Moons";
        AssetDatabase.SaveAssets();
        Verify();
        Debug.Log("S10 scene built: real ball hierarchy, parentless diamond chains, three looping clips.");
    }

    [MenuItem("S10/Verify assignment scene")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(ScenePath);
        string[] ballNames = { "BallStage", "Ball_Orbit", "Ball_Offset", "Ball_Sphere", "Ball_Mesh" };
        for (int i = 1; i < ballNames.Length; ++i)
            Check(Find(ballNames[i]).parent == Find(ballNames[i - 1]), "Ball hierarchy " + ballNames[i]);
        string[] moonNames = { "Moon_Orbit", "Moon_Offset", "Moon_Sphere", "Moon_Mesh" };
        Check(Find(moonNames[0]).parent == Find("Ball_Sphere"), "Moon_Orbit parent");
        for (int i = 1; i < moonNames.Length; ++i)
            Check(Find(moonNames[i]).parent == Find(moonNames[i - 1]), "Ball moon hierarchy " + moonNames[i]);

        string[] diamondNames =
        {
            "DiamondStage", "Diamond_Orbit", "Diamond_Offset", "Diamond_Ball", "Diamond_Mesh",
            "Diamond_Moon_Orbit", "Diamond_Moon_Offset", "Diamond_Moon_Sphere", "Diamond_Moon_Mesh"
        };
        foreach (string name in diamondNames)
            Check(Find(name).parent == null, name + " must remain a root");
        string[] expectedChain =
        {
            "DiamondStage", "Diamond_Orbit", "Diamond_Offset", "Diamond_Ball",
            "Diamond_Moon_Orbit", "Diamond_Moon_Offset", "Diamond_Moon_Sphere"
        };
        S10_DiamondChain_Finish moonChain = Find("Diamond_Moon_Mesh").GetComponent<S10_DiamondChain_Finish>();
        Check(moonChain != null && moonChain.Chain.Length == expectedChain.Length, "Moon chain length");
        for (int i = 0; i < expectedChain.Length; ++i)
            Check(moonChain.Chain[i] == Find(expectedChain[i]), "Moon chain order " + i);
        Check(Find("Diamond_Moon_Mesh").GetComponent<DiamondMesh_Rewinded>() != null, "Moon mesh component");

        float ballMoonRadius = Vector3.Distance(Find("Moon_Mesh").position, Find("Ball_Sphere").position);
        Vector3 diamondCenter = Find("Diamond_Mesh").GetComponent<S10_DiamondChain_Finish>().ComposeMatrix().MultiplyPoint3x4(Vector3.zero);
        Vector3 diamondMoonCenter = moonChain.ComposeMatrix().MultiplyPoint3x4(Vector3.zero);
        float diamondMoonRadius = Vector3.Distance(diamondMoonCenter, diamondCenter);
        float ballMoonDiameter = Find("Moon_Mesh").lossyScale.x;
        float diamondMoonDiameter = moonChain.ComposeMatrix().MultiplyVector(Vector3.right).magnitude;
        Check(Mathf.Abs(ballMoonRadius - 0.15f) < 0.0001f, "Ball moon radius");
        Check(Mathf.Abs(diamondMoonRadius - 0.15f) < 0.0001f, "Diamond moon radius");
        Check(Mathf.Abs(ballMoonDiameter - 0.05f) < 0.0001f, "Ball moon diameter");
        Check(Mathf.Abs(diamondMoonDiameter - 0.05f) < 0.0001f, "Diamond moon diameter");

        AnimationClip moonClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animations/Moon_Orbit_120frames.anim");
        Check(moonClip != null && Mathf.Abs(moonClip.length - 2f) < 0.0001f && moonClip.frameRate == 60f,
            "Moon must make one orbit in 120 frames at 60 FPS");
        Check(Find("Ball_Orbit").GetComponent<Animator>() != null, "Ball orbit animation");
        Check(Find("Ball_Sphere").GetComponent<Animator>() != null, "Ball bounce animation");
        Check(Find("Moon_Orbit").GetComponent<Animator>() != null, "Ball moon animation");
        Check(Find("Diamond_Moon_Orbit").GetComponent<Animator>() != null, "Diamond moon animation");
        Debug.Log("S10 verified: both moons diameter=0.05, radius=0.15, orbit=120 frames at 60 FPS; chains and animations connected.");
    }

    private static Transform Find(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found == null) throw new InvalidOperationException("Missing S10 object: " + name);
        return found.transform;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("S10 verification failed: " + description);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("No Lit shader is available.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.48f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.17f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimationClip MakeClip(string name, string property, float duration, params Keyframe[] keys)
    {
        string path = Root + "/Animations/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = 60f;
        AnimationCurve curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; ++i)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), property), curve);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        EditorUtility.SetDirty(clip);
        Check(Mathf.Abs(clip.length - duration) < 0.0001f, name + " clip duration");
        return clip;
    }

    private static AnimatorController MakeController(string name, AnimationClip clip)
    {
        string path = Root + "/Animations/" + name + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Loop");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    private static Transform MakeNode(string name, Transform parent, Vector3 localPosition, Vector3? localScale = null)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.transform.localRotation = Quaternion.identity;
        node.transform.localScale = localScale ?? Vector3.one;
        return node.transform;
    }

    private static Transform MakePrimitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject mesh = GameObject.CreatePrimitive(type);
        mesh.name = name;
        mesh.transform.SetParent(parent, false);
        mesh.transform.localPosition = localPosition;
        mesh.transform.localScale = localScale;
        mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
        return mesh.transform;
    }

    private static Transform MakeDiamond(string name, Material material)
    {
        Transform mesh = MakeNode(name, null, Vector3.zero);
        mesh.gameObject.AddComponent<DiamondMesh_Rewinded>();
        mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        mesh.gameObject.AddComponent<S10_DiamondChain_Finish>();
        return mesh;
    }

    private static void AddAnimator(Transform node, AnimatorController controller)
    {
        Animator animator = node.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private static void CreateCameraAndLight()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 1.27f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 30f;
        cameraObject.transform.position = new Vector3(0f, 1.35f, -3.8f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.75f, 0f));
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.92f, 0.96f, 1f);
        light.intensity = 1.3f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        RenderSettings.ambientLight = new Color(0.32f, 0.39f, 0.5f);
    }

    private static void CreateStage(string name, float x, Color accent, Material floorMaterial)
    {
        Transform platform = MakePrimitive(name, PrimitiveType.Cylinder, null,
            new Vector3(x, -0.02f, 0f), new Vector3(0.55f, 0.035f, 0.55f), floorMaterial);
        Material rim = MakeMaterial(name + "_Accent", accent * 0.55f);
        MakePrimitive(name + "_Rim", PrimitiveType.Cylinder, null,
            new Vector3(x, 0.005f, 0f), new Vector3(0.48f, 0.008f, 0.48f), rim);
    }
}
