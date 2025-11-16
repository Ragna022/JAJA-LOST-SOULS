using UnityEngine;
using UnityEngine.AI;

public class AICharacterLocomotionManager : CharacterLocomotionManager
{
    private AICharacterManager aiCharacter;
    
    [Header("AI Movement Settings")]
    [SerializeField] private float movementSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 10f;

    protected override void Awake()
    {
        base.Awake();
        aiCharacter = GetComponent<AICharacterManager>();
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
        // Safety check: Don't process if CharacterController is not ready
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

        // Handle AI movement using NavMeshAgent data + CharacterController
        if (aiCharacter != null && aiCharacter.navMeshAgent != null && aiCharacter.navMeshAgent.enabled)
        {
            HandleAIMovement();
        }

        // Apply gravity through CharacterController
        character.characterController.Move(yVelocity * Time.deltaTime);
    }

    private void HandleAIMovement()
    {
        NavMeshAgent agent = aiCharacter.navMeshAgent;
        
        // Check if we have a valid path and should move
        if (!agent.pathPending && agent.hasPath)
        {
            Vector3 direction = agent.steeringTarget - transform.position;
            direction.y = 0; // Keep movement horizontal
            
            float distanceToTarget = direction.magnitude;

            // Only move if we're beyond stopping distance
            if (distanceToTarget > agent.stoppingDistance)
            {
                // Normalize direction for consistent movement speed
                direction.Normalize();

                // Calculate movement speed (can be modified based on AI state)
                float currentSpeed = movementSpeed;
                
                // If AI is sprinting or in combat, you can modify speed here
                // currentSpeed = aiCharacter.isSprinting ? sprintSpeed : movementSpeed;

                // Move using CharacterController
                Vector3 movement = direction * currentSpeed * Time.deltaTime;
                character.characterController.Move(movement);

                // Rotate towards movement direction
                if (character.canRotate && direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, 
                        targetRotation, 
                        rotationSpeed * Time.deltaTime
                    );
                }

                // Update animator parameters based on actual movement
                UpdateAIAnimator(direction, currentSpeed);
            }
            else
            {
                // At destination, stop moving
                UpdateAIAnimator(Vector3.zero, 0f);
            }
        }
        else
        {
            // No path, idle
            UpdateAIAnimator(Vector3.zero, 0f);
        }

        // Sync NavMeshAgent position to CharacterController position
        // This prevents NavMeshAgent from drifting
        if (agent.enabled)
        {
            agent.nextPosition = transform.position;
        }
    }

    private void UpdateAIAnimator(Vector3 direction, float speed)
    {
        if (character.characterAnimatorManager == null)
            return;

        // Convert world direction to local space
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        
        float forward = localDirection.z;
        float strafe = localDirection.x;

        // Calculate move amount (0 to 1)
        float moveAmount = Mathf.Clamp01(speed / movementSpeed);

        // Update animator
        character.characterAnimatorManager.UpdateAnimatorMovementParameters(
            strafe, 
            forward, 
            false // isSprinting - you can make this dynamic
        );
    }

    // Optional: Public method to set movement speed dynamically
    public void SetMovementSpeed(float speed)
    {
        movementSpeed = speed;
    }
}