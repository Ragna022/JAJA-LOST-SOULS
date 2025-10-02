using Unity.Netcode;
using UnityEngine;

public class TitleScreenManager : MonoBehaviour
{
    public void StartNetworkAsHost()
    {
        // Code to start the network as host
        Debug.Log("Starting network as host...");

        NetworkManager.Singleton.StartHost();
    }

    public void StartNewGame()
    {
        StartCoroutine(WorldSaveGameManager.Instance.LoadNewGame());
    }
}
