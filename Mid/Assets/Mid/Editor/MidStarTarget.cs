using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MidStarTarget
{
    static Material Material(string name,Color color)
    {
        string path="Assets/Mid/Generated/Star_"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",.12f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
    }
    public static void Replace(MidBallTracking controller)
    {
        if(controller.Ball)Object.DestroyImmediate(controller.Ball.gameObject);
        var root=new GameObject("Little shooting star - mouse target").transform;
        var yellow=Material("Lemon",new Color(1,.94f,.22f));var brown=Material("Cocoa",new Color(.16f,.085f,.045f));var white=Material("Eye sparkle",Color.white);
        var corners=new Vector2[10];
        for(int i=0;i<10;i++){float a=Mathf.PI/2+i*Mathf.PI/5;float r=i%2==0?.49f:.32f;corners[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
        var outline=new List<Vector2>();
        for(int i=0;i<10;i++){
            var p=corners[i];var start=Vector2.Lerp(p,corners[(i+9)%10],.44f);var end=Vector2.Lerp(p,corners[(i+1)%10],.44f);
            for(int k=0;k<12;k++){float t=k/12f,u=1-t;outline.Add(u*u*start+2*u*t*p+t*t*end);}
        }
        int n=outline.Count;const int rings=16;var v=new List<Vector3>();var triangles=new List<int>();
        for(int side=0;side<2;side++)for(int ring=0;ring<=rings;ring++)for(int k=0;k<=n;k++){
            float r=(float)ring/rings;Vector2 xy=outline[k%n]*r;float z=.135f*Mathf.Sqrt(Mathf.Max(0,1-r*r))*(side==0?-1:1);
            int i=v.Count;v.Add(new Vector3(xy.x,xy.y,z));
            if(ring<rings&&k<n){int next=i+n+1;if(side==0)triangles.AddRange(new[]{i,i+1,next,i+1,next+1,next});else triangles.AddRange(new[]{i,next,i+1,i+1,next,next+1});}
        }
        var mesh=new Mesh{name="Rounded five point star",vertices=v.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
        root.gameObject.AddComponent<MeshFilter>().sharedMesh=MidChiikawaBuilder.SaveMesh("ShootingStar",mesh);root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=yellow;
        var rim=new Vector3[n+1];for(int i=0;i<=n;i++)rim[i]=new Vector3(outline[i%n].x,outline[i%n].y,0);
        Line("Soft cocoa outline",root,rim,.013f,brown);
        for(int i=0;i<2;i++){
            float x=i==0?-.088f:.088f;
            MidPrototypeBuilder.Sphere("Eye",root,new Vector3(x,.025f,-.139f),new Vector3(.065f,.072f,.019f),brown);
            MidPrototypeBuilder.Sphere("Eye shine",root,new Vector3(x-.008f,.038f,-.151f),new Vector3(.029f,.026f,.008f),white);
            MidPrototypeBuilder.Sphere("Eye shine lower",root,new Vector3(x+.006f,.006f,-.151f),new Vector3(.021f,.009f,.006f),white);
        }
        Line("Little smile",root,new[]{new Vector3(-.027f,-.027f,-.141f),new Vector3(-.018f,-.036f,-.142f),new Vector3(0,-.031f,-.143f),new Vector3(.020f,-.037f,-.142f),new Vector3(.029f,-.025f,-.141f)},.009f,brown);
        Line("Chin",root,new[]{new Vector3(-.014f,-.058f,-.139f),new Vector3(.010f,-.059f,-.139f)},.007f,brown);
        var face=root.gameObject.AddComponent<MidStarFacing>();face.ViewCamera=controller.ViewCamera;root.rotation=controller.ViewCamera.transform.rotation;
        controller.Ball=root;controller.DisplaySubtitle="Nine little spectators. Follow the little star.";
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Mid/Prefabs/LittleShootingStar.prefab");
        Debug.Log("STAR_TARGET_OK: rounded five-point mesh, face and outline, existing mouse target connected.");
    }
    static void Line(string name,Transform parent,Vector3[] points,float width,Material material)
    {
        var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,false);line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;line.numCornerVertices=5;line.numCapVertices=5;line.sharedMaterial=material;line.shadowCastingMode=ShadowCastingMode.Off;
    }
}
