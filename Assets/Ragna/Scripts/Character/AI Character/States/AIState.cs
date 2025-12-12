using UnityEngine;

public class AIState : ScriptableObject
{
    public virtual AIState Tick(AICharacterManager aiCharacter)
    {
        return this;
    }

    protected virtual AIState SwitchState(AICharacterManager aiCharacter, AIState newState)
    {
        ResetStateFlags(aiCharacter);
        return newState;
    }

    protected virtual void ResetStateFlags(AICharacterManager aiCharacter)
    {
        // Reset flags here in overrides
    }

    // FIX: Added this method so CombatStanceState can override it
    protected virtual bool RollForOutcomeChance(int outcomeChance)
    {
        bool outcomeWillBePerformed = false;
        int randomPercentage = Random.Range(0, 100);

        if (randomPercentage < outcomeChance)
            outcomeWillBePerformed = true;

        return outcomeWillBePerformed;
    }
}