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
        
        // CRITICAL: Don't process action recovery if dead
        if (!isDead)
        {
            aiCharacterCombatManager.HandleActionRecovery(this);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if(IsOwner)
            ProcessStateMachine();
    }

    private void ProcessStateMachine()
    {
        // *** CRITICAL: Stop ALL AI processing if dead ***
        if (isDead)
        {
            Debug.Log($"[AICharacterManager] AI is DEAD, stopping state machine for {gameObject.name}");
            
            // Stop the NavMeshAgent
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
            
            // Set isMoving to false
            aiCharacterNetworkManager.isMoving.Value = false;
            
            return; // Exit immediately - no more AI processing
        }

        // Normal AI state machine processing
        AIState nextState = currentState?.Tick(this);

        if (nextState != null)
        {
            currentState = nextState;
        }

        // Keep NavMeshAgent transform synced
        navMeshAgent.transform.localPosition = Vector3.zero;
        navMeshAgent.transform.localRotation = Quaternion.identity;

        // Update target tracking
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
        // Don't set destination if dead
        if (isDead)
            return;
            
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.SetDestination(destination);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if(characterUIManager != null && characterUIManager.hasFloatingHPBar)
        {
            characterNetworkManager.currentHealth.OnValueChanged += characterUIManager.OnHPChanged;
            Debug.Log($"[AICharacterManager] HP bar subscription enabled for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[AICharacterManager] Could not subscribe to HP changes for {gameObject.name} - characterUIManager null or hasFloatingHPBar false");
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if(characterUIManager != null && characterUIManager.hasFloatingHPBar)
        {
            characterNetworkManager.currentHealth.OnValueChanged -= characterUIManager.OnHPChanged;
            Debug.Log($"[AICharacterManager] HP bar subscription disabled for {gameObject.name}");
        }
    }
}