using UnityEngine;

[CreateAssetMenu(menuName = "A.I/States/Idle")]
public class IdleState : AIState
{
    public override AIState Tick(AICharacterManager aICharacter)
    {
        if (aICharacter.characterCombatManager.currentTarget != null)
        {
            // RETURN THE PURSUE TARGET STATE (CHANGE THE STATE TO THE PURSUE TARGET STATE)
            Debug.Log("WE HAVE A TARGET");
            return this;
        }
        else
        {
            Debug.Log("SEARCHIGN FOR A TARGET");
            aICharacter.aICharacterCombatManager.FindATargetViaLineOfOfSight(aICharacter);
            return this;
        }
        
    }
}
