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

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerManager] OnNetworkSpawn START - ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}");

        base.OnNetworkSpawn();  // Calls base, which handles isDead subscription

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;

        if (IsOwner)
        {
            Debug.Log($"[PlayerManager] Setting up owner-specific subscriptions");

            PlayerCamera.instance.player = this;
            PlayerInputManager.instance.player = this;
            WorldSaveGameManager.instance.player = this;

            playerNetworkManager.vitality.OnValueChanged += playerNetworkManager.SetNewMaxHealthValue;
            playerNetworkManager.endurance.OnValueChanged += playerNetworkManager.SetNewMaxStaminaValue;

            playerNetworkManager.currentHealth.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
            playerNetworkManager.currentStamina.OnValueChanged += playerStatsManager.ResetStaminaRegenTimer;

            // FLAGS 
            //playerNetworkManager.isChargingAttack.OnValueChanged += playerNetworkManager.OnIsChargingAttackChanged;

            LoadGameDataFromCurrentCharacterData(ref WorldSaveGameManager.instance.currentCharacterData);

            playerNetworkManager.SetNewMaxHealthValue(0, playerNetworkManager.vitality.Value);
            playerNetworkManager.SetNewMaxStaminaValue(0, playerNetworkManager.endurance.Value);

            PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue(0, playerNetworkManager.currentHealth.Value);
            PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue(0f, playerNetworkManager.currentStamina.Value);
            PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
        }

        // NOTE: We removed the duplicate subscription here since it's now in CharacterNetworkManager
        // playerNetworkManager.currentHealth.OnValueChanged += playerNetworkManager.CheckHp;

        //playerNetworkManager.isChargingAttack.OnValueChanged += playerNetworkManager.OnIsChargingAttackChanged;

        playerNetworkManager.currentWeaponBeingUsed.OnValueChanged += playerNetworkManager.OnCurrentWeaponBeingUsedIDChange;

        Debug.Log($"[PlayerManager] OnNetworkSpawn COMPLETE");

        // EQUIPMENT
        playerNetworkManager.currentRightHandWeaponID.OnValueChanged += playerNetworkManager.OnCurrentRightHandWeaponIDChange;
        playerNetworkManager.currentLeftHandWeaponID.OnValueChanged += playerNetworkManager.OnCurrentLeftHandWeaponIDChange;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

        if (IsOwner)
        {

            playerNetworkManager.vitality.OnValueChanged -= playerNetworkManager.SetNewMaxHealthValue;
            playerNetworkManager.endurance.OnValueChanged -= playerNetworkManager.SetNewMaxStaminaValue;

            playerNetworkManager.currentHealth.OnValueChanged -= PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged -= PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
            playerNetworkManager.currentStamina.OnValueChanged -= playerStatsManager.ResetStaminaRegenTimer;

            // FLAGS 
            //playerNetworkManager.isChargingAttack.OnValueChanged -= playerNetworkManager.OnIsChargingAttackChanged;

            playerNetworkManager.currentWeaponBeingUsed.OnValueChanged += playerNetworkManager.OnCurrentWeaponBeingUsedIDChange;

        }
    }

    private void OnClientConnectedCallback(ulong clientID)
    {
        WorldGameSessionManager.instance.AddPlayerToActivePlayerList(this);

        if(!IsServer && IsOwner)
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

    public override IEnumerator ProcessDeathEvent(bool manuallySelectDeathAnimation = false)
    {
        Debug.Log($"[PlayerManager] ProcessDeathEvent STARTED - ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}");
        
        if (IsOwner)
        {
            Debug.Log($"[PlayerManager] Owner processing death - showing UI popup");
            
            PlayerUIManager.instance.playerUIPopUpManager.SendYouDiedPopUp();

            characterNetworkManager.currentHealth.Value = 0;
            isDead.Value = true;
            
            Debug.Log($"[PlayerManager] Set currentHealth to 0 and isDead to true");

            // RESET ANY FLAGS HERE THAT NEED TO BE RESET
            // NOTHING YET

            // IF WE ARE NOT GROUNDED, PLAY AN AERIAL DEATH ANIMATION (update in OnIsDeadChanged if needed)
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
        
        if (IsOwner)
        {
            playerNetworkManager.currentHealth.Value = playerNetworkManager.maxHealth.Value;
            playerNetworkManager.currentStamina.Value = playerNetworkManager.maxStamina.Value;

            // RESTORE FOCUS POINT

            // PLAY REBIRTH EFFECTS
            playerAnimatorManager.PlayTargetActionAnimation("Empty", false);

            // Reset isDead for revive (syncs to all)
            isDead.Value = false;
            
            Debug.Log($"[PlayerManager] Character revived - isDead set to false");
        }
    }

    public void SaveGameDataToCurrentCharacterData(ref CharacterSaveData currentCharacterData)
    {
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
        playerNetworkManager.characterName.Value = currentCharacterData.characterName;

        // A small offset to spawn the player just above the ground
        float verticalOffset = 0.5f; 
    
        // Set the position slightly higher than the saved location
        Vector3 myPosition = new Vector3(
            currentCharacterData.xPosition, 
            currentCharacterData.yPosition + verticalOffset, // Add the offset here
            currentCharacterData.zPosition
        );
        transform.position = myPosition;

        int vitalityToSet = currentCharacterData.vitality > 0 ? currentCharacterData.vitality : DefaultVitality;
        int enduranceToSet = currentCharacterData.endurance > 0 ? currentCharacterData.endurance : DefaultEndurance;
        playerNetworkManager.vitality.Value = vitalityToSet;
        playerNetworkManager.endurance.Value = enduranceToSet;

        playerNetworkManager.maxHealth.Value = playerStatsManager.CalculateHealthBasedOnVitalityLevel(playerNetworkManager.vitality.Value);
        playerNetworkManager.maxStamina.Value = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(playerNetworkManager.endurance.Value);

        PlayerUIManager.instance.playerUIHudManager.SetMaxHealthValue(playerNetworkManager.maxHealth.Value);
        PlayerUIManager.instance.playerUIHudManager.SetMaxStaminaValue(playerNetworkManager.maxStamina.Value);

        int healthToSet = Mathf.Clamp(currentCharacterData.currentHealth, 0, playerNetworkManager.maxHealth.Value);
        if (healthToSet <= 0) healthToSet = playerNetworkManager.maxHealth.Value;
        playerNetworkManager.currentHealth.Value = healthToSet;

        float staminaToSet = Mathf.Clamp(currentCharacterData.currentStamina, 0f, playerNetworkManager.maxStamina.Value);
        if (staminaToSet <= 0f) staminaToSet = playerNetworkManager.maxStamina.Value;
        playerNetworkManager.currentStamina.Value = staminaToSet;

        PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue(0, playerNetworkManager.currentHealth.Value);
        PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue(0f, playerNetworkManager.currentStamina.Value);
        PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
    }

    public void LoadOtherPlayerCharacterWhenJoiningServer()
    {
        // SYNC WEAPONS
        playerNetworkManager.OnCurrentRightHandWeaponIDChange(0, playerNetworkManager.currentRightHandWeaponID.Value);
        playerNetworkManager.OnCurrentLeftHandWeaponIDChange(0, playerNetworkManager.currentLeftHandWeaponID.Value);
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
            playerEquipmentManager.SwitchRightWeapon();
        }
    }
}