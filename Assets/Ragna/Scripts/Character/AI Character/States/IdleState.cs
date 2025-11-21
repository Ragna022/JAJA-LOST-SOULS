using UnityEngine;

[CreateAssetMenu(menuName = "A.I/States/Idle")]
public class IdleState : AIState
{
    public override AIState Tick(AICharacterManager aiCharacter)
    {
        if (aiCharacter.characterCombatManager.currentTarget != null)
        {
            // RETURN THE PURSUE TARGET STATE (CHANGE THE STATE TO THE PURSUE TARGET STATE)
            return SwitchState(aiCharacter, aiCharacter.pursueTarget);
        }
        else
        {
            aiCharacter.aiCharacterCombatManager.FindATargetViaLineOfOfSight(aiCharacter);
            return this;
        }
         
    }
}
