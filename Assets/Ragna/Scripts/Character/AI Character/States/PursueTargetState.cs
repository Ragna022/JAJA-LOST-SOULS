using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "A.I/States/Pursue Target")]
public class PursueTargetState : AIState
{
    public override AIState Tick(AICharacterManager aiCharacter)
    {
        if (aiCharacter.isPerformingAction)
            return this;

        if (aiCharacter.characterCombatManager.currentTarget == null)
            return SwitchState(aiCharacter, aiCharacter.idle);

        if (!aiCharacter.navMeshAgent.enabled)
            aiCharacter.navMeshAgent.enabled = true;

        // FIX: Ensure stopping distance is reset for chasing
        // If it was set to 0.2 in combat, we set it back to ~2.0 here so it stops naturally near the player
        if (aiCharacter.navMeshAgent.stoppingDistance < 2.0f) 
        {
            aiCharacter.navMeshAgent.stoppingDistance = 2.0f; 
        }

        aiCharacter.aiCharacterLocomotionManager.RotateTowardsAgent(aiCharacter);

        // Switch to Combat Stance if close enough
        if (aiCharacter.aiCharacterCombatManager.distanceFromTarget <= aiCharacter.combatStance.maximumEngagementDistance)
            return SwitchState(aiCharacter, aiCharacter.combatStance);

        // Also switch if we physically reached the NavMesh stopping distance
        if (aiCharacter.aiCharacterCombatManager.distanceFromTarget <= aiCharacter.navMeshAgent.stoppingDistance)
            return SwitchState(aiCharacter, aiCharacter.combatStance);

        NavMeshPath path = new NavMeshPath();
        aiCharacter.navMeshAgent.CalculatePath(aiCharacter.aiCharacterCombatManager.currentTarget.transform.position, path);
        aiCharacter.navMeshAgent.SetPath(path);

        return this;
    }
}