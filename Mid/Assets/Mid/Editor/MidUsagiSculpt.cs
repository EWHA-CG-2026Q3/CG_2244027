using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Independently sampled surface profiles: no primitive objects are used for the character.
public static class MidUsagiSculpt
{
    const string ScenePath="Assets/Mid/Scenes/Mid_UsagiSculpt.unity";
    static Material white, ink, pink, shine;
    static float Signed(float x,float exponent)=>Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),exponent);
    static Mesh Surface(string name, Vector3 radius,float verticalPower,float roundPower,float taper=0)
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
        m.normals=normals;m.RecalculateBounds();return MidChiikawaBuilder.SaveMesh("UsagiSculpt_"+name,m);
    }
    static Material Mat(string name,Color color)
    {
        string path="Assets/Mid/Generated/US_Sculpt_"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",.16f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
    }
    static Transform Part(string name,Transform parent,Mesh mesh,Vector3 position,Material material)
    {
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=position;
        o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=material;return o.transform;
    }
    // Reference coordinates: 1020 x 1346, 300 pixels per world unit.
    static readonly Vector3[] HeadContour={
        new Vector3(451,522,522),new Vector3(460,438,610),new Vector3(480,373,670),
        new Vector3(520,300,739),new Vector3(560,254,781),new Vector3(620,214,820),
        new Vector3(680,194,845),new Vector3(730,189,855),new Vector3(780,194,850),
        new Vector3(840,211,831),new Vector3(890,241,805),new Vector3(930,289,758),
        new Vector3(955,361,690),new Vector3(968,456,591),new Vector3(973,522,522)};
    static readonly Vector3[] BodyContour={
        new Vector3(905,522,522),new Vector3(930,318,730),new Vector3(990,315,730),
        new Vector3(1070,319,727),new Vector3(1120,331,714),new Vector3(1160,358,689),
        new Vector3(1188,415,635),new Vector3(1203,481,565),new Vector3(1206,522,522)};
    static readonly Vector3[] LeftEar={new Vector3(103,449,449),new Vector3(112,431,472),new Vector3(145,413,489),new Vector3(200,399,499),new Vector3(270,395,504),new Vector3(340,402,503),new Vector3(410,416,500),new Vector3(460,429,495),new Vector3(490,463,463)};
    static readonly Vector3[] RightEar={new Vector3(103,589,589),new Vector3(112,573,611),new Vector3(145,557,628),new Vector3(200,545,643),new Vector3(270,539,648),new Vector3(340,540,643),new Vector3(410,544,629),new Vector3(460,549,615),new Vector3(490,582,582)};
    static Vector2 WidthAt(Vector3[] profile,float y)
    {
        int i=0;while(i<profile.Length-2 && y>profile[i+1].x)i++;
        Vector3 a=profile[i],b=profile[i+1];float t=Mathf.Clamp01((y-a.x)/(b.x-a.x));
        // Rounded caps have a vertical tangent in width(y), avoiding pointed poles.
        if(i==0 || i==profile.Length-2){
            float center=Mathf.Lerp((a.y+a.z)*.5f,(b.y+b.z)*.5f,t);
            float radius=i==0?(b.z-b.y)*.5f*Mathf.Sqrt(t):(a.z-a.y)*.5f*Mathf.Sqrt(1-t);
            return new Vector2(center-radius,center+radius);
        }
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
        float maxRadius=0;foreach(var sample in contour)maxRadius=Mathf.Max(maxRadius,(sample.z-sample.y)/600);
        for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++)
        {
            float t=(float)row/rows,py=Mathf.Lerp(contour[contour.Length-1].x,contour[0].x,(1-Mathf.Cos(t*Mathf.PI))*.5f);
            Vector2 edges=WidthAt(contour,py);float radius=(edges.y-edges.x)/600f,center=((edges.x+edges.y)*.5f-510)/300;
            float a=2*Mathf.PI*col/cols;int i=row*(cols+1)+col;
            v[i]=new Vector3(center+radius*Signed(Mathf.Cos(a),.82f),(pivotPixelY-py)/300,(name=="Photo head"?HeadDepthOffset(py):0)+depth*Mathf.Pow(Mathf.Max(0,radius/maxRadius),.85f)*Signed(Mathf.Sin(a),.82f));
            uv[i]=new Vector2((float)col/cols,t);
            if(row<rows&&col<cols){int n=i+cols+1;triangles.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
        }
        var m=new Mesh{name=name,vertices=v,triangles=triangles.ToArray(),uv=uv};m.RecalculateNormals();
        var ns=m.normals;for(int j=0;j<=rows;j++){int a=j*(cols+1),b=a+cols;ns[a]=ns[b]=(ns[a]+ns[b]).normalized;}m.normals=ns;m.RecalculateBounds();return MidChiikawaBuilder.SaveMesh("UsagiSculpt_"+name,m);
    }
    static float HeadDepthOffset(float py)=>-.22f*Mathf.Pow(Mathf.Clamp01((py-840)/133),2);
    public static Vector3 Face(float x,float y,float layer=.003f)
    {
        Vector2 edges=WidthAt(HeadContour,940-y*300);float width=(edges.y-edges.x)/600,center=((edges.x+edges.y)*.5f-510)/300;
        float depth=.96f*Mathf.Pow(Mathf.Max(.0001f,width/1.11f),.85f);
        float z=depth*Mathf.Pow(Mathf.Max(.0001f,1-Mathf.Pow(Mathf.Abs((x-center)/Mathf.Max(.001f,width)),2/.82f)),.82f/2);
        return new Vector3(x,y,HeadDepthOffset(940-y*300)-z-layer);
    }
    static Vector2 F(float x,float y)=>new Vector2((x-510)/300,(940-y)/300);
    static Vector3 W(float x,float y,float z=0)=>new Vector3((x-510)/300,(1280-y)/300,z);
    static void PhotoPatch(string name,Transform neck,float x,float y,float rx,float ry,Material mat,float rotation=0,float layer=.004f){Vector2 p=F(x,y);Patch(name,neck,p.x,p.y,rx/300,ry/300,mat,rotation,layer);}
    static void PhotoCurve(string name,Transform neck,Vector2 a,Vector2 b,Vector2 c,Vector2 d,float width,Material mat){Stroke(name,neck,Bezier(F(a.x,a.y),F(b.x,b.y),F(c.x,c.y),F(d.x,d.y)),width/300,mat);}
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
        Part(name,parent,MidChiikawaBuilder.SaveMesh("UsagiSculpt_"+name,mesh),Vector3.zero,mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
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
        Part(name,parent,MidChiikawaBuilder.SaveMesh("UsagiSculpt_"+name,mesh),Vector3.zero,mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        for(int i=0;i<2;i++){var p=points[i==0?0:points.Length-1];Patch(name+" cap"+i,parent,p.x,p.y,width*.5f,width*.5f,mat,0,layer+.0005f);}
    }
    static Vector2[] Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        var p=new Vector2[32];for(int i=0;i<p.Length;i++){float t=(float)i/(p.Length-1),u=1-t;p[i]=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}return p;
    }
    static void EarLining(Transform neck,string name,Vector3[] ear,Vector3[] inset,Material material)
    {
        const int rows=64,cols=12;var v=new Vector3[(rows+1)*(cols+1)];var tris=new List<int>();
        float maximum=0;foreach(var p in ear)maximum=Mathf.Max(maximum,(p.z-p.y)/600);
        for(int j=0;j<=rows;j++)for(int k=0;k<=cols;k++){
            float py=Mathf.Lerp(inset[inset.Length-1].x,inset[0].x,(float)j/rows);Vector2 bounds=WidthAt(inset,py),outer=WidthAt(ear,py);
            float px=Mathf.Lerp(bounds.x,bounds.y,(float)k/cols),half=(outer.y-outer.x)/600,center=((outer.x+outer.y)*.5f-510)/300,x=(px-510)/300;
            float depth=.19f*Mathf.Pow(Mathf.Max(.0001f,half/maximum),.85f);
            float z=-depth*Mathf.Pow(Mathf.Max(.0001f,1-Mathf.Pow(Mathf.Abs((x-center)/Mathf.Max(.001f,half)),2/.82f)),.82f/2)-.004f;
            int i=j*(cols+1)+k;v[i]=new Vector3(x,(940-py)/300,z);
            if(j<rows&&k<cols){int n=i+cols+1;tris.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
        }
        var mesh=new Mesh{name=name,vertices=v,triangles=tris.ToArray()};mesh.RecalculateNormals();
        Part(name,neck,MidChiikawaBuilder.SaveMesh("UsagiSculpt_"+name,mesh),Vector3.zero,material).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    [MenuItem("Mid/Build sculpted Usagi study")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        white=Mat("Vanilla",new Color(1f,.92f,.69f));ink=Mat("Cocoa",new Color(.075f,.043f,.023f));pink=Mat("Blush",new Color(.96f,.51f,.48f));shine=Mat("Glints",new Color(.99f,.99f,.94f));
        var brows=Mat("Soft brown brows",new Color(.34f,.25f,.12f));var inner=Mat("Ear pink",new Color(1,.58f,.49f));var tongue=Mat("Tongue",new Color(.94f,.48f,.40f));
        var root=new GameObject("Usagi - reference modeled surfaces").transform;
        Part("Body",root,ProfileSurface("Photo torso",BodyContour,1280,.43f),new Vector3(0,0,.30f),white);
        for(int i=0;i<2;i++){
            Vector3[] foot=i==0?new[]{new Vector3(1150,452,452),new Vector3(1180,398,499),new Vector3(1220,429,499),new Vector3(1255,448,497),new Vector3(1274,462,490),new Vector3(1279,478,478)}:new[]{new Vector3(1150,585,585),new Vector3(1180,545,640),new Vector3(1220,545,613),new Vector3(1255,548,593),new Vector3(1274,558,578),new Vector3(1279,567,567)};
            Part("Foot "+i,root,ProfileSurface("Foot "+i,foot,1280,.22f),new Vector3(0,0,.33f),white);
            float side=i==0?-1:1;
            Part("Arm "+i,root,Surface("Arm "+i,new Vector3(.104f,.26f,.15f),.88f,1,.22f),W(i==0?279:771,1005,.22f),white).localRotation=Quaternion.Euler(0,0,side*31);
        }
        var neck=new GameObject("NeckPivot").transform;neck.SetParent(root,false);neck.localPosition=W(510,940);
        Part("Head",neck,ProfileSurface("Photo head",HeadContour,940,.96f),Vector3.zero,white);
        Part("Ear left",neck,ProfileSurface("Ear left",LeftEar,940,.19f),Vector3.zero,white);
        Part("Ear right",neck,ProfileSurface("Ear right",RightEar,940,.19f),Vector3.zero,white);
        EarLining(neck,"Pink left ear",LeftEar,new[]{new Vector3(120,431,431),new Vector3(145,414,442),new Vector3(200,401,440),new Vector3(270,398,437),new Vector3(340,405,442),new Vector3(410,420,451),new Vector3(448,434,453),new Vector3(454,444,444)},inner);
        EarLining(neck,"Pink right ear",RightEar,new[]{new Vector3(117,607,607),new Vector3(145,595,626),new Vector3(200,602,641),new Vector3(270,605,645),new Vector3(340,602,640),new Vector3(410,593,625),new Vector3(448,592,614),new Vector3(456,606,606)},inner);
        for(int i=0;i<2;i++){
            float x=i==0?414:626,y=i==0?710:720,cx=i==0?342:712,cy=i==0?790:787;
            PhotoPatch("Blush "+i,neck,cx,cy,i==0?46:51,46,pink,i==0?.15f:-.15f);
            PhotoPatch("Eye "+i,neck,x,y,43,44,ink,i==0?-.15f:.05f);
            PhotoPatch("Glint top "+i,neck,x,y-10,17,12,shine,i==0?.45f:-.12f,.010f);
            PhotoPatch("Glint lower "+i,neck,x+5,y+21,12,3,shine,.10f,.010f);
            for(int k=0;k<4;k++){
                float xx=cx+(k-1.5f)*22;float half=k==0||k==3?14:19;Stroke("Cheek stitch "+i+"-"+k,neck,new[]{F(xx-6,cy+half),F(xx+6,cy-half)},8f/300,ink);
            }
        }
        PhotoCurve("Long left brow",neck,new Vector2(317,619),new Vector2(327,558),new Vector2(375,530),new Vector2(431,527),12,brows);
        PhotoCurve("Long right brow",neck,new Vector2(616,526),new Vector2(665,510),new Vector2(749,565),new Vector2(748,641),13,brows);
        PhotoPatch("Open mouth",neck,521,826,34,44,ink);
        PhotoPatch("Tongue",neck,523,832,17,22,tongue,0,.009f);
        PhotoPatch("Nose",neck,523,777,11,16,ink,-.2f);
        PhotoCurve("Smile left",neck,new Vector2(472,778),new Vector2(478,808),new Vector2(516,798),new Vector2(522,779),18,ink);
        PhotoCurve("Smile right",neck,new Vector2(522,779),new Vector2(526,809),new Vector2(566,809),new Vector2(567,785),17,ink);
        PhotoCurve("Chin smile",neck,new Vector2(513,887),new Vector2(516,902),new Vector2(533,904),new Vector2(540,886),10,ink);
        Directory.CreateDirectory("Assets/Mid/Prefabs");PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Mid/Prefabs/Usagi_Sculpt.prefab");
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Charcoal studio floor";ground.transform.localScale=Vector3.one*10;ground.GetComponent<Renderer>().sharedMaterial=Mat("Studio",new Color(.045f,.05f,.055f));
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.48f,.49f);
        var light=new GameObject("Soft key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=.65f;light.transform.rotation=Quaternion.Euler(35,-30,0);light.shadows=LightShadows.Soft;light.shadowStrength=.25f;
        var fill=new GameObject("Fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.18f;fill.transform.rotation=Quaternion.Euler(25,135,0);
        var camera=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=1346f/600;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.14f);
        float viewY=(1280-673)/300f;camera.transform.position=new Vector3(0,viewY,-9);camera.transform.LookAt(new Vector3(0,viewY,0));
        QualitySettings.antiAliasing=8;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
        var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.name="Mouse target ball";ball.transform.localScale=Vector3.one*.15f;ball.GetComponent<Renderer>().sharedMaterial=Mat("Ball",new Color(.53f,.76f,.74f));
        var controller=new GameObject("Mouse head tracking").AddComponent<MidBallTracking>();controller.ViewCamera=camera;controller.Ball=ball.transform;controller.Neck=neck;controller.Spectators=new[]{new MidBallTracking.Spectator{Neck=neck,FollowSpeed=4}};controller.ResetBallPosition=new Vector3(0,1.5f,-3);controller.HorizontalBounds=new Vector2(-2,2);controller.VerticalBounds=new Vector2(.4f,4);controller.MaxYaw=35;controller.MaxUpPitch=22;controller.MaxDownPitch=20;controller.DisplayTitle="USAGI / MODEL STUDY";controller.DisplaySubtitle="Reference modeled head, ears and body";controller.ResetPose();
        Vector3 bodyPosition=root.Find("Body").position;controller.SetWorldTarget(new Vector3(2,3,-3));for(int i=0;i<90;i++)controller.Step(1f/60);
        if(Quaternion.Angle(neck.localRotation,Quaternion.identity)<5||root.Find("Body").position!=bodyPosition)throw new Exception("Usagi head tracking verification failed");controller.ResetPose();ball.SetActive(false);
        Directory.CreateDirectory("Previews");Capture(camera,"Previews/usagi_sculpt_front.png");ground.SetActive(false);CaptureOverlay(camera);ground.SetActive(true);
        camera.transform.position=new Vector3(4,viewY+.45f,-8);camera.transform.LookAt(new Vector3(0,viewY,0));Capture(camera,"Previews/usagi_sculpt_angle.png");
        camera.transform.position=new Vector3(0,viewY,-9);camera.transform.LookAt(new Vector3(0,viewY,0));ball.SetActive(true);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh==null)throw new Exception("Missing mesh: "+f.name);
        Debug.Log("USAGI_SCULPT_OK: separate assets, custom head/ears/body, fixed body and head tracking, front/angle/overlay previews.");
    }
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1020,1346,24){antiAliasing=4};var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var t=new Texture2D(1020,1346,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1020,1346),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    static void CaptureOverlay(Camera camera)
    {
        var original=new Texture2D(2,2);original.LoadImage(File.ReadAllBytes("References/usagi_figure_reference.png"));
        var rt=new RenderTexture(1020,1346,24){antiAliasing=4};var previous=RenderTexture.active;Color bg=camera.backgroundColor;
        camera.backgroundColor=new Color(0,0,0,0);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var model=new Texture2D(1020,1346,TextureFormat.RGBA32,false);model.ReadPixels(new Rect(0,0,1020,1346),0,0);model.Apply();
        File.WriteAllBytes("Previews/usagi_reference_aligned.png",model.EncodeToPNG());
        var rendered=model.GetPixels();var reference=original.GetPixels();
        for(int i=0;i<rendered.Length;i++){float opacity=.5f*rendered[i].a;rendered[i]=Color.Lerp(reference[i],rendered[i],opacity);rendered[i].a=1;}
        model.SetPixels(rendered);model.Apply();File.WriteAllBytes("Previews/usagi_reference_overlay_50.png",model.EncodeToPNG());
        camera.backgroundColor=bg;camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(original);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    public static void Open(){EditorSceneManager.OpenScene(ScenePath);EditorApplication.ExecuteMenuItem("Window/General/Game");}
}
