using Unity.Netcode;
using UnityEngine;

public class LobbySceneSetup : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("🎮 LobbyScene loaded!");

        // Check if we're a client connected to a host
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("✅ CLIENT detected in lobby scene - Connected to host!");
            
            // CRITICAL FIX: Find and ensure LobbyManager is properly networked
            EnsureLobbyManagerIsSpawned();
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("🎯 SERVER detected in lobby scene");
            
            // Server should spawn the LobbyManager
            EnsureLobbyManagerIsSpawned();
        }
        else
        {
            Debug.LogWarning("⚠️ Not connected to network in lobby scene");
        }

        ScanForProblematicNetworkObjects();
    }

    private void EnsureLobbyManagerIsSpawned()
    {
        Debug.Log("🔄 Ensuring LobbyManager is properly spawned on network...");
        
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        
        if (lobbyManager != null)
        {
            Debug.Log("✅ LobbyManager found in scene");
            
            NetworkObject lobbyNetObj = lobbyManager.GetComponent<NetworkObject>();
            
            if (lobbyNetObj != null)
            {
                if (!lobbyNetObj.IsSpawned)
                {
                    Debug.Log("⚠️ LobbyManager exists but not spawned on network - attempting to spawn...");
                    
                    // If we're the server, spawn it
                    if (NetworkManager.Singleton.IsServer)
                    {
                        Debug.Log("🎯 SERVER: Spawning LobbyManager on network...");
                        lobbyNetObj.Spawn();
                        Debug.Log("✅ LobbyManager spawned successfully by server!");
                    }
                    else
                    {
                        Debug.LogWarning("❌ CLIENT: Cannot spawn LobbyManager - only server can spawn network objects");
                        Debug.Log("💡 Waiting for server to spawn LobbyManager...");
                        
                        // Try again after a delay
                        Invoke(nameof(CheckLobbyManagerSpawnStatus), 2f);
                    }
                }
                else
                {
                    Debug.Log("✅ LobbyManager is already spawned on network!");
                }
            }
            else
            {
                Debug.LogError("❌ LobbyManager is missing NetworkObject component!");
            }
        }
        else
        {
            Debug.LogError("❌ LobbyManager not found in LobbyScene!");
        }
    }

    private void CheckLobbyManagerSpawnStatus()
    {
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            NetworkObject lobbyNetObj = lobbyManager.GetComponent<NetworkObject>();
            if (lobbyNetObj != null && !lobbyNetObj.IsSpawned)
            {
                Debug.LogError("💔 LobbyManager still not spawned after waiting!");
                Debug.Log("💡 The host needs to ensure LobbyManager is spawned when loading the scene");
            }
        }
    }

    public void ScanForProblematicNetworkObjects()
    {
        Debug.Log("🔍 SCANNING LOBBY SCENE FOR PROBLEMATIC NETWORKOBJECTS...");
        Debug.Log("==========================================================");

        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        Debug.Log($"📋 Found {networkObjects.Length} NetworkObjects in LobbyScene:");

        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj != null)
            {
                Debug.Log($"📍 {netObj.name}");
                Debug.Log($"   Path: {GetFullPath(netObj.transform)}");
                Debug.Log($"   InstanceID: {netObj.GetInstanceID()}");
                Debug.Log($"   IsSceneObject: {netObj.IsSceneObject}");
                Debug.Log($"   IsSpawned: {netObj.IsSpawned}");
                Debug.Log($"   OwnerClientId: {netObj.OwnerClientId}");
                
                if (netObj.name.Contains("LobbyManager"))
                {
                    Debug.Log("🎯 THIS IS THE LOBBYMANAGER - CHECKING SPAWN STATUS...");
                    
                    LobbyManager lobbyManager = netObj.GetComponent<LobbyManager>();
                    if (lobbyManager != null)
                    {
                        Debug.Log("✅ Has LobbyManager component");
                    }
                }
            }
        }

        Debug.Log("🔎 Looking for specific problematic object types...");
        ScanForUIWithNetworkObjects();
        ScanForPlayerRelatedObjects();
        ScanForDynamicallySpawnedObjects();
    }

    private void ScanForUIWithNetworkObjects()
    {
        Debug.Log("🎯 Scanning for UI elements with NetworkObject components...");
        
        // Your existing UI scanning code...
    }

    private void ScanForPlayerRelatedObjects()
    {
        Debug.Log("🎯 Scanning for player-related objects...");
        
        // Your existing player scanning code...
    }

    private void ScanForDynamicallySpawnedObjects()
    {
        Debug.Log("🎯 Scanning for dynamically spawned objects...");
        
        // Your existing dynamic objects scanning code...
    }

    private string GetFullPath(Transform transform)
    {
        if (transform == null) return "null";
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}