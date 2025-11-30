using UnityEngine;

public class PlayerAnimatorManager : CharacterAnimatorManager
{
    PlayerManager player;

    protected override void Awake()
    {
        base.Awake();
        player = GetComponent<PlayerManager>();
    }

    private void OnAnimatorMove()
    {
        // 1. Check if Root Motion is allowed
        if (player.applyRootMotion)
        {
            // 2. SAFETY CHECK: Ensure the controller exists and is ENABLED
            // This prevents the "Move called on inactive controller" error
            if (player.characterController != null && player.characterController.enabled)
            {
                Vector3 velocity = player.animator.deltaPosition;
                player.characterController.Move(velocity);
                player.transform.rotation *= player.animator.deltaRotation;
            }
        }
    }

    // ANIMATION EVENT CALLS

    public override void EnableCanDoCombo()
    {
        if (player.playerNetworkManager.isUsingRightHand.Value)
        {
            player.playerCombatManager.canComboWithMainHandWeapon = true;
        }
        else
        {
            // Handle off-hand logic if needed
        }
    }

    public override void DisableCanDoCombo()
    {
        player.playerCombatManager.canComboWithMainHandWeapon = false;
    }
}