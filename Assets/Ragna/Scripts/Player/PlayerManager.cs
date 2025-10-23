using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerManager : CharacterManager
{
    [Header("Debug Menu")]
    [SerializeField] bool respawnCharacter = false;
    [SerializeField] bool SwitchRightWeapon = false;

    [HideInInspector] public PlayerAnimatorManager playerAnimatorManager;
    [HideInInspector] public PlayerLocomotionManager playerLocomotionManager;
    [HideInInspector] public PlayerNetworkManager playerNetworkManager;
    [HideInInspector] public PlayerStatsManager playerStatsManager;
    [HideInInspector] public PlayerInventoryManager playerInventoryManager;
    [HideInInspector] public PlayerEquipmentManager playerEquipmentManager;
    [HideInInspector] public PlayerCombatManager playerCombatManager;

    private const int DefaultVitality = 15;
    private const int DefaultEndurance = 10;

    protected override void Awake()
    {
        base.Awake();

        playerLocomotionManager = GetComponent<PlayerLocomotionManager>();
        playerAnimatorManager = GetComponent<PlayerAnimatorManager>();
        playerNetworkManager = GetComponent<PlayerNetworkManager>();
        playerStatsManager = GetComponent<PlayerStatsManager>();
        playerInventoryManager = GetComponent<PlayerInventoryManager>();
        playerEquipmentManager = GetComponent<PlayerEquipmentManager>();
        playerCombatManager = GetComponent<PlayerCombatManager>();
        
        // Ensure we have a NetworkObject component
        if (GetComponent<NetworkObject>() == null)
        {
            Debug.LogError($"❌ PlayerManager: {gameObject.name} is missing NetworkObject component! This will cause network spawning to fail.");
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!IsOwner)
            return;

        playerLocomotionManager.HandleAllMovement();

        // REGENERATE STAMINA 
        playerStatsManager.RegenerateStamina();

        DebugMenu();
    }

    protected override void LateUpdate()
    {
        if (!IsOwner)
            return;

        base.LateUpdate();

        PlayerCamera.instance.HandleAllCameraActions();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerManager] OnNetworkSpawn START - ClientID: {OwnerClientId}, IsOwner: {IsOwner}, NetworkObjectId: {NetworkObjectId}");

        base.OnNetworkSpawn();  // Calls base, which handles isDead subscription

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }

        if (IsOwner)
        {
            Debug.Log($"[PlayerManager] Setting up owner-specific subscriptions");

            // Setup camera and input
            if (PlayerCamera.instance != null)
            {
                PlayerCamera.instance.player = this;
                //PlayerCamera.instance.SetPlayerTarget(transform);
            }

            if (PlayerInputManager.instance != null)
            {
                PlayerInputManager.instance.player = this;
                PlayerInputManager.instance.SetPlayer(this);
            }

            // Load game data
            WorldSaveGameManager saveManager = FindObjectOfType<WorldSaveGameManager>();
            if (saveManager != null)
            {
                Debug.Log($"[PlayerManager] Found WorldSaveGameManager, loading game data");
                LoadGameDataFromCurrentCharacterData(ref saveManager.currentCharacterData);
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] WorldSaveGameManager not found in scene");
                // Set default values if no save manager
                SetDefaultPlayerValues();
            }

            // Subscribe to network variable changes
            SetupNetworkSubscriptions();
        }
        else
        {
            // Remote player setup
            characterNetworkManager.currentHealth.OnValueChanged += characterUIManager.OnHPChanged;
        }

        // Weapon subscriptions (for all players)
        playerNetworkManager.currentRightHandWeaponID.OnValueChanged += playerNetworkManager.OnCurrentRightHandWeaponIDChange;
        playerNetworkManager.currentLeftHandWeaponID.OnValueChanged += playerNetworkManager.OnCurrentLeftHandWeaponIDChange;

        Debug.Log($"[PlayerManager] OnNetworkSpawn COMPLETE");
    }

    private void SetupNetworkSubscriptions()
    {
        if (playerNetworkManager == null) return;

        // Stats subscriptions
        playerNetworkManager.vitality.OnValueChanged += playerNetworkManager.SetNewMaxHealthValue;
        playerNetworkManager.endurance.OnValueChanged += playerNetworkManager.SetNewMaxStaminaValue;

        // UI subscriptions
        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            playerNetworkManager.currentHealth.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
        }

        playerNetworkManager.currentStamina.OnValueChanged += playerStatsManager.ResetStaminaRegenTimer;

        // Combat flags
        playerNetworkManager.isChargingAttack.OnValueChanged += playerNetworkManager.OnIsChargingAttackChanged;

        // Initialize values
        playerNetworkManager.SetNewMaxHealthValue(0, playerNetworkManager.vitality.Value);
        playerNetworkManager.SetNewMaxStaminaValue(0, playerNetworkManager.endurance.Value);

        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue(0, playerNetworkManager.currentHealth.Value);
            PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue(0f, playerNetworkManager.currentStamina.Value);
            PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
        }
    }

    private void SetDefaultPlayerValues()
    {
        if (playerNetworkManager != null)
        {
            playerNetworkManager.vitality.Value = DefaultVitality;
            playerNetworkManager.endurance.Value = DefaultEndurance;
            playerNetworkManager.maxHealth.Value = playerStatsManager.CalculateHealthBasedOnVitalityLevel(DefaultVitality);
            playerNetworkManager.maxStamina.Value = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(DefaultEndurance);
            playerNetworkManager.currentHealth.Value = playerNetworkManager.maxHealth.Value;
            playerNetworkManager.currentStamina.Value = playerNetworkManager.maxStamina.Value;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        }

        if (IsOwner)
        {
            CleanupNetworkSubscriptions();
        }
        else
        {
            if (characterNetworkManager != null)
                characterNetworkManager.currentHealth.OnValueChanged -= characterUIManager.OnHPChanged;
        }

        // Weapon unsubscriptions
        if (playerNetworkManager != null)
        {
            playerNetworkManager.currentRightHandWeaponID.OnValueChanged -= playerNetworkManager.OnCurrentRightHandWeaponIDChange;
            playerNetworkManager.currentLeftHandWeaponID.OnValueChanged -= playerNetworkManager.OnCurrentLeftHandWeaponIDChange;
        }
    }

    private void CleanupNetworkSubscriptions()
    {
        if (playerNetworkManager == null) return;

        playerNetworkManager.vitality.OnValueChanged -= playerNetworkManager.SetNewMaxHealthValue;
        playerNetworkManager.endurance.OnValueChanged -= playerNetworkManager.SetNewMaxStaminaValue;

        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            playerNetworkManager.currentHealth.OnValueChanged -= PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged -= PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
        }

        playerNetworkManager.currentStamina.OnValueChanged -= playerStatsManager.ResetStaminaRegenTimer;
        playerNetworkManager.isChargingAttack.OnValueChanged -= playerNetworkManager.OnIsChargingAttackChanged;
    }

    private void OnClientConnectedCallback(ulong clientID)
    {
        if (WorldGameSessionManager.instance != null)
        {
            WorldGameSessionManager.instance.AddPlayerToActivePlayerList(this);
        }

        if (!IsServer && IsOwner)
        {
            if (WorldGameSessionManager.instance != null)
            {
                foreach(var player in WorldGameSessionManager.instance.players)
                {
                    if(player != this)
                    {
                        player.LoadOtherPlayerCharacterWhenJoiningServer();
                    }
                }
            }
        }
    }

    public override IEnumerator ProcessDeathEvent(bool manuallySelectDeathAnimation = false)
    {
        Debug.Log($"[PlayerManager] ProcessDeathEvent STARTED - ClientID: {OwnerClientId}, IsOwner: {IsOwner}");
        
        if (IsOwner)
        {
            Debug.Log($"[PlayerManager] Owner processing death - showing UI popup");
            
            if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIPopUpManager != null)
            {
                PlayerUIManager.instance.playerUIPopUpManager.SendYouDiedPopUp();
            }

            if (characterNetworkManager != null)
            {
                characterNetworkManager.currentHealth.Value = 0;
            }
            isDead.Value = true;
            
            Debug.Log($"[PlayerManager] Set currentHealth to 0 and isDead to true");
        }
        else
        {
            Debug.Log($"[PlayerManager] Non-owner in ProcessDeathEvent, waiting for sync");
        }

        // PLAY SOME DEATH SFX

        yield return new WaitForSeconds(5);

        Debug.Log($"[PlayerManager] ProcessDeathEvent finished 5 second wait");
        
        // AWARD PLAYERS WITH RUNES
        // DISABLE CHARACTER
    }

    public override void ReviveCharacter()
    {
        base.ReviveCharacter();
        
        Debug.Log($"[PlayerManager] ReviveCharacter called - IsOwner: {IsOwner}");
        
        if (IsOwner && playerNetworkManager != null)
        {
            playerNetworkManager.currentHealth.Value = playerNetworkManager.maxHealth.Value;
            playerNetworkManager.currentStamina.Value = playerNetworkManager.maxStamina.Value;

            // RESTORE FOCUS POINT

            // PLAY REBIRTH EFFECTS
            if (playerAnimatorManager != null)
            {
                playerAnimatorManager.PlayTargetActionAnimation("Empty", false);
            }

            // Reset isDead for revive (syncs to all)
            isDead.Value = false;
            
            Debug.Log($"[PlayerManager] Character revived - isDead set to false");
        }
    }

    public void SaveGameDataToCurrentCharacterData(ref CharacterSaveData currentCharacterData)
    {
        if (playerNetworkManager == null) return;

        currentCharacterData.characterName = playerNetworkManager.characterName.Value.ToString();
        currentCharacterData.xPosition = transform.position.x;
        currentCharacterData.yPosition = transform.position.y;
        currentCharacterData.zPosition = transform.position.z;

        currentCharacterData.currentHealth = playerNetworkManager.currentHealth.Value;
        currentCharacterData.currentStamina = playerNetworkManager.currentStamina.Value;

        currentCharacterData.vitality = playerNetworkManager.vitality.Value;
        currentCharacterData.endurance = playerNetworkManager.endurance.Value;
    }

    public void LoadGameDataFromCurrentCharacterData(ref CharacterSaveData currentCharacterData)
    {
        if (playerNetworkManager == null || playerStatsManager == null) return;

        playerNetworkManager.characterName.Value = currentCharacterData.characterName;
        Vector3 myPosition = new Vector3(currentCharacterData.xPosition, currentCharacterData.yPosition, currentCharacterData.zPosition);
        transform.position = myPosition;

        int vitalityToSet = currentCharacterData.vitality > 0 ? currentCharacterData.vitality : DefaultVitality;
        int enduranceToSet = currentCharacterData.endurance > 0 ? currentCharacterData.endurance : DefaultEndurance;
        playerNetworkManager.vitality.Value = vitalityToSet;
        playerNetworkManager.endurance.Value = enduranceToSet;

        playerNetworkManager.maxHealth.Value = playerStatsManager.CalculateHealthBasedOnVitalityLevel(playerNetworkManager.vitality.Value);
        playerNetworkManager.maxStamina.Value = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(playerNetworkManager.endurance.Value);

        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            PlayerUIManager.instance.playerUIHudManager.SetMaxHealthValue(playerNetworkManager.maxHealth.Value);
            PlayerUIManager.instance.playerUIHudManager.SetMaxStaminaValue(playerNetworkManager.maxStamina.Value);
        }

        int healthToSet = Mathf.Clamp(currentCharacterData.currentHealth, 0, playerNetworkManager.maxHealth.Value);
        if (healthToSet <= 0) healthToSet = playerNetworkManager.maxHealth.Value;
        playerNetworkManager.currentHealth.Value = healthToSet;

        float staminaToSet = Mathf.Clamp(currentCharacterData.currentStamina, 0f, playerNetworkManager.maxStamina.Value);
        if (staminaToSet <= 0f) staminaToSet = playerNetworkManager.maxStamina.Value;
        playerNetworkManager.currentStamina.Value = staminaToSet;

        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue(0, playerNetworkManager.currentHealth.Value);
            PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue(0f, playerNetworkManager.currentStamina.Value);
            PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
        }
    }

    public void LoadOtherPlayerCharacterWhenJoiningServer()
    {
        // SYNC WEAPONS
        // This method can be used to sync other players' data when joining
    }

    private void DebugMenu()
    {
        if (respawnCharacter)
        {
            respawnCharacter = false;
            ReviveCharacter();
        }

        if(SwitchRightWeapon)
        {
            SwitchRightWeapon = false;
            if (playerEquipmentManager != null)
            {
                playerEquipmentManager.SwitchRightWeapon();
            }
        }
    }
}