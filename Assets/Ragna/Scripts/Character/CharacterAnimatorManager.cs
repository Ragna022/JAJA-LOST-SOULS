using Unity.Netcode;
using UnityEngine;

public class CharacterAnimatorManager : MonoBehaviour
{
    CharacterManager character;
    int vertical;
    int horizontal;

    [Header("Damage Animations")]
    public string hit_Forward = "hit_front";
    public string hit_Backwards = "hit_back";
    public string hit_Left = "hit_left";
    public string hit_Right = "hit_right";

    protected virtual void Awake()
    {
        character = GetComponent<CharacterManager>();

        vertical = Animator.StringToHash("Vertical");
        horizontal = Animator.StringToHash("Horizontal");
    }

    public void UpdateAnimatorMovementParameters(float horizontalMovement, float verticalMovement, bool isSprinting)
    {
        float snappedHorizontal;
        float snappedVertical;

        if (horizontalMovement > 0 && horizontalMovement <= 0.5f)
        {
            snappedHorizontal = 0.5f;
        }
        else if (horizontalMovement > 0.5f && horizontalMovement <= 1)
        {
            snappedHorizontal = 1;
        }
        else if (horizontalMovement < 0 && horizontalMovement >= -0.5f)
        {
            snappedHorizontal = -0.5f;
        }
        else if (horizontalMovement < -0.5f && horizontalMovement >= -1)
        {
            snappedHorizontal = -1;
        }
        else
        {
            snappedHorizontal = 0;
        }

        if (verticalMovement > 0 && verticalMovement <= 0.5f)
        {
            snappedVertical = 0.5f;
        }
        else if (verticalMovement > 0.5f && verticalMovement <= 1)
        {
            snappedVertical = 1;
        }
        else if (verticalMovement < 0 && verticalMovement >= -0.5f)
        {
            snappedVertical = -0.5f;
        }
        else if (verticalMovement < -0.5f && verticalMovement >= -1)
        {
            snappedVertical = -1;
        }
        else
        {
            snappedVertical = 0;
        }

        if (isSprinting)
        {
            snappedVertical = 2;
        }
        
        character.animator.SetFloat(horizontal, snappedHorizontal, 0.1f, Time.deltaTime);
        character.animator.SetFloat(vertical, snappedVertical, 0.1f, Time.deltaTime);
    }

    public virtual void PlayTargetActionAnimation(
        string targetAnimation,
        bool isPerformingAction,
        bool applyRootMotion = true,
        bool canRotate = false,
        bool canMove = false)
    {
        Debug.Log($"PlayTargetActionAnimation called for '{targetAnimation}' on IsOwner={character.IsOwner}, LocalClientId={NetworkManager.Singleton.LocalClientId}");

        character.applyRootMotion = applyRootMotion;
        character.animator.CrossFade(targetAnimation, 0.2f);
        character.isPerformingAction = isPerformingAction;
        character.canRotate = canRotate;
        character.canMove = canMove;

        character.characterNetworkManager.NotifyTheServerOfActionAnimationServerRpc(NetworkManager.Singleton.LocalClientId, targetAnimation, applyRootMotion);
    }

    public virtual void PlayTargetAttackActionAnimation(WeaponItem weapon,
        AttackType attackType,
        string targetAnimation,
        bool isPerformingAction,
        bool applyRootMotion = true,
        bool canRotate = false,
        bool canMove = false)
    {
        character.characterCombatManager.currentAttackType = attackType;
        character.characterCombatManager.lastAttackAnimationPerformed = targetAnimation;
        UpdateAnimatorController(weapon.weaponAnimator);
        character.applyRootMotion = applyRootMotion;
        character.animator.CrossFade(targetAnimation, 0.2f);
        character.isPerformingAction = isPerformingAction;
        character.canRotate = canRotate;
        character.canMove = canMove;

        character.characterNetworkManager.NotifyTheServerOfAttackActionAnimationServerRpc(NetworkManager.Singleton.LocalClientId, targetAnimation, applyRootMotion);
    }

    // NEW METHOD: For AI attacks without weapon requirements
    public virtual void PlayTargetAttackActionAnimationForAI(
        AttackType attackType,
        string targetAnimation,
        bool isPerformingAction,
        bool applyRootMotion = true,
        bool canRotate = false,
        bool canMove = false)
    {
        Debug.Log($"[AI ATTACK] Playing animation: {targetAnimation}, AttackType: {attackType}");

        // Track attack info
        character.characterCombatManager.currentAttackType = attackType;
        character.characterCombatManager.lastAttackAnimationPerformed = targetAnimation;
        
        character.applyRootMotion = applyRootMotion;
        character.animator.CrossFade(targetAnimation, 0.2f);
        character.isPerformingAction = isPerformingAction;
        character.canRotate = canRotate;
        character.canMove = canMove;

        // Notify network
        character.characterNetworkManager.NotifyTheServerOfAttackActionAnimationServerRpc(
            NetworkManager.Singleton.LocalClientId, 
            targetAnimation, 
            applyRootMotion
        );
    }

    public virtual void EnableCanDoCombo()
    {
        
    }

    public virtual void DisableCanDoCombo()
    {

    }
    
    public void UpdateAnimatorController(AnimatorOverrideController weaponController)
    {
        character.animator.runtimeAnimatorController = weaponController;
    }
}