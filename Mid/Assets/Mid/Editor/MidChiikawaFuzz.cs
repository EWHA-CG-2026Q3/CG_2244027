using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MidChiikawaFuzz
{
    public static void Add(Transform root,string species)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mid/Generated/PlushFiber.mat");
        int seed=830;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            bool head=filter.name=="Head";
            if(!head && filter.name!="Ear" && filter.name!="Blue cat ear") continue;
            var rng=new System.Random(seed++);
            float R()=>(float)rng.NextDouble();
            var mesh=filter.sharedMesh;var sv=mesh.vertices;var sn=mesh.normals;var st=mesh.triangles;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            Color color=filter.GetComponent<Renderer>().sharedMaterial.color;
            int target=head?1100:500;
            for(int attempt=0;attempt<target*45 && vertices.Count<target*6;attempt++)
            {
                int triangle=rng.Next(st.Length/3)*3;
                float u=Mathf.Sqrt(R()),w=R();
                int a=st[triangle],b=st[triangle+1],c=st[triangle+2];
                Vector3 p=sv[a]*(1-u)+sv[b]*u*(1-w)+sv[c]*u*w;
                Vector3 n=(sn[a]*(1-u)+sn[b]*u*(1-w)+sn[c]*u*w).normalized;
                // Only lateral lower cheeks, never the eye/nose/mouth region.
                if(head && !(Mathf.Abs(p.x)>.365f && p.y<.06f && p.y>-.28f)) continue;
                // Ear side contours; leave the front lining clean.
                if(!head && Mathf.Abs(n.z)>.65f) continue;
                Vector3 tangent=Vector3.Cross(n,Mathf.Abs(n.y)>.9f?Vector3.right:Vector3.up).normalized;
                float length=.005f+R()*.006f, width=.0009f;
                Vector3 tip=p+n*length+tangent*length*.35f;
                int i=vertices.Count;
                vertices.Add(p-tangent*width);vertices.Add(p+tangent*width);
                vertices.Add(Vector3.Lerp(p,tip,.55f)-tangent*width*.55f);
                vertices.Add(Vector3.Lerp(p,tip,.55f)+tangent*width*.55f);
                vertices.Add(tip);vertices.Add(tip+tangent*.0001f);
                for(int k=0;k<6;k++){normals.Add(n);colors.Add(color*(.97f+R()*.04f));}
                indices.AddRange(new[]{i,i+2,i+1,i+1,i+2,i+3,i+2,i+4,i+3});
            }
            var fuzz=new Mesh{name=species+" cheek or ear fine nap"};fuzz.SetVertices(vertices);fuzz.SetNormals(normals);fuzz.SetColors(colors);fuzz.SetTriangles(indices,0);fuzz.RecalculateBounds();
            string path="Assets/Mid/Generated/CC_Fuzz_"+species+"_"+seed+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){old.Clear();old.vertices=fuzz.vertices;old.normals=fuzz.normals;old.colors=fuzz.colors;old.triangles=fuzz.triangles;old.bounds=fuzz.bounds;Object.DestroyImmediate(fuzz);fuzz=old;EditorUtility.SetDirty(old);} else AssetDatabase.CreateAsset(fuzz,path);
            var go=new GameObject("Cheek and ear edge nap",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(filter.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=fuzz;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}
