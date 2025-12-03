using UnityEngine;

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
            DontDestroyOnLoad(gameObject); // Persist across scenes like PlayerInputManager
            Debug.Log("MobileInputManager: Instance created successfully");
        }
        else
        {
            Debug.LogWarning("MobileInputManager: Duplicate instance detected, destroying");
            Destroy(gameObject);
            return;
        }

        // Auto-detect mobile platform (but can be overridden in inspector)
        #if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) // Only auto-enable on actual mobile devices
            {
                useMobileControls = true;
            }
        #endif

        Debug.Log($"MobileInputManager: Mobile controls {(useMobileControls ? "ENABLED" : "DISABLED")}");
    }

    private void Start()
    {
        // Validate joystick references
        if (movementJoystick == null)
        {
            Debug.LogError("MobileInputManager: Movement Joystick is NOT assigned!");
        }
        else
        {
            Debug.Log($"MobileInputManager: Movement Joystick assigned: {movementJoystick.gameObject.name}");
        }

        if (cameraTouch == null)
        {
            Debug.LogWarning("MobileInputManager: Camera Touch is not assigned");
        }
        else
        {
            Debug.Log($"MobileInputManager: Camera Touch assigned: {cameraTouch.gameObject.name}");
        }

        // Subscribe to button events
        if (dodgeButton != null)
        {
            dodgeButton.OnButtonPressed += () => dodgePressed = true;
        }

        if (jumpButton != null)
        {
            jumpButton.OnButtonPressed += () => jumpPressed = true;
        }

        if (sprintButton != null)
        {
            sprintButton.OnButtonPressed += () => sprintHeld = true;
            sprintButton.OnButtonReleased += () => sprintHeld = false;
        }

        if (rbButton != null)
        {
            rbButton.OnButtonPressed += () => rbPressed = true;
        }

        if (rtButton != null)
        {
            rtButton.OnButtonPressed += () => rtPressed = true;
            rtButton.OnButtonPressed += () => rtHeld = true;
            rtButton.OnButtonReleased += () => rtHeld = false;
        }

        if (lockOnButton != null)
        {
            lockOnButton.OnButtonPressed += () => lockOnPressed = true;
        }

        // Apply initial visibility
        UpdateMobileControlsVisibility();
        
        // Tell PlayerInputManager to use mobile input
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.SetMobileInputMode(useMobileControls);
        }
    }

    private void OnValidate()
    {
        // This is called when you change values in the Inspector
        // Update visibility immediately when you toggle the checkbox
        if (Application.isPlaying)
        {
            UpdateMobileControlsVisibility();
            
            // Sync with PlayerInputManager
            if (PlayerInputManager.instance != null)
            {
                PlayerInputManager.instance.SetMobileInputMode(useMobileControls);
            }
        }
    }

    private void Update()
    {
        if (!useMobileControls)
            return;

        // Update joystick inputs continuously
        if (movementJoystick != null)
        {
            movementInput = movementJoystick.GetInputVector();
        }

        // Get camera input from touch
        if (cameraTouch != null)
        {
            cameraInput = cameraTouch.GetCameraInput();
        }
    }

    private void UpdateMobileControlsVisibility()
    {
        bool shouldShow = useMobileControls;
        
        Debug.Log($"MobileInputManager: Setting controls visibility to {shouldShow}");

        if (movementJoystick != null)
        {
            movementJoystick.gameObject.SetActive(shouldShow);
            Debug.Log($"Movement Joystick set to: {(shouldShow ? "ACTIVE" : "INACTIVE")}");
        }
        
        if (cameraTouch != null)
        {
            cameraTouch.gameObject.SetActive(shouldShow);
            Debug.Log($"Camera Touch set to: {(shouldShow ? "ACTIVE" : "INACTIVE")}");
        }

        if (dodgeButton != null)
            dodgeButton.gameObject.SetActive(shouldShow);
        
        if (jumpButton != null)
            jumpButton.gameObject.SetActive(shouldShow);
        
        if (sprintButton != null)
            sprintButton.gameObject.SetActive(shouldShow);
        
        if (rbButton != null)
            rbButton.gameObject.SetActive(shouldShow);
        
        if (rtButton != null)
            rtButton.gameObject.SetActive(shouldShow);
        
        if (lockOnButton != null)
            lockOnButton.gameObject.SetActive(shouldShow);
    }

    // Getters for button states
    public bool GetDodgeInput()
    {
        bool result = dodgePressed;
        dodgePressed = false;
        return result;
    }

    public bool GetJumpInput()
    {
        bool result = jumpPressed;
        jumpPressed = false;
        return result;
    }

    public bool GetSprintInput()
    {
        return sprintHeld;
    }

    public bool GetRBInput()
    {
        bool result = rbPressed;
        rbPressed = false;
        return result;
    }

    public bool GetRTInput()
    {
        bool result = rtPressed;
        rtPressed = false;
        return result;
    }

    public bool GetRTHoldInput()
    {
        return rtHeld;
    }

    public bool GetLockOnInput()
    {
        bool result = lockOnPressed;
        lockOnPressed = false;
        return result;
    }

    // Public method to toggle mobile controls at runtime
    public void SetMobileControls(bool enable)
    {
        useMobileControls = enable;
        UpdateMobileControlsVisibility();
        
        // Sync with PlayerInputManager
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.SetMobileInputMode(enable);
        }
        
        Debug.Log($"MobileInputManager: Mobile controls {(enable ? "ENABLED" : "DISABLED")}");
    }
}