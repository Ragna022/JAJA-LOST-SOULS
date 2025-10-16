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
        // This method would typically update animator parameters.
        // Implementation depends on the specific animator setup.

        float horizontalAmount = horizontalMovement;
        float verticalAmount = verticalMovement;

        if (isSprinting)
        {
            verticalAmount = 2;
        }
        character.animator.SetFloat(horizontal, horizontalAmount, 0.1f, Time.deltaTime);
        character.animator.SetFloat(vertical, verticalAmount, 0.1f, Time.deltaTime);
        

        
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
        // CAN BE USED TO STOP CHARACTER FROM ATTEMPTING NEW ACTIONS
        // E.G IF YOU GET DAMAGED, AND BEGIN PERFORMING A DAMAGE ANIMATION
        // THIS FLAG WILL TURN TRUE IF YOU ARE STUNNED
        // WE CAN THEN CHECK FOR THIS BEFORE ATTEMPTING NEW ACTIONS
        character.isPerformingAction = isPerformingAction;
        character.canRotate = canRotate;
        character.canMove = canMove;

        // TELL THE SERVER /HOST TO PLAY THE ANIMATION, AMD TO PLAY THAT ANIMATION FOR EVERYBODY ELSE PRESENT
        character.characterNetworkManager.NotifyTheServerOfActionAnimationServerRpc(NetworkManager.Singleton.LocalClientId, targetAnimation, applyRootMotion);
    }

    public virtual void PlayTargetAttackActionAnimation(AttackType attackType,
        string targetAnimation,
        bool isPerformingAction,
        bool applyRootMotion = true,
        bool canRotate = false,
        bool canMove = false)
    {
        // KEEP TRACK OF LAST ATTACK PERFORMED (FOR COMBOS)
        // KEEP TRACK OF CURRENT ATTACK TYPE (LIGHT, HEAVY, ETC)
        // UPDATE ANIMATION SET TO CURRENT WEAPONS ANIMATIONS
        // DECIDE IF OUR ATTACK CAN BE PARRIED
        // TELL THE NETWORK OUR "ISATTACKING" FLAG IS ACTIVE (FOR COUNTER DAMAGE ETC)
        character.characterCombatManager.currentAttackType = attackType;
        character.characterCombatManager.lastAttackAnimationPerformed = targetAnimation;
        character.applyRootMotion = applyRootMotion;
        character.animator.CrossFade(targetAnimation, 0.2f);
        character.isPerformingAction = isPerformingAction;
        character.canRotate = canRotate;
        character.canMove = canMove;

        // TELL THE SERVER /HOST TO PLAY THE ANIMATION, AMD TO PLAY THAT ANIMATION FOR EVERYBODY ELSE PRESENT
        character.characterNetworkManager.NotifyTheServerOfAttackActionAnimationServerRpc(NetworkManager.Singleton.LocalClientId, targetAnimation, applyRootMotion);
    }

    public virtual void EnableCanDoCombo()
    {
        
    }
    
    public virtual void DisableCanDoCombo()
    {
        
    }
}