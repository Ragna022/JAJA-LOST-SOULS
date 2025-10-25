using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;
    
    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string lobbyScene = "LobbyScene"; 
    public string gameScene = "GameScene";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void LoadMainMenu()
    {
        // Shutdown network if active
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        SceneManager.LoadScene(mainMenuScene);
    }
    
    public void LoadLobby()
    {
        SceneManager.LoadScene(lobbyScene);
    }
    
    public void LoadGameScene()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(gameScene);
        }
    }
}