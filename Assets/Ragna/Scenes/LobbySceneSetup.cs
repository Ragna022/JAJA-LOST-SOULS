using UnityEngine;
using Unity.Netcode;

public class LobbySceneSetup : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("🎮 LobbyScene loaded!");
        
        // Check network status
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    Debug.Log("✅ HOST detected in lobby scene - Ready for players!");
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("✅ CLIENT detected in lobby scene - Connected to host!");
                }
                else if (NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("✅ HOST (Server+Client) detected in lobby scene!");
                }
                
                // Check if LobbyManager exists and is spawned
                LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
                if (lobbyManager != null)
                {
                    Debug.Log("✅ LobbyManager found in scene");
                    
                    // Check if it's spawned on network
                    NetworkObject lobbyNetObj = lobbyManager.GetComponent<NetworkObject>();
                    if (lobbyNetObj != null)
                    {
                        if (lobbyNetObj.IsSpawned)
                        {
                            Debug.Log("✅ LobbyManager is spawned on network");
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ LobbyManager exists but not spawned on network");
                        }
                    }
                }
                else
                {
                    Debug.LogError("❌ LobbyManager not found in scene!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ NetworkManager exists but not listening - Did host start fail?");
                Debug.Log("💡 Press H to start host manually, or C to start client");
            }
        }
        else
        {
            Debug.LogError("❌ NetworkManager not found in scene!");
        }
    }
}