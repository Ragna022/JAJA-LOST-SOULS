using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager instance;

    [Header("NETWORK JOIN")]
    [SerializeField] bool startGameAsClient;

    [Header("UI Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 1f;

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

        // Get CanvasGroup if not assigned
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // Ensure it starts at 0
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene loaded event to fade in UI when entering game world
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Fade in UI when entering the game world (not MainMenu or Lobby)
        if (scene.name == "World_01" || scene.name.Contains("World"))
        {
            StartCoroutine(FadeInUI());
        }
        // Keep UI hidden in menus and lobby
        else if (scene.name == "MainMenu" || scene.name == "Lobby")
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
    }

    private IEnumerator FadeInUI()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        Debug.Log("✅ PlayerUI faded in");
    }

    public void ShowUI()
    {
        StartCoroutine(FadeInUI());
    }

    public void HideUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
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