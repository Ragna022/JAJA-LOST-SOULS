using UnityEngine;

[CreateAssetMenu(menuName = "Character Actions/Weapon Actions/Light Attack Action")]
public class LightAttackWeaponItemAction : WeaponItemAction
{
    [SerializeField] string light_Attack_01 = "LightAttack";
    public override void AttemptToPerformAction(PlayerManager playerPerformingAction, WeaponItem weaponPerformingAction)
    {
        base.AttemptToPerformAction(playerPerformingAction, weaponPerformingAction);

        if (!playerPerformingAction.IsOwner)
            return;

        // CHECK FOR STOPS
        if (playerPerformingAction.playerNetworkManager.currentStamina.Value <= 0)
            return;

        if (!playerPerformingAction.isGrounded)
            return;

        PerformLightAttack(playerPerformingAction, weaponPerformingAction);
    }

    private void PerformLightAttack(PlayerManager playerPerformingAction, WeaponItem weaponPerformingAction)
    {
        if (playerPerformingAction.playerNetworkManager.isUsingRightHand.Value)
        {
            playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(light_Attack_01, true);
        }
        if (playerPerformingAction.playerNetworkManager.isUsingLeftHand.Value)
        {
            //playerPerformingAction.playerAnimatorManager.PlayTargetAttackActionAnimation(light_Attack_01, true);
        }
    }
}
