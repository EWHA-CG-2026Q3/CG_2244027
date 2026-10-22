#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S02_TableSetup
{
    private const string PrefabPath = "Assets/S02/Prefabs/S02_Table.prefab";
    private const string ScenePath = "Assets/S02/Scenes/S02_Table_Final.unity";

    [MenuItem("S02/Create my table")]
    public static void CreateMyTable()
    {
        if (!AssetDatabase.IsValidFolder("Assets/S02/Prefabs"))
            AssetDatabase.CreateFolder("Assets/S02", "Prefabs");

        GameObject table = new GameObject("S02_MyTable");
        AddCube(table.transform, "Tabletop", new Vector3(0f, 0.76f, 0f), new Vector3(2f, 0.12f, 1.25f));
        AddCube(table.transform, "Leg_FrontLeft", new Vector3(-0.85f, 0.35f, -0.48f), new Vector3(0.13f, 0.7f, 0.13f));
        AddCube(table.transform, "Leg_FrontRight", new Vector3(0.85f, 0.35f, -0.48f), new Vector3(0.13f, 0.7f, 0.13f));
        AddCube(table.transform, "Leg_BackLeft", new Vector3(-0.85f, 0.35f, 0.48f), new Vector3(0.13f, 0.7f, 0.13f));
        AddCube(table.transform, "Leg_BackRight", new Vector3(0.85f, 0.35f, 0.48f), new Vector3(0.13f, 0.7f, 0.13f));

        PrefabUtility.SaveAsPrefabAsset(table, PrefabPath);
        Object.DestroyImmediate(table);

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        instance.transform.position = Vector3.zero;
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(3f, 2.1f, -3.2f);
        camera.transform.LookAt(new Vector3(0f, 0.42f, 0f));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("S02 table prefab and scene created: " + PrefabPath + ", " + ScenePath);
    }

    private static void AddCube(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
    }
}
#endif
