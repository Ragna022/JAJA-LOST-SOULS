using System.Collections.Generic;
using Unity.VisualScripting;
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

        if(!aiCharacter.aiCharacterNetworkManager.isMoving.Value)
        {
            /*if(aiCharacter.aiCharacterCombatManager.viewableAngle < -30 || aiCharacter.aiCharacterCombatManager.viewableAngle > 30)
                aiCharacter.aiCharacterCombatManager.PivotTowardsTarget(aiCharacter);*/
        }

        aiCharacter.aiCharacterCombatManager.RotateTowardsAgent(aiCharacter);

        if(aiCharacter.aiCharacterCombatManager.currentTarget == null)
            return SwitchState(aiCharacter, aiCharacter.idle);

        if(!hasAttack)
        {
            GetNewAttack(aiCharacter);
        }
        else
        {
            Debug.Log("CombatStance: Has attack, switching to AttackState");
            aiCharacter.attack.currentAttack = choosenAttack;
            return SwitchState(aiCharacter, aiCharacter.attack);
        }

        if(aiCharacter.aiCharacterCombatManager.distanceFromTarget > maximumEngagementDistance)
            return SwitchState(aiCharacter, aiCharacter.pursueTarget);

        NavMeshPath path = new NavMeshPath();
        aiCharacter.navMeshAgent.CalculatePath(aiCharacter.aiCharacterCombatManager.currentTarget.transform.position, path);
        aiCharacter.navMeshAgent.SetPath(path);

        return this;
    }

    protected virtual void GetNewAttack(AICharacterManager aiCharacter)
    {
        /*Debug.Log("=== GetNewAttack Called ===");
        Debug.Log("Current Distance: " + aiCharacter.aiCharacterCombatManager.distanceFromTarget);
        Debug.Log("Current Angle: " + aiCharacter.aiCharacterCombatManager.viewableAngle);
        Debug.Log("Available attacks in list: " + aiCharacterAttacks.Count);*/

        potentialAttacks = new List<AICharacterAttackAction>();

        foreach(var potentialAttack in aiCharacterAttacks)
        {
            /*Debug.Log("Checking attack: " + potentialAttack.name);
            Debug.Log("  Min Distance: " + potentialAttack.minimumAttackDistance + " | Max Distance: " + potentialAttack.maximumAttackDistance);
            Debug.Log("  Min Angle: " + potentialAttack.minimumAttackAngle + " | Max Angle: " + potentialAttack.maximumAttackAngle);*/

            // Check minimum distance
            if(potentialAttack.minimumAttackDistance > aiCharacter.aiCharacterCombatManager.distanceFromTarget)
            {
                Debug.Log("  ❌ FAILED: Distance too close");
                continue;
            }

            // Check maximum distance
            if(potentialAttack.maximumAttackDistance < aiCharacter.aiCharacterCombatManager.distanceFromTarget)
            {
                Debug.Log("  ❌ FAILED: Distance too far");
                continue;
            }

            // Check minimum angle
            if(potentialAttack.minimumAttackAngle > aiCharacter.aiCharacterCombatManager.viewableAngle)
            {
                Debug.Log("  ❌ FAILED: Angle too low");
                continue;
            }

            // FIXED: Was checking maximumAttackDistance instead of maximumAttackAngle
            if(potentialAttack.maximumAttackAngle < aiCharacter.aiCharacterCombatManager.viewableAngle)
            {
                Debug.Log("  ❌ FAILED: Angle too high");
                continue;
            }

            // CRITICAL FIX: Actually add the attack to the list!
            Debug.Log("  ✅ PASSED: Attack added to potential attacks!");
            potentialAttacks.Add(potentialAttack);
        }

        Debug.Log("Total potential attacks after filtering: " + potentialAttacks.Count);

        if(potentialAttacks.Count <= 0)
        {
            Debug.LogWarning("No valid attacks found!");
            return;
        }

        var totalWeight = 0;

        foreach(var attack in potentialAttacks)
        {
            totalWeight += attack.attackWeight;
        }

        Debug.Log("Total weight of attacks: " + totalWeight);

        var randomWeightValue = Random.Range(1, totalWeight + 1);
        var processedWeight = 0;

        foreach(var attack in potentialAttacks)
        {
            processedWeight += attack.attackWeight;

            if(randomWeightValue <= processedWeight)
            {
                choosenAttack = attack;
                previousAttack = choosenAttack;
                hasAttack = true;
                Debug.Log("✅ Attack chosen: " + attack.name);
                return; // Exit after choosing
            }
        }
    }

    protected virtual bool RollForOutcomeChance(int outcomeChance)
    {
        bool outcomeWillBePerformed = false;

        int randomPercentage = Random.Range(0, 100);

        if(randomPercentage < outcomeChance)
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