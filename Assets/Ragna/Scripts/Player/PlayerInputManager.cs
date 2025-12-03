using System;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager instance;
    public PlayerManager player;
    PlayerControls playerControls;

    [Header("Mobile Input")]
    [SerializeField] private bool useMobileInput = false; // Controlled by MobileInputManager

    [Header("Movement Input")]
    [SerializeField] Vector2 movementInput;
    public float horizontalInput;
    public float verticalInput;
    public float moveAmount;

    [Header("Camera Input")]
    [SerializeField] Vector2 cameraInput;
    public float cameraHorizontalInput;
    public float cameraVerticalInput;

    [Header("Lock On Input")]
    [SerializeField] bool lockOn_Input;
    [SerializeField] bool lockOn_Left_Input;
    [SerializeField] bool lockOn_Right_Input;
    private Coroutine lockOnCoroutine;

    [Header("Player Actions Input")]
    [SerializeField] bool dodgeInput = false;
    [SerializeField] bool sprintInput = false;
    [SerializeField] bool jumpInput = false;
    [SerializeField] bool RB_Input = false;

    [Header("TRIGGER INPUTS")]
    [SerializeField] bool RT_Input = false;
    [SerializeField] bool Hold_RT_Input = false;

    [Header("Debug Mobile Input")]
    [SerializeField] private Vector2 debugMobileMovement;
    [SerializeField] private Vector2 debugMobileCamera;

    private bool isReady = false;

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

        Debug.Log($"PlayerInputManager: Initialized");
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChange;
        instance.enabled = false;

        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }

    private void OnSceneChange(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
    {
        if (newScene.buildIndex == WorldSaveGameManager.instance.GetWorldSceneIndex())
        {
            instance.enabled = true;
            isReady = false;
            
            if (playerControls != null && !useMobileInput)
            {
                playerControls.Enable();
            }
            
            if (player == null)
            {
                FindPlayerInScene();
            }
        }
        else
        {
            instance.enabled = false;
            isReady = false;

            if (playerControls != null)
            {
                playerControls.Disable();
            }
        }
    }

    private void FindPlayerInScene()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager foundPlayer in players)
        {
            if (foundPlayer.IsOwner)
            {
                player = foundPlayer;
                isReady = true;
                Debug.Log($"PlayerInputManager: Found player {player.gameObject.name}");
                break;
            }
        }
        
        if (player == null)
        {
            Debug.LogWarning("PlayerInputManager: No player found in scene, will retry");
        }
    }

    public void SetPlayer(PlayerManager newPlayer)
    {
        player = newPlayer;
        isReady = true;
        Debug.Log($"PlayerInputManager: Player set to {player.gameObject.name}");
    }

    private void OnEnable()
    {
        if (playerControls == null && !useMobileInput)
        {
            playerControls = new PlayerControls();
            playerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
            playerControls.PlayerCamera.Movement.performed += i => cameraInput = i.ReadValue<Vector2>();
            playerControls.PlayerActions.Dodge.performed += i => dodgeInput = true;
            playerControls.PlayerActions.Jump.performed += i => jumpInput = true;
            playerControls.PlayerActions.RB.performed += i => RB_Input = true;

            playerControls.PlayerActions.LockOn.performed += i => lockOn_Input = true;
            playerControls.PlayerActions.SeekLeftLockOnTarget.performed += i => lockOn_Left_Input = true;
            playerControls.PlayerActions.SeekRightLockOnTarget.performed += i => lockOn_Right_Input = true;

            playerControls.PlayerActions.RT.performed += i => RT_Input = true;
            playerControls.PlayerActions.HoldRT.performed += i => Hold_RT_Input = true;
            playerControls.PlayerActions.HoldRT.canceled += i => Hold_RT_Input = false;

            playerControls.PlayerActions.Sprint.performed += i => sprintInput = true;
            playerControls.PlayerActions.Sprint.canceled += i => sprintInput = false;
        }

        if (playerControls != null && !useMobileInput)
        {
            playerControls.Enable();
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChange;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (useMobileInput)
            return;

        if (enabled)
        {
            if (playerControls != null)
                playerControls.Enable();
        }
        else
        {
            if (playerControls != null)
                playerControls.Disable();
        }
    }

    private void Update()
    {
        if (!isReady && player == null)
        {
            FindPlayerInScene();
            return;
        }
        
        HandleAllInput();
    }

    private void HandleAllInput()
    {
        if (!isReady || player == null)
            return;

        // Get input from mobile or gamepad/keyboard
        if (useMobileInput)
        {
            HandleMobileInput();
        }
        // If not using mobile input, the playerControls events will update movementInput/cameraInput automatically

        HandleLockOnInput();
        HandleLockOnSwitchTargetInput();
        HandlePlayerMovementInput();
        HandleCameraMovementInput();
        HandleDodgeInput();
        HandleSpringInput();
        HandleJumpInput();
        HandleRBInput();
        HandleRTInput();
        HandleChargeRTInput();
    }

    private void HandleMobileInput()
    {
        if (MobileInputManager.instance == null)
        {
            Debug.LogWarning("PlayerInputManager: MobileInputManager.instance is NULL!");
            return;
        }

        // Movement - DIRECTLY read from joystick
        movementInput = MobileInputManager.instance.movementInput;
        debugMobileMovement = movementInput; // For inspector debugging
        
        // Camera
        cameraInput = MobileInputManager.instance.cameraInput;
        debugMobileCamera = cameraInput;

        // Log for debugging (you can remove this later)
        if (movementInput.magnitude > 0)
        {
            Debug.Log($"Mobile Movement Input: {movementInput}");
        }

        // Actions
        if (MobileInputManager.instance.GetDodgeInput())
            dodgeInput = true;

        if (MobileInputManager.instance.GetJumpInput())
            jumpInput = true;

        sprintInput = MobileInputManager.instance.GetSprintInput();

        if (MobileInputManager.instance.GetRBInput())
            RB_Input = true;

        if (MobileInputManager.instance.GetRTInput())
            RT_Input = true;

        Hold_RT_Input = MobileInputManager.instance.GetRTHoldInput();

        if (MobileInputManager.instance.GetLockOnInput())
            lockOn_Input = true;
    }

    private void HandleLockOnInput()
    {
        if (player.playerNetworkManager.isLockedOn.Value)
        {
            if (player.playerCombatManager.currentTarget == null)
                return;

            if (player.playerCombatManager.currentTarget.isDead)
            {
                player.playerNetworkManager.isLockedOn.Value = false;
            }

            if (lockOnCoroutine != null)
                StopCoroutine(lockOnCoroutine);
                
            lockOnCoroutine = StartCoroutine(PlayerCamera.instance.WaitThenFindNewTarget());
        }

        if (lockOn_Input && player.playerNetworkManager.isLockedOn.Value)
        {
            lockOn_Input = false;
            PlayerCamera.instance.ClearLockOnTargets();
            player.playerNetworkManager.isLockedOn.Value = false;
            return;
        }

        if (lockOn_Input && !player.playerNetworkManager.isLockedOn.Value)
        {
            lockOn_Input = false;
            PlayerCamera.instance.HandleLocatingLockedOnTarget();

            if (PlayerCamera.instance.nearestLockOnTarget != null)
            {
                player.playerCombatManager.SetTarget(PlayerCamera.instance.nearestLockOnTarget);
                player.playerNetworkManager.isLockedOn.Value = true;
            }
        }
    }

    private void HandleLockOnSwitchTargetInput()
    {
        if (lockOn_Left_Input)
        {
            lockOn_Left_Input = false;

            if (player.playerNetworkManager.isLockedOn.Value)
            {
                PlayerCamera.instance.HandleLocatingLockedOnTarget();

                if (PlayerCamera.instance.leftLockOnTarget != null)
                {
                    player.playerCombatManager.SetTarget(PlayerCamera.instance.leftLockOnTarget);
                }
            }
        }
        
        if(lockOn_Right_Input)
        {
            lockOn_Right_Input = false;

            if(player.playerNetworkManager.isLockedOn.Value)
            {
                PlayerCamera.instance.HandleLocatingLockedOnTarget();

                if(PlayerCamera.instance.rightLockOnTarget != null)
                {
                    player.playerCombatManager.SetTarget(PlayerCamera.instance.rightLockOnTarget);
                }
            }
        }
    }

    private void HandlePlayerMovementInput()
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;

        moveAmount = Mathf.Clamp01(Mathf.Abs(verticalInput) + Mathf.Abs(horizontalInput));

        if (moveAmount <= 0.5f && moveAmount > 0)
        {
            moveAmount = 0.5f;
        }
        else if (moveAmount > 0.5f && moveAmount <= 1)
        {
            moveAmount = 1;
        }

        if (player == null)
            return;

        if (moveAmount != 0)
        {
            player.playerNetworkManager.isMoving.Value = true;
        }
        else
        {
            player.playerNetworkManager.isMoving.Value = false;
        }
        
        if (!player.playerNetworkManager.isLockedOn.Value || player.playerNetworkManager.isSprinting.Value)
        {
            player.playerAnimatorManager.UpdateAnimatorMovementParameters(0, moveAmount, player.playerNetworkManager.isSprinting.Value);
        }
        else
        {
            player.playerAnimatorManager.UpdateAnimatorMovementParameters(horizontalInput, verticalInput, player.playerNetworkManager.isSprinting.Value);
        }
    }

    private void HandleCameraMovementInput()
    {
        cameraHorizontalInput = cameraInput.x;
        cameraVerticalInput = cameraInput.y;
    }

    private void HandleDodgeInput()
    {
        if (dodgeInput)
        {
            dodgeInput = false;
            player.playerLocomotionManager.AttemptToPerformDodge();
        }
    }

    private void HandleSpringInput()
    {
        if (sprintInput)
        {
            player.playerLocomotionManager.HandleSprinting();
        }
        else
        {
            player.playerNetworkManager.isSprinting.Value = false;
        }
    }

    private void HandleJumpInput()
    {
        if (jumpInput)
        {
            jumpInput = false;
            player.playerLocomotionManager.AttemptToPerformJump();
        }
    }

    private void HandleRBInput()
    {
        if (RB_Input)
        {
            RB_Input = false;
            player.playerNetworkManager.SetCharacterActionHand(true);
            player.playerCombatManager.PerformingWeaponBasedAction(player.playerInventoryManager.currentRightHandWeapon.oh_RB_Actions, player.playerInventoryManager.currentRightHandWeapon);
        }
    }

    private void HandleRTInput()
    {
        if (RT_Input)
        {
            RT_Input = false;
            player.playerNetworkManager.SetCharacterActionHand(true);
            player.playerCombatManager.PerformingWeaponBasedAction(player.playerInventoryManager.currentRightHandWeapon.oh_RT_Actions, player.playerInventoryManager.currentRightHandWeapon);
        }
    }
    
    private void HandleChargeRTInput()
    {
        if(player.isPerformingAction)
        {
            if(player.playerNetworkManager.isUsingRightHand.Value)
            {
                player.playerNetworkManager.isChargingAttack.Value = Hold_RT_Input;
            }
        }
    }

    // Public method to toggle mobile input for testing in editor
    public void SetMobileInputMode(bool enable)
    {
        useMobileInput = enable;
        
        if (enable && playerControls != null)
        {
            playerControls.Disable();
        }
        else if (!enable && playerControls != null)
        {
            playerControls.Enable();
        }
        
        Debug.Log($"PlayerInputManager: Mobile Input set to {(useMobileInput ? "ENABLED" : "DISABLED")}");
    }
}