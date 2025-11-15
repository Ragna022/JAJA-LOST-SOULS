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

    // ADDED: Track if we're ready to process input
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
            
            // ADDED: Reset ready state and wait for player to be assigned
            isReady = false;
            
            if (playerControls != null)
            {
                playerControls.Enable();
            }
            
            // ADDED: Try to find player if not already assigned
            if (player == null)
            {
                FindPlayerInScene();
            }
        }
        else
        {
            instance.enabled = false;
            isReady = false; // ADDED: Reset ready state

            if (playerControls != null)
            {
                playerControls.Disable();
            }
        }
    }

    // ADDED: Method to find player in scene
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
            // We'll keep trying in Update until we find a player
        }
    }

    // ADDED: Method to set player reference (can be called from PlayerManager)
    public void SetPlayer(PlayerManager newPlayer)
    {
        player = newPlayer;
        isReady = true;
        Debug.Log($"PlayerInputManager: Player set to {player.gameObject.name}");
    }

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
            playerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
            playerControls.PlayerCamera.Movement.performed += i => cameraInput = i.ReadValue<Vector2>();
            playerControls.PlayerActions.Dodge.performed += i => dodgeInput = true;
            playerControls.PlayerActions.Jump.performed += i => jumpInput = true;
            playerControls.PlayerActions.RB.performed += i => RB_Input = true;

            // LOCK ON
            playerControls.PlayerActions.LockOn.performed += i => lockOn_Input = true;
            playerControls.PlayerActions.SeekLeftLockOnTarget.performed += i => lockOn_Left_Input = true;
            playerControls.PlayerActions.SeekRightLockOnTarget.performed += i => lockOn_Right_Input = true;

            // TRIGGERS
            playerControls.PlayerActions.RT.performed += i => RT_Input = true;
            playerControls.PlayerActions.HoldRT.performed += i => Hold_RT_Input = true;
            playerControls.PlayerActions.HoldRT.canceled += i => Hold_RT_Input = false;

            // HOLDING THE SPRINT SETS THE BOOL TO TRUE
            playerControls.PlayerActions.Sprint.performed += i => sprintInput = true;
            // PRESSING THE INPUT SETS THE BOOL TO FALSE
            playerControls.PlayerActions.Sprint.canceled += i => sprintInput = false;
        }

        playerControls.Enable();
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChange;
    }

    // IF WE MINIMIZE, STOP ADJUSTING INPUTS
    private void OnApplicationFocus(bool focus)
    {
        if (enabled)
        {
            playerControls.Enable();
        }
        else
        {
            playerControls.Disable();
        }
    }

    private void Update()
    {
        // ADDED: Keep trying to find player if not ready
        if (!isReady && player == null)
        {
            FindPlayerInScene();
            return; // Don't process input until we have a player
        }
        
        HandleAllInput();
    }

    private void HandleAllInput()
    {
        // ADDED: Safety check
        if (!isReady || player == null)
            return;

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

    // Lock ON
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

    // MOVEMENT

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
            return;//

        if (moveAmount != 0)
        {
            player.playerNetworkManager.isMoving.Value = true;
        }
        else
        {
            player.playerNetworkManager.isMoving.Value = false;
        }
        
        // REMOVED: Redundant null check since we check in HandleAllInput
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

    // ACTION

    private void HandleDodgeInput()
    {
        if (dodgeInput)
        {
            dodgeInput = false;

            // FUTURE NOTE; RETURN (DO NOTHING) IF MENU OR UI WINDOW IS OPEN

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

            // IF WE HAVE A UI WINDOW OPEN, SIMPLY RETURN WITHOUT DOING ANYTHING

            // ATTEMPT TO PERFORM JUMP

            player.playerLocomotionManager.AttemptToPerformJump();
        }
    }

    private void HandleRBInput()
    {
        if (RB_Input)
        {
            RB_Input = false;

            // TODO: IF WE HAVE A UI WINDOW OPEN, RETURN AND DO NOTHING

            player.playerNetworkManager.SetCharacterActionHand(true);

            // TODO: IF WE ARE TWO HANDING THE WEAPON, USE THE TWO HANDED ACTION

            player.playerCombatManager.PerformingWeaponBasedAction(player.playerInventoryManager.currentRightHandWeapon.oh_RB_Actions, player.playerInventoryManager.currentRightHandWeapon);
        }
    }

    private void HandleRTInput()
    {
        if (RT_Input)
        {
            RT_Input = false;

            // TODO: IF WE HAVE A UI WINDOW OPEN, RETURN AND DO NOTHING

            player.playerNetworkManager.SetCharacterActionHand(true);

            // TODO: IF WE ARE TWO HANDING THE WEAPON, USE THE TWO HANDED ACTION

            player.playerCombatManager.PerformingWeaponBasedAction(player.playerInventoryManager.currentRightHandWeapon.oh_RT_Actions, player.playerInventoryManager.currentRightHandWeapon);
        }
    }
    
    private void HandleChargeRTInput()
    {
        // WE ONLY WANT TO CHECK FOR A CHARGE IF WE ARE IN AN ACTION THAT REQUIRES IT(attacking)
        if(player.isPerformingAction)
        {
            if(player.playerNetworkManager.isUsingRightHand.Value)
            {
                player.playerNetworkManager.isChargingAttack.Value = Hold_RT_Input;
            }
        }
    }
}