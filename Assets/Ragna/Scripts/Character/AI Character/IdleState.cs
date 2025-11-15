using UnityEngine;

[CreateAssetMenu(menuName = "A.I/States/Idle")]
public class IdleState : AIState
{
    public override AIState Tick(AICharacterManager aiCharacter)
    {
        if (aiCharacter.characterCombatManager.currentTarget != null)
        {
            // RETURN THE PURSUE TARGET STATE (CHANGE THE STATE TO THE PURSUE TARGET STATE)
            Debug.Log("WE HAVE A TARGET");
            return this; 
        }
        else
        {
            aiCharacter.aiCharacterCombatManager.FindATargetViaLineOfOfSight(aiCharacter);
            Debug.Log("SEARCHIGN FOR A TARGET");
            return this;
        }
         
    }
}
