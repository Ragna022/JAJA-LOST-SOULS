using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class PlayerLocomotionManager : CharacterLocomotionManager
{

    PlayerManager player;
    [HideInInspector] public float verticalMovement;
    [HideInInspector] public float horizontalMovement;
    [HideInInspector] public float moveAmount;

    [Header("Movement Settings")]
    private Vector3 moveDirection;
    private Vector3 targetRotationDirection;
    [SerializeField] float walkingSpeed = 2;
    [SerializeField] float runningSpeed = 5;
    [SerializeField] float sprintingSpeed = 5;
    [SerializeField] float rotationSpeed = 15;
    [SerializeField] float sprintingStaminaCost = 2;

    [Header("Dodge")]
    private Vector3 rollDirection;
    [SerializeField] float dodgeStaminaCost = 25;
    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<PlayerManager>();
    }

    protected override void Update()
    {
        base.Update();
        if (player.IsOwner)
        {
            player.characterNetworkManager.verticalMovement.Value = verticalMovement;
            player.characterNetworkManager.horizontalMovement.Value = horizontalMovement;
            player.characterNetworkManager.moveAmount.Value = moveAmount;
        }
        else
        {
            verticalMovement = player.characterNetworkManager.verticalMovement.Value;
            horizontalMovement = player.characterNetworkManager.horizontalMovement.Value;
            moveAmount = player.characterNetworkManager.moveAmount.Value;

            player.playerAnimatorManager.UpdateAnimatorMovementParameters(0, moveAmount, player.playerNetworkManager.isSprinting.Value);
        }
    }

    public void HandleAllMovement()
    {
        HandleGroundedMovement();
        HandleRotation();
        
        // AERIAL MOVEMENT HANDLE LATER
    }

    private void GetMovementValues()
    {
        verticalMovement = PlayerInputManager.instance.verticalInput;
        horizontalMovement = PlayerInputManager.instance.horizontalInput;
        moveAmount = PlayerInputManager.instance.moveAmount;
    }

    private void HandleGroundedMovement()
    {
        //if (!player.canMove)
        //    return;

        GetMovementValues();
        //Movement direction is based on camera perspective and directions
        moveDirection = PlayerCamera.instance.transform.forward * verticalMovement;
        moveDirection = moveDirection + PlayerCamera.instance.transform.right * horizontalMovement;
        moveDirection.Normalize();
        moveDirection.y = 0;

        if (player.playerNetworkManager.isSprinting.Value)
        {
            player.characterController.Move(moveDirection * sprintingSpeed * Time.deltaTime);
        }
        else
        {
            if (PlayerInputManager.instance.moveAmount > 0.5f)
            {
                player.characterController.Move(moveDirection * runningSpeed * Time.deltaTime);
            }
            else if (PlayerInputManager.instance.moveAmount <= 0.5f)
            {
                player.characterController.Move(moveDirection * walkingSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleRotation()
    {
        if(!player.canRotate)
            return;
        targetRotationDirection = Vector3.zero;
        targetRotationDirection = PlayerCamera.instance.cameraObject.transform.forward * verticalMovement;
        targetRotationDirection = targetRotationDirection + PlayerCamera.instance.cameraObject.transform.right * horizontalMovement;
        targetRotationDirection.Normalize();
        targetRotationDirection.y = 0;

        if (targetRotationDirection == Vector3.zero)
        {
            targetRotationDirection = transform.forward;
        }

        Quaternion newRotation = Quaternion.LookRotation(targetRotationDirection);
        Quaternion targetRotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
        transform.rotation = targetRotation;
    }

    public void HandleSprinting()
    {
        if (player.isPerformingAction)
        {
            // SET SPRINTING TO FALSE
            player.playerNetworkManager.isSprinting.Value = false;
        }

        // IF WE ARE OUT OF STAMINA, SET SPTINTING TO FALSE
        if (player.playerNetworkManager.currentStamina.Value <= 0)
        {
            player.playerNetworkManager.isSprinting.Value = false;
            return;
        }

        // IF WE ARE MOVING, SET SPRINTING TO TRUE
        if (moveAmount >= 0.5)
        {
            player.playerNetworkManager.isSprinting.Value = true;
        }
        // IF WE ARE NOT MOVING, SET SPRINTING TO FALSE
        else
        {
            player.playerNetworkManager.isSprinting.Value = false;
        }

        // If we are sprinting, decrease stamina gradually
        if (player.playerNetworkManager.isSprinting.Value)
        {
            // Smooth stamina drain
            float staminaDrain = sprintingStaminaCost * Time.deltaTime;
            
            // Ensure we don't go below 0
            player.playerNetworkManager.currentStamina.Value = Mathf.Max(
                0, 
                player.playerNetworkManager.currentStamina.Value - staminaDrain
            );
        }
    }

    public void AttemptToPerformDodge()
    {
        if (player.isPerformingAction)
            return;

        if (player.playerNetworkManager.currentStamina.Value <= 0)
            return;

        //IF WE ARE MOVING WHEN ATTEMPTING TO DODGE, WE PERFORM A ROLL IN THE DIRECTION WE ARE MOVING
            if (PlayerInputManager.instance.moveAmount > 0)
            {
                rollDirection = PlayerCamera.instance.cameraObject.transform.forward * PlayerInputManager.instance.verticalInput;
                rollDirection += PlayerCamera.instance.cameraObject.transform.right * PlayerInputManager.instance.horizontalInput;
                rollDirection.y = 0;
                rollDirection.Normalize();

                Quaternion playerRotation = Quaternion.LookRotation(rollDirection);
                player.transform.rotation = playerRotation;

                // PERFORM A ROLL ANIMATION
                player.playerAnimatorManager.PlayTargetActionAnimation("RollForward", true, true);
            }
            // If WE ARE NOT MOVING WHEN ATTEMPTING TO DODGE, WE PERFORM A BACKSTEP
            else
            {
                // PERFORM A BACKSTEP ANIMATION
                player.playerAnimatorManager.PlayTargetActionAnimation("BackStep", true, true);
            }

        player.playerNetworkManager.currentStamina.Value -= dodgeStaminaCost;
    }
}
