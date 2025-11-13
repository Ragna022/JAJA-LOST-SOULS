using UnityEngine;

public class AIState : ScriptableObject
{
    public virtual AIState Tick(AICharacterManager aICharacterManager)
    {
        return this;
    }
}
