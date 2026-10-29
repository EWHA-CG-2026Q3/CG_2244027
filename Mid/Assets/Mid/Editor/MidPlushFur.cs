using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MidPlushFur
{
    public static void Add(Transform doll, string species = "Bear")
    {
        string path="Assets/Mid/Generated/PlushFiber.mat";
        Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) { material=new Material(Shader.Find("Mid/PlushFiber")); AssetDatabase.CreateAsset(material,path); }
        int seed=120;
        // Snapshot the original meshes so generated fibers are never sampled recursively.
        foreach(var filter in doll.GetComponentsInChildren<MeshFilter>())
        {
            string n=filter.name;
            if(n!="Head" && n!="Ear" && n!="Arm" && n!="Paw" && n!="Procedural pear body" && n!="Belly patch") continue;
            int count=n=="Head"?16000:n=="Procedural pear body"?10000:n=="Belly patch"?2500:2200;
            Mesh mesh=Create(filter.sharedMesh, count, seed++, filter.GetComponent<Renderer>().sharedMaterial.color, species, n);
            string meshPath="Assets/Mid/Generated/Fibers_"+species+"_"+seed+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(old) { old.Clear(); old.indexFormat=mesh.indexFormat; old.vertices=mesh.vertices; old.normals=mesh.normals; old.colors=mesh.colors; old.triangles=mesh.triangles; old.bounds=mesh.bounds; EditorUtility.SetDirty(old); UnityEngine.Object.DestroyImmediate(mesh); mesh=old; }
            else AssetDatabase.CreateAsset(mesh,meshPath);
            var child=new GameObject("Short plush fibers",typeof(MeshFilter),typeof(MeshRenderer));
            child.transform.SetParent(filter.transform,false);
            child.GetComponent<MeshFilter>().sharedMesh=mesh;
            child.GetComponent<Renderer>().sharedMaterial=material;
            child.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }

    static Mesh Create(Mesh source,int count,int seed,Color baseColor,string species,string part)
    {
        var rng=new System.Random(seed);
        float R() => (float)rng.NextDouble();
        var sv=source.vertices; var sn=source.normals; var st=source.triangles;
        var areas=new float[st.Length/3]; float area=0;
        for(int t=0;t<areas.Length;t++) { int i=t*3; area+=Vector3.Cross(sv[st[i+1]]-sv[st[i]],sv[st[i+2]]-sv[st[i]]).magnitude; areas[t]=area; }
        bool rabbit=species=="Rabbit", bear=species=="Bear";
        int rings=bear?6:rabbit?3:4;
        var v=new List<Vector3>(count*(rings*3+1)); var normals=new List<Vector3>(count*7);
        var colors=new List<Color>(count*7); var indices=new List<int>(count*27);
        for(int hair=0;hair<count;hair++)
        {
            int tri=Array.BinarySearch(areas,R()*area); if(tri<0) tri=~tri; tri=Mathf.Min(tri,areas.Length-1)*3;
            float u=Mathf.Sqrt(R()), w=R();
            float a=1-u,b=u*(1-w),c=u*w;
            Vector3 p=sv[st[tri]]*a+sv[st[tri+1]]*b+sv[st[tri+2]]*c;
            Vector3 n=(sn[st[tri]]*a+sn[st[tri+1]]*b+sn[st[tri+2]]*c).normalized;
            Vector3 tangent=Vector3.Cross(n,Mathf.Abs(n.y)>.9f?Vector3.right:Vector3.up).normalized;
            Vector3 bitangent=Vector3.Cross(n,tangent);
            // Nearby roots share a lay direction: small tufts, rather than outward spikes.
            float clump=Mathf.PerlinNoise(p.x*11+seed,p.y*11+p.z*5);
            float phase=clump*Mathf.PI*2;
            float length=(rabbit?.012f:bear?.055f:.027f)*( .7f+R()*.65f );
            if(part=="Head" && p.z<-.28f) length*=.55f; // Keep eyes and muzzle readable.
            if(part=="Belly patch") length*=.6f;
            float width=(rabbit?.0017f:bear?.0031f:.0021f)*(.8f+R()*.5f);
            Vector3 lay=tangent*Mathf.Cos(phase)+bitangent*Mathf.Sin(phase);
            float tint=.94f+clump*.08f+(R()-.5f)*.06f;
            int start=v.Count;
            Vector3 end=p;
            for(int ring=0;ring<rings;ring++)
            {
                float t=(float)ring/(rings-1);
                float curl=bear?Mathf.Sin(t*Mathf.PI*2.1f+phase)-Mathf.Sin(phase):Mathf.Sin(t*Mathf.PI);
                Vector3 center=p+n*(length*(t-.18f*t*t)-.001f)
                    +lay*length*t*t*.85f+tangent*curl*length*(bear?.33f:.10f)*t;
                end=center;
                for(int side=0;side<3;side++)
                {
                    float angle=side*Mathf.PI*2/3;
                    Vector3 radial=tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle);
                    v.Add(center+radial*width*(1-t*.88f));
                    normals.Add((n+radial*.22f+lay*t*.12f).normalized);
                    colors.Add(baseColor*tint*(.87f+t*.13f));
                }
                if(ring==0) continue;
                for(int side=0;side<3;side++)
                {
                    int next=(side+1)%3, prev=start+(ring-1)*3, current=start+ring*3;
                    indices.Add(prev+side); indices.Add(prev+next); indices.Add(current+side);
                    indices.Add(prev+next); indices.Add(current+next); indices.Add(current+side);
                }
            }
            int tip=v.Count; v.Add(end+n*.001f); normals.Add(n); colors.Add(baseColor*tint);
            for(int side=0;side<3;side++)
            { indices.Add(start+(rings-1)*3+side); indices.Add(start+(rings-1)*3+(side+1)%3); indices.Add(tip); }

        }
        var mesh=new Mesh {name=species+" groomed plush fibers",indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(v); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetTriangles(indices,0); mesh.RecalculateBounds();
        return mesh;
    }
}
