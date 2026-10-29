using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Independently sampled surface profiles: no primitive objects are used for the character.
public static class MidHachiwareSculpt
{
    const string ScenePath="Assets/Mid/Scenes/Mid_HachiwareSculpt.unity";
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
        m.normals=normals;m.RecalculateBounds();return MidChiikawaBuilder.SaveMesh("HachiwareSculpt_"+name,m);
    }
    static Material Mat(string name,Color color)
    {
        string path="Assets/Mid/Generated/HC_Sculpt_"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",.16f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
    }
    static Transform Part(string name,Transform parent,Mesh mesh,Vector3 position,Material material)
    {
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=position;
        o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=material;return o.transform;
    }
    // Front contours from the 500 x 670 reference.
    static readonly Vector3[] HeadContour={new Vector3(84,250,250),new Vector3(90,192,310),new Vector3(110,132,368),new Vector3(145,79,422),new Vector3(195,41,461),new Vector3(240,23,479),new Vector3(285,19,482),new Vector3(330,26,473),new Vector3(375,48,451),new Vector3(410,94,408),new Vector3(430,163,343),new Vector3(438,250,250)};
    static readonly Vector3[] BodyContour={new Vector3(395,250,250),new Vector3(410,101,397),new Vector3(465,105,393),new Vector3(515,116,380),new Vector3(555,140,357),new Vector3(579,180,320),new Vector3(590,230,272),new Vector3(592,250,250)};
    static Vector2 WidthAt(Vector3[] profile,float y)
    {
        int i=0;while(i<profile.Length-2 && y>profile[i+1].x)i++;
        Vector3 a=profile[i],b=profile[i+1];float t=Mathf.Clamp01((y-a.x)/(b.x-a.x));
        if(i==0||i==profile.Length-2){float center=Mathf.Lerp((a.y+a.z)*.5f,(b.y+b.z)*.5f,t);float radius=i==0?(b.z-b.y)*.5f*Mathf.Sqrt(t):(a.z-a.y)*.5f*Mathf.Sqrt(1-t);return new Vector2(center-radius,center+radius);}
        // Monotonic contour interpolation.
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
        float maxRadius=0;foreach(var sample in contour)maxRadius=Mathf.Max(maxRadius,(sample.z-sample.y)/400);
        for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++)
        {
            float t=(float)row/rows,py=Mathf.Lerp(contour[contour.Length-1].x,contour[0].x,(1-Mathf.Cos(t*Mathf.PI))*.5f);
            Vector2 edges=WidthAt(contour,py);float radius=(edges.y-edges.x)/400f,center=((edges.x+edges.y)*.5f-250)/200;
            float a=2*Mathf.PI*col/cols;int i=row*(cols+1)+col;
            v[i]=new Vector3(center+radius*Signed(Mathf.Cos(a),.82f),(pivotPixelY-py)/200,(name=="Photo head"?HeadDepthOffset(py):0)+depth*Mathf.Pow(Mathf.Max(0,radius/maxRadius),.85f)*Signed(Mathf.Sin(a),.82f));
            uv[i]=new Vector2((float)col/cols,t);
            if(row<rows&&col<cols){int n=i+cols+1;triangles.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
        }
        var m=new Mesh{name=name,vertices=v,triangles=triangles.ToArray(),uv=uv};m.RecalculateNormals();
        var ns=m.normals;for(int j=0;j<=rows;j++){int a=j*(cols+1),b=a+cols;ns[a]=ns[b]=(ns[a]+ns[b]).normalized;}m.normals=ns;m.RecalculateBounds();return MidChiikawaBuilder.SaveMesh("HachiwareSculpt_"+name,m);
    }
    static float HeadDepthOffset(float py)=>-.22f*Mathf.Pow(Mathf.Clamp01((py-350)/88),2);
    public static Vector3 Face(float x,float y,float layer=.003f)
    {
        Vector2 edges=WidthAt(HeadContour,420-y*200);float width=(edges.y-edges.x)/400,center=((edges.x+edges.y)*.5f-250)/200;
        float depth=.96f*Mathf.Pow(Mathf.Max(.0001f,width/1.1575f),.85f);
        float z=depth*Mathf.Pow(Mathf.Max(.0001f,1-Mathf.Pow(Mathf.Abs((x-center)/Mathf.Max(.001f,width)),2/.82f)),.82f/2);
        return new Vector3(x,y,HeadDepthOffset(420-y*200)-z-layer);
    }
    static Vector2 F(float x,float y)=>new Vector2((x-250)/200,(420-y)/200);
    static Vector3 W(float x,float y,float z=0)=>new Vector3((x-250)/200,(641-y)/200,z);
    static void PhotoPatch(string name,Transform neck,float x,float y,float rx,float ry,Material mat,float rotation=0,float layer=.004f){Vector2 p=F(x,y);Patch(name,neck,p.x,p.y,rx/200,ry/200,mat,rotation,layer);}
    static void PhotoCurve(string name,Transform neck,Vector2 a,Vector2 b,Vector2 c,Vector2 d,float width,Material mat){Stroke(name,neck,Bezier(F(a.x,a.y),F(b.x,b.y),F(c.x,c.y),F(d.x,d.y)),width/200,mat);}
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
        Part(name,parent,MidChiikawaBuilder.SaveMesh("HachiwareSculpt_"+name,mesh),Vector3.zero,mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
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
        Part(name,parent,MidChiikawaBuilder.SaveMesh("HachiwareSculpt_"+name,mesh),Vector3.zero,mat).GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        for(int i=0;i<2;i++){var p=points[i==0?0:points.Length-1];Patch(name+" cap"+i,parent,p.x,p.y,width*.5f,width*.5f,mat,0,layer+.0005f);}
    }
    static Vector2[] Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        var p=new Vector2[32];for(int i=0;i<p.Length;i++){float t=(float)i/(p.Length-1),u=1-t;p[i]=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}return p;
    }
    static void BlueCap(Transform neck,Material blue)
    {
        // One continuous shell around the entire crown; no front/back seam.
        const int cols=192,rows=64;var v=new Vector3[(cols+1)*(rows+1)];var indices=new List<int>();
        float Boundary(float x)=>139+61*Mathf.Pow(Mathf.Clamp01(Mathf.Abs(x-256)/220),.55f);
        for(int k=0;k<=cols;k++){
            float angle=2*Mathf.PI*k/cols,c=Signed(Mathf.Cos(angle),.82f),z=Signed(Mathf.Sin(angle),.82f);
            float lo=84,hi=202;
            for(int it=0;it<24;it++){float py=(lo+hi)*.5f;Vector2 e=WidthAt(HeadContour,py);float px=(e.x+e.y)*.5f+(e.y-e.x)*.5f*c;if(py<Boundary(px))lo=py;else hi=py;}
            float bottom=(lo+hi)*.5f;
            for(int j=0;j<=rows;j++){
                float py=Mathf.Lerp(bottom,84,(float)j/rows);Vector2 e=WidthAt(HeadContour,py);float r=(e.y-e.x)/400,center=((e.x+e.y)*.5f-250)/200;
                int i=j*(cols+1)+k;v[i]=new Vector3(center+r*c*1.008f,(420-py)/200+.006f,.96f*Mathf.Pow(Mathf.Max(0,r/1.1575f),.85f)*z*1.008f);
                if(j<rows&&k<cols){int n=i+cols+1;indices.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
            }
        }
        var m=new Mesh{name="Continuous blue crown",vertices=v,triangles=indices.ToArray()};m.RecalculateNormals();Part("Blue forehead",neck,MidChiikawaBuilder.SaveMesh("HachiwareSculpt_BlueCap",m),Vector3.zero,blue);
    }
    [MenuItem("Mid/Build sculpted Hachiware study")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        white=Mat("White",new Color(.92f,.93f,.96f));ink=Mat("Ink",new Color(.035f,.032f,.03f));pink=Mat("Blush",new Color(1,.57f,.63f));shine=Mat("Glints",Color.white);var blue=Mat("Sky blue",new Color(.20f,.62f,.89f));
        var root=new GameObject("Hachiware - custom surface model").transform;
        Part("Body",root,ProfileSurface("Photo torso",BodyContour,641,.43f),new Vector3(0,0,.30f),white);
        for(int i=0;i<2;i++){
            float side=i==0?-1:1;
            var foot=i==0?new[]{new Vector3(555,190,190),new Vector3(578,162,221),new Vector3(610,173,223),new Vector3(632,185,216),new Vector3(641,201,201)}:new[]{new Vector3(555,292,292),new Vector3(578,267,324),new Vector3(610,265,311),new Vector3(632,271,299),new Vector3(641,285,285)};
            Part("Foot "+i,root,ProfileSurface("Foot "+i,foot,641,.22f),new Vector3(0,0,.33f),white);
            Part("Arm "+i,root,Surface("Arm "+i,new Vector3(.092f,.25f,.15f),.93f,1,.20f),W(i==0?77:422,453,.22f),white).localRotation=Quaternion.Euler(0,0,side*24);
        }
        var neck=new GameObject("NeckPivot").transform;neck.SetParent(root,false);neck.localPosition=W(250,420);
        Part("Head",neck,ProfileSurface("Photo head",HeadContour,420,.96f),Vector3.zero,white);
        var left=new[]{new Vector3(30,114,114),new Vector3(40,96,136),new Vector3(65,84,162),new Vector3(100,75,192),new Vector3(145,70,211),new Vector3(185,139,139)};
        var right=new[]{new Vector3(34,396,396),new Vector3(42,375,411),new Vector3(65,348,424),new Vector3(100,318,432),new Vector3(150,298,438),new Vector3(185,365,365)};
        Part("Ear left",neck,ProfileSurface("Ear left",left,420,.22f),new Vector3(0,0,.08f),blue);
        Part("Ear right",neck,ProfileSurface("Ear right",right,420,.22f),new Vector3(0,0,.08f),blue);
        BlueCap(neck,blue);
        for(int i=0;i<2;i++){
            float x=i==0?178:327,cx=i==0?128:371,cy=i==0?337:344;
            PhotoPatch("Blush "+i,neck,cx,cy,36,29,pink,i==0?-.17f:.1f);
            PhotoPatch("Eye "+i,neck,x,279,28,29,ink);
            PhotoPatch("Glint top "+i,neck,x+1,269,12,9,shine,.5f,.010f);
            PhotoCurve("Glint lower "+i,neck,new Vector2(x-9,290),new Vector2(x-2,296),new Vector2(x+3,295),new Vector2(x+11,289),4,shine);
            PhotoPatch("Short brow "+i,neck,i==0?179:323,i==0?218:222,7,5,ink);
            for(int k=0;k<4;k++){float xx=cx+(k-1.5f)*16;float h=(k==0||k==3)?10:13;Stroke("Cheek stitch "+i+"-"+k,neck,new[]{F(xx-4,cy+h),F(xx+4,cy-h)},5.5f/200,ink);}
        }
        PhotoPatch("Open mouth",neck,253,352,22,26,ink);
        PhotoPatch("Tongue",neck,254,352,11,15,pink,0,.009f);
        PhotoPatch("Nose",neck,254,322,6,9,ink);
        PhotoCurve("Mouth left",neck,new Vector2(219,326),new Vector2(222,345),new Vector2(247,346),new Vector2(253,328),10,ink);
        PhotoCurve("Mouth right",neck,new Vector2(253,328),new Vector2(256,347),new Vector2(281,344),new Vector2(285,325),10,ink);
        PhotoPatch("Chin",neck,254,388,10,4,ink);
        Directory.CreateDirectory("Assets/Mid/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Mid/Prefabs/Hachiware_Sculpt.prefab");
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Warm studio floor";ground.transform.localScale=Vector3.one*10;ground.GetComponent<Renderer>().sharedMaterial=Mat("Studio",new Color(.58f,.49f,.51f));
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.48f,.49f);
        var light=new GameObject("Large soft key",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=.65f;light.transform.rotation=Quaternion.Euler(35,-30,0);light.shadows=LightShadows.Soft;light.shadowStrength=.25f;
        var fill=new GameObject("Soft fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.18f;fill.transform.rotation=Quaternion.Euler(25,135,0);
        var camera=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=670f/400f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.79f,.74f,.74f);
        camera.transform.position=new Vector3(0,1.53f,-9);camera.transform.LookAt(new Vector3(0,1.53f,0));
        QualitySettings.antiAliasing=8;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
        var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.name="Mouse target ball";ball.transform.localScale=Vector3.one*.20f;ball.GetComponent<Renderer>().sharedMaterial=Mat("Ball",new Color(.53f,.76f,.74f));
        var controller=new GameObject("Mouse head tracking").AddComponent<MidBallTracking>();controller.ViewCamera=camera;controller.Ball=ball.transform;controller.Neck=neck;controller.Spectators=new[]{new MidBallTracking.Spectator{Neck=neck,FollowSpeed=4}};controller.ResetBallPosition=new Vector3(0,1.5f,-3);controller.HorizontalBounds=new Vector2(-2,2);controller.VerticalBounds=new Vector2(.4f,3.5f);controller.MaxYaw=35;controller.MaxUpPitch=22;controller.MaxDownPitch=20;controller.DisplayTitle="HACHIWARE / MODEL STUDY";controller.DisplaySubtitle="Custom head, body and limbs";controller.ResetPose();
        Vector3 bodyPosition=root.Find("Body").position;
        controller.SetWorldTarget(new Vector3(2,3,-3));for(int i=0;i<90;i++)controller.Step(1f/60);
        if(Quaternion.Angle(neck.localRotation,Quaternion.identity)<5 || root.Find("Body").position!=bodyPosition)throw new Exception("Head tracking verification failed");
        controller.ResetPose();
        ball.SetActive(false);
        Directory.CreateDirectory("Previews");Capture(camera,"Previews/hachiware_sculpt_front.png");
        ground.SetActive(false);CaptureOverlay(camera);ground.SetActive(true);
        camera.transform.position=new Vector3(4,2.1f,-8);camera.transform.LookAt(new Vector3(0,1.53f,0));Capture(camera,"Previews/hachiware_sculpt_angle.png");
        camera.transform.position=new Vector3(0,1.53f,-9);camera.transform.LookAt(new Vector3(0,1.53f,0));ball.SetActive(true);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();
        foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh==null)throw new Exception("Missing mesh: "+f.name);
        if(root.Find("NeckPivot/Head").GetComponent<MeshFilter>().sharedMesh.vertexCount<6000)throw new Exception("Head detail missing");
        Debug.Log("HACHIWARE_SCULPT_OK: custom head and limbs, conforming face graphics, prefab and two previews saved.");
    }
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1000,1340,24){antiAliasing=4};var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var t=new Texture2D(1000,1340,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1000,1340),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    static void CaptureOverlay(Camera camera)
    {
        var original=new Texture2D(2,2);original.LoadImage(File.ReadAllBytes("References/hachiware_figure_reference.jpg"));
        var rt=new RenderTexture(500,670,24){antiAliasing=4};var previous=RenderTexture.active;Color bg=camera.backgroundColor;
        camera.backgroundColor=new Color(0,0,0,0);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var model=new Texture2D(500,670,TextureFormat.RGBA32,false);model.ReadPixels(new Rect(0,0,500,670),0,0);model.Apply();
        File.WriteAllBytes("Previews/hachiware_reference_aligned.png",model.EncodeToPNG());
        var rendered=model.GetPixels();var reference=original.GetPixels();
        for(int i=0;i<rendered.Length;i++){float opacity=.5f*rendered[i].a;rendered[i]=Color.Lerp(reference[i],rendered[i],opacity);rendered[i].a=1;}
        model.SetPixels(rendered);model.Apply();File.WriteAllBytes("Previews/hachiware_reference_overlay_50.png",model.EncodeToPNG());
        camera.backgroundColor=bg;camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(original);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
    public static void Open(){EditorSceneManager.OpenScene(ScenePath);EditorApplication.ExecuteMenuItem("Window/General/Game");}
}
