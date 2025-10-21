using Unity.Netcode;
using UnityEngine;

public class ConnectionApprovalHandler : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
            Debug.Log("✅ ConnectionApprovalHandler registered.");
        }
    }

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Approve all connections
        response.Approved = true;

        // DO NOT create a player object automatically.
        // The LobbyManager will handle spawning players.
        response.CreatePlayerObject = false;

        // You could add other logic here (e.g., password check, max players check)
        
        Debug.Log($"Connection approved for {request.ClientNetworkId}, player object creation set to false.");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
        }
    }
}