using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerNetworkManager : CharacterNetworkManager
{
    PlayerManager player;
    public NetworkVariable<FixedString64Bytes> characterName = new NetworkVariable<FixedString64Bytes>("Character", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Equipment")]
    public NetworkVariable<int> currentWeaponBeingUsed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isUsingRightHand = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isUsingLeftHand = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<PlayerManager>();
    }

    public void SetCharacterActionHand(bool rightHandedAction)
    {
        if (rightHandedAction)
        {
            isUsingLeftHand.Value = false;
            isUsingRightHand.Value = true;
        }
        else
        {
            isUsingRightHand.Value = false;
            isUsingLeftHand.Value = true;
        }
    }

    public void SetNewMaxHealthValue(int oldVitality, int newVitality)
    {
        maxHealth.Value = player.playerStatsManager.CalculateHealthBasedOnVitalityLevel(newVitality);
        PlayerUIManager.instance.playerUIHudManager.SetMaxHealthValue(maxHealth.Value);
        currentHealth.Value = maxHealth.Value;
    }

    public void SetNewMaxStaminaValue(int oldEndurance, int newEndurance)
    {
        maxStamina.Value = player.playerStatsManager.CalculateStaminaBasedOnEnduranceLevel(newEndurance);
        PlayerUIManager.instance.playerUIHudManager.SetMaxStaminaValue(maxStamina.Value);
        currentStamina.Value = maxStamina.Value;
    }

    public void OnCurrentWeaponBeingUsedIDChange(int oldID, int newID)
    {
        WeaponItem newWeapon = Instantiate(WorldItemDatabase.Instance.GetWeaponByID(newID));
        player.playerCombatManager.currentWeaponBeingUsed = newWeapon;
        //player.playerEquipmentManager.LoadWeaponOnBothHands();
    }

    // ITEMS ACTIONS
    [ServerRpc]
    public void NotifyTheServerOfWeaponActionServerRpc(ulong clientID, int actionID, int weaponID)
    {
        if(IsServer)
        {
            NotifyTheServerOfWeaponActionClientRpc(clientID, actionID, weaponID);
        }
    }

    [ClientRpc]
    private void NotifyTheServerOfWeaponActionClientRpc(ulong clientID, int actionID, int weaponID)
    {
        // WE DO NOT PLAY THE ACTION AGAIN FOR THE CHARACTER WHO CALLED IT, BECAUSE THEY ALREADY PLAYED IT LOCALLY
        if(clientID != NetworkManager.Singleton.LocalClientId)
        {
            PerformWeaponBasedAction(actionID, weaponID);
        }
    }
    
    private void PerformWeaponBasedAction(int actionID, int weaponID)
    {
        WeaponItemAction weaponAction = WorldActionManager.instance.GetWeaponItemActionByID(actionID);

        if (weaponAction != null)
        {
            weaponAction.AttemptToPerformAction(player, WorldItemDatabase.Instance.GetWeaponByID(weaponID));
        }
        else
        {
            Debug.Log("ACTION IS NULL, CANNOT BE PERFORMED");
        }
    }
}
