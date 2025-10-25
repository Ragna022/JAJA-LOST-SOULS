using UnityEditor;
using UnityEngine;
using Unity.Netcode;

public class NetworkHashFinder : EditorWindow
{
    [MenuItem("Tools/Network Hash Finder")]
    public static void ShowWindow()
    {
        GetWindow<NetworkHashFinder>("Network Hash Finder");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Log All NetworkObject Hashes"))
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var netObj = prefab?.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                   Debug.Log($"Prefab: {prefab.name}, GUID: {guid}");
                }
            }
        }
    }
}