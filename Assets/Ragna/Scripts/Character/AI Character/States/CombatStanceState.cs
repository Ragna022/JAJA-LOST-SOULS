using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "A.I/States/Combat Stance")]
public class CombatStanceState : AIState
{
    [Header("Attacks")]
    public List<AICharacterAttackAction> aiCharacterAttacks;
    protected List<AICharacterAttackAction> potentialAttacks;
    private AICharacterAttackAction choosenAttack;
    private AICharacterAttackAction previousAttack;
    protected bool hasAttack = false;

    [Header("Combo")]
    [SerializeField] protected bool canPerformCombo = false;
    [SerializeField] protected int chanceToPerformCombo = 25;
    protected bool hasRolledForComboChance = false;

    [Header("Engagement Distance")]
    [SerializeField] public float maximumEngagementDistance = 5;

    public override AIState Tick(AICharacterManager aiCharacter)
    {
        if (aiCharacter.isPerformingAction)
            return this;

        if (!aiCharacter.navMeshAgent.enabled)
            aiCharacter.navMeshAgent.enabled = true;

        // Optional: Pivot logic if not moving
        if (!aiCharacter.aiCharacterNetworkManager.isMoving.Value)
        {
            // aiCharacter.aiCharacterCombatManager.PivotTowardsTarget(aiCharacter);
        }

        aiCharacter.aiCharacterCombatManager.RotateTowardsAgent(aiCharacter);

        if (aiCharacter.aiCharacterCombatManager.currentTarget == null)
            return SwitchState(aiCharacter, aiCharacter.idle);

        // 1. TRY TO GET NEW ATTACK
        if (!hasAttack)
        {
            GetNewAttack(aiCharacter);
        }

        // 2. IF WE HAVE ATTACK, SWITCH STATE
        if (hasAttack)
        {
            // Reset stopping distance to default (e.g. 1.0f or 2.0f) before attacking
            // This ensures they don't get stuck trying to hug the player next time
            aiCharacter.navMeshAgent.stoppingDistance = 1.0f; 

            Debug.Log("CombatStance: Has attack, switching to AttackState");
            aiCharacter.attack.currentAttack = choosenAttack;
            return SwitchState(aiCharacter, aiCharacter.attack);
        }

        // 3. CHECK IF WE SHOULD GO BACK TO PURSUE (Target ran away)
        if (aiCharacter.aiCharacterCombatManager.distanceFromTarget > maximumEngagementDistance)
        {
            return SwitchState(aiCharacter, aiCharacter.pursueTarget);
        }

        // 4. MOVEMENT LOGIC (The Fix for "Waiting")
        // If we are here, we are in Combat Stance but have NO valid attack (likely too far away).
        // We must move closer to get into range.
        
        NavMeshPath path = new NavMeshPath();
        aiCharacter.navMeshAgent.CalculatePath(aiCharacter.aiCharacterCombatManager.currentTarget.transform.position, path);
        aiCharacter.navMeshAgent.SetPath(path);

        // CRITICAL FIX: Reduce stopping distance to almost zero so AI walks right up to the player
        // to get into attack range. If we don't do this, the AI stops at "StoppingDistance" (e.g. 2m)
        // but needs to be at 1.5m to attack, causing the deadlock.
        if (aiCharacter.navMeshAgent.stoppingDistance > 0.2f)
        {
            aiCharacter.navMeshAgent.stoppingDistance = 0.2f; 
        }

        return this;
    }

    protected virtual void GetNewAttack(AICharacterManager aiCharacter)
    {
        potentialAttacks = new List<AICharacterAttackAction>();

        foreach (var potentialAttack in aiCharacterAttacks)
        {
            // Distance Checks
            if (potentialAttack.minimumAttackDistance > aiCharacter.aiCharacterCombatManager.distanceFromTarget)
                continue;

            if (potentialAttack.maximumAttackDistance < aiCharacter.aiCharacterCombatManager.distanceFromTarget)
                continue;

            // Angle Checks
            if (potentialAttack.minimumAttackAngle > aiCharacter.aiCharacterCombatManager.viewableAngle)
                continue;

            if (potentialAttack.maximumAttackAngle < aiCharacter.aiCharacterCombatManager.viewableAngle)
                continue;

            potentialAttacks.Add(potentialAttack);
        }

        if (potentialAttacks.Count <= 0)
            return;

        var totalWeight = 0;
        foreach (var attack in potentialAttacks)
        {
            totalWeight += attack.attackWeight;
        }

        var randomWeightValue = Random.Range(1, totalWeight + 1);
        var processedWeight = 0;

        foreach (var attack in potentialAttacks)
        {
            processedWeight += attack.attackWeight;

            if (randomWeightValue <= processedWeight)
            {
                choosenAttack = attack;
                previousAttack = choosenAttack;
                hasAttack = true;
                return;
            }
        }
    }

    // This now works because AIState has the virtual method
    protected override bool RollForOutcomeChance(int outcomeChance)
    {
        bool outcomeWillBePerformed = false;
        int randomPercentage = Random.Range(0, 100);

        if (randomPercentage < outcomeChance)
            outcomeWillBePerformed = true;

        return outcomeWillBePerformed;
    }

    protected override void ResetStateFlags(AICharacterManager aiCharacter)
    {
        base.ResetStateFlags(aiCharacter);

        hasAttack = false;
        hasRolledForComboChance = false;
    }
}