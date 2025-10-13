using UnityEngine;

[CreateAssetMenu(menuName = "Character Actions/Weapon Actions/Heavy Attack Action")]
public class HeavyAttackWeaponItemAction : WeaponItemAction
{
    [SerializeField] string heavy_Attack_01 = "HeavyAttack";

    public override void AttemptToPerformAction(PlayerManager playerPerformingAction, WeaponItem weaponPerformingAction)
    {
        base.AttemptToPerformAction(playerPerformingAction, weaponPerformingAction);

        // Set local weapon reference immediately (avoids null in drain call; NetworkVariable sync happens async via OnValueChanged)
        playerPerformingAction.playerCombatManager.currentWeaponBeingUsed = weaponPerformingAction;

        if (!playerPerformingAction.IsOwner)
            return;

        // CHECK FOR STOPS
        if (playerPerformingAction.playerNetworkManager.currentStamina.Value <= 0)
            return;

        if (!playerPerformingAction.isGrounded)
            return;

        // Sync the weapon ID (triggers OnCurrentWeaponBeingUsedIDChange on all clients, including owner)
        playerPerformingAction.playerNetworkManager.currentWeaponBeingUsed.Value = weaponPerformingAction.itemID;

        // Set attack type for stamina calculation
        playerPerformingAction.playerCombatManager.currentAttackType = AttackType.LightAttack01;

        // Drain stamina (this will fire your debugs and deduct)
        playerPerformingAction.playerCombatManager.DrainStaminaBasedOnAttack();

        PerformHeavyAttack(playerPerformingAction, weaponPerformingAction);
    }

    private void PerformHeavyAttack(PlayerManager playerPerformingAction, WeaponItem weaponPerformingAction)
    {
        if (playerPerformingAction.playerNetworkManager.isUsingRightHand.Value)
        {
            playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(AttackType.HeavyAttack01, heavy_Attack_01, true);
        }
        if (playerPerformingAction.playerNetworkManager.isUsingLeftHand.Value)
        {
            //playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(light_Attack_01, true);
        }
    }
}
