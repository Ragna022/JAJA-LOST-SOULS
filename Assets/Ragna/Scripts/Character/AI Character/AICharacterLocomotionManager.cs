using UnityEngine;
using UnityEngine.AI;

public class AICharacterLocomotionManager : CharacterLocomotionManager
{
    private AICharacterManager aiCharacter;

    [Header("AI Movement Settings")]
    [SerializeField] private float movementSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float animationBlendSpeed = 5f;

    private Vector3 lastAgentVelocity;
    private float currentAnimatedSpeed;

    protected override void Awake()
    {
        base.Awake();
        aiCharacter = GetComponent<AICharacterManager>();
    }

    private void Start()
    {
        // CRITICAL: Disable NavMeshAgent's built-in movement
        if (aiCharacter?.navMeshAgent != null)
        {
            aiCharacter.navMeshAgent.updatePosition = false; // Let CharacterController handle position
            aiCharacter.navMeshAgent.updateRotation = false; // Let us handle rotation
        }
    }

    public void RotateTowardsAgent(AICharacterManager aiCharacter)
    {
        if(aiCharacter.aiCharacterNetworkManager.isMoving.Value)
        {
            aiCharacter.transform.rotation = aiCharacter.navMeshAgent.transform.rotation;
        }
    }

    protected override void Update()
    {
        // Safety check
        if (character.characterController == null || !character.characterController.enabled)
            return;

        // Handle ground check and gravity
        HandleGroundCheck();
        
        if (character.isGrounded)
        {
            if (yVelocity.y < 0)
            {
                inAirTimer = 0;
                fallingVelocityhasBeenSet = false;
                yVelocity.y = groundedYVelocity;
            }
        }
        else
        {
            if (!character.characterNetworkManager.isJumping.Value && !fallingVelocityhasBeenSet)
            {
                fallingVelocityhasBeenSet = true;
                yVelocity.y = fallStartYVelocity;
            }

            inAirTimer = inAirTimer + Time.deltaTime;
            character.animator.SetFloat("inAirTimer", inAirTimer);
            yVelocity.y += gravityForce * Time.deltaTime;
        }

        // Handle AI movement
        if (aiCharacter != null && aiCharacter.navMeshAgent != null && aiCharacter.navMeshAgent.enabled)
        {
            HandleAIMovement();
        }

        // Apply gravity
        character.characterController.Move(yVelocity * Time.deltaTime);
    }

    private void HandleAIMovement()
    {
        // STOP SCRIPT MOVEMENT IF PERFORMING ACTION (Attack)
        // This prevents "sliding" while the root motion animation plays
        if (character.isPerformingAction)
        {
            character.characterAnimatorManager.UpdateAnimatorMovementParameters(0, 0, false);
            return;
        }

        NavMeshAgent agent = aiCharacter.navMeshAgent;

        // Check if agent has reached destination
        bool hasReachedDestination = !agent.pathPending && 
                                      agent.remainingDistance <= agent.stoppingDistance;

        // Get desired velocity from NavMeshAgent
        Vector3 desiredVelocity = agent.desiredVelocity;
        desiredVelocity.y = 0; // Prevent Floating: Ignore vertical velocity from NavMesh
        
        // If at destination, force stop
        if (hasReachedDestination)
        {
            desiredVelocity = Vector3.zero;
        }
        
        // Check if agent wants to move
        if (desiredVelocity.magnitude > 0.1f && !hasReachedDestination)
        {
            Debug.Log($"[AI DEBUG] Moving! DesiredVelocity: {desiredVelocity}, Magnitude: {desiredVelocity.magnitude}");
            // Smoothly transition velocity for natural movement
            lastAgentVelocity = Vector3.Lerp(lastAgentVelocity, desiredVelocity, Time.deltaTime * 10f);
            
            // Move using CharacterController
            character.characterController.Move(lastAgentVelocity * Time.deltaTime);

            // Sync NavMeshAgent position to actual CharacterController position
            agent.nextPosition = transform.position;

            // Smooth rotation towards movement direction
            if (character.canRotate && lastAgentVelocity.magnitude > 0.1f)
            {
                Vector3 lookDirection = lastAgentVelocity;
                lookDirection.y = 0;
                
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }

            // Update animator with smoothing
            float targetSpeed = lastAgentVelocity.magnitude;
            currentAnimatedSpeed = Mathf.Lerp(currentAnimatedSpeed, targetSpeed, animationBlendSpeed * Time.deltaTime);
            UpdateAIAnimator(lastAgentVelocity.normalized, currentAnimatedSpeed);
        }
        else
        {
            // Agent wants to stop - smooth deceleration
            lastAgentVelocity = Vector3.Lerp(lastAgentVelocity, Vector3.zero, Time.deltaTime * 10f);
            
            if (lastAgentVelocity.magnitude > 0.01f)
            {
                character.characterController.Move(lastAgentVelocity * Time.deltaTime);
            }
            else
            {
                // Fully stopped, zero out velocity
                lastAgentVelocity = Vector3.zero;
            }
            
            // Sync position
            agent.nextPosition = transform.position;
            
            // Smooth stop animation - force to zero when stopped
            currentAnimatedSpeed = Mathf.Lerp(currentAnimatedSpeed, 0f, animationBlendSpeed * Time.deltaTime);
            
            // Force animation to idle if speed is very low
            if (currentAnimatedSpeed < 0.05f)
            {
                currentAnimatedSpeed = 0f;
            }
            
            UpdateAIAnimator(Vector3.zero, currentAnimatedSpeed);
        }
    }

    private void UpdateAIAnimator(Vector3 direction, float speed)
    {
        if (character.characterAnimatorManager == null) return;

        // Convert world direction to local space
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        
        // Normalize to -1 to 1 range
        float forward = localDirection.z;
        float strafe = localDirection.x;

        // Calculate move amount based on actual speed
        float moveAmount = Mathf.Clamp01(speed / movementSpeed);

        // Update animator with smooth values
        Debug.Log($"[AI DEBUG] Animator Update: Strafe={strafe}, Forward={forward}, Speed={speed}, MoveAmount={moveAmount}");
        character.characterAnimatorManager.UpdateAnimatorMovementParameters(
            strafe * moveAmount,
            forward * moveAmount,
            false // isSprinting
        );
    }

    public void SetMovementSpeed(float speed)
    {
        movementSpeed = speed;
        
        // Also update NavMeshAgent speed
        if (aiCharacter?.navMeshAgent != null)
        {
            aiCharacter.navMeshAgent.speed = speed;
        }
    }
}