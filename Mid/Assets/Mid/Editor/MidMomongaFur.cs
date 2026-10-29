using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class MidMomongaFur
{
    public static void Add(Transform root)
    {
        var fiber=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mid/Generated/PlushFiber.mat");
        if(!fiber)throw new Exception("Missing plush fiber shader material");
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
            bool cheek=filter.name.StartsWith("Cheek fluff"),tail=filter.name=="Tail body",tip=filter.name=="Tail white tip";
            if(!cheek&&!tail&&!tip)continue;
            string name=filter.name.Replace(" ","_");var renderer=filter.GetComponent<Renderer>();Color baseColor=renderer.sharedMaterial.color;
            string path="Assets/Mid/Generated/Momonga_Soft_"+name+".mat";
            var soft=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!soft){soft=new Material(Shader.Find("Mid/FeltSurface"));AssetDatabase.CreateAsset(soft,path);}
            soft.color=baseColor;soft.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mid/Generated/FeltFiber.png"));
            soft.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mid/Generated/FeltNormal.png"));
            soft.SetFloat("_Scale",7);soft.SetFloat("_BumpStrength",.10f);EditorUtility.SetDirty(soft);renderer.sharedMaterial=soft;
            var mesh=filter.sharedMesh;var points=mesh.vertices;var normals=mesh.normals;var triangles=mesh.triangles;
            // Area-weighted sampling avoids stripes from the source mesh's narrow pole rings.
            var cumulative=new float[triangles.Length/3];float area=0;
            for(int i=0;i<cumulative.Length;i++){Vector3 a=points[triangles[i*3]],b=points[triangles[i*3+1]],c=points[triangles[i*3+2]];area+=Vector3.Cross(b-a,c-a).magnitude*.5f;cumulative[i]=area;}
            var rng=new System.Random(cheek?(name.EndsWith("L")?761:762):tail?763:764);float R()=>(float)rng.NextDouble();
            int strands=cheek?10000:tail?22000:9000;
            var v=new List<Vector3>();var ns=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            for(int strand=0;strand<strands;strand++){
                int tri=Array.BinarySearch(cumulative,R()*area);if(tri<0)tri=~tri;tri=Mathf.Min(tri,cumulative.Length-1)*3;
                int ia=triangles[tri],ib=triangles[tri+1],ic=triangles[tri+2];float u=Mathf.Sqrt(R()),w=R();
                Vector3 p=points[ia]*(1-u)+points[ib]*u*(1-w)+points[ic]*u*w;
                Vector3 n=(normals[ia]*(1-u)+normals[ib]*u*(1-w)+normals[ic]*u*w).normalized;
                Vector3 t=Vector3.Cross(n,Mathf.Abs(n.y)>.95f?Vector3.right:Vector3.up).normalized,b=Vector3.Cross(n,t);
                float angle=R()*Mathf.PI*2;Vector3 across=t*Mathf.Cos(angle)+b*Mathf.Sin(angle);
                Vector3 lay=Vector3.ProjectOnPlane(Vector3.up,n).normalized*.35f+across*(R()-.5f)*.3f;
                float length=(cheek?.010f:.016f)+R()*(cheek?.014f:.022f),width=cheek?.00075f:.0010f;
                int start=v.Count;Color color=baseColor*(.97f+R()*.05f);
                for(int ring=0;ring<3;ring++){
                    float f=ring*.5f;Vector3 center=p+n*(length*f)+lay*(length*f*f);float half=width*(1-f*.96f);
                    v.Add(center-across*half);v.Add(center+across*half);ns.Add(n);ns.Add(n);colors.Add(color);colors.Add(color);
                }
                indices.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3,start+2,start+4,start+3,start+3,start+4,start+5});
            }
            var fuzz=new Mesh{name="Momonga soft nap "+name,indexFormat=IndexFormat.UInt32};fuzz.SetVertices(v);fuzz.SetNormals(ns);fuzz.SetColors(colors);fuzz.SetTriangles(indices,0);fuzz.RecalculateBounds();
            string asset="Assets/Mid/Generated/Momonga_Nap_"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
            if(old){old.Clear();old.indexFormat=IndexFormat.UInt32;old.vertices=fuzz.vertices;old.normals=fuzz.normals;old.colors=fuzz.colors;old.triangles=fuzz.triangles;old.bounds=fuzz.bounds;Object.DestroyImmediate(fuzz);fuzz=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(fuzz,asset);
            var child=new GameObject("Soft fine nap",typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(filter.transform,false);child.GetComponent<MeshFilter>().sharedMesh=fuzz;
            var r=child.GetComponent<MeshRenderer>();r.sharedMaterial=fiber;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
        }
        Debug.Log("MOMONGA_FUR_OK: cheek and tail only; short area-weighted fibers; smooth face preserved.");
    }
}
