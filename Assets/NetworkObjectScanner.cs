using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class NetworkObjectScanner : MonoBehaviour
{
    [Header("Scan Settings")]
    public bool scanOnStart = true;
    public bool findProblematicHash = true;
    
    private void Start()
    {
        if (scanOnStart)
        {
            ScanAllNetworkObjects();
        }
    }
    
    [ContextMenu("Scan All Network Objects")]
    public void ScanAllNetworkObjects()
    {
        Debug.Log("🕵️ NETWORK OBJECT SCANNER: Starting comprehensive scan...");
        
        // Scan current scene for NetworkObjects
        ScanSceneNetworkObjects();
        
        // Check NetworkManager registered prefabs
        ScanRegisteredPrefabs();
        
        // Look for specific problematic objects
        if (findProblematicHash)
        {
            FindProblematicObjects();
        }
    }
    
    private void ScanSceneNetworkObjects()
    {
        Debug.Log("🏠 Scanning current scene for NetworkObjects...");
        
        // Use the non-deprecated method
        NetworkObject[] sceneObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"   Found {sceneObjects.Length} NetworkObjects in scene:");
        
        foreach (NetworkObject netObj in sceneObjects)
        {
            if (netObj != null)
            {
                bool isSceneObject = netObj.IsSceneObject != null ? netObj.IsSceneObject.Value : false;
                bool isSpawned = netObj.IsSpawned;
                
                string prefabStatus = isSceneObject ? "Scene Object" : "Prefab Instance";
                string spawnedStatus = isSpawned ? "Spawned" : "Not Spawned";
                
                // Get the prefab hash using our method
                ulong prefabHash = GetPrefabHash(netObj.gameObject);
                
                Debug.Log($"   📍 {netObj.name} (InstanceID: {prefabHash}) - {prefabStatus}, {spawnedStatus}");
                
                // Check parent hierarchy
                Transform current = netObj.transform;
                string hierarchy = current.name;
                while (current.parent != null)
                {
                    current = current.parent;
                    hierarchy = current.name + "/" + hierarchy;
                }
                Debug.Log($"        Hierarchy: {hierarchy}");
                Debug.Log($"        Scene: {netObj.gameObject.scene.name}");
                
                // Check components
                Component[] components = netObj.GetComponents<Component>();
                Debug.Log($"        Components: {components.Length}");
                foreach (Component comp in components)
                {
                    if (comp != null)
                    {
                        Debug.Log($"          - {comp.GetType().Name}");
                    }
                }
            }
        }
    }
    
    private void ScanRegisteredPrefabs()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("   NetworkManager not available for prefab scanning");
            return;
        }
        
        Debug.Log("📋 Scanning NetworkManager registered prefabs...");
        
        var registeredPrefabs = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;
        Debug.Log($"   Found {registeredPrefabs.Count} registered prefabs:");
        
        foreach (var netPrefab in registeredPrefabs)
        {
            if (netPrefab != null && netPrefab.Prefab != null)
            {
                NetworkObject netObj = netPrefab.Prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    ulong prefabHash = GetPrefabHash(netPrefab.Prefab);
                    Debug.Log($"   ✅ {netPrefab.Prefab.name} (InstanceID: {prefabHash})");
                }
                else
                {
                    Debug.LogError($"   ❌ {netPrefab.Prefab.name} - REGISTERED BUT MISSING NETWORKOBJECT!");
                }
            }
            else
            {
                Debug.LogError($"   ❌ NULL prefab in registered list!");
            }
        }
    }
    
    private void FindProblematicObjects()
    {
        Debug.Log("🔍 Searching for problematic objects that might cause spawn failures...");
        
        // Check for objects that might be trying to spawn but aren't registered
        ScanForUnregisteredNetworkObjects();
        
        // Check for NetworkObjects in the scene that shouldn't be there
        ScanForSceneNetworkObjects();
    }
    
    private void ScanForUnregisteredNetworkObjects()
    {
        Debug.Log("🔍 Checking for unregistered NetworkObjects...");
        
        if (NetworkManager.Singleton == null) return;
        
        // Get all prefab assets that might have NetworkObject components
        var registeredPrefabs = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;
        
        // Check scene objects against registered prefabs
        NetworkObject[] sceneObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (NetworkObject sceneObj in sceneObjects)
        {
            if (sceneObj == null) continue;
            
            // Check if this scene object's prefab is registered
            bool isRegistered = false;
            ulong sceneObjHash = GetPrefabHash(sceneObj.gameObject);
            
            foreach (var registeredPrefab in registeredPrefabs)
            {
                if (registeredPrefab?.Prefab != null)
                {
                    ulong registeredHash = GetPrefabHash(registeredPrefab.Prefab);
                    if (sceneObjHash == registeredHash)
                    {
                        isRegistered = true;
                        break;
                    }
                }
            }
            
            bool isSceneObject = sceneObj.IsSceneObject != null ? sceneObj.IsSceneObject.Value : false;
            
            if (!isRegistered && !isSceneObject)
            {
                Debug.LogError($"❌ UNREGISTERED PREFAB INSTANCE: {sceneObj.name} (InstanceID: {sceneObjHash})");
                Debug.LogError($"   This object is a prefab instance but its prefab is not registered with NetworkManager!");
            }
        }
    }
    
    private void ScanForSceneNetworkObjects()
    {
        Debug.Log("🔍 Checking for scene NetworkObjects that might cause issues...");
        
        NetworkObject[] sceneObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (NetworkObject sceneObj in sceneObjects)
        {
            if (sceneObj == null) continue;
            
            // Look for objects that are scene objects but might be problematic
            bool isSceneObject = sceneObj.IsSceneObject != null ? sceneObj.IsSceneObject.Value : false;
            
            if (isSceneObject)
            {
                // Check if this object has any NetworkBehaviour components
                NetworkBehaviour[] behaviours = sceneObj.GetComponents<NetworkBehaviour>();
                if (behaviours.Length > 0)
                {
                    Debug.Log($"⚠️ SCENE OBJECT WITH NETWORKBEHAVIOURS: {sceneObj.name}");
                    Debug.Log($"   Location: {GetFullPath(sceneObj.transform)}");
                    Debug.Log($"   NetworkBehaviours: {behaviours.Length}");
                    
                    foreach (NetworkBehaviour behaviour in behaviours)
                    {
                        if (behaviour != null)
                        {
                            Debug.Log($"     - {behaviour.GetType().Name}");
                        }
                    }
                }
            }
        }
    }
    
    private ulong GetPrefabHash(GameObject prefab)
    {
        // Use the instance ID as a unique identifier
        if (prefab == null) return 0;
        return (ulong)prefab.GetInstanceID();
    }
    
    private string GetFullPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
    
    [ContextMenu("Check All Scenes for NetworkObjects")]
    public void CheckAllScenes()
    {
        Debug.Log("🌍 Checking all loaded scenes for NetworkObjects...");
        
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                Debug.Log($"🔍 Scanning scene: {scene.name}");
                
                GameObject[] rootObjects = scene.GetRootGameObjects();
                int networkObjectCount = 0;
                
                foreach (GameObject rootObj in rootObjects)
                {
                    NetworkObject[] netObjs = rootObj.GetComponentsInChildren<NetworkObject>(true);
                    networkObjectCount += netObjs.Length;
                    
                    foreach (NetworkObject netObj in netObjs)
                    {
                        if (netObj != null)
                        {
                            Debug.Log($"   📍 {netObj.name} in {scene.name}");
                        }
                    }
                }
                
                Debug.Log($"   Total NetworkObjects in {scene.name}: {networkObjectCount}");
            }
        }
    }
    
    [ContextMenu("Debug Network Spawn Issues")]
    public void DebugSpawnIssues()
    {
        Debug.Log("🐛 DEBUGGING NETWORK SPAWN ISSUES...");
        
        // Check common issues:
        
        // 1. NetworkManager status
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager.Singleton is null!");
        }
        else
        {
            Debug.Log($"✅ NetworkManager exists - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}");
        }
        
        // 2. Registered prefab count
        if (NetworkManager.Singleton != null)
        {
            int prefabCount = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.Count;
            Debug.Log($"📋 Registered prefabs: {prefabCount}");
            
            if (prefabCount == 0)
            {
                Debug.LogError("❌ No prefabs registered with NetworkManager!");
            }
        }
        
        // 3. Scene objects with NetworkObject components
        NetworkObject[] sceneObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"🏠 Scene NetworkObjects: {sceneObjects.Length}");
        
        foreach (NetworkObject obj in sceneObjects)
        {
            if (obj != null && !obj.IsSpawned)
            {
                Debug.Log($"⚠️ Unspawned NetworkObject: {obj.name} at {GetFullPath(obj.transform)}");
            }
        }
        
        // 4. Check for the specific error pattern
        Debug.Log("🔍 Looking for objects that might cause 'Failed to create object locally' errors...");
        CheckForCommonSpawnIssues();
    }
    
    private void CheckForCommonSpawnIssues()
    {
        // Common causes of spawn failures:
        
        // 1. Objects with NetworkObject but no NetworkManager registration
        NetworkObject[] allObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (NetworkObject obj in allObjects)
        {
            if (obj == null) continue;
            
            // Check if this is a prefab instance that's not registered
            bool isSceneObject = obj.IsSceneObject != null ? obj.IsSceneObject.Value : false;
            
            if (!isSceneObject)
            {
                // This is a prefab instance - check if the prefab is registered
                GameObject prefabRoot = FindPrefabRoot(obj.gameObject);
                if (prefabRoot != null)
                {
                    bool isRegistered = IsPrefabRegistered(prefabRoot);
                    if (!isRegistered)
                    {
                        Debug.LogError($"❌ SPAWN ISSUE: Prefab instance '{obj.name}' has unregistered prefab '{prefabRoot.name}'");
                        Debug.LogError($"   InstanceID: {prefabRoot.GetInstanceID()}");
                    }
                }
            }
        }
        
        // 2. Objects that are disabled but have NetworkObject components
        foreach (NetworkObject obj in allObjects)
        {
            if (obj != null && !obj.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"⚠️ Disabled NetworkObject: {obj.name} - this might cause issues if it tries to spawn");
            }
        }
    }
    
    private GameObject FindPrefabRoot(GameObject obj)
    {
        // Try to find the original prefab root
        if (obj == null) return null;
        
        // In a proper setup, you'd use PrefabUtility, but for runtime we'll use a simpler approach
        // Look for the root object in the hierarchy that has the NetworkObject
        Transform current = obj.transform;
        while (current.parent != null)
        {
            NetworkObject parentNetObj = current.parent.GetComponent<NetworkObject>();
            if (parentNetObj != null)
            {
                current = current.parent;
            }
            else
            {
                break;
            }
        }
        
        return current.gameObject;
    }
    
    private bool IsPrefabRegistered(GameObject prefab)
    {
        if (NetworkManager.Singleton == null || prefab == null) return false;
        
        var registeredPrefabs = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;
        foreach (var registeredPrefab in registeredPrefabs)
        {
            if (registeredPrefab?.Prefab != null && registeredPrefab.Prefab == prefab)
            {
                return true;
            }
        }
        
        return false;
    }
    
    [ContextMenu("List All NetworkBehaviours")]
    public void ListAllNetworkBehaviours()
    {
        Debug.Log("📋 Listing all NetworkBehaviours in scene...");
        
        NetworkBehaviour[] behaviours = FindObjectsByType<NetworkBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"Found {behaviours.Length} NetworkBehaviours:");
        
        foreach (NetworkBehaviour behaviour in behaviours)
        {
            if (behaviour != null)
            {
                NetworkObject netObj = behaviour.NetworkObject;
                if (netObj != null)
                {
                    Debug.Log($"   {behaviour.GetType().Name} on {netObj.name}");
                }
                else
                {
                    Debug.LogWarning($"   {behaviour.GetType().Name} has no NetworkObject!");
                }
            }
        }
    }
}