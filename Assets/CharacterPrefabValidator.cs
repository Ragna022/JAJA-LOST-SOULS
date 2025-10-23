using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq; // Add this for LINQ support

public class CharacterPrefabValidator : MonoBehaviour
{
    [Header("Character Prefabs to Check")]
    public GameObject[] characterPrefabs;
    
    [Header("Validation Results")]
    public List<string> validPrefabs = new List<string>();
    public List<string> invalidPrefabs = new List<string>();

    private void Start()
    {
        ValidateAllCharacterPrefabs();
    }

    [ContextMenu("Validate Character Prefabs")]
    public void ValidateAllCharacterPrefabs()
    {
        validPrefabs.Clear();
        invalidPrefabs.Clear();
        
        Debug.Log("🔍 VALIDATING CHARACTER PREFABS FOR NETWORKING...");
        
        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogError("❌ No character prefabs assigned to validator!");
            return;
        }

        bool allValid = true;
        
        foreach (GameObject prefab in characterPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("❌ NULL prefab found in array!");
                continue;
            }

            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            PlayerManager playerManager = prefab.GetComponent<PlayerManager>();
            
            if (networkObject == null)
            {
                Debug.LogError($"❌ CHARACTER PREFAB MISSING NETWORKOBJECT: '{prefab.name}'");
                invalidPrefabs.Add(prefab.name);
                allValid = false;
                
                // Additional diagnostics
                if (playerManager != null)
                {
                    Debug.LogError($"   - Has PlayerManager but no NetworkObject!");
                }
            }
            else
            {
                Debug.Log($"✅ VALID: '{prefab.name}' has NetworkObject component");
                validPrefabs.Add(prefab.name);
                
                // Check if prefab is registered with NetworkManager
                CheckPrefabRegistration(prefab);
            }
        }

        if (allValid)
        {
            Debug.Log("🎉 ALL CHARACTER PREFABS ARE PROPERLY CONFIGURED!");
            RegisterAllPrefabsWithNetworkManager();
        }
        else
        {
            Debug.LogError($"🚨 {invalidPrefabs.Count} CHARACTER PREFABS NEED FIXING:");
            foreach (string invalidName in invalidPrefabs)
            {
                Debug.LogError($"   - {invalidName}");
            }
            Debug.LogError("💡 SOLUTION: Open each prefab in Unity Editor and add NetworkObject component to the root GameObject");
        }
    }

    private void CheckPrefabRegistration(GameObject prefab)
    {
        if (NetworkManager.Singleton == null) return;
        
        // FIXED: Use Any() instead of Exists
        bool isRegistered = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
            .Any(np => np.Prefab == prefab);
            
        if (!isRegistered)
        {
            Debug.LogWarning($"   - '{prefab.name}' is not registered with NetworkManager");
        }
        else
        {
            Debug.Log($"   - '{prefab.name}' is registered with NetworkManager");
        }
    }

    [ContextMenu("Register All Prefabs with NetworkManager")]
    public void RegisterAllPrefabsWithNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        int registeredCount = 0;
        
        foreach (GameObject prefab in characterPrefabs)
        {
            if (prefab == null) continue;
            
            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                // FIXED: Use Any() instead of Exists
                bool alreadyRegistered = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
                    .Any(np => np.Prefab == prefab);
                
                if (!alreadyRegistered)
                {
                    NetworkManager.Singleton.NetworkConfig.Prefabs.Add(new NetworkPrefab 
                    { 
                        Prefab = prefab 
                    });
                    registeredCount++;
                    Debug.Log($"✅ Registered: {prefab.name}");
                }
            }
        }
        
        Debug.Log($"📝 Registered {registeredCount} character prefabs with NetworkManager");
    }

    [ContextMenu("Print Fix Instructions")]
    public void PrintFixInstructions()
    {
        Debug.Log("🛠️ HOW TO FIX MISSING NETWORKOBJECT COMPONENTS:");
        Debug.Log("1. In Unity Editor, find your character prefabs (JAJA, etc.)");
        Debug.Log("2. Open each prefab that's listed as invalid above");
        Debug.Log("3. Select the ROOT GameObject of the prefab");
        Debug.Log("4. Click 'Add Component' in the Inspector");
        Debug.Log("5. Search for 'NetworkObject' and add it");
        Debug.Log("6. Save the prefab (Ctrl+S)");
        Debug.Log("7. Repeat for all invalid character prefabs");
        Debug.Log("8. Come back to this validator and click 'Validate Character Prefabs' again");
    }
}