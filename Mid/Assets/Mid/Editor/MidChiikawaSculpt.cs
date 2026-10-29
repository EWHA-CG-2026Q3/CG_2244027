using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Independently sampled surface profiles: no primitive objects are used for the character.
public static class MidChiikawaSculpt
{
    const string ScenePath="Assets/Mid/Scenes/Mid_ChiikawaSculpt.unity";
    static Material white, ink, pink, shine;
    static float Signed(float x,float exponent)=>Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),exponent);
    public static Mesh Surface(string name, Vector3 radius,float verticalPower,float roundPower,float taper=0)
    {
        const int rows=64,cols=96;
        var v=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[v.Length];var tr=new List<int>();
        for(int j=0;j<=rows;j++) for(int k=0;k<=cols;k++)
        {
            float lat=-Mathf.PI/2+Mathf.PI*j/rows, a=2*Mathf.PI*k/cols;
            float y=Signed(Mathf.Sin(lat),verticalPower), ring=Mathf.Pow(Mathf.Max(0,Mathf.Cos(lat)),verticalPower);
            float breadth=1-taper*y;
            int i=j*(cols+1)+k;
            v[i]=new Vector3(radius.x*ring*Signed(Mathf.Cos(a),roundPower)*breadth,radius.y*y,radius.z*ring*Signed(Mathf.Sin(a),roundPower));
            uv[i]=new Vector2((float)k/cols,(float)j/rows);
            if(j<rows&&k<cols){int n=i+cols+1;tr.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
        }
        Mesh m=new Mesh{name=name,vertices=v,uv=uv,triangles=tr.ToArray()};m.RecalculateNormals();
        // Match the duplicated seam normals for a continuous silhouette and lighting.
        var normals=m.normals;
        for(int j=0;j<=rows;j++){int a=j*(cols+1),b=a+cols;Vector3 n=(normals[a]+normals[b]).normalized;normals[a]=normals[b]=n;}
        for(int k=0;k<=cols;k++){normals[k]=Vector3.down;normals[rows*(cols+1)+k]=Vector3.up;}
        m.normals=normals;m.RecalculateBounds();return MidChiikawaBuilder.SaveMesh("Sculpt_"+name,m);
    }
    static Material Mat(string name,Color color)
    {
        string path="Assets/Mid/Generated/CC_Sculpt_"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",.16f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
    }
    static Transform Part(string name,Transform parent,Mesh mesh,Vector3 position,Material material)
    {
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=position;
        o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=material;return o.transform;
    }
    // Measured front-view contours in the 500 x 500 reference photograph.
    // Coordinate conversion is kept explicit so the 50% overlay is reproducible.
    const float PixelsPerUnit=160f;
    public static readonly Vector3[] HeadContour={
        new Vector3(40,247,247),new Vector3(43,207,291),new Vector3(50,170,328),
        new Vector3(60,144,353),new Vector3(80,116,381),new Vector3(100,98,397),
        new Vector3(130,82,412),new Vector3(160,73,421),new Vector3(190,69,425),
        new Vector3(220,73,422),new Vector3(250,85,412),new Vector3(275,103,395),
        new Vector3(295,132,371),new Vector3(310,171,332),new Vector3(317,209,295),
        new Vector3(319,250,250)};
    static readonly Vector3[] BodyContour={
        new Vector3(286,247,247),new Vector3(295,145,351),new Vector3(310,135,357),
        new Vector3(340,135,358),new Vector3(365,139,354),new Vector3(390,149,344),
        new Vector3(410,170,327),new Vector3(425,203,296),new Vector3(432,233,270),
        new Vector3(434,251,251)};
    public static Vector2 WidthAt(Vector3[] profile,float y)
    {
        int i=0;while(i<profile.Length-2 && y>profile[i+1].x)i++;
        Vector3 a=profile[i],b=profile[i+1];float t=Mathf.Clamp01((y-a.x)/(b.x-a.x));
        // Monotonic cubic interpolation prevents overshooting the silhouette near poles.
        float h=b.x-a.x;
        float Slope(int index,bool right){
            if(index==0||index==profile.Length-1)return 0;
            float Value(int k)=>right?profile[k].z:profile[k].y;
            float l=(Value(index)-Value(index-1))/(profile[index].x-profile[index-1].x);
            float r=(Value(index+1)-Value(index))/(profile[index+1].x-profile[index].x);
            return l*r<=0?0:2*l*r/(l+r);
        }
        float Interp(bool right){float va=right?a.z:a.y,vb=right?b.z:b.y;return (2*t*t*t-3*t*t+1)*va+(t*t*t-2*t*t+t)*h*Slope(i,right)+(-2*t*t*t+3*t*t)*vb+(t*t*t-t*t)*h*Slope(i+1,right);}
        return new Vector2(Interp(false),Interp(true));
    }
    static Mesh ProfileSurface(string name,Vector3[] contour,float pivotPixelY,float depth)
    {
        const int rows=128,cols=96;
        var v=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[v.Length];var triangles=new List<int>();
        float maxRadius=0;foreach(var sample in contour)maxRadius=Mathf.Max(maxRadius,(sample.z-sample.y)/320);
        for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++)
        {
            float t=(float)row/rows,py=Mathf.Lerp(contour[contour.Length-1].x,contour[0].x,(1-Mathf.Cos(t*Mathf.PI))*.5f);
            Vector2 edges=WidthAt(contour,py);float radius=(edges.y-edges.x)/320f,center=((edges.x+edges.y)*.5f-250)/160;
            float a=2*Mathf.PI*col/cols;int i=row*(cols+1)+col;
            v[i]=new Vector3(center+radius*Signed(Mathf.Cos(a),.82f),(pivotPixelY-py)/160,(name=="Photo head"?HeadDepthOffset(py):0)+depth*Mathf.Pow(Mathf.Max(0,radius/maxRadius),.85f)*Signed(Mathf.Sin(a),.82f));
            uv[i]=new Vector2((float)col/cols,t);
            if(row<rows&&col<cols){int n=i+cols+1;triangles.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
        }
        var m=new Mesh{name=name,vertices=v,triangles=triangles.ToArray(),uv=uv};m.RecalculateNormals();
        var ns=m.normals;for(int j=0;j<=rows;j++){int a=j*(cols+1),b=a+cols;ns[a]=ns[b]=(ns[a]+ns[b]).normalized;}m.normals=ns;m.RecalculateBounds();return MidChiikawaBuilder.SaveMesh("Sculpt_"+name,m);
    }
    static float HeadDepthOffset(float py)=>-.22f*Mathf.Pow(Mathf.Clamp01((py-250)/69),2);
    public static Vector3 Face(float x,float y,float layer=.003f)
    {
        Vector2 edges=WidthAt(HeadContour,300-y*160);float width=(edges.y-edges.x)/320,center=((edges.x+edges.y)*.5f-250)/160;
        float depth=.96f*Mathf.Pow(Mathf.Max(.0001f,width/1.1125f),.85f);
        float z=depth*Mathf.Pow(Mathf.Max(.0001f,1-Mathf.Pow(Mathf.Abs((x-center)/Mathf.Max(.001f,width)),2/.82f)),.82f/2);
        return new Vector3(x,y,HeadDepthOffset(300-y*160)-z-layer);
    }
    static Vector2 F(float x,float y)=>new Vector2((x-250)/160,(300-y)/160);
    static Vector3 W(float x,float y,float z=0)=>new Vector3((x-250)/160,(466-y)/160,z);
    static void PhotoPatch(string name,Transform neck,float x,float y,float rx,float ry,Material mat,float rotation=0,float layer=.004f){Vector2 p=F(x,y);Patch(name,neck,p.x,p.y,rx/160,ry/160,mat,rotation,layer);}
    static void PhotoCurve(string name,Transform neck,Vector2 a,Vector2 b,Vector2 c,Vector2 d,float width,Material mat){Stroke(name,neck,Bezier(F(a.x,a.y),F(b.x,b.y),F(c.x,c.y),F(d.x,d.y)),width/160,mat);}
    static void Patch(string name,Transform parent,float x,float y,float rx,float ry,Material mat,float rotation=0,float layer=.004f)
    {
        const int rings=8,segments=64;
        var vertices=new Vector3[(rings+1)*(segments+1)];var tris=new List<int>();
        float c=Mathf.Cos(rotation),s=Mathf.Sin(rotation);
        for(int j=0;j<=rings;j++)for(int k=0;k<=segments;k++)
        {
            float a=k*2*Mathf.PI/segments,r=(float)j/rings,dx=rx*r*Mathf.Cos(a),dy=ry*r*Mathf.Sin(a);
            int i=j*(segments+1)+k;vertices[i]=Face(x+dx*c-dy*s,y+dx*s+dy*c,layer);
            if(j<rings&&k<segments){int n=i+segments+1;tris.AddRange(new[]{i,i+1,n,i+1,n+1,n});}
        }
        var mesh=new Mesh{name=name,vertices=vertices,triangles=tris.ToArray()};mesh.RecalculateNormals();
        Part(name,parent,MidChiikawaBuilder.SaveMesh("Sculpt_"+name,mesh),Vector3.zero,mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Stroke(string name,Transform parent,Vector2[] points,float width,Material mat,float layer=.010f)
    {
        var v=new List<Vector3>();var tr=new List<int>();
        for(int i=0;i<points.Length;i++)
        {
            Vector2 d=points[Math.Min(i+1,points.Length-1)]-points[Math.Max(0,i-1)];Vector2 n=new Vector2(-d.y,d.x).normalized*width*.5f;
            v.Add(Face(points[i].x+n.x,points[i].y+n.y,layer));v.Add(Face(points[i].x-n.x,points[i].y-n.y,layer));
            if(i<points.Length-1){int a=i*2;tr.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        }
        var mesh=new Mesh{name=name,vertices=v.ToArray(),triangles=tr.ToArray()};mesh.RecalculateNormals();
        Part(name,parent,MidChiikawaBuilder.SaveMesh("Sculpt_"+name,mesh),Vector3.zero,mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        for(int i=0;i<2;i++){var p=points[i==0?0:points.Length-1];Patch(name+" cap"+i,parent,p.x,p.y,width*.5f,width*.5f,mat,0,layer+.0005f);}
    }
    static Vector2[] Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        var p=new Vector2[32];for(int i=0;i<p.Length;i++){float t=(float)i/(p.Length-1),u=1-t;p[i]=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}return p;
    }
    [MenuItem("Mid/Build sculpted Chiikawa study")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        white=Mat("Porcelain",new Color(.92f,.915f,.90f));ink=Mat("Face",new Color(.045f,.035f,.03f));pink=Mat("Blush",new Color(1,.48f,.55f));shine=Mat("Eye glints",Color.white);
        var root=new GameObject("Chiikawa - custom surface model").transform;
        Part("Body",root,ProfileSurface("Photo torso",BodyContour,466,.43f),new Vector3(0,0,.30f),white);
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;
            Vector3[] foot=i==0?new[]{new Vector3(400,200,200),new Vector3(415,177,225),new Vector3(430,181,224),new Vector3(447,185,222),new Vector3(460,192,217),new Vector3(466,204,204)}:new[]{new Vector3(400,290,290),new Vector3(416,269,310),new Vector3(431,267,306),new Vector3(448,268,300),new Vector3(461,275,293),new Vector3(465,284,284)};
            Part("Foot "+i,root,ProfileSurface("Photo foot "+i,foot,466,.22f),new Vector3(0,0,.33f),white);
            Part("Arm "+i,root,Surface("Photo arm "+i,new Vector3(.083f,i==0?.245f:.225f,.14f),.93f,1,.20f),W(i==0?117:379,i==0?339:332,.22f),white).localRotation=Quaternion.Euler(0,0,side*24);
        }
        var neck=new GameObject("NeckPivot").transform;neck.SetParent(root,false);neck.localPosition=W(250,300);
        Part("Head",neck,ProfileSurface("Photo head",HeadContour,300,.96f),Vector3.zero,white);
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;Vector2 ear=F(i==0?148:348,i==0?49:50);
            Part("Ear "+i,neck,Surface("Photo ear "+i,new Vector3(i==0?.181f:.187f,.19f,.14f),1,1),new Vector3(ear.x,ear.y,.025f),white).localRotation=Quaternion.Euler(0,0,-side*8);
            float eyeX=i==0?194:301, cheekX=i==0?147:350,cheekY=i==0?223:225;
            PhotoPatch("Blush "+i,neck,cheekX,cheekY,26,23,pink,-side*.10f);
            PhotoPatch("Eye "+i,neck,eyeX,191,21.5f,22.5f,ink);
            PhotoPatch("Glint top "+i,neck,eyeX-2,184,9.2f,6.4f,shine,.55f,.009f);
            PhotoPatch("Glint lower "+i,neck,eyeX+1,202,7.6f,2.3f,shine,.10f,.009f);
            if(i==0)PhotoCurve("Brow 0",neck,new Vector2(177,146),new Vector2(184,141),new Vector2(194,139),new Vector2(203,140),5,ink);
            else PhotoCurve("Brow 1",neck,new Vector2(293,143),new Vector2(300,142),new Vector2(309,146),new Vector2(317,151),5,ink);
            for(int k=0;k<4;k++){
                float x=cheekX+(k-1.5f)*10.5f;
                Stroke("Blush stitch "+i+"-"+k,neck,new[]{F(x-3,cheekY+7),F(x+3,cheekY-8)},3.5f/160,ink);
            }
        }
        PhotoPatch("Nose",neck,251,223,4.5f,5,ink);
        PhotoCurve("Mouth left",neck,new Vector2(224,229),new Vector2(223,242),new Vector2(250,244),new Vector2(251,227),7,ink);
        PhotoCurve("Mouth right",neck,new Vector2(251,227),new Vector2(251,243),new Vector2(270,244),new Vector2(274,232),7,ink);
        PhotoCurve("Open smile",neck,new Vector2(237,239),new Vector2(236,275),new Vector2(264,273),new Vector2(265,240),5,ink);
        PhotoCurve("Chin smile",neck,new Vector2(244,277),new Vector2(248,281),new Vector2(255,281),new Vector2(259,276),4.5f,ink);
        Directory.CreateDirectory("Assets/Mid/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Mid/Prefabs/Chiikawa_Sculpt.prefab");
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Warm studio floor";ground.transform.localScale=Vector3.one*10;ground.GetComponent<Renderer>().sharedMaterial=Mat("Studio",new Color(.58f,.49f,.51f));
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.48f,.49f);
        var light=new GameObject("Large soft key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=.65f;light.transform.rotation=Quaternion.Euler(35,-30,0);light.shadows=LightShadows.Soft;light.shadowStrength=.25f;
        var fill=new GameObject("Soft fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.18f;fill.transform.rotation=Quaternion.Euler(25,135,0);
        var camera=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=500f/320f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.79f,.74f,.74f);
        camera.transform.position=new Vector3(0,1.35f,-9);camera.transform.LookAt(new Vector3(0,1.35f,0));
        QualitySettings.antiAliasing=8;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
        var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.name="Mouse target ball";ball.transform.localScale=Vector3.one*.20f;ball.GetComponent<Renderer>().sharedMaterial=Mat("Ball",new Color(.53f,.76f,.74f));
        var controller=new GameObject("Mouse head tracking").AddComponent<MidBallTracking>();controller.ViewCamera=camera;controller.Ball=ball.transform;controller.Neck=neck;controller.Spectators=new[]{new MidBallTracking.Spectator{Neck=neck,FollowSpeed=4}};controller.ResetBallPosition=new Vector3(0,1.5f,-3);controller.HorizontalBounds=new Vector2(-2,2);controller.VerticalBounds=new Vector2(.4f,3.5f);controller.MaxYaw=35;controller.MaxUpPitch=22;controller.MaxDownPitch=20;controller.DisplayTitle="CHIIKAWA / MODEL STUDY";controller.DisplaySubtitle="Custom head, body and limbs";controller.ResetPose();
        Vector3 bodyPosition=root.Find("Body").position;
        controller.SetWorldTarget(new Vector3(2,3,-3));for(int i=0;i<90;i++)controller.Step(1f/60);
        if(Quaternion.Angle(neck.localRotation,Quaternion.identity)<5 || root.Find("Body").position!=bodyPosition)throw new Exception("Head tracking verification failed");
        controller.ResetPose();
        ball.SetActive(false);
        Directory.CreateDirectory("Previews");Capture(camera,"Previews/chiikawa_sculpt_front.png");
        ground.SetActive(false);CaptureOverlay(camera);ground.SetActive(true);
        camera.transform.position=new Vector3(4,2.1f,-8);camera.transform.LookAt(new Vector3(0,1.35f,0));Capture(camera,"Previews/chiikawa_sculpt_angle.png");
        camera.transform.position=new Vector3(0,1.35f,-9);camera.transform.LookAt(new Vector3(0,1.35f,0));ball.SetActive(true);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh==null)throw new Exception("Missing mesh: "+f.name);
        if(root.Find("NeckPivot/Head").GetComponent<MeshFilter>().sharedMesh.vertexCount<6000)throw new Exception("Head detail missing");
        Debug.Log("CHIIKAWA_SCULPT_OK: custom head and limbs, conforming face graphics, prefab and two previews saved.");
    }
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1200,1200,24){antiAliasing=4};var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var t=new Texture2D(1200,1200,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1200,1200),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    static void CaptureOverlay(Camera camera)
    {
        var original=new Texture2D(2,2);original.LoadImage(File.ReadAllBytes("References/chiikawa_figure_reference.jpg"));
        var rt=new RenderTexture(500,500,24){antiAliasing=4};var previous=RenderTexture.active;Color bg=camera.backgroundColor;
        camera.backgroundColor=new Color(0,0,0,0);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var model=new Texture2D(500,500,TextureFormat.RGBA32,false);model.ReadPixels(new Rect(0,0,500,500),0,0);model.Apply();
        File.WriteAllBytes("Previews/chiikawa_reference_aligned.png",model.EncodeToPNG());
        var rendered=model.GetPixels();var reference=original.GetPixels();
        for(int i=0;i<rendered.Length;i++){float opacity=.5f*rendered[i].a;rendered[i]=Color.Lerp(reference[i],rendered[i],opacity);rendered[i].a=1;}
        model.SetPixels(rendered);model.Apply();File.WriteAllBytes("Previews/chiikawa_reference_overlay_50.png",model.EncodeToPNG());
        camera.backgroundColor=bg;camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(original);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    public static void Open(){EditorSceneManager.OpenScene(ScenePath);EditorApplication.ExecuteMenuItem("Window/General/Game");}
}
