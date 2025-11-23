using UnityEngine;

[CreateAssetMenu(menuName = "A.I/Actions/Attack")]
public class AICharacterAttackAction : ScriptableObject
{
    [Header("Attack")]
    [SerializeField] private string attackAnimation;

    [Header("Combo Action")]
    public AICharacterAttackAction comboAction;

    [Header("Action Values")]
    [SerializeField] AttackType attackType;
    public int attackWeight = 50;
    public float actionRecoveryTime = 1.5f;
    public float minimumAttackAngle = -35f;
    public float maximumAttackAngle = 35f;
    public float minimumAttackDistance = 0;
    public float maximumAttackDistance = 2;

    public void AttemptToPerformAction(AICharacterManager aiCharacter)
    {
        Debug.Log("=== AttemptToPerformAction CALLED ===");
        Debug.Log("Attack Animation: " + attackAnimation);
        Debug.Log("ScriptableObject Name: " + name);
        Debug.Log("Attack Type: " + attackType);
        Debug.Log("Recovery Time: " + actionRecoveryTime);
        
        // Use the AI-specific attack animation method
        aiCharacter.characterAnimatorManager.PlayTargetAttackActionAnimationForAI(
            attackType,      // AttackType
            attackAnimation, // string targetAnimation
            true,            // bool isPerformingAction
            true,            // bool applyRootMotion
            true,            // bool canRotate
            false            // bool canMove
        );
    }
}