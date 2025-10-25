using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Character Actions/Weapon Actions/Light Attack Action")]
public class LightAttackWeaponItemAction : WeaponItemAction
{
    [SerializeField] string light_Attack_01 = "LightAttack";
    [SerializeField] string light_Attack_02 = "LightComboAttack";

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

        PerformLightAttack(playerPerformingAction, weaponPerformingAction);
    }

    private void PerformLightAttack(PlayerManager playerPerformingAction, WeaponItem weaponPerformingAction)
    {
        // IF WE ARE ATTACKING  CURRENTLY, AND WE CAN DO COMBO, PERFORM THE COMBO ATTACK
        if (playerPerformingAction.playerCombatManager.canComboWithMainHandWeapon && playerPerformingAction.isPerformingAction)
        {
            playerPerformingAction.playerCombatManager.canComboWithMainHandWeapon = false;

            // PERFORM AN ATTACK BASED ON THE PREVIOUS ATTACK WE JUST PLAYED
            if (playerPerformingAction.characterCombatManager.lastAttackAnimationPerformed == light_Attack_01)
            {
                playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(weaponPerformingAction, AttackType.LightAttack02, light_Attack_02, true);
            }
            else
            {
                playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(weaponPerformingAction,AttackType.LightAttack01, light_Attack_01, true);
            }
        }
        // OTHERWISE, JUST PERFORM A REGULAR ATTACK
        else
        {
            playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(weaponPerformingAction, AttackType.LightAttack01, light_Attack_01, true);
        }
    }
}