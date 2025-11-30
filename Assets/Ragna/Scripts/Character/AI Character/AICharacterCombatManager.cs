using UnityEngine;

public class AICharacterCombatManager : CharacterCombatManager
{
    [Header("Action Recovery")]
    public float actionRecoveryTimer = 0;
     
    [Header("Target Information")]
    public float distanceFromTarget;
    public float viewableAngle;
    public Vector3 targetsDirection;

    [Header("Detection")] 
    public float detectionRadius = 15;
    public float minimumFOV = -35;
    public float maximumFOV = 35;

    [Header("Attack Rotation Speed")]
    public float attackRotationSpeed = 25;

    [SerializeField] AI_Enemy_DamageCollider rightHandDamageCollider;
    [SerializeField] AI_Enemy_DamageCollider leftHandDamageCollider;

    [Header("Damage")]
    [SerializeField] int baseDamage = 25;
    [SerializeField] float attack01DamageMultiplier = 1f;
    [SerializeField] float attack02DamageMultiplier = 1.4f;


    protected override void Awake()
    {
        base.Awake();

        lockOnTransform = GetComponentInChildren<LockOnTransform>().transform;
    }

    public void SetAttack01Damage()
    {
        rightHandDamageCollider.physicalDamage = baseDamage * attack01DamageMultiplier;
        leftHandDamageCollider.physicalDamage = baseDamage * attack01DamageMultiplier;
    }

    public void SetAttack02Damage()
    {
        rightHandDamageCollider.physicalDamage = baseDamage * attack02DamageMultiplier;
        leftHandDamageCollider.physicalDamage = baseDamage * attack02DamageMultiplier;
    }

    public void OpenRightHandDamage()
    {
        rightHandDamageCollider.EnableDamageCollider();
    }

    public void CloseRightHandDamage()
    {
        rightHandDamageCollider.DisableDamageCollider();
    }

    public void OpenLeftHandDamage()
    {
        leftHandDamageCollider.EnableDamageCollider();
    }

    public void FindATargetViaLineOfOfSight(AICharacterManager aiCharacter)
    {
        if (currentTarget != null)
            return;

        Collider[] colliders = Physics.OverlapSphere(aiCharacter.transform.position, detectionRadius, WorldUtilityManager.Instance.GetCharacterLayers());

        for (int i = 0; i < colliders.Length; i++)
        {
            CharacterManager targetCharacter = colliders[i].transform.GetComponent<CharacterManager>();

            if (targetCharacter == null)
                continue;

            if (targetCharacter == aiCharacter)
                continue;

            /*if (targetCharacter.isDead.Value)
                continue;*/

            if (WorldUtilityManager.Instance.CanIDamageThisTarget(aiCharacter.characterGroup, targetCharacter.characterGroup))
            {
                Vector3 targetDirection = targetCharacter.transform.position - aiCharacter.transform.position;
                float angleOfPotentialTarget = Vector3.Angle(targetDirection, aiCharacter.transform.forward);

                if (angleOfPotentialTarget > minimumFOV && angleOfPotentialTarget < maximumFOV)
                {
                    if (Physics.Linecast(
                        aiCharacter.characterCombatManager.lockOnTransform.position,
                        targetCharacter.characterCombatManager.lockOnTransform.position,
                        WorldUtilityManager.Instance.GetEnviroLayers()))
                    {
                        Debug.DrawLine(aiCharacter.characterCombatManager.lockOnTransform.position, targetCharacter.characterCombatManager.lockOnTransform.position);
                    }
                    else
                    {
                        targetDirection = targetCharacter.transform.position - transform.position;
                        viewableAngle = WorldUtilityManager.Instance.GetAngleOfTarget(transform, targetDirection);
                        aiCharacter.characterCombatManager.SetTarget(targetCharacter);
                        //PivotTowardsTarget(aiCharacter);
                    }
                }
            }
        }
    }
    

    public void HandleActionRecovery(AICharacterManager aiCharacter)
    {
        if(actionRecoveryTimer > 0)
        {
            if(!aiCharacter.isPerformingAction)
            {
                actionRecoveryTimer -= Time.deltaTime;
            }
        }
    }

    public void RotateTowardsAgent(AICharacterManager aiCharacter)
    {
        if(aiCharacter.aiCharacterNetworkManager.isMoving.Value)
        {
            aiCharacter.transform.rotation = aiCharacter.navMeshAgent.transform.rotation;
        }
    }

    public void RotateTowardsTargetWhilstAttacking(AICharacterManager aiCharacter)
    {
        if(currentTarget == null)
            return;

        if(!aiCharacter.canRotate)
            return;

        if(!aiCharacter.isPerformingAction)
            return;

        Vector3 targetDirection = currentTarget.transform.position - aiCharacter.transform.position;
        targetDirection.y = 0;
        targetDirection.Normalize();

        if(targetDirection == Vector3.zero)
            targetDirection = aiCharacter.transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        aiCharacter.transform.rotation = Quaternion.Slerp(aiCharacter.transform.rotation, targetRotation, attackRotationSpeed * Time.deltaTime);
    }
}
