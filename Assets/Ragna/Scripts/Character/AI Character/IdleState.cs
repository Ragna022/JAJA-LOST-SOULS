using UnityEngine;

[CreateAssetMenu(menuName = "A.I/States/Idle")]
public class IdleState : AIState
{
    public override AIState Tick(AICharacterManager aICharacter)
    {
        Debug.Log("WE HAVE A TARGET");
        /*/if (aICharacter.characterCombatManager.currentTarget != null)
        {
            // RETURN THE PURSUE TARGET STATE
            Debug.Log("WE HAVE A TARGET");
        }
        else
        {
            Debug.Log("WE HAVE NO TARGET");
        }*/
        return this;
    }
}
