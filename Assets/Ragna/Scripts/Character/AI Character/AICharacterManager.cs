using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class AICharacterManager : CharacterManager
{
    [HideInInspector] public AICharacterNetworkManager aiCharacterNetworkManager;
    [HideInInspector] public AICharacterCombatManager aiCharacterCombatManager;
    [HideInInspector] public AICharacterLocomotionManager aiCharacterLocomotionManager;

    [Header("Navmesh Agent")]
    public NavMeshAgent navMeshAgent;

    [Header("Current States")]
    [SerializeField] AIState currentState;

    [Header("States")]
    public IdleState idle;
    public PursueTargetState pursueTarget;
    public CombatStanceState combatStance;
    public AttackState attack;

    protected override void Awake()
    {
        base.Awake();

        aiCharacterCombatManager = GetComponent<AICharacterCombatManager>();
        aiCharacterNetworkManager = GetComponent<AICharacterNetworkManager>();
        aiCharacterLocomotionManager = GetComponent<AICharacterLocomotionManager>();

        navMeshAgent = GetComponentInChildren<NavMeshAgent>();

        // Ensure CharacterController is enabled first
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // CRITICAL: Configure NavMeshAgent to NOT control position/rotation
        // We only use it for pathfinding data
        if (navMeshAgent != null)
        {
            navMeshAgent.updatePosition = false; // CharacterController handles position
            navMeshAgent.updateRotation = false; // We handle rotation manually
            navMeshAgent.updateUpAxis = false;   // CharacterController handles this
        }

        // FIXED: Instantiate ALL states
        idle = Instantiate(idle);
        pursueTarget = Instantiate(pursueTarget);
        combatStance = Instantiate(combatStance);
        attack = Instantiate(attack);

        currentState = idle;
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Additional safety check after all components are initialized
        if (characterController != null && !characterController.enabled)
        {
            characterController.enabled = true;
        }
    }

    protected override void Update()
    {
        base.Update();
        
        aiCharacterCombatManager.HandleActionRecovery(this);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if(IsOwner)
            ProcessStateMachine();
    }

    private void ProcessStateMachine()
    {
        AIState nextState = currentState?.Tick(this);

        if (nextState != null)
        {
            currentState = nextState;
        }

        //
        navMeshAgent.transform.localPosition = Vector3.zero;
        navMeshAgent.transform.localRotation = Quaternion.identity;

        if(aiCharacterCombatManager.currentTarget != null)
        {
            aiCharacterCombatManager.targetsDirection = aiCharacterCombatManager.currentTarget.transform.position - transform.position;
            aiCharacterCombatManager.viewableAngle = WorldUtilityManager.Instance.GetAngleOfTarget(transform, aiCharacterCombatManager.targetsDirection);
            aiCharacterCombatManager.distanceFromTarget = Vector3.Distance(transform.position, aiCharacterCombatManager.currentTarget.transform.position);
        }

        // Update isMoving based on NavMeshAgent path status
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            // Check if agent has a path and is beyond stopping distance
            if (navMeshAgent.hasPath && !navMeshAgent.pathPending)
            {
                float remainingDistance = navMeshAgent.remainingDistance;
                
                if (remainingDistance > navMeshAgent.stoppingDistance)
                {
                    aiCharacterNetworkManager.isMoving.Value = true;
                }
                else
                {
                    aiCharacterNetworkManager.isMoving.Value = false;
                }
            }
            else
            {
                aiCharacterNetworkManager.isMoving.Value = false;
            }
        }
        else 
        {
            aiCharacterNetworkManager.isMoving.Value = false;
        }
    }

    // Helper method to set destination (call from AI states)
    public void SetDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(destination);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if(characterUIManager.hasFloatingHPBar)
            characterNetworkManager.currentHealth.OnValueChanged += characterUIManager.OnHPChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if(characterUIManager.hasFloatingHPBar)
            characterNetworkManager.currentHealth.OnValueChanged -= characterUIManager.OnHPChanged;
    }
}