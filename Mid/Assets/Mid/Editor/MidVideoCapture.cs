using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MidVideoCapture
{
    public static void BuildAndRecord()
    {
        MidPrototypeBuilder.Build();
        Record();
    }

    [MenuItem("Mid/Render 16 second felt demo frames")]
    public static void Record() { RecordTo("Recordings/FeltFrames"); }

    public static void RecordTo(string directory, float closeX=0f)
    {
        var c=Object.FindFirstObjectByType<MidBallTracking>();
        Directory.CreateDirectory(directory);
        var rt=new RenderTexture(1280,800,24) {antiAliasing=4};
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        c.ResetPose();
        Vector3 originalPosition=c.ViewCamera.transform.position;
        Quaternion originalRotation=c.ViewCamera.transform.rotation;
        float originalSize=c.ViewCamera.orthographicSize;
        for(int frame=0;frame<384;frame++)
        {
            float t=frame/24f;
            // Identical controller and limits to mouse interaction, driven automatically for this clip.
            c.SetWorldTarget(new Vector3(4f*Mathf.Sin(t*Mathf.PI/3),3.5f+2.7f*Mathf.Sin(t*Mathf.PI/2),-3f));
            if(t>11f) c.SetWorldTarget(new Vector3(2.8f*Mathf.Sin(t*Mathf.PI/3),2.1f+.8f*Mathf.Sin(t*Mathf.PI/2),-3f));
            c.Step(1f/24);
            float close=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-9)/2f));
            c.ViewCamera.transform.position=Vector3.Lerp(originalPosition,new Vector3(closeX,3.5f,-10),close);
            Quaternion closeRotation=Quaternion.LookRotation(new Vector3(closeX,1.55f,0)-new Vector3(closeX,3.5f,-10));
            c.ViewCamera.transform.rotation=Quaternion.Slerp(originalRotation,closeRotation,close);
            c.ViewCamera.orthographicSize=Mathf.Lerp(originalSize,2.65f,close);
            c.ViewCamera.targetTexture=rt; c.ViewCamera.Render(); RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
            File.WriteAllBytes(directory+"/"+frame.ToString("D4")+".png",image.EncodeToPNG());
        }
        c.ViewCamera.transform.position=originalPosition;
        c.ViewCamera.transform.rotation=originalRotation;
        c.ViewCamera.orthographicSize=originalSize;
        c.ViewCamera.targetTexture=null; RenderTexture.active=previous;
        Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt); c.ResetPose();
        Debug.Log("MID_VIDEO_FRAMES_OK: 384 frames, 24 fps, 16 seconds, automatic target demo.");
    }

    public static void OpenDemo()
    {
        EditorSceneManager.OpenScene("Assets/Mid/Scenes/Mid_PlushAudience.unity");
        EditorApplication.delayCall += () => {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying=true;
        };
    }
}
