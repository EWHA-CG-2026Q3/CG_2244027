using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class MidFriendsBuilder
{
    const string ScenePath="Assets/Mid/Scenes/Mid_EightFriendsAudience.unity";
    static Material ink,pink,cream,orange,brown,glint;
    static string key;
    static Func<float,float,float,Vector3> project;
    static Material Mat(string name,Color color)
    {
        string path="Assets/Mid/Generated/Friends_"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Glossiness",.13f);EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static void MeshPart(string name,Transform parent,Mesh mesh,Material mat)
    {
        var t=Group(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=MidChiikawaBuilder.SaveMesh("Friends_"+key+"_"+name,mesh);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void Disc(string name,Transform parent,float x,float y,float rx,float ry,Material mat,float offset=.012f)
    {
        const int rings=8,cols=48;var v=new Vector3[(rings+1)*(cols+1)];var tri=new List<int>();
        for(int r=0;r<=rings;r++)for(int k=0;k<=cols;k++){
            float a=k*Mathf.PI*2/cols,t=(float)r/rings;int i=r*(cols+1)+k;v[i]=project(x+rx*t*Mathf.Cos(a),y+ry*t*Mathf.Sin(a),offset);
            if(r<rings&&k<cols){int n=i+cols+1;tri.AddRange(new[]{i,i+1,n,i+1,n+1,n});}
        }
        var m=new Mesh{name=name,vertices=v,triangles=tri.ToArray()};m.RecalculateNormals();MeshPart(name,parent,m,mat);
    }
    static void Stroke(string name,Transform parent,Vector2[] points,float width,Material mat)
    {
        var v=new List<Vector3>();var tri=new List<int>();
        for(int k=0;k<points.Length;k++){
            Vector2 d=(points[Math.Min(k+1,points.Length-1)]-points[Math.Max(k-1,0)]).normalized,n=new Vector2(-d.y,d.x)*width*.5f;
            v.Add(project(points[k].x+n.x,points[k].y+n.y,.018f));v.Add(project(points[k].x-n.x,points[k].y-n.y,.018f));
            if(k<points.Length-1){int i=2*k;tri.AddRange(new[]{i,i+2,i+1,i+1,i+2,i+3});}
        }
        var m=new Mesh{name=name,vertices=v.ToArray(),triangles=tri.ToArray()};m.RecalculateNormals();MeshPart(name,parent,m,mat);
        for(int i=0;i<2;i++){var p=points[i==0?0:points.Length-1];Disc(name+" end "+i,parent,p.x,p.y,width*.5f,width*.5f,mat,.019f);}
    }
    static Vector2[] Arc(float x,float y,float width,float arch)
    {var p=new Vector2[24];for(int i=0;i<p.Length;i++){float t=(float)i/(p.Length-1);p[i]=new Vector2(x+(t-.5f)*width,y+arch*Mathf.Sin(t*Mathf.PI));}return p;}
    static void Blob(string name,Transform parent,Vector3 p,Vector3 radius,Material mat,float angle=0)
    {
        var t=Group(name,parent);t.localPosition=p;t.localRotation=Quaternion.Euler(0,0,angle);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=MidChiikawaSculpt.Surface("Friends_"+key+"_"+name,radius,.9f,1);
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static Vector3 KuriFace(float x,float y,float offset)
    {
        float depth=1.02f*Mathf.Sqrt(Mathf.Max(.0001f,1-x*x/(1.10f*1.10f)-(y-.75f)*(y-.75f)/(.99f*.99f)));
        return new Vector3(x,y,-depth-offset);
    }
    static void ChestnutCap(Transform neck)
    {
        const int rows=48,cols=144;var v=new Vector3[(rows+1)*(cols+1)];var tri=new List<int>();
        // A circular patch centered on the upper-front crown, rather than a latitude band.
        Vector3 axis=new Vector3(0,.88f,-.475f).normalized;
        Vector3 across=Vector3.right,along=Vector3.Cross(axis,across);
        for(int j=0;j<=rows;j++)for(int k=0;k<=cols;k++){
            float a=k*2*Mathf.PI/cols,theta=.68f*(1f-(float)j/rows);
            Vector3 d=axis*Mathf.Cos(theta)+(across*Mathf.Cos(a)+along*Mathf.Sin(a))*Mathf.Sin(theta);
            int i=j*(cols+1)+k;
            v[i]=new Vector3(1.107f*d.x,.75f+.997f*d.y,1.027f*d.z);
            if(j<rows&&k<cols){int n=i+cols+1;tri.AddRange(new[]{i,i+1,n,i+1,n+1,n});}
        }
        var mesh=new Mesh{name="Rounded chestnut cap",vertices=v,triangles=tri.ToArray()};mesh.RecalculateNormals();MeshPart("Chestnut cap",neck,mesh,brown);
    }
    static Vector3 RakkoFace(float x,float y,float offset)
    {
        float z=.98f*Mathf.Sqrt(Mathf.Max(.0001f,1-x*x/(1.04f*1.04f)-(y-.75f)*(y-.75f)/(1.04f*1.04f)));
        return new Vector3(x,y,-z-offset);
    }
    static GameObject Rakko()
    {
        key="Rakko";project=RakkoFace;
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Chiikawa_Sculpt.prefab"));root.name="Rakko";
        var neck=root.transform.Find("NeckPivot");var remove=new List<GameObject>();foreach(Transform t in neck)if(t.name!="Head")remove.Add(t.gameObject);foreach(var o in remove)Object.DestroyImmediate(o);
        var head=neck.Find("Head");head.localPosition=new Vector3(0,.75f,0);
        head.GetComponent<MeshFilter>().sharedMesh=MidChiikawaSculpt.Surface("Rakko_OvalHead",new Vector3(1.04f,1.04f,.98f),1,1);
        var body=Mat("Rakko ivory",new Color(.98f,.90f,.78f));var dark=Mat("Rakko dark paws",new Color(.10f,.065f,.045f));var stitch=Mat("Rakko embroidery",new Color(.24f,.105f,.055f));var cloth=Mat("Rakko white cloak",new Color(.94f,.93f,.99f));var gold=Mat("Rakko gold scar",new Color(.94f,.65f,.16f));
        foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial=body;
        foreach(Transform t in root.transform)if(t.name.StartsWith("Arm")||t.name.StartsWith("Foot"))t.GetComponent<Renderer>().sharedMaterial=dark;
        foreach(float side in new[]{-1f,1f}){
            string id=side<0?"L":"R";
            Blob("Small dark ear "+id,neck,new Vector3(side*.99f,1.14f,.02f),new Vector3(.075f,.08f,.10f),dark,-side*20);
            float x=side*.36f;
            Disc("Eye "+id,neck,x,.79f,.145f,.103f,stitch);
            Disc("Glint upper "+id,neck,x,.825f,.084f,.028f,glint,.023f);
            Stroke("Glint lower "+id,neck,Arc(x,.755f,.19f,-.023f),.015f,glint);
            Stroke("Eyebrow "+id,neck,new[]{new Vector2(side*.22f,.96f),new Vector2(side*.25f,.975f)},.022f,stitch);
            Disc("Blush "+id,neck,side*.64f,.50f,.18f,.13f,pink);
            for(int k=0;k<4;k++){float xx=side*.64f+(k-1.5f)*.072f;Stroke("Blush stitch "+id+k,neck,new[]{new Vector2(xx-.014f,.46f),new Vector2(xx+.014f,.54f)},.021f,stitch);}
        }
        Disc("Nose",neck,0,.58f,.034f,.030f,stitch);
        Stroke("Nose stem",neck,new[]{new Vector2(0,.58f),new Vector2(0,.49f)},.027f,stitch);
        Stroke("Smile L",neck,Arc(-.06f,.49f,.12f,-.035f),.029f,stitch);Stroke("Smile R",neck,Arc(.06f,.49f,.12f,-.035f),.029f,stitch);
        Stroke("Chin",neck,Arc(0,.36f,.045f,-.007f),.019f,stitch);
        // Two tapered embroidered slashes form the distinctive forehead scar.
        void Slash(string name,Vector2 a,Vector2 b,float width){
            Vector2 d=(b-a).normalized,n=new Vector2(-d.y,d.x)*width;Vector2 m=Vector2.Lerp(a,b,.48f);
            var points=new List<Vector3>();var indices=new List<int>();
            void Triangle(Vector2 p0,Vector2 p1,Vector2 p2,int depth){
                if(depth>0){var ab=(p0+p1)*.5f;var bc=(p1+p2)*.5f;var ca=(p2+p0)*.5f;Triangle(p0,ab,ca,depth-1);Triangle(ab,p1,bc,depth-1);Triangle(ca,bc,p2,depth-1);Triangle(ab,bc,ca,depth-1);return;}
                int start=points.Count;foreach(var p in new[]{p0,p1,p2})points.Add(project(p.x,p.y,.023f));indices.AddRange(new[]{start,start+1,start+2,start+2,start+1,start});
            }
            Triangle(a,m+n,b,4);Triangle(a,b,m-n,4);
            var mesh=new Mesh{name=name,vertices=points.ToArray(),triangles=indices.ToArray()};var normals=new Vector3[points.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.back;mesh.normals=normals;MeshPart(name,neck,mesh,gold);
        }
        Slash("Gold scar long",new Vector2(-.70f,1.02f),new Vector2(-.43f,1.39f),.040f);
        Slash("Gold scar cross",new Vector2(-.70f,1.30f),new Vector2(-.49f,1.14f),.025f);
        // Softly folded cape wraps the back and sides, leaving the cream belly visible.
        const int rows=24,cols=96;var vertices=new Vector3[(rows+1)*(cols+1)];var tri=new List<int>();
        for(int j=0;j<=rows;j++)for(int k=0;k<=cols;k++){
            float t=(float)j/rows,a=Mathf.Lerp(-2.12f,2.12f,(float)k/cols),fold=.018f*Mathf.Cos(a*9)*t;
            int i=j*(cols+1)+k;vertices[i]=new Vector3((Mathf.Lerp(.68f,.87f,t)+fold)*Mathf.Sin(a),Mathf.Lerp(1.13f,.22f,t)+.025f*t*Mathf.Cos(a*4),(Mathf.Lerp(.41f,.49f,t)+fold)*Mathf.Cos(a));
            if(j<rows&&k<cols){int n=i+cols+1;tri.AddRange(new[]{i,n,i+1,i+1,n,n+1,i,i+1,n,i+1,n+1,n});}
        }
        var cape=new Mesh{name="Rakko draped cape",vertices=vertices,triangles=tri.ToArray()};var capeNormals=new Vector3[vertices.Length];for(int j=0;j<=rows;j++)for(int k=0;k<=cols;k++){float a=Mathf.Lerp(-2.12f,2.12f,(float)k/cols);capeNormals[j*(cols+1)+k]=new Vector3(Mathf.Sin(a),.15f,Mathf.Cos(a)).normalized;}cape.normals=capeNormals;MeshPart("White draped cape",root.transform,cape,cloth);
        // Elliptical padded scarf sits below the independently rotating head.
        const int around=128,tube=24;var sv=new Vector3[(around+1)*(tube+1)];var st=new List<int>();
        for(int j=0;j<=around;j++)for(int k=0;k<=tube;k++){
            float a=j*2*Mathf.PI/around,b=k*2*Mathf.PI/tube;int i=j*(tube+1)+k;
            sv[i]=new Vector3((.76f+.055f*Mathf.Cos(b))*Mathf.Cos(a),1.02f+.105f*Mathf.Sin(b),(.55f+.055f*Mathf.Cos(b))*Mathf.Sin(a));
            if(j<around&&k<tube){int n=i+tube+1;st.AddRange(new[]{i,i+1,n,i+1,n+1,n});}
        }
        var scarf=new Mesh{name="Rakko padded scarf",vertices=sv,triangles=st.ToArray()};scarf.RecalculateNormals();MeshPart("White neck scarf",root.transform,scarf,cloth);
        MidRakkoFur.Add(root.transform);
        return root;
    }
    static GameObject Kani()
    {
        key="Kani";project=MidChiikawaSculpt.Face;
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Chiikawa_Sculpt.prefab"));model.name="Kani";
        var neck=model.transform.Find("NeckPivot");var remove=new List<GameObject>();foreach(Transform t in neck)if(t.name!="Head")remove.Add(t.gameObject);foreach(var o in remove)Object.DestroyImmediate(o);
        var peach=Mat("Kani peach",new Color(1f,.86f,.79f));var rose=Mat("Kani crown pink",new Color(.96f,.55f,.61f));
        foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=peach;
        // Continuous crown follows the same rounded profile as the head.
        const int rows=48,cols=128;var vertices=new Vector3[(rows+1)*(cols+1)];var triangles=new List<int>();
        float Pow(float x,float e)=>Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),e);
        for(int j=0;j<=rows;j++)for(int k=0;k<=cols;k++){
            float a=k*2*Mathf.PI/cols,py=Mathf.Lerp(40,119-12*Mathf.Cos(a),(float)j/rows);
            var edges=MidChiikawaSculpt.WidthAt(MidChiikawaSculpt.HeadContour,py);float radius=(edges.y-edges.x)/320,center=((edges.x+edges.y)*.5f-250)/160;
            int i=j*(cols+1)+k;vertices[i]=new Vector3(center+(radius+.006f)*Pow(Mathf.Cos(a),.82f),(300-py)/160+.005f,(.96f*Mathf.Pow(Mathf.Max(.0001f,radius/1.1125f),.85f)+.007f)*Pow(Mathf.Sin(a),.82f));
            if(j<rows&&k<cols){int n=i+cols+1;triangles.AddRange(new[]{i,i+1,n,i+1,n+1,n});}
        }
        var cap=new Mesh{name="Kani curved pink crown",vertices=vertices,triangles=triangles.ToArray()};cap.RecalculateNormals();MeshPart("Pink crown",neck,cap,rose);
        foreach(float side in new[]{-1f,1f}){
            string id=side<0?"L":"R";
            // A single inflated, notched silhouette gives each claw a soft continuous surface.
            const int rings=32,segments=96;var v=new List<Vector3>();var tr=new List<int>();
            for(int face=0;face<2;face++)for(int r=0;r<=rings;r++)for(int k=0;k<=segments;k++){
                float t=(float)r/rings,a=k*2*Mathf.PI/segments;
                float notch=.12f*Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(a*Mathf.Rad2Deg,90)/18,2));
                float x=.29f*Mathf.Cos(a),y=.34f*Mathf.Sin(a)-notch;
                v.Add(new Vector3(x*t,y*t,(face==0?-1:1)*.17f*Mathf.Sqrt(Mathf.Max(0,1-t*t))));
                if(r<rings&&k<segments){int i=face*(rings+1)*(segments+1)+r*(segments+1)+k,n=i+segments+1;if(face==0)tr.AddRange(new[]{i,i+1,n,i+1,n+1,n});else tr.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
            }
            var claw=Group("Rounded claw "+id,neck);claw.localPosition=new Vector3(side*.66f,1.76f,.02f);claw.localRotation=Quaternion.Euler(0,0,-side*19);
            var mesh=new Mesh{name="Kani soft claw "+id,vertices=v.ToArray(),triangles=tr.ToArray()};mesh.RecalculateNormals();MeshPart("Claw surface "+id,claw,mesh,rose);
            float xEye=side*.36f;
            Disc("Eye "+id,neck,xEye,.70f,.105f,.115f,ink);Disc("Glint "+id,neck,xEye-.018f,.745f,.045f,.034f,glint,.023f);
            Stroke("Glint lower "+id,neck,Arc(xEye,.645f,.067f,-.012f),.012f,glint);
            for(int line=0;line<2;line++)Stroke("Eyebrow "+id+line,neck,Arc(side*.34f,1.00f+line*.075f,.10f,.005f),.022f,ink);
            Disc("Blush "+id,neck,side*.64f,.43f,.18f,.13f,pink);
            for(int k=0;k<4;k++){float x=side*.64f+(k-1.5f)*.075f;Stroke("Blush stitch "+id+k,neck,new[]{new Vector2(x-.014f,.39f),new Vector2(x+.014f,.47f)},.021f,ink);}
        }
        Disc("Open reference mouth",neck,0,.37f,.075f,.10f,ink);
        Stroke("Smile left",neck,Arc(-.06f,.46f,.12f,-.04f),.032f,ink);Stroke("Smile right",neck,Arc(.06f,.46f,.12f,-.04f),.032f,ink);
        Disc("Nose",neck,0,.47f,.022f,.025f,ink,.023f);
        Stroke("Chin",neck,Arc(0,.23f,.055f,-.013f),.020f,ink);
        return model;
    }
    static GameObject Momonga()
    {
        key="Momonga";project=MidChiikawaSculpt.Face;
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Chiikawa_Sculpt.prefab"));model.name="Momonga";
        var neck=model.transform.Find("NeckPivot");var remove=new List<GameObject>();foreach(Transform t in neck)if(t.name!="Head")remove.Add(t.gameObject);foreach(var o in remove)Object.DestroyImmediate(o);
        var white=Mat("Momonga snow",new Color(.94f,.96f,.98f));var blue=Mat("Momonga sky",new Color(.53f,.79f,.88f));
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=white;
        foreach(float side in new[]{-1f,1f}){
            string id=side<0?"L":"R";
            Blob("Tall round ear "+id,neck,new Vector3(side*.70f,1.60f,.02f),new Vector3(.28f,.40f,.20f),white,-side*16);
            Blob("Blue inner ear "+id,neck,new Vector3(side*.73f,1.64f,-.165f),new Vector3(.16f,.25f,.035f),blue,-side*16);
            Blob("Inner ear tuft "+id,neck,new Vector3(side*.55f,1.52f,-.19f),new Vector3(.09f,.13f,.055f),white,side*15);
            // A continuous scalloped cheek mesh instead of separate round balls.
            const int rows=64,cols=48;var vertices=new Vector3[(rows+1)*(cols+1)];var tri=new List<int>();
            for(int r=0;r<=rows;r++)for(int k=0;k<=cols;k++){
                float t=(float)r/rows,a=k*Mathf.PI*2/cols,cap=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.40f);
                float scallop=.17f+.075f*(.5f+.5f*Mathf.Cos(t*Mathf.PI*8));int i=r*(cols+1)+k;
                vertices[i]=new Vector3(side*(1.02f+cap*scallop*Mathf.Cos(a)),.20f+t*.88f,.03f+.24f*cap*Mathf.Sin(a));
                if(r<rows&&k<cols){int n=i+cols+1;if(side>0)tri.AddRange(new[]{i,n,i+1,i+1,n,n+1});else tri.AddRange(new[]{i,i+1,n,i+1,n+1,n});}
            }
            var mesh=new Mesh{name="Scalloped cheek "+id,vertices=vertices,triangles=tri.ToArray()};mesh.RecalculateNormals();MeshPart("Cheek fluff "+id,neck,mesh,white);
            float x=side*.40f;
            Disc("Eye "+id,neck,x,.73f,.205f,.135f,ink);
            Disc("Glint upper "+id,neck,x,.77f,.105f,.044f,glint,.022f);
            Stroke("Glint lower "+id,neck,Arc(x,.68f,.24f,-.025f),.019f,glint);
            Stroke("Eyebrow "+id,neck,new[]{new Vector2(side*.21f,.92f),new Vector2(side*.245f,.945f)},.022f,ink);
            Disc("Blush "+id,neck,side*.65f,.43f,.18f,.14f,pink);
            for(int k=0;k<4;k++){float xx=side*.65f+(k-1.5f)*.075f;Stroke("Blush stitch "+id+k,neck,new[]{new Vector2(xx-.014f,.39f),new Vector2(xx+.014f,.48f)},.020f,ink);}
        }
        Disc("Blue nose patch",neck,0,.72f,.145f,.145f,blue);
        Disc("Nostril L",neck,-.022f,.80f,.012f,.025f,ink,.023f);Disc("Nostril R",neck,.022f,.80f,.012f,.025f,ink,.023f);
        Disc("Nose",neck,0,.51f,.028f,.028f,ink);
        Stroke("Smile L",neck,Arc(-.065f,.48f,.13f,-.045f),.032f,ink);Stroke("Smile R",neck,Arc(.065f,.48f,.13f,-.045f),.032f,ink);
        Stroke("Mouth bottom",neck,Arc(0,.41f,.08f,-.03f),.022f,ink);Stroke("Chin",neck,Arc(0,.34f,.07f,-.014f),.020f,ink);
        // Tail belongs to the body, leaving head and ears independently rotatable.
        var tail=Group("Fluffy blue tail",model.transform);tail.localPosition=new Vector3(.40f,.93f,.72f);tail.localRotation=Quaternion.Euler(-18,0,-22);
        Blob("Tail body",tail,Vector3.zero,new Vector3(.43f,.86f,.32f),blue);
        Blob("Tail white tip",tail,new Vector3(-.03f,.57f,-.015f),new Vector3(.31f,.32f,.28f),white,-12);
        MidMomongaFur.Add(model.transform);
        return model;
    }
    static GameObject NewFriend(string kind)
    {
        key=kind;project=kind=="Kurimanju"?KuriFace:MidChiikawaSculpt.Face;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Chiikawa_Sculpt.prefab");var root=Object.Instantiate(source);root.name=kind;
        var neck=root.transform.Find("NeckPivot");var delete=new List<GameObject>();foreach(Transform t in neck)if(t.name!="Head")delete.Add(t.gameObject);foreach(var o in delete)Object.DestroyImmediate(o);
        Material body=kind=="Shisa"?cream:Mat("Kuri biscuit",new Color(.91f,.77f,.52f));
        foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial=body;
        if(kind=="Shisa"){
            foreach(float side in new[]{-1f,1f}){
                string id=side<0?"L":"R";
                for(int j=0;j<3;j++){
                    Blob("Mane "+id+j,neck,new Vector3(side*1.02f,.35f+j*.36f,.06f),new Vector3(.26f,.25f,.28f),orange,-side*(20+j*10));
                    Blob("Mane curl "+id+j,neck,new Vector3(side*1.15f,.44f+j*.36f,.04f),new Vector3(.12f,.13f,.23f),orange,side*25);
                }
                Blob("Curled ear "+id,neck,new Vector3(side*.70f,1.48f,.05f),new Vector3(.20f,.22f,.20f),cream,-side*20);
                Blob("Ear curl "+id,neck,new Vector3(side*.77f,1.57f,.05f),new Vector3(.12f,.15f,.18f),cream,-side*25);
                Vector3 brow=project(side*.36f,1.00f,.025f);
                Blob("Orange brow "+id,neck,brow,new Vector3(.14f,.14f,.055f),orange,side*25);
                Blob("Orange brow curl "+id,neck,project(side*.46f,1.02f,.03f),new Vector3(.12f,.07f,.05f),orange,side*22);
            }
        }else{
            var head=neck.Find("Head");head.localPosition=new Vector3(0,.75f,0);
            head.GetComponent<MeshFilter>().sharedMesh=MidChiikawaSculpt.Surface("Kurimanju_RoundHead",new Vector3(1.10f,.99f,1.02f),1,1);
            ChestnutCap(neck);
            foreach(float side in new[]{-1f,1f})Blob("Small ear "+side,neck,new Vector3(side*.84f,1.43f,-.07f),new Vector3(.115f,.085f,.10f),body,-side*25);
        }
        for(int i=0;i<2;i++){
            float side=i==0?-1:1;
            Disc("Blush "+i,neck,side*.65f,.49f,.18f,.14f,pink);
            for(int k=0;k<4;k++){float x=side*.65f+(k-1.5f)*.075f;Stroke("Blush stitch "+i+k,neck,new[]{new Vector2(x-.015f,.44f),new Vector2(x+.015f,.54f)},.021f,ink);}
            if(kind=="Shisa")Stroke("Eye "+i,neck,Arc(side*.35f,.72f,.18f,.035f),.025f,ink);
            else{Disc("Eye "+i,neck,side*.35f,.72f,.10f,.10f,ink);Disc("Glint "+i,neck,side*.35f-.008f,.75f,.034f,.026f,glint,.020f);Stroke("Glint smile "+i,neck,Arc(side*.35f,.68f,.07f,-.014f),.012f,glint);}
        }
        Disc("Nose",neck,0,.53f,.028f,.036f,ink);
        Stroke("Smile L",neck,Arc(-.07f,.47f,.14f,-.05f),.034f,ink);Stroke("Smile R",neck,Arc(.07f,.47f,.14f,-.05f),.034f,ink);
        if(kind=="Shisa"){Disc("Open mouth",neck,0,.33f,.095f,.15f,ink);Disc("Tongue",neck,0,.30f,.052f,.075f,pink,.023f);}
        Stroke("Chin",neck,Arc(0,kind=="Shisa"?.13f:.35f,.08f,-.018f),.022f,ink);
        // Props stay with the body when the head turns.
        if(kind=="Shisa"){
            var paper=Mat("Book ivory",new Color(.95f,.94f,.85f));var cover=Mat("Book mint",new Color(.44f,.68f,.55f));
            for(int i=0;i<2;i++){float side=i==0?-1:1;var page=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(page.GetComponent<Collider>());page.name="Open book page";page.transform.SetParent(root.transform,false);page.transform.localPosition=new Vector3(side*.23f,.69f,-.50f);page.transform.localScale=new Vector3(.47f,.53f,.045f);page.transform.localRotation=Quaternion.Euler(-10,-side*22,side*8);page.GetComponent<Renderer>().sharedMaterial=paper;
                var badge=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(badge.GetComponent<Collider>());badge.name="Book cover motif";badge.transform.SetParent(page.transform,false);badge.transform.localPosition=new Vector3(0,0,-.6f);badge.transform.localScale=new Vector3(.42f,.08f,.10f);badge.GetComponent<Renderer>().sharedMaterial=cover;
            }
        }else{
            var silver=Mat("Can silver",new Color(.64f,.68f,.72f));silver.SetFloat("_Metallic",.7f);silver.SetFloat("_Glossiness",.45f);
            var can=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Object.DestroyImmediate(can.GetComponent<Collider>());can.name="Small silver can";can.transform.SetParent(root.transform,false);can.transform.localPosition=new Vector3(-.57f,.68f,-.48f);can.transform.localScale=new Vector3(.25f,.21f,.25f);can.GetComponent<Renderer>().sharedMaterial=silver;
        }
        return root;
    }
    static bool FaceFeature(string name)
    {return name.StartsWith("Eye")||name.StartsWith("Glint")||name.StartsWith("Mouth")||name.StartsWith("Smile")||name.StartsWith("Open")||name.StartsWith("Tongue")||name.StartsWith("Nose")||name.StartsWith("Chin");}
    static void Expressions(GameObject root,string kind)
    {
        var neck=root.transform.Find("NeckPivot");var features=new List<Transform>();foreach(Transform t in neck)if(FaceFeature(t.name))features.Add(t);
        var normal=Group("Expression - Default",neck);foreach(var t in features)t.SetParent(normal,false);
        // Only the original character-specific face is retained.
        var state=root.AddComponent<MidExpression>();
        state.Faces=new[]{normal.gameObject};state.Expression=MidExpression.Mood.Default;state.Apply();
    }
    static GameObject CrossedUsagi(GameObject source)
    {
        key="Usagi_CrossedArms";project=MidUsagiSculpt.Face;
        var model=Object.Instantiate(source);model.name="Usagi - crossed arms and closed mouth";
        var state=model.GetComponent<MidExpression>();var face=state.Faces[0].transform;
        for(int i=1;i<state.Faces.Length;i++)if(state.Faces[i])Object.DestroyImmediate(state.Faces[i]);state.Faces=new[]{face.gameObject};state.Expression=MidExpression.Mood.Default;state.Apply();
        var remove=new List<GameObject>();foreach(Transform t in face)if(!(t.name.StartsWith("Eye")||t.name.StartsWith("Glint")))remove.Add(t.gameObject);foreach(var o in remove)Object.DestroyImmediate(o);
        Disc("Closed nose",face,.04f,.55f,.025f,.032f,ink);
        Stroke("Closed smile left",face,Arc(-.025f,.51f,.15f,-.045f),.032f,ink);
        Stroke("Closed smile right",face,Arc(.115f,.51f,.13f,-.04f),.032f,ink);
        Stroke("Closed lip hook",face,new[]{new Vector2(.10f,.49f),new Vector2(.10f,.44f),new Vector2(.075f,.42f),new Vector2(.025f,.42f)},.028f,ink);
        remove.Clear();foreach(Transform t in model.transform)if(t.name.StartsWith("Arm"))remove.Add(t.gameObject);foreach(var o in remove)Object.DestroyImmediate(o);
        var body=model.transform.Find("Body").GetComponent<Renderer>().sharedMaterial;
        // Two short, nearly horizontal arms: no elbow pieces or long crossing segments.
        Blob("Short folded arm behind",model.transform,new Vector3(.14f,.77f,-.15f),new Vector3(.245f,.105f,.13f),body,7);
        Blob("Short folded arm front",model.transform,new Vector3(-.14f,.82f,-.20f),new Vector3(.245f,.105f,.13f),body,-7);
        return model;
    }
    static GameObject KuriReferenceExpression(GameObject source)
    {
        key="Kurimanju_ReferenceExpression";project=KuriFace;
        var root=Object.Instantiate(source);root.name="Kurimanju - reference expression";
        var face=root.GetComponent<MidExpression>().Faces[0].transform;
        var remove=new List<GameObject>();foreach(Transform t in face)if(!t.name.StartsWith("Eye")&&!t.name.StartsWith("Glint"))remove.Add(t.gameObject);foreach(var o in remove)Object.DestroyImmediate(o);
        // The crinkled nose and scalloped open mouth are specific to the supplied reference.
        Disc("Open mouth",face,0,.29f,.115f,.15f,ink);
        Stroke("Smile upper left",face,Arc(-.075f,.46f,.15f,-.05f),.043f,ink);
        Stroke("Smile upper right",face,Arc(.075f,.46f,.15f,-.05f),.043f,ink);
        Disc("Nose",face,0,.46f,.035f,.035f,ink,.023f);
        Stroke("Mouth teeth",face,new[]{new Vector2(-.065f,.36f),new Vector2(-.035f,.375f),new Vector2(-.012f,.355f),new Vector2(.015f,.373f),new Vector2(.04f,.35f),new Vector2(.07f,.365f)},.014f,glint);
        Disc("Tongue left",face,-.030f,.275f,.036f,.044f,pink,.025f);
        Disc("Tongue right",face,.035f,.275f,.036f,.044f,pink,.025f);
        Stroke("Mouth lower glint",face,Arc(0,.19f,.073f,-.017f),.014f,glint);
        Stroke("Chin",face,Arc(0,.105f,.09f,-.027f),.030f,ink);
        for(int side=-1;side<=1;side+=2){
            Stroke("Nose wrinkle inner "+side,face,new[]{new Vector2(side*.025f,.60f),new Vector2(side*.028f,.64f)},.024f,ink);
            Stroke("Nose wrinkle middle "+side,face,new[]{new Vector2(side*.095f,.61f),new Vector2(side*.12f,.63f)},.024f,ink);
            Stroke("Nose wrinkle outer "+side,face,Arc(side*.15f,.57f,.043f,.012f),.022f,ink);
            Stroke("Forehead wrinkle inner "+side,face,new[]{new Vector2(side*.030f,.72f),new Vector2(side*.038f,.77f),new Vector2(side*.025f,.79f)},.026f,ink);
            Stroke("Forehead wrinkle outer "+side,face,new[]{new Vector2(side*.105f,.70f),new Vector2(side*.125f,.73f)},.024f,ink);
        }
        return root;
    }
    [MenuItem("Mid/Apply simplified character faces")]
    public static void Build()
    {
        ink=Mat("Ink",new Color(.07f,.04f,.025f));pink=Mat("Pink",new Color(1,.55f,.62f));cream=Mat("Shisa cream",new Color(.98f,.92f,.70f));orange=Mat("Shisa orange",new Color(1,.43f,.13f));brown=Mat("Chestnut brown",new Color(.34f,.17f,.09f));glint=Mat("Glints",Color.white);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        string[] kinds={"Chiikawa","Usagi","Hachiware","Shisa","Kurimanju","Momonga","Kani","Rakko"};var prefabs=new GameObject[8];
        for(int i=0;i<8;i++){
            var model=i<3?Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/"+kinds[i]+"_Sculpt.prefab")):i==5?Momonga():i==6?Kani():i==7?Rakko():NewFriend(kinds[i]);
            model.name=kinds[i];
            if(i==5){var face=model.AddComponent<MidExpression>();var group=Group("Expression - Default",model.transform.Find("NeckPivot"));var parts=new List<Transform>();foreach(Transform t in model.transform.Find("NeckPivot"))if(FaceFeature(t.name))parts.Add(t);foreach(var t in parts)t.SetParent(group,false);face.Faces=new[]{group.gameObject};face.Apply();}
            else Expressions(model,kinds[i]);
            prefabs[i]=PrefabUtility.SaveAsPrefabAsset(model,"Assets/Mid/Prefabs/"+kinds[i]+"_Expressions.prefab");Object.DestroyImmediate(model);
        }
        var crossed=CrossedUsagi(prefabs[1]);
        var crossedPrefab=PrefabUtility.SaveAsPrefabAsset(crossed,"Assets/Mid/Prefabs/Usagi_CrossedArms.prefab");Object.DestroyImmediate(crossed);
        var kuriVariant=KuriReferenceExpression(prefabs[4]);
        var kuriVariantPrefab=PrefabUtility.SaveAsPrefabAsset(kuriVariant,"Assets/Mid/Prefabs/Kurimanju_ReferenceExpression.prefab");Object.DestroyImmediate(kuriVariant);
        EditorSceneManager.OpenScene("Assets/Mid/Scenes/Mid_ThreeSculptAudience.unity");var c=Object.FindAnyObjectByType<MidBallTracking>();
        foreach(var spectator in c.Spectators)Object.DestroyImmediate(spectator.Neck.parent.gameObject);
        var chair=Object.Instantiate(GameObject.Find("Seat 1"));chair.name="Chair template";
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(root.name.StartsWith("Seat "))Object.DestroyImmediate(root);
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(root.name.StartsWith("Terrace "))root.transform.localScale=new Vector3(11.3f,root.transform.localScale.y,root.transform.localScale.z);
        var viewers=new List<MidBallTracking.Spectator>();
        var unusedTerrace=GameObject.Find("Terrace 3");if(unusedTerrace)Object.DestroyImmediate(unusedTerrace);
        // Eight original faces, plus only the two distinct requested variants.
        int[] layout={0,1,2,4,7,5,6,3,1,4};
        var uniqueModels=new HashSet<GameObject>();
        for(int index=0;index<layout.Length;index++){
            int row=index/5,col=index%5,kind=layout[index];
            var prefab=index==8?crossedPrefab:index==9?kuriVariantPrefab:prefabs[kind];
            if(!uniqueModels.Add(prefab))throw new Exception("Duplicate character/expression in audience");
            Vector3 position=new Vector3((col-2f)*2.15f+(row%2)*.18f,row*1.05f,row*1.85f);
            var seat=Object.Instantiate(chair);seat.name="Seat "+(index+1);seat.transform.position=position;
            var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.name=index==8?"Usagi crossed arms":index==9?"Kurimanju reference expression":kinds[kind]+" Default";
            model.transform.localScale=Vector3.one*.66f;model.transform.position=position+new Vector3(0,kind==1?.65f:.68f,-.54f);
            var mood=model.GetComponent<MidExpression>();mood.Expression=MidExpression.Mood.Default;mood.Apply();
            viewers.Add(new MidBallTracking.Spectator{Neck=model.transform.Find("NeckPivot"),FollowSpeed=2.8f+index%7*.55f});
        }
        Object.DestroyImmediate(chair);c.Spectators=viewers.ToArray();c.Neck=viewers[0].Neck;
        c.DisplayTitle="EIGHT LITTLE FRIENDS";MidStarTarget.Replace(c);c.DisplaySubtitle="Chiikawa / Usagi / Hachiware / Shisa / Kurimanju / Momonga / Kani / Rakko";
        c.ViewCamera.orthographicSize=4.2f;c.ViewCamera.transform.position=new Vector3(0,6.5f,-14);c.ViewCamera.transform.LookAt(new Vector3(0,2.05f,.8f));c.ResetPose();
        MidPrototypeBuilder.VerifyCount(10);
        foreach(var s in c.Spectators){var mood=s.Neck.parent.GetComponent<MidExpression>();int active=0;foreach(var f in mood.Faces)if(f && f.activeSelf)active++;if(active!=1)throw new Exception("Expression group state failed");}
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        var buildScenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);if(!buildScenes.Exists(s=>s.path==ScenePath)){buildScenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=buildScenes.ToArray();}
        AssetDatabase.SaveAssets();
        Capture(c.ViewCamera,"Previews/eight_friends_audience.png");
        // A separate close-up lineup makes the two new models and all three moods reviewable.
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(root!=c.gameObject && root!=c.ViewCamera.gameObject && !root.GetComponent<Light>())root.SetActive(false);
        for(int type=3;type<5;type++){
            var models=new List<GameObject>();for(int mood=0;mood<1;mood++){var model=Object.Instantiate(prefabs[type]);model.transform.position=new Vector3(0,0,0);var state=model.GetComponent<MidExpression>();state.Expression=(MidExpression.Mood)mood;state.Apply();models.Add(model);}
            c.ViewCamera.orthographicSize=3.1f;c.ViewCamera.transform.position=new Vector3(0,2,-10);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/"+kinds[type].ToLower()+"_expressions.png");foreach(var model in models)Object.DestroyImmediate(model);
        }
        var kuriPreview=Object.Instantiate(kuriVariantPrefab);
        c.ViewCamera.orthographicSize=1.85f;c.ViewCamera.transform.position=new Vector3(0,1.65f,-10);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/kurimanju_reference_expression.png");
        c.ViewCamera.transform.position=new Vector3(3.5f,2.7f,-8);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/kurimanju_round_cap_angle.png");Object.DestroyImmediate(kuriPreview);
        var momongaPreview=Object.Instantiate(prefabs[5]);
        c.ViewCamera.orthographicSize=1.95f;c.ViewCamera.transform.position=new Vector3(0,1.75f,-10);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/momonga_front.png");
        c.ViewCamera.transform.position=new Vector3(4,2.3f,-8);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/momonga_angle.png");
        c.ViewCamera.transform.position=new Vector3(4,2.3f,8);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/momonga_tail.png");Object.DestroyImmediate(momongaPreview);
        var kaniPreview=Object.Instantiate(prefabs[6]);
        c.ViewCamera.orthographicSize=1.95f;c.ViewCamera.transform.position=new Vector3(0,1.85f,-10);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/kani_front.png");
        c.ViewCamera.transform.position=new Vector3(4,2.2f,-8);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/kani_angle.png");Object.DestroyImmediate(kaniPreview);
        var rakkoPreview=Object.Instantiate(prefabs[7]);
        c.ViewCamera.orthographicSize=1.85f;c.ViewCamera.transform.position=new Vector3(0,1.65f,-10);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/rakko_front.png");
        c.ViewCamera.transform.position=new Vector3(4,2.4f,-8);c.ViewCamera.transform.LookAt(new Vector3(0,1.65f,0));Capture(c.ViewCamera,"Previews/rakko_angle.png");Object.DestroyImmediate(rakkoPreview);
        var crossedPreview=Object.Instantiate(crossedPrefab);
        c.ViewCamera.orthographicSize=2.25f;c.ViewCamera.transform.position=new Vector3(0,2.2f,-10);c.ViewCamera.transform.LookAt(new Vector3(0,1.95f,0));Capture(c.ViewCamera,"Previews/usagi_crossed_arms.png");Object.DestroyImmediate(crossedPreview);
        foreach(var prefab in prefabs)if(prefab.GetComponent<MidExpression>().Faces.Length!=1)throw new Exception("Unrequested face variations remain");
        if(crossedPrefab.GetComponent<MidExpression>().Faces.Length!=1)throw new Exception("Unexpected added Usagi expressions");
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("EIGHT_FRIENDS_OK: Rakko added; Kurimanju circular crown, larger ears and reference expression added; Shisa smile only; one crossed-arm closed-mouth Usagi variant; 10 unique character/expression spectators; fixed short feet; rounded star; tracking verified.");
    }
    static void Capture(Camera camera,string path)
    {var rt=new RenderTexture(1500,900,24){antiAliasing=4};var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var t=new Texture2D(1500,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1500,900),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(t);rt.Release();Object.DestroyImmediate(rt);}
    public static void OpenDemo()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.delayCall+=()=>{
            var controller=Object.FindAnyObjectByType<MidBallTracking>();
            controller.AutoDemo=false;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying=true;
        };
    }
    public static void BuildAndRecord(){Build();MidVideoCapture.RecordTo("Recordings/SixFriendsFrames",1.8f);}
}
