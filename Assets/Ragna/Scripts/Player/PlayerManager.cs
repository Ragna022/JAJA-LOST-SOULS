using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Linq;

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
    private const float DefaultStamina = 100f; // A safe fallback value

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

        base.OnNetworkSpawn();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }

        if (IsOwner)
        {
            Debug.Log($"[PlayerManager] Setting up owner-specific subscriptions");

            if (PlayerCamera.instance != null)
            {
                PlayerCamera.instance.player = this;
            }

            if (PlayerInputManager.instance != null)
            {
                PlayerInputManager.instance.player = this;
                PlayerInputManager.instance.SetPlayer(this);
            }
            
            WorldSaveGameManager saveManager = FindFirstObjectByType<WorldSaveGameManager>(); 

            if (saveManager == null)
            {
                 Debug.LogWarning($"[PlayerManager] WorldSaveGameManager not found in scene. Using default values.");
                 SetDefaultPlayerValues();
            }
            else
            {
                if (saveManager.currentCharacterData == null)
                {
                    Debug.Log("[PlayerManager] Lobby flow detected: currentCharacterData is null. Creating temporary data...");
                    
                    LobbyPlayerData? myLobbyData = null;
                    if (LobbyManager.PublicPersistentLobbyData != null)
                    {
                        myLobbyData = LobbyManager.PublicPersistentLobbyData
                            .FirstOrDefault(p => p.clientId == NetworkManager.Singleton.LocalClientId);
                    }

                    if (myLobbyData.HasValue)
                    {
                        saveManager.currentCharacterData = new CharacterSaveData();
                        saveManager.currentCharacterData.characterName = myLobbyData.Value.playerName.ToString();
                        saveManager.currentCharacterData.characterPrefabIndex = myLobbyData.Value.characterPrefabIndex;
                        saveManager.currentCharacterData.vitality = DefaultVitality;
                        saveManager.currentCharacterData.endurance = DefaultEndurance;
                        
                        int maxHealth = playerStatsManager.CalculateHealthBasedOnVitalityLevel(DefaultVitality);
                        float maxStamina = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(DefaultEndurance);

                        if (float.IsNaN(maxStamina) || float.IsInfinity(maxStamina))
                        {
                            maxStamina = DefaultStamina;
                        }
                        
                        saveManager.currentCharacterData.currentHealth = maxHealth; // int to float is fine
                        saveManager.currentCharacterData.currentStamina = maxStamina;
                        
                        Debug.Log($"[PlayerManager] Created temporary save data for {saveManager.currentCharacterData.characterName}");
                    }
                    else
                    {
                         Debug.LogError($"[PlayerManager] Could not find lobby data for client {NetworkManager.Singleton.LocalClientId}! Using defaults.");
                         SetDefaultPlayerValues();
                    }
                }
                
                Debug.Log($"[PlayerManager] Loading from currentCharacterData...");
                LoadGameDataFromCurrentCharacterData(ref saveManager.currentCharacterData);
            }

            SetupNetworkSubscriptions();
        }
        else
        {
            // Only subscribe if the components exist
            if (characterNetworkManager != null && characterUIManager != null)
            {
                characterNetworkManager.currentHealth.OnValueChanged += characterUIManager.OnHPChanged;
            }
        }

        if (playerNetworkManager != null)
        {
            playerNetworkManager.currentRightHandWeaponID.OnValueChanged += playerNetworkManager.OnCurrentRightHandWeaponIDChange;
            playerNetworkManager.currentLeftHandWeaponID.OnValueChanged += playerNetworkManager.OnCurrentLeftHandWeaponIDChange;
        }

        Debug.Log($"[PlayerManager] OnNetworkSpawn COMPLETE");
    }

    private void SetupNetworkSubscriptions()
    {
        if (playerNetworkManager == null) return;

        playerNetworkManager.vitality.OnValueChanged += playerNetworkManager.SetNewMaxHealthValue;
        playerNetworkManager.endurance.OnValueChanged += playerNetworkManager.SetNewMaxStaminaValue;

        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            playerNetworkManager.currentHealth.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
        }

        if (playerStatsManager != null)
        {
            playerNetworkManager.currentStamina.OnValueChanged += playerStatsManager.ResetStaminaRegenTimer;
        }
        
        playerNetworkManager.isChargingAttack.OnValueChanged += playerNetworkManager.OnIsChargingAttackChanged;
        
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
        if (playerNetworkManager == null || playerStatsManager == null)
        {
            Debug.LogError("[PlayerManager] SetDefaultPlayerValues: Critical components are NULL!");
            return;
        }

        try
        {
            playerNetworkManager.characterName.Value = new FixedString64Bytes("Player");
            playerNetworkManager.vitality.Value = DefaultVitality;
            playerNetworkManager.endurance.Value = DefaultEndurance;

            int calculatedMaxHealth = playerStatsManager.CalculateHealthBasedOnVitalityLevel(DefaultVitality);
            playerNetworkManager.maxHealth.Value = calculatedMaxHealth;

            float calculatedMaxStamina = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(DefaultEndurance);

            if (float.IsNaN(calculatedMaxStamina) || float.IsInfinity(calculatedMaxStamina))
            {
                Debug.LogError($"[PlayerManager] Calculated Max Stamina was invalid ({calculatedMaxStamina})! Defaulting to {DefaultStamina}.");
                calculatedMaxStamina = DefaultStamina;
            }
            
            // --- FIX for float/int: maxStamina is an int (network variable), so cast from float to int ---
            playerNetworkManager.maxStamina.Value = (int)calculatedMaxStamina;

            // --- FIX for float/int: currentHealth is an int, so we cast maxHealth (a float) ---
            playerNetworkManager.currentHealth.Value = (int)playerNetworkManager.maxHealth.Value;
            
            playerNetworkManager.currentStamina.Value = playerNetworkManager.maxStamina.Value;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerManager] CRITICAL ERROR in SetDefaultPlayerValues!");
            Debug.LogException(ex);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        }

        // --- FIX for Shutdown Error ---
        // We must check if we are the owner AND our components still exist,
        // as this runs during shutdown when things are being destroyed.
        if (IsOwner)
        {
            CleanupNetworkSubscriptions();
        }
        else
        {
            // Add null checks for safety during shutdown
            if (characterNetworkManager != null && characterUIManager != null)
            {
                characterNetworkManager.currentHealth.OnValueChanged -= characterUIManager.OnHPChanged;
            }
        }

        if (playerNetworkManager != null)
        {
            playerNetworkManager.currentRightHandWeaponID.OnValueChanged -= playerNetworkManager.OnCurrentRightHandWeaponIDChange;
            playerNetworkManager.currentLeftHandWeaponID.OnValueChanged -= playerNetworkManager.OnCurrentLeftHandWeaponIDChange;
        }
        // --- END FIX ---
    }

    private void CleanupNetworkSubscriptions()
    {
        // --- FIX for Shutdown Error ---
        // Add null checks for all external objects, as they might be
        // destroyed before this runs during OnApplicationQuit.
        
        if (playerNetworkManager == null) return; // Guard clause

        playerNetworkManager.vitality.OnValueChanged -= playerNetworkManager.SetNewMaxHealthValue;
        playerNetworkManager.endurance.OnValueChanged -= playerNetworkManager.SetNewMaxStaminaValue;

        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            playerNetworkManager.currentHealth.OnValueChanged -= PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged -= PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
        }

        if (playerStatsManager != null)
        {
            playerNetworkManager.currentStamina.OnValueChanged -= playerStatsManager.ResetStaminaRegenTimer;
        }
        
        playerNetworkManager.isChargingAttack.OnValueChanged -= playerNetworkManager.OnIsChargingAttackChanged;
        // --- END FIX ---
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

        yield return new WaitForSeconds(5);

        Debug.Log($"[PlayerManager] ProcessDeathEvent finished 5 second wait");
    }

    public override void ReviveCharacter()
    {
        base.ReviveCharacter();
        
        Debug.Log($"[PlayerManager] ReviveCharacter called - IsOwner: {IsOwner}");
        
        if (IsOwner && playerNetworkManager != null)
        {
            // --- FIX for float/int: currentHealth is int, maxHealth is float ---
            playerNetworkManager.currentHealth.Value = (int)playerNetworkManager.maxHealth.Value; 
            playerNetworkManager.currentStamina.Value = playerNetworkManager.maxStamina.Value;

            if (playerAnimatorManager != null)
            {
                playerAnimatorManager.PlayTargetActionAnimation("Empty", false);
            }

            isDead.Value = false;
            
            Debug.Log($"[PlayerManager] Character revived - isDead set to false");
        }
    }

    public void SaveGameDataToCurrentCharacterData(ref CharacterSaveData currentCharacterData)
    {
        if (playerNetworkManager == null) return;
        
        if (currentCharacterData == null)
        {
            currentCharacterData = new CharacterSaveData();
        }

        currentCharacterData.characterName = playerNetworkManager.characterName.Value.ToString();
        currentCharacterData.xPosition = transform.position.x;
        currentCharacterData.yPosition = transform.position.y;
        currentCharacterData.zPosition = transform.position.z;

        currentCharacterData.currentHealth = playerNetworkManager.currentHealth.Value; // int to float is fine
        currentCharacterData.currentStamina = playerNetworkManager.currentStamina.Value;

        currentCharacterData.vitality = playerNetworkManager.vitality.Value;
        currentCharacterData.endurance = playerNetworkManager.endurance.Value;
    }

    // Inside PlayerManager.cs

    public void LoadGameDataFromCurrentCharacterData(ref CharacterSaveData currentCharacterData)
    {
        if (playerNetworkManager == null || playerStatsManager == null)
        {
            Debug.LogError("[PlayerManager] LoadGameData: Critical components are NULL!");
            return;
        }

        if (currentCharacterData == null)
        {
            Debug.LogError("[PlayerManager] LoadGameData was called with null data! Using defaults.");
            SetDefaultPlayerValues();
            return;
        }

        string charName = string.IsNullOrEmpty(currentCharacterData.characterName) ? "Player" : currentCharacterData.characterName;
        playerNetworkManager.characterName.Value = new FixedString64Bytes(charName);

        // --- THIS IS THE FIX ---
        // REMOVE OR COMMENT OUT the lines that set the position here.
        // LobbyManager handles the initial spawn position. Only load position
        // from save data if you are explicitly loading a saved game session,
        // not during the initial spawn event.

        // Vector3 myPosition = new Vector3(currentCharacterData.xPosition, currentCharacterData.yPosition, currentCharacterData.zPosition);
        // transform.position = myPosition;
        // --- END FIX ---


        int vitalityToSet = currentCharacterData.vitality > 0 ? currentCharacterData.vitality : DefaultVitality;
        int enduranceToSet = currentCharacterData.endurance > 0 ? currentCharacterData.endurance : DefaultEndurance;
        playerNetworkManager.vitality.Value = vitalityToSet;
        playerNetworkManager.endurance.Value = enduranceToSet;

        // Note: maxHealth calculation uses vitality, which is now set.
        playerNetworkManager.maxHealth.Value = playerStatsManager.CalculateHealthBasedOnVitalityLevel(playerNetworkManager.vitality.Value);

        float maxStamina = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(playerNetworkManager.endurance.Value);
        if (float.IsNaN(maxStamina) || float.IsInfinity(maxStamina))
        {
            Debug.LogWarning($"[PlayerManager] Loaded Max Stamina was invalid. Defaulting to {DefaultStamina}.");
            maxStamina = DefaultStamina;
        }
        playerNetworkManager.maxStamina.Value = (int)maxStamina;


        if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
        {
            // Update UI Max values *after* NetworkVariables are set
            PlayerUIManager.instance.playerUIHudManager.SetMaxHealthValue(playerNetworkManager.maxHealth.Value);
            PlayerUIManager.instance.playerUIHudManager.SetMaxStaminaValue(playerNetworkManager.maxStamina.Value);
        }

        int healthToSet = (int)Mathf.Clamp(currentCharacterData.currentHealth, 0, (int)playerNetworkManager.maxHealth.Value);
        if (healthToSet <= 0) healthToSet = (int)playerNetworkManager.maxHealth.Value;
        playerNetworkManager.currentHealth.Value = healthToSet;

        float staminaToSet = Mathf.Clamp(currentCharacterData.currentStamina, 0f, playerNetworkManager.maxStamina.Value);
        // Ensure staminaToSet uses the *int* value of maxStamina for comparison if currentStamina is also int
        if (staminaToSet <= 0f) staminaToSet = playerNetworkManager.maxStamina.Value;
        playerNetworkManager.currentStamina.Value = staminaToSet; // Assuming currentStamina is float
    }

    public void LoadOtherPlayerCharacterWhenJoiningServer()
    {
        // SYNC WEAPONS
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