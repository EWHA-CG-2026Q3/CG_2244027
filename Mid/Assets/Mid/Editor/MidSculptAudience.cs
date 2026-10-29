using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MidSculptAudience
{
    const string ScenePath="Assets/Mid/Scenes/Mid_ThreeSculptAudience.unity";
    [MenuItem("Mid/Build sculpted Chiikawa and Usagi audience")]
    public static void Build()
    {
        var chiikawa=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Chiikawa_Sculpt.prefab");
        var usagi=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Usagi_Sculpt.prefab");
        var hachiware=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mid/Prefabs/Hachiware_Sculpt.prefab");
        if(!chiikawa||!usagi||!hachiware)throw new Exception("Build the two character prefabs first.");
        EditorSceneManager.OpenScene("Assets/Mid/Scenes/Mid_PlushAudience.unity");
        var controller=UnityEngine.Object.FindAnyObjectByType<MidBallTracking>();
        foreach(var spectator in controller.Spectators)UnityEngine.Object.DestroyImmediate(spectator.Neck.parent.gameObject);
        var viewers=new List<MidBallTracking.Spectator>();
        float[] speeds={3.2f,6.3f,4.2f,5.5f,2.7f,4.8f,3.7f,6f,4.5f};
        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
        {
            int i=row*3+col;int kind=(row+col)%3;bool rabbit=kind==1;
            var model=(GameObject)PrefabUtility.InstantiatePrefab(kind==0?chiikawa:kind==1?usagi:hachiware);
            model.name=(kind==0?"Chiikawa":kind==1?"Usagi":"Hachiware")+" spectator "+(i+1);
            model.transform.localScale=Vector3.one*.66f;
            model.transform.position=new Vector3((col-1)*2.15f+(row%2)*.18f,row*1.05f+(rabbit?.65f:.68f),row*1.85f-.54f);
            // Keep the original short feet attached to the torso; no hanging-leg offset.
            var neck=model.transform.Find("NeckPivot");
            viewers.Add(new MidBallTracking.Spectator{Neck=neck,FollowSpeed=speeds[i]});
            if(neck.Find("Head").GetComponent<MeshFilter>().sharedMesh.vertexCount<6000)throw new Exception("Custom head missing");
        }
        controller.Spectators=viewers.ToArray();controller.Neck=viewers[0].Neck;
        controller.MaxYaw=35;controller.MaxUpPitch=22;controller.MaxDownPitch=20;
        controller.DisplayTitle="CHIIKAWA / USAGI / HACHIWARE";
        controller.DisplaySubtitle="Nine little spectators. Follow the ball.";
        MidStarTarget.Replace(controller);
        controller.ResetPose();
        MidPrototypeBuilder.Verify();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if(!scenes.Exists(s=>s.path==ScenePath)){scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();}
        AssetDatabase.SaveAssets();Preview();
        Debug.Log("SCULPT_AUDIENCE_OK: 3 Chiikawa, 3 Usagi, 3 Hachiware, linked prefabs, nine independent head followers.");
    }
    static void Preview()
    {
        var c=UnityEngine.Object.FindAnyObjectByType<MidBallTracking>();
        c.SetWorldTarget(new Vector3(1.8f,3.8f,-3));for(int i=0;i<120;i++)c.Step(1f/60);
        var rt=new RenderTexture(1280,800,24){antiAliasing=4};var old=RenderTexture.active;
        c.ViewCamera.targetTexture=rt;c.ViewCamera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
        File.WriteAllBytes("Previews/three_sculpt_audience.png",image.EncodeToPNG());
        c.ViewCamera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);c.ResetPose();
    }
    public static void BuildAndRecord(){MidChiikawaSculpt.Build();MidUsagiSculpt.Build();MidHachiwareSculpt.Build();Build();MidVideoCapture.RecordTo("Recordings/ThreeSculptAudienceFrames");}
    public static void ApplyStarAndRecord(){Build();MidVideoCapture.RecordTo("Recordings/StarAudienceFrames");}
    public static void OpenDemo(){EditorSceneManager.OpenScene(ScenePath);EditorApplication.delayCall+=()=>{EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;};}
}
