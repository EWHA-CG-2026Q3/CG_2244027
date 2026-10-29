using UnityEngine;

public sealed class MidExpression : MonoBehaviour
{
    public enum Mood { Default, Happy, Surprised }
    public Mood Expression;
    public GameObject[] Faces;
    public void Apply(){
        if(Faces==null)return;
        int selected=(int)Expression;
        if(selected>=Faces.Length || !Faces[selected]){
            for(int i=0;i<Faces.Length;i++)if(Faces[i]){selected=i;Expression=(Mood)i;break;}
        }
        for(int i=0;i<Faces.Length;i++)if(Faces[i])Faces[i].SetActive(i==selected);
    }
    void OnEnable()=>Apply();
    void OnValidate()=>Apply();
}
