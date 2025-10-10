using System.Collections;
using UnityEngine;

public class PlayerManager : CharacterManager
{
    [Header("Debug Menu")]
    [SerializeField] bool respawnCharacter = false;
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
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            PlayerCamera.instance.player = this;
            PlayerInputManager.instance.player = this;
            WorldSaveGameManager.instance.player = this;

            playerNetworkManager.vitality.OnValueChanged += playerNetworkManager.SetNewMaxHealthValue;
            playerNetworkManager.endurance.OnValueChanged += playerNetworkManager.SetNewMaxStaminaValue;

            playerNetworkManager.currentHealth.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue;
            playerNetworkManager.currentStamina.OnValueChanged += PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue;
            playerNetworkManager.currentStamina.OnValueChanged += playerStatsManager.ResetStaminaRegenTimer;

            // NEW: Use load for all owners (host/clients), falling back to defaults if save invalid
            LoadGameDataFromCurrentCharacterData(ref WorldSaveGameManager.instance.currentCharacterData);

            // Manual trigger for max setters (in case no change)
            playerNetworkManager.SetNewMaxHealthValue(0, playerNetworkManager.vitality.Value);
            playerNetworkManager.SetNewMaxStaminaValue(0, playerNetworkManager.endurance.Value);

            // Force UI updates
            PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue(0, playerNetworkManager.currentHealth.Value);
            PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue(0f, playerNetworkManager.currentStamina.Value);
            PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
        }

        playerNetworkManager.currentHealth.OnValueChanged += playerNetworkManager.CheckHp;

        // EQUIPMENT
        playerNetworkManager.currentWeaponBeingUsed.OnValueChanged += playerNetworkManager.OnCurrentWeaponBeingUsedIDChange;

        // REMOVED: Client-only load check—now handled uniformly above
    }

    public override IEnumerator ProcessDeathEvent(bool manuallySelectDeathAnimation = false)
    {
        if (IsOwner)
        {
            PlayerUIManager.instance.playerUIPopUpManager.SendYouDiedPopUp();
        }

        // CHECK FOR ALL PLAYERS THAT ARE ALIVE, IF 0 RESPAWN CHARACTERS

        return base.ProcessDeathEvent(manuallySelectDeathAnimation);
    }

    public override void ReviveCharacter()
    {
        base.ReviveCharacter();
        if (IsOwner)
        {
            playerNetworkManager.currentHealth.Value = playerNetworkManager.maxHealth.Value;
            playerNetworkManager.currentStamina.Value = playerNetworkManager.maxStamina.Value;

            // RESTORE FOCUS POINT

            // PLAY REBIRTH EFFECTS
            playerAnimatorManager.PlayTargetActionAnimation("Empty", false);
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
        // Load position and name as before
        playerNetworkManager.characterName.Value = currentCharacterData.characterName;
        Vector3 myPosition = new Vector3(currentCharacterData.xPosition, currentCharacterData.yPosition, currentCharacterData.zPosition);
        transform.position = myPosition;

        // NEW: Use save if valid, else defaults (prevents zeroed clients)
        int vitalityToSet = currentCharacterData.vitality > 0 ? currentCharacterData.vitality : DefaultVitality;
        int enduranceToSet = currentCharacterData.endurance > 0 ? currentCharacterData.endurance : DefaultEndurance;
        playerNetworkManager.vitality.Value = vitalityToSet;
        playerNetworkManager.endurance.Value = enduranceToSet;

        // Calculate max (will use defaults if applied)
        playerNetworkManager.maxHealth.Value = playerStatsManager.CalculateHealthBasedOnVitalityLevel(playerNetworkManager.vitality.Value);
        playerNetworkManager.maxStamina.Value = playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(playerNetworkManager.endurance.Value);

        // Update UI max
        PlayerUIManager.instance.playerUIHudManager.SetMaxHealthValue(playerNetworkManager.maxHealth.Value);
        PlayerUIManager.instance.playerUIHudManager.SetMaxStaminaValue(playerNetworkManager.maxStamina.Value);

        // Compute and set currents (clamp to max, default to max if invalid)
        int healthToSet = Mathf.Clamp(currentCharacterData.currentHealth, 0, playerNetworkManager.maxHealth.Value);
        if (healthToSet <= 0) healthToSet = playerNetworkManager.maxHealth.Value;
        playerNetworkManager.currentHealth.Value = healthToSet;

        float staminaToSet = Mathf.Clamp(currentCharacterData.currentStamina, 0f, playerNetworkManager.maxStamina.Value);
        if (staminaToSet <= 0f) staminaToSet = playerNetworkManager.maxStamina.Value;
        playerNetworkManager.currentStamina.Value = staminaToSet;

        // Force UI current updates and refresh
        PlayerUIManager.instance.playerUIHudManager.SetNewHealthValue(0, playerNetworkManager.currentHealth.Value);
        PlayerUIManager.instance.playerUIHudManager.SetNewStaminaValue(0f, playerNetworkManager.currentStamina.Value);
        PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
    }

    private void DebugMenu()
    {
        if (respawnCharacter)
        {
            respawnCharacter = false;
            ReviveCharacter();
        }
    }
}
