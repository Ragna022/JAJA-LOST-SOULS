// PlayerSpawner.cs
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // We only want the server to execute this.
        if (!IsServer) return;

        // This callback is triggered for every client that connects, including the host.
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

        // Handle the host player who is already connected when this spawns.
        if (IsHost)
        {
            SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        SpawnPlayerForClient(clientId);
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        // Use the prefab selected in the TitleScreenManager.
        GameObject playerPrefab = TitleScreenManager.selectedPlayerPrefab;

        if (playerPrefab == null)
        {
            Debug.LogError("CRITICAL: No player prefab was selected before starting the game!");
            // You should have a default fallback prefab here.
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        // Spawn the object and give ownership to the client.
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        // Unsubscribe from the callback to prevent memory leaks.
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }
}