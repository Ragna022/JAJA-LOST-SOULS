using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;

    [Header("Mobile Controls Toggle")]
    [Tooltip("Enable this to use mobile controls (automatically enabled on Android/iOS)")]
    public bool useMobileControls = false;
    
    [Header("Joysticks")]
    public MobileJoystick movementJoystick;
    
    [Header("Camera Touch Control")]
    public MobileCameraTouch cameraTouch;

    [Header("Action Buttons")]
    public MobileButton dodgeButton;
    public MobileButton jumpButton;
    public MobileButton sprintButton;
    public MobileButton rbButton;
    public MobileButton rtButton;
    public MobileButton lockOnButton;

    [Header("Mobile Input Values - Read Only")]
    public Vector2 movementInput;
    public Vector2 cameraInput;
    
    private bool dodgePressed;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool rbPressed;
    private bool rtPressed;
    private bool rtHeld;
    private bool lockOnPressed;

    private void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("MobileInputManager: Instance created successfully");
        }
        else
        {
            Debug.LogWarning("MobileInputManager: Duplicate instance detected, destroying");
            Destroy(gameObject);
            return;
        }

        #if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor)
            {
                useMobileControls = true;
            }
        #endif

        Debug.Log($"MobileInputManager: Mobile controls {(useMobileControls ? "ENABLED" : "DISABLED")}");
    }

    private void Start()
    {
        if (movementJoystick == null) Debug.LogError("MobileInputManager: Movement Joystick is NOT assigned!");
        if (cameraTouch == null) Debug.LogWarning("MobileInputManager: Camera Touch is not assigned");

        if (dodgeButton != null) dodgeButton.OnButtonPressed += () => dodgePressed = true;
        if (jumpButton != null) jumpButton.OnButtonPressed += () => jumpPressed = true;
        
        if (sprintButton != null)
        {
            sprintButton.OnButtonPressed += () => sprintHeld = true;
            sprintButton.OnButtonReleased += () => sprintHeld = false;
        }

        if (rbButton != null) rbButton.OnButtonPressed += () => rbPressed = true;
        
        if (rtButton != null)
        {
            rtButton.OnButtonPressed += () => rtPressed = true;
            rtButton.OnButtonPressed += () => rtHeld = true;
            rtButton.OnButtonReleased += () => rtHeld = false;
        }

        if (lockOnButton != null) lockOnButton.OnButtonPressed += () => lockOnPressed = true;

        UpdateMobileControlsVisibility();
        
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.SetMobileInputMode(useMobileControls);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateMobileControlsVisibility();
            if (PlayerInputManager.instance != null)
            {
                PlayerInputManager.instance.SetMobileInputMode(useMobileControls);
            }
        }
    }

    private void Update()
    {
        if (!useMobileControls) return;

        if (movementJoystick != null) movementInput = movementJoystick.GetInputVector();
        if (cameraTouch != null) cameraInput = cameraTouch.GetCameraInput();
    }

    private void UpdateMobileControlsVisibility()
    {
        bool shouldShow = useMobileControls;
        
        if (movementJoystick != null) movementJoystick.gameObject.SetActive(shouldShow);
        if (cameraTouch != null) cameraTouch.gameObject.SetActive(shouldShow);
        if (dodgeButton != null) dodgeButton.gameObject.SetActive(shouldShow);
        if (jumpButton != null) jumpButton.gameObject.SetActive(shouldShow);
        if (sprintButton != null) sprintButton.gameObject.SetActive(shouldShow);
        if (rbButton != null) rbButton.gameObject.SetActive(shouldShow);
        if (rtButton != null) rtButton.gameObject.SetActive(shouldShow);
        if (lockOnButton != null) lockOnButton.gameObject.SetActive(shouldShow);
    }

    public bool GetDodgeInput() { bool r = dodgePressed; dodgePressed = false; return r; }
    public bool GetJumpInput() { bool r = jumpPressed; jumpPressed = false; return r; }
    public bool GetSprintInput() => sprintHeld;
    public bool GetRBInput() { bool r = rbPressed; rbPressed = false; return r; }
    public bool GetRTInput() { bool r = rtPressed; rtPressed = false; return r; }
    public bool GetRTHoldInput() => rtHeld;
    public bool GetLockOnInput() { bool r = lockOnPressed; lockOnPressed = false; return r; }

    public void SetMobileControls(bool enable)
    {
        useMobileControls = enable;
        UpdateMobileControlsVisibility();
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.SetMobileInputMode(enable);
        }
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadMainMenuWithLoadingScreen());
    }

    private IEnumerator LoadMainMenuWithLoadingScreen()
    {
        // --- FIX: HIDE PLAYER UI INSTANTLY ---
        // This sets the CanvasGroup alpha to 0 so the HUD disappears immediately
        if (PlayerUIManager.instance != null)
        {
            PlayerUIManager.instance.HideUI();
        }
        // -------------------------------------

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowWithFakeProgress("Quitting Game...");
        }

        yield return new WaitForSeconds(0.3f);

        // Destroy managers to prevent errors
        if (TitleScreenManager.Instance != null)
        {
            Destroy(TitleScreenManager.Instance.gameObject);
        }

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