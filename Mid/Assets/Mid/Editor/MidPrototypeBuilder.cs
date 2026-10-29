using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MidPrototypeBuilder
{
    const string Root = "Assets/Mid";
    const string ScenePath = Root + "/Scenes/Mid_PlushAudience.unity";

    [MenuItem("Mid/Build plush audience")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Generated");
        Directory.CreateDirectory(Root + "/Scenes");
        MidFeltTexture.Generate();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cream = Mat("Fur", new Color(0.83f, 0.65f, 0.43f));
        var muzzle = Mat("Cream", new Color(1f, 0.88f, 0.67f));
        var dark = Mat("Cocoa", new Color(0.10f, 0.065f, 0.045f));
        var pink = Mat("Blush", new Color(0.91f, 0.48f, 0.39f));
        var green = Mat("SeatGreen", new Color(0.07f, 0.29f, 0.19f));
        var gold = Mat("SeatFrame", new Color(0.23f, 0.34f, 0.24f));
        var floor = Mat("Sand", new Color(0.83f, 0.83f, 0.69f));
        var ballMat = Mat("BallCoral", new Color(1f, 0.31f, 0.24f));
        var white = Mat("White", new Color(1f, 0.96f, 0.85f));
        Mesh round = RoundedMesh();
        var chair = new GameObject("Chair").transform;
        Part("Seat cushion", chair, round, new Vector3(0, .66f, .08f), new Vector3(1.85f, .3f, 1.3f), green);
        Part("Rounded backrest", chair, round, new Vector3(0, 1.45f, .59f), new Vector3(1.9f, 1.65f, .32f), green);
        foreach (float x in new[] { -.74f, .74f })
        {
            Part("Front leg", chair, round, new Vector3(x, .3f, -.35f), new Vector3(.12f, .7f, .12f), gold);
            Part("Rear leg", chair, round, new Vector3(x, .3f, .55f), new Vector3(.12f, .7f, .12f), gold);
        }
        var doll = new GameObject("Bear - stationary body").transform;
        Mesh body = Lathe("PearBody", 32, 20, t =>
        {
            float y = t * 2 - 1;
            return new Vector2(Mathf.Sqrt(Mathf.Max(0, 1-y*y)) * (.48f - .12f*y), y * .60f);
        });
        Part("Procedural pear body", doll, body, new Vector3(0, 1.31f, -.05f), Vector3.one, cream);
        Sphere("Belly patch", doll, new Vector3(0, 1.28f, -.51f), new Vector3(.60f, .72f, .24f), muzzle);
        foreach (float side in new[] { -1f, 1f })
        {
            var arm = Sphere("Arm", doll, new Vector3(side*.49f, 1.26f, -.13f), new Vector3(.29f, .68f, .34f), cream);
            arm.localRotation = Quaternion.Euler(0, 0, side*17);
            Sphere("Paw", doll, new Vector3(side*.34f, .84f, -.48f), new Vector3(.42f, .31f, .57f), cream);
            Sphere("Paw pad", doll, new Vector3(side*.34f, .83f, -.72f), new Vector3(.24f, .19f, .12f), muzzle);
        }
        var neck = new GameObject("NeckPivot - yaw and pitch").transform;
        neck.SetParent(doll, false); neck.localPosition = new Vector3(0, 1.76f, -.06f);
        Sphere("Head", neck, new Vector3(0, .40f, 0), new Vector3(1.17f, 1.02f, .93f), cream);
        foreach (float side in new[] { -1f, 1f })
        {
            Sphere("Ear", neck, new Vector3(side*.46f, .82f, 0), new Vector3(.39f, .39f, .22f), cream);
            Sphere("Ear inner", neck, new Vector3(side*.46f, .82f, -.105f), new Vector3(.23f, .24f, .055f), pink);
            Sphere("Eye", neck, new Vector3(side*.225f, .47f, -.425f), new Vector3(.085f, .105f, .065f), dark);
            Sphere("Eye glint", neck, new Vector3(side*.225f-.012f, .49f, -.456f), Vector3.one*.022f, white);
        }
        Sphere("Muzzle", neck, new Vector3(0, .24f, -.43f), new Vector3(.48f, .31f, .24f), muzzle);
        Sphere("Nose", neck, new Vector3(0, .31f, -.55f), new Vector3(.13f, .085f, .07f), dark);
        Sphere("Mouth", neck, new Vector3(0, .18f, -.546f), new Vector3(.065f, .035f, .018f), dark);
        // Make three species once; the crowd instances share their generated fur meshes.
        var rabbit = UnityEngine.Object.Instantiate(doll); rabbit.name = "Rabbit - stationary body";
        var dog = UnityEngine.Object.Instantiate(doll); dog.name = "Dog - stationary body";
        ConfigureSpecies(rabbit, "Rabbit", Mat("RabbitFur", new Color(.87f,.84f,.77f)), muzzle, pink);
        ConfigureSpecies(dog, "Dog", Mat("DogFur", new Color(.76f,.57f,.35f)), muzzle, Mat("DogEar", new Color(.37f,.22f,.12f)));
        MidPlushFur.Add(doll, "Bear"); MidPlushFur.Add(rabbit, "Rabbit"); MidPlushFur.Add(dog, "Dog");
        var prototypes = new[] { rabbit, dog, doll };
        var spectators = new System.Collections.Generic.List<MidBallTracking.Spectator>();
        float[] speeds = { 3.2f, 6.3f, 4.2f, 5.5f, 2.7f, 4.8f, 3.7f, 6.0f, 4.5f };
        for (int row=0; row<3; row++)
        for (int col=0; col<3; col++)
        {
            int index=row*3+col, kind=(col+row)%3;
            Vector3 position=new Vector3((col-1)*2.15f + (row%2)*.18f, row*1.05f, row*1.85f);
            var seat=UnityEngine.Object.Instantiate(chair); seat.name="Seat "+(index+1); seat.position=position;
            var animal=UnityEngine.Object.Instantiate(prototypes[kind]); animal.name=new[]{"Rabbit", "Dog", "Bear"}[kind]+" "+(index+1); animal.position=position;
            spectators.Add(new MidBallTracking.Spectator {Neck=animal.Find("NeckPivot - yaw and pitch"), FollowSpeed=speeds[index]});
        }
        foreach(var prototype in prototypes) UnityEngine.Object.DestroyImmediate(prototype.gameObject);
        UnityEngine.Object.DestroyImmediate(chair.gameObject);
        for(int row=0;row<3;row++)
            Part("Terrace "+(row+1), null, round, new Vector3(0,row*.525f-.08f,row*1.85f+.2f), new Vector3(7.2f,row*1.05f+.16f,1.9f), green);
        var ball = Sphere("Mouse ball", null, new Vector3(0, 3.2f, -3), Vector3.one*.45f, ballMat);
        Sphere("Ball spot", ball, new Vector3(0, .20f, -.42f), Vector3.one*.33f, white);
        Part("Stage", null, round, new Vector3(0, -.12f, 0), new Vector3(60, .22f, 60), floor);

        var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 6.5f, -13f);
        camera.transform.LookAt(new Vector3(0, 2.55f, 1.3f));
        camera.orthographic = true; camera.orthographicSize = 4.25f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.90f, .90f, .79f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 50;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.53f, .53f, .51f);
        Light("Large soft key", new Vector3(42,-32,0), .58f, new Color(1,.96f,.90f), true);
        Light("Soft fill", new Vector3(30, 145, 0), .30f, new Color(.78f, .87f, 1), false);
        var controller = new GameObject("Mouse and head controller").AddComponent<MidBallTracking>();
        controller.ViewCamera = camera; controller.Ball = ball; controller.Spectators = spectators.ToArray(); controller.Neck = spectators[0].Neck;
        controller.ResetPose();
        PlayerSettings.productName = "Little Spectators";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 800;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.antiAliasing = 4;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath, true)};
        AssetDatabase.SaveAssets();
        Verify();
        Capture();
        Debug.Log("MID_BUILD_OK");
    }

    static void ConfigureSpecies(Transform doll, string species, Material fur, Material cream, Material ear)
    {
        foreach(var renderer in doll.GetComponentsInChildren<MeshRenderer>())
        {
            string n=renderer.name;
            if(n=="Head" || n=="Procedural pear body" || n=="Arm" || n=="Paw" || n=="Ear") renderer.sharedMaterial=fur;
        }
        Transform neck=doll.Find("NeckPivot - yaw and pitch");
        foreach(Transform part in neck)
        {
            float side=Mathf.Sign(part.localPosition.x);
            if(part.name=="Ear")
            {
                if(species=="Rabbit")
                {
                    part.localPosition=new Vector3(side*.31f,1.02f,.02f);
                    part.localScale=new Vector3(.29f,.96f,.25f);
                    part.localRotation=Quaternion.Euler(0,0,-side*12);
                }
                else
                {
                    part.localPosition=new Vector3(side*.53f,.40f,.02f);
                    part.localScale=new Vector3(.37f,.85f,.34f);
                    part.localRotation=Quaternion.Euler(0,0,side*12);
                    part.GetComponent<Renderer>().sharedMaterial=ear;
                }
            }
            if(part.name=="Ear inner")
            {
                if(species=="Rabbit")
                {
                    part.localPosition=new Vector3(side*.31f,1.04f,-.105f);
                    part.localScale=new Vector3(.13f,.68f,.04f);
                    part.localRotation=Quaternion.Euler(0,0,-side*12);
                    part.GetComponent<Renderer>().sharedMaterial=cream;
                }
                else part.gameObject.SetActive(false);
            }
            if(species=="Rabbit" && part.name=="Nose") { part.localScale=new Vector3(.085f,.065f,.045f); part.GetComponent<Renderer>().sharedMaterial=ear; }
            if(species=="Dog" && part.name=="Muzzle") part.localScale=new Vector3(.55f,.36f,.32f);
            if(species=="Dog" && part.name=="Nose") { part.localPosition=new Vector3(0,.32f,-.60f); part.localScale=new Vector3(.19f,.13f,.10f); }
        }
    }

    public static Material Mat(string name, Color color)
    {
        string path = Root + "/Generated/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Mid/FeltSurface")); AssetDatabase.CreateAsset(mat, path); }
        mat.shader=Shader.Find("Mid/FeltSurface");
        mat.color = color;
        mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Generated/FeltFiber.png"));
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Generated/FeltNormal.png"));
        mat.SetFloat("_Scale",name=="Sand"?.65f:name.StartsWith("Seat")?1.2f:2.5f);
        mat.SetFloat("_BumpStrength",name=="Cocoa"?.08f:.32f);
        EditorUtility.SetDirty(mat); return mat;
    }

    public static Transform Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; return go.transform;
    }

    static Transform Part(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<Renderer>().sharedMaterial = material;
        return go.transform;
    }

    // Ring-by-ring vertex and triangle construction, shared by the custom body and rounded seat.
    static Mesh Lathe(string name, int sides, int rings, Func<float, Vector2> profile)
    {
        var vertices = new Vector3[(rings+1)*(sides+1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[rings*sides*6];
        int k = 0;
        for (int row=0; row<=rings; row++)
        {
            float t = (float)row/rings; var p = profile(t);
            for (int col=0; col<=sides; col++)
            {
                float angle = (float)col/sides*Mathf.PI*2;
                int index = row*(sides+1)+col;
                vertices[index] = new Vector3(p.x*Mathf.Cos(angle), p.y, p.x*Mathf.Sin(angle));
                uv[index] = new Vector2((float)col/sides, t);
                if (row == rings || col == sides) continue;
                int next = index+sides+1;
                triangles[k++]=index; triangles[k++]=next; triangles[k++]=index+1;
                triangles[k++]=index+1; triangles[k++]=next; triangles[k++]=next+1;
            }
        }
        var mesh = new Mesh {name=name, vertices=vertices, triangles=triangles, uv=uv};
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Root+"/Generated/"+name+".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) { existing.Clear(); existing.vertices=mesh.vertices; existing.triangles=mesh.triangles; existing.normals=mesh.normals; existing.uv=mesh.uv; existing.bounds=mesh.bounds; EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); return existing; }
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    static Mesh RoundedMesh()
    {
        // Superellipse rings form a rounded cushion, unlike the pear-shaped body.
        Mesh mesh = Lathe("RoundedCushion", 48, 24, t => {
            float a = (t-.5f)*Mathf.PI;
            return new Vector2(.5f*Mathf.Pow(Mathf.Max(0, Mathf.Cos(a)), .35f),
                .5f*Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), .35f));
        });
        var v = mesh.vertices;
        for (int i=0;i<v.Length;i++)
        {
            float r = new Vector2(v[i].x,v[i].z).magnitude;
            if(r < .00001f) continue;
            float x = v[i].x/r, z=v[i].z/r;
            v[i].x=r*Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x), .35f);
            v[i].z=r*Mathf.Sign(z)*Mathf.Pow(Mathf.Abs(z), .35f);
        }
        mesh.vertices=v; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); return mesh;
    }

    static void Light(string name, Vector3 rotation, float intensity, Color color, bool shadows)
    {
        var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
        light.type=LightType.Directional; light.transform.rotation=Quaternion.Euler(rotation);
        light.intensity=intensity; light.color=color;
        light.shadows=shadows ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength=.38f; light.shadowBias=.06f;
    }

    [MenuItem("Mid/Verify tracking")]
    public static void Verify() => VerifyCount(9);

    public static void VerifyCount(int expectedCount)
    {
        var c=UnityEngine.Object.FindFirstObjectByType<MidBallTracking>();
        if(!c) throw new Exception("Missing tracking controller");
        if(c.Spectators.Length!=expectedCount) throw new Exception("Unexpected spectator count");
        var poses=new Matrix4x4[expectedCount];
        for(int i=0;i<expectedCount;i++) poses[i]=c.Spectators[i].Neck.parent.localToWorldMatrix;
        // Four diagonals + upper/lower + horizontal targets test combined rotations.
        foreach(float x in new[]{-100f,0f,100f})
        foreach(float y in new[]{-100f,100f})
        {
            c.ResetPose(); c.SetWorldTarget(new Vector3(x,y,20));
            for(int frame=0;frame<360;frame++) c.Step(1f/60);
            if(Mathf.Abs(c.Ball.position.x-Mathf.Clamp(x,c.HorizontalBounds.x,c.HorizontalBounds.y))>.001f || Mathf.Abs(c.Ball.position.z-c.BallPlaneZ)>.001f)
                throw new Exception("Ball bounds/plane failed");
            for(int i=0;i<expectedCount;i++)
            {
                var spectator=c.Spectators[i];
                Vector3 direction=spectator.Neck.parent.InverseTransformDirection(c.Ball.position-spectator.Neck.position);
                float expectedYaw=Mathf.Clamp(Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg,-c.MaxYaw,c.MaxYaw);
                float expectedPitch=Mathf.Clamp(Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg,-c.MaxDownPitch,c.MaxUpPitch);
                if(Mathf.Abs(spectator.Yaw-expectedYaw)>.01f || Mathf.Abs(spectator.Pitch-expectedPitch)>.01f) throw new Exception("Yaw/pitch target failed");
                var face=spectator.Neck.localRotation*Vector3.back;
                if(Mathf.Sign(face.y)!=Mathf.Sign(expectedPitch)) throw new Exception("Pitch sign inverted");
                if(Mathf.Abs(spectator.Yaw)>c.MaxYaw+.01f || spectator.Pitch>c.MaxUpPitch+.01f || spectator.Pitch < -c.MaxDownPitch-.01f) throw new Exception("Neck limit failed");
                if(spectator.Neck.parent.localToWorldMatrix!=poses[i]) throw new Exception("Body moved");
            }
        }
        // Same geometric target, different response speeds must yield different transient poses.
        var fast=c.Spectators[1]; var slow=c.Spectators[0];
        Transform original=fast.Neck; float oldSpeed=fast.FollowSpeed;
        fast.Neck=slow.Neck; fast.FollowSpeed=slow.FollowSpeed*2;
        c.ResetPose(); c.SetWorldTarget(new Vector3(4,6,-3)); c.Step(.1f);
        if(Mathf.Abs(fast.Yaw)<=Mathf.Abs(slow.Yaw)) throw new Exception("Independent speed failed");
        fast.Neck=original; fast.FollowSpeed=oldSpeed;
        c.ResetPose(); var before=c.TargetPosition;
        if(c.MoveCursor(new Vector2(-100,-100)) || c.TargetPosition!=before) throw new Exception("Outside pointer changed target");
        Vector3 desired=new Vector3(.75f,3.5f,-3);
        if(!c.MoveCursor(c.ViewCamera.WorldToScreenPoint(desired)) || Vector3.Distance(c.TargetPosition,desired)>.01f) throw new Exception("Screen projection failed");
        c.ResetPose();
        foreach(var spectator in c.Spectators)
            if(spectator.Yaw!=0 || spectator.Pitch!=0 || Quaternion.Angle(spectator.Neck.localRotation,Quaternion.identity)>.01f) throw new Exception("Reset failed");
        foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if(filter.name!="Short plush fibers") continue;
            var source=filter.transform.parent.GetComponent<MeshFilter>().sharedMesh.bounds;
            var generated=filter.sharedMesh.bounds;
            if(Vector3.Distance(source.center,generated.center)>.15f || (generated.extents-source.extents).magnitude>.25f)
                throw new Exception("Fur does not fit source part: "+filter.transform.parent.name);
        }
        if(!AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Generated/FeltNormal.png")) throw new Exception("Missing felt texture");
        Debug.Log("MID_VERIFY_OK: spectator count verified; 6 directional targets; independent speeds; pitch/yaw limits; bodies fixed; mouse projection; reset.");
    }

    [MenuItem("Mid/Save preview")]
    public static void Capture()
    {
        var c=UnityEngine.Object.FindFirstObjectByType<MidBallTracking>();
        Directory.CreateDirectory("Previews");
        foreach(float side in new[]{-1f,0f,1f})
        {
            c.ResetPose(); c.SetWorldTarget(new Vector3(side*3.7f,side==0?3.2f:side<0?1f:6.2f,-3));
            for(int i=0;i<120;i++) c.Step(1f/60);
            var rt=new RenderTexture(1280,800,24); var old=RenderTexture.active;
            c.ViewCamera.targetTexture=rt; c.ViewCamera.Render(); RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
            File.WriteAllBytes("Previews/audience_"+(side<0?"left":side>0?"right":"center")+".png",image.EncodeToPNG());
            c.ViewCamera.targetTexture=null; RenderTexture.active=old;
            UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        }
        c.ResetPose();
    }
}
