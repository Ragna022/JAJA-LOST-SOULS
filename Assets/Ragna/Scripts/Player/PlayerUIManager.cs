using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager instance;

    [Header("NETWORK JOIN")]
    [SerializeField] bool startGameAsClient;

    [HideInInspector] public PlayerUIHudManager playerUIHudManager;
    [HideInInspector] public PlayerUIPopUpManager playerUIPopUpManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        playerUIHudManager = GetComponentInChildren<PlayerUIHudManager>();
        playerUIPopUpManager = GetComponentInChildren<PlayerUIPopUpManager>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (startGameAsClient)
        {
            startGameAsClient = false;
            StartCoroutine(RestartAsClient());
        }
    }

    private IEnumerator RestartAsClient()
    {
        // Show loading screen
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowWithFakeProgress("Connecting to game...");
        }

        // WE MUST FIRST SHUTDOWN, BECAUSE WE HAVE STARTED AS A HOST DURING THE TITLE SCREEN
        NetworkManager.Singleton.Shutdown();
        
        // Wait a frame for shutdown to complete
        yield return new WaitForSeconds(0.5f);
        
        // Update loading text
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.UpdateLoadingText("Joining as client...");
        }
        
        // WE THEN RESTART AS A CLIENT
        NetworkManager.Singleton.StartClient();
        
        // Wait for connection
        yield return new WaitForSeconds(1f);
        
        // Hide loading screen
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.Complete();
        }
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadMainMenuWithLoadingScreen());
    }

    private IEnumerator LoadMainMenuWithLoadingScreen()
    {
        // Show loading screen
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowWithFakeProgress("Quitting Game...");
        }

        yield return new WaitForSeconds(0.3f);

        // Shutdown network if active
        if (NetworkManager.Singleton != null)
        {
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.UpdateLoadingText("Disconnecting...");
            }
            
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }

        // Destroy TitleScreenManager if it exists (clean slate)
        if (TitleScreenManager.Instance != null)
        {
            Destroy(TitleScreenManager.Instance.gameObject);
        }

        // Clear lobby data
        LobbyManager.PublicPersistentLobbyData = null;

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.UpdateLoadingText("Returning to Main Menu...");
        }

        // Start loading the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        
        if (asyncLoad != null)
        {
            // Update progress bar as scene loads
            while (!asyncLoad.isDone)
            {
                if (LoadingScreenManager.Instance != null)
                {
                    // Map the 0-0.9 range to our loading bar (scene loading is 0-0.9, last 0.1 is activation)
                    float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                    LoadingScreenManager.Instance.SetProgress(progress);
                }
                yield return null;
            }
        }
        else
        {
            // Fallback if async load fails
            SceneManager.LoadScene("MainMenu");
        }

        // Complete loading screen - just hide it directly since we're in a new scene
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.Hide();
        }

        Debug.Log("🔄 Returned to Main Menu - Fresh start");
    }
}