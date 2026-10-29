using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MidChiikawaBuilder
{
    const string Scene="Assets/Mid/Scenes/Mid_ChiikawaAudience.unity";
    const string ModeledScene="Assets/Mid/Scenes/Mid_ChiikawaModeled.unity";
    static Material white, yellow, blue, pink, ink, glint;
    static Transform Ball(string name,Transform parent,Vector3 p,Vector3 scale,Material mat)
        => MidPrototypeBuilder.Sphere(name,parent,p,scale,mat);

    [MenuItem("Mid/Build Chiikawa version (separate scene)")]
    public static void Build()
    {
        // Reuse the seating scene without changing its original animal version.
        EditorSceneManager.OpenScene("Assets/Mid/Scenes/Mid_PlushAudience.unity");
        var controller=Object.FindFirstObjectByType<MidBallTracking>();
        foreach(var spectator in controller.Spectators) Object.DestroyImmediate(spectator.Neck.parent.gameObject);
        white=MidPrototypeBuilder.Mat("CC_White",new Color(.97f,.96f,.91f));
        yellow=MidPrototypeBuilder.Mat("CC_Usagi",new Color(1f,.86f,.53f));
        blue=MidPrototypeBuilder.Mat("CC_Blue",new Color(.38f,.66f,.76f));
        pink=MidPrototypeBuilder.Mat("CC_EarPink",new Color(1f,.66f,.70f));
        ink=MidPrototypeBuilder.Mat("CC_Ink",new Color(.10f,.07f,.055f));
        glint=MidPrototypeBuilder.Mat("CC_Glint",Color.white);
        foreach(var material in new[]{white,yellow,blue,pink}) material.SetFloat("_BumpStrength",.065f);
        var prototypes=new[]{Character("Chiikawa"),Character("Hachiware"),Character("Usagi")};
        var viewers=new List<MidBallTracking.Spectator>();
        float[] speeds={3.2f,6.3f,4.2f,5.5f,2.7f,4.8f,3.7f,6f,4.5f};
        for(int row=0;row<3;row++) for(int col=0;col<3;col++)
        {
            int i=row*3+col;
            var doll=Object.Instantiate(prototypes[(col+row)%3]);
            doll.name=new[]{"Chiikawa","Hachiware","Usagi"}[(col+row)%3]+" "+(i+1);
            doll.position=new Vector3((col-1)*2.15f+(row%2)*.18f,row*1.05f,row*1.85f);
            viewers.Add(new MidBallTracking.Spectator {Neck=doll.Find("NeckPivot"),FollowSpeed=speeds[i]});
        }
        foreach(var prototype in prototypes) Object.DestroyImmediate(prototype.gameObject);
        controller.Spectators=viewers.ToArray(); controller.Neck=viewers[0].Neck;
        controller.DisplayTitle="CHIIKAWA LITTLE AUDIENCE";
        controller.DisplaySubtitle="Chiikawa / Hachiware / Usagi - fan study";
        controller.ResetPose();
        MidPrototypeBuilder.Verify();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Mid/Scenes/Mid_PlushAudience.unity",true),new EditorBuildSettingsScene(Scene,true)};
        AssetDatabase.SaveAssets();
        Preview();
        // A second scene replaces primitive bodies with individually profiled meshes.
        foreach(var spectator in controller.Spectators)
        {
            Transform root=spectator.Neck.parent;
            string species=root.name.Split(' ')[0];
            Transform torso=root.Find("Body");
            torso.localScale=Vector3.one; torso.localPosition=Vector3.zero;
            torso.GetComponent<MeshFilter>().sharedMesh=ModeledBody(species);
        }
        controller.DisplaySubtitle="Custom body mesh / soft cheek and ear fuzz";
        VerifyCharacters(controller);
        MidPrototypeBuilder.Verify();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ModeledScene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Mid/Scenes/Mid_PlushAudience.unity",true),new EditorBuildSettingsScene(Scene,true),new EditorBuildSettingsScene(ModeledScene,true)};
        AssetDatabase.SaveAssets();
        Preview();
        Debug.Log("CHIIKAWA_BUILD_OK: separate scene, three character types, nine spectators, oval arms.");
    }

    public static void BuildAndRecord()
    {
        Build();
        MidVideoCapture.RecordTo("Recordings/ChiikawaFrames");
    }

    static Vector3 FacePoint(float x,float y,float offset)
    {
        float depth=.525f*Mathf.Sqrt(Mathf.Max(.01f,1-x*x/(.74f*.74f)-(y-.42f)*(y-.42f)/(.595f*.595f)));
        return new Vector3(x,y,-depth-offset);
    }

    static Mesh ModeledBody(string species)
    {
        // A squat, slightly squared plush torso, with a full lower belly and tapered shoulders.
        const int rows=36,cols=48;
        var v=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[v.Length];var triangles=new List<int>();
        float width=species=="Usagi"?.53f:species=="Hachiware"?.54f:.57f;
        for(int row=0;row<=rows;row++) for(int col=0;col<=cols;col++)
        {
            float t=(float)row/rows,angle=col*Mathf.PI*2/cols;
            float cap=Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)),.30f);
            float belly=1-.18f*t+.045f*Mathf.Sin(t*Mathf.PI*2);
            float x=Mathf.Sign(Mathf.Cos(angle))*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)),.83f);
            float z=Mathf.Sign(Mathf.Sin(angle))*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle)),.91f);
            int i=row*(cols+1)+col;
            v[i]=new Vector3(width*cap*belly*x,.82f+t*.82f,-.10f+.38f*cap*z);
            uv[i]=new Vector2((float)col/cols,t);
            if(row<rows&&col<cols){int n=i+cols+1;triangles.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
        }
        var mesh=new Mesh{name=species+" custom plush torso",vertices=v,triangles=triangles.ToArray(),uv=uv};mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh(species+"Body",mesh);
    }

    static void VerifyCharacters(MidBallTracking controller)
    {
        foreach(var spectator in controller.Spectators)
        {
            Transform root=spectator.Neck.parent;
            if(root.Find("Body").GetComponent<MeshFilter>().sharedMesh.vertexCount<1000) throw new System.Exception("Custom torso missing");
            int blush=0;
            foreach(Transform part in spectator.Neck)
            {
                if(part.name=="Pink cheek") blush++;
                if(part.name=="Blue cat ear" && part.localPosition.y>.8f) throw new System.Exception("Cat ear root too high");
            }
            if(blush!=2) throw new System.Exception("Missing blush");
        }
        Debug.Log("CHIIKAWA_CHARACTER_CHECK_OK: modeled torsos, blush, anchored cat ears.");
    }

    static Transform Character(string kind)
    {
        Material body=kind=="Usagi"?yellow:white;
        var root=new GameObject(kind).transform;
        Ball("Body",root,new Vector3(0,1.19f,-.1f),new Vector3(1.06f,.85f,.72f),body);
        foreach(float side in new[]{-1f,1f})
        {
            Ball("Arm",root,new Vector3(side*.51f,1.22f,-.18f),new Vector3(.19f,.39f,.22f),body);
            Ball("Foot",root,new Vector3(side*.27f,.83f,-.33f),new Vector3(.18f,.14f,.23f),body);
        }
        var neck=new GameObject("NeckPivot").transform;
        neck.SetParent(root,false); neck.localPosition=new Vector3(0,1.55f,-.1f);
        Ball("Head",neck,new Vector3(0,.42f,0),new Vector3(1.48f,1.19f,1.05f),body);
        foreach(float side in new[]{-1f,1f})
        {
            if(kind=="Chiikawa")
                Ball("Ear",neck,new Vector3(side*.48f,.93f,0),new Vector3(.24f,.25f,.20f),body);
            else if(kind=="Usagi")
            {
                var ear=Ball("Ear",neck,new Vector3(side*.19f,1.20f,.02f),new Vector3(.26f,.90f,.21f),body);
                ear.localRotation=Quaternion.Euler(0,0,-side*3);
                var inner=Ball("Ear lining",neck,new Vector3(side*.19f,1.22f,-.08f),new Vector3(.12f,.66f,.035f),pink);
                inner.localRotation=ear.localRotation;
            }
            else
            {
                var ear=new GameObject("Blue cat ear",typeof(MeshFilter),typeof(MeshRenderer)).transform;
                ear.SetParent(neck,false); ear.localPosition=new Vector3(side*.45f,.76f,0);
                ear.localRotation=Quaternion.Euler(0,0,-side*9);
                ear.GetComponent<MeshFilter>().sharedMesh=CatEar(); ear.GetComponent<Renderer>().sharedMaterial=blue;
            }
            Ball("Eye",neck,new Vector3(side*.245f,.44f,-.485f),new Vector3(.13f,.16f,.055f),ink);
            Ball("Eye shine",neck,new Vector3(side*.245f-.01f,.475f,-.513f),new Vector3(.065f,.04f,.012f),glint);
            Ball("Eye shine lower",neck,new Vector3(side*.245f+.012f,.408f,-.513f),new Vector3(.045f,.025f,.012f),glint);
            float browLength=kind=="Usagi"?.24f:kind=="Hachiware"?.07f:.12f;
            var brow=new Vector3[12];
            for(int k=0;k<brow.Length;k++)
            {
                float t=(float)k/(brow.Length-1);
                float x=side*(.245f+(t-.5f)*browLength);
                float y=.65f+Mathf.Sin(t*Mathf.PI)*(kind=="Usagi"?.075f:.018f);
                brow[k]=FacePoint(x,y,.012f);
            }
            Curve("Eyebrow",neck,brow,.018f);
            Vector3 blushCenter=FacePoint(side*.43f,.285f,.015f);
            Ball("Pink cheek",neck,blushCenter,new Vector3(.20f,.115f,.035f),pink);
            for(int stroke=0;stroke<3;stroke++)
            {
                float x=side*.43f+(stroke-1)*.045f;
                Curve("Cheek embroidery",neck,new[]{new Vector3(x-.008f,.255f,blushCenter.z-.025f),new Vector3(x+.012f,.298f,blushCenter.z-.025f)},.009f);
            }
        }
        // Embroidered face with the requested character-specific blush and brows.
        Ball("Tiny nose",neck,new Vector3(0,.32f,-.526f),new Vector3(.055f,.035f,.018f),ink);
        Curve("Smile",neck,new[]{new Vector3(-.105f,.255f,-.515f),new Vector3(-.08f,.22f,-.518f),new Vector3(-.045f,.215f,-.521f),new Vector3(0,.255f,-.527f),new Vector3(.045f,.215f,-.521f),new Vector3(.08f,.22f,-.518f),new Vector3(.105f,.255f,-.515f)},.016f);
        if(kind=="Hachiware") Cap(neck);
        MidChiikawaFuzz.Add(root,kind);
        return root;
    }

    static void Curve(string name,Transform parent,Vector3[] points,float width)
    {
        var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();
        line.transform.SetParent(parent,false); line.useWorldSpace=false;
        line.positionCount=points.Length; line.SetPositions(points); line.widthMultiplier=width;
        line.numCapVertices=5; line.numCornerVertices=5; line.sharedMaterial=ink;
        line.shadowCastingMode=ShadowCastingMode.Off;
    }

    public static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path="Assets/Mid/Generated/CC_"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old) {old.Clear();old.vertices=mesh.vertices;old.triangles=mesh.triangles;old.normals=mesh.normals;old.uv=mesh.uv;old.bounds=mesh.bounds;EditorUtility.SetDirty(old);Object.DestroyImmediate(mesh);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }

    static Mesh CatEar()
    {
        var mesh=new Mesh {name="Soft triangle cat ear"};
        mesh.vertices=new[]{new Vector3(-.19f,0,-.12f),new Vector3(.19f,0,-.12f),new Vector3(0,.40f,-.04f),new Vector3(-.15f,0,.12f),new Vector3(.15f,0,.12f)};
        mesh.triangles=new[]{0,2,1,3,4,2,0,3,2,1,2,4,0,1,4,0,4,3};
        mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh("CatEar",mesh);
    }

    static void Cap(Transform parent)
    {
        const int cols=64,rows=16;
        var v=new Vector3[(cols+1)*(rows+1)];var indices=new List<int>();
        for(int row=0;row<=rows;row++) for(int col=0;col<=cols;col++)
        {
            float angle=col*Mathf.PI*2/cols;
            // Centre white wedge rises between the two blue forehead lobes.
            float front=Mathf.Clamp01(-Mathf.Sin(angle));
            float minLatitude=.24f+front*.36f*Mathf.Exp(-Mathf.Pow(Mathf.Cos(angle)/.24f,2));
            float lat=Mathf.Lerp(minLatitude,Mathf.PI/2,(float)row/rows);
            int i=row*(cols+1)+col;
            v[i]=new Vector3(.747f*Mathf.Cos(lat)*Mathf.Cos(angle),.42f+.600f*Mathf.Sin(lat),.531f*Mathf.Cos(lat)*Mathf.Sin(angle));
            if(row<rows&&col<cols){int next=i+cols+1;indices.AddRange(new[]{i,next,i+1,i+1,next,next+1});}
        }
        var mesh=new Mesh {name="Hachiware blue forehead",vertices=v,triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
        var cap=new GameObject("Blue forehead",typeof(MeshFilter),typeof(MeshRenderer));cap.transform.SetParent(parent,false);
        cap.GetComponent<MeshFilter>().sharedMesh=SaveMesh("BlueCap",mesh);cap.GetComponent<Renderer>().sharedMaterial=blue;
    }

    public static void Preview()
    {
        var c=Object.FindFirstObjectByType<MidBallTracking>();c.ResetPose();c.SetWorldTarget(new Vector3(3.5f,3.7f,-3));for(int i=0;i<120;i++) c.Step(1f/60);
        var rt=new RenderTexture(1280,800,24){antiAliasing=4};var previous=RenderTexture.active;
        c.ViewCamera.targetTexture=rt;c.ViewCamera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
        System.IO.File.WriteAllBytes(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==ModeledScene?"Previews/chiikawa_modeled.png":"Previews/chiikawa.png",image.EncodeToPNG());
        c.ViewCamera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);c.ResetPose();
    }

    public static void OpenDemo()
    {
        EditorSceneManager.OpenScene(ModeledScene);
        EditorApplication.delayCall+=()=>{EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;};
    }
}
