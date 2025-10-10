using Unity.Netcode;
using UnityEngine;

public class PlayerCombatManager : CharacterCombatManager
{
    PlayerManager player;

    public WeaponItem currentWeaponBeingUsed;

    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<PlayerManager>();
    }

    public void PerformingWeaponBasedAction(WeaponItemAction weaponAction, WeaponItem weaponPerformingAction)
    {
        if(player.IsOwner)
        {
            // PERFORM THE ACTION
            weaponAction.AttemptToPerformAction(player, weaponPerformingAction);

            // NOTIFY THE SERVER WE HAVE PERFORMED THE ACTION, SO WE PERFORM IT FROM THERE PERSPECTIVE ALSO
            player.playerNetworkManager.NotifyTheServerOfWeaponActionServerRpc(NetworkManager.Singleton.LocalClientId, weaponAction.actionID, weaponPerformingAction.itemID);
        }
    }
}
