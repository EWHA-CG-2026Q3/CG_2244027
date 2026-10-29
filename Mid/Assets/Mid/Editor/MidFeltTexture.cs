using System.IO;
using UnityEditor;
using UnityEngine;

// Seamless, deterministic wool-fiber albedo and micro-normal textures.
public static class MidFeltTexture
{
    public static void Generate()
    {
        const int size=512;
        var random=new System.Random(418);
        var height=new float[size*size];
        for(int i=0;i<height.Length;i++) height[i]=.48f+(float)random.NextDouble()*.10f;
        for(int fiber=0;fiber<26000;fiber++)
        {
            float x=(float)random.NextDouble()*size, y=(float)random.NextDouble()*size;
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float length=3+(float)random.NextDouble()*13;
            float value=((float)random.NextDouble()-.38f)*.17f;
            for(int step=0;step<(int)length;step++)
            {
                int px=((int)(x+Mathf.Cos(angle)*step)+size*2)%size;
                int py=((int)(y+Mathf.Sin(angle)*step)+size*2)%size;
                height[py*size+px]+=value*Mathf.Sin((step+1)/(length+1)*Mathf.PI);
            }
        }
        var color=new Texture2D(size,size,TextureFormat.RGB24,false);
        var normal=new Texture2D(size,size,TextureFormat.RGB24,false);
        var colors=new Color[size*size]; var normals=new Color[size*size];
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            int i=y*size+x; float h=height[i];
            float value=Mathf.Clamp(.90f+(h-.5f)*.65f,.65f,1f);
            colors[i]=new Color(value,value,value);
            float dx=height[y*size+(x+1)%size]-height[y*size+(x+size-1)%size];
            float dy=height[((y+1)%size)*size+x]-height[((y+size-1)%size)*size+x];
            Vector3 n=new Vector3(-dx*1.4f,-dy*1.4f,1).normalized;
            normals[i]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f);
        }
        color.SetPixels(colors); color.Apply(); normal.SetPixels(normals); normal.Apply();
        const string root="Assets/Mid/Generated/";
        File.WriteAllBytes(root+"FeltFiber.png",color.EncodeToPNG());
        File.WriteAllBytes(root+"FeltNormal.png",normal.EncodeToPNG());
        Object.DestroyImmediate(color); Object.DestroyImmediate(normal);
        AssetDatabase.ImportAsset(root+"FeltFiber.png",ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(root+"FeltNormal.png",ImportAssetOptions.ForceSynchronousImport);
        foreach(string name in new[]{"FeltFiber.png","FeltNormal.png"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(root+name);
            importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=8;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            if(name=="FeltNormal.png") importer.textureType=TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }
}
