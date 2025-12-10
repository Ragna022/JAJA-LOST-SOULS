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

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "World_01" || scene.name.Contains("World"))
        {
            StartCoroutine(FadeInUI());
        }
        else if (scene.name == "MainMenu" || scene.name == "Lobby")
        {
            // Ensure UI stays hidden if we load into menu
            HideUI();
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
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowWithFakeProgress("Connecting to game...");
        }

        NetworkManager.Singleton.Shutdown();
        yield return new WaitForSeconds(0.5f);
        
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.UpdateLoadingText("Joining as client...");
        }
        
        NetworkManager.Singleton.StartClient();
        yield return new WaitForSeconds(1f);
        
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
        // 1. INSTANTLY HIDE THE HUD UI
        HideUI(); 

        // Show loading screen
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowWithFakeProgress("Quitting Game...");
        }

        yield return new WaitForSeconds(0.3f);

        // --- PREVENT RACE CONDITION ---
        // Destroy TitleScreenManager FIRST so it doesn't panic when we disconnect
        if (TitleScreenManager.Instance != null)
        {
            Destroy(TitleScreenManager.Instance.gameObject);
        }

        // --- FIX: Access the static list through the Class Name "LobbyManager" ---
        if (LobbyManager.PublicPersistentLobbyData != null)
        {
             LobbyManager.PublicPersistentLobbyData = null; 
        }

        if (NetworkManager.Singleton != null)
        {
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.UpdateLoadingText("Disconnecting...");
            }
            
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.UpdateLoadingText("Returning to Main Menu...");
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(0);
        
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                if (LoadingScreenManager.Instance != null)
                {
                    float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                    LoadingScreenManager.Instance.SetProgress(progress);
                }
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(0);
        }

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.Hide();
        }

        Debug.Log("🔄 Returned to Main Menu - Fresh start");
    }
}