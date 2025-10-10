using System;
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

    [Header("Jump")]
    [SerializeField] float jumpStaminaCost = 25;
    [SerializeField] float jumpHeight = 4;
    [SerializeField] float jumpForwardSpeed = 5;
    [SerializeField] float freeFallSpeed = 2;
    private Vector3 jumpDirection;


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
        HandleJumpingMovement();
        HandleFreeFallMovement();
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

    private void HandleJumpingMovement()
    {
        if (player.playerNetworkManager.isJumping.Value)
        {
            player.characterController.Move(jumpDirection * jumpForwardSpeed * Time.deltaTime);
        }
    }

    private void HandleFreeFallMovement()
    {
        if (!player.isGrounded)
        {
            Vector3 freeFallDirection;

            freeFallDirection = PlayerCamera.instance.transform.forward * PlayerInputManager.instance.verticalInput;
            freeFallDirection = freeFallDirection + PlayerCamera.instance.transform.right * PlayerInputManager.instance.horizontalInput;
            freeFallDirection.y = 0;

            player.characterController.Move(freeFallDirection * freeFallSpeed * Time.deltaTime);
        }
    }

    private void HandleRotation()
    {
        if (!player.canRotate)
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

    public void AttemptToPerformJump()
    {
        // IF WE ARE PERFORMING A GENERAL ACTION, WE DO NOT WANT TO ALLOW A JUMP (WILL CHANGE WHEN COMBAT IS ADDED)
        if (player.isPerformingAction)
            return;

        // IF WE ARE OUT OF STAMINA, WE DO NOT WISH TO ALLOW JUMP
        if (player.playerNetworkManager.currentStamina.Value <= 0)
            return;

        // IF WE ARE ALREADY IN A JUMP, WE DO NOT WANT TO ALLOW A JUMP AGAIN UHNTIL THE CURRENT JUMP HAS FINISHED
        if (player.playerNetworkManager.isJumping.Value)
            return;

        // IF WE ARE NOT GROUNDED, WE DO NOT WAN TOT ALLOW JMUP
        if (!player.isGrounded)
            return;

        // IF WE ARE TWO HANDING OUR WEAPON, PLAY THE TWO HANDED JUMP ANIMATION, OTHERWISE PLAY THE ONE HANDED ANIMATION (TODO)
        player.playerAnimatorManager.PlayTargetActionAnimation("HumanM@Jump01 - Begin", false);

        player.playerNetworkManager.isJumping.Value = true;
        player.playerNetworkManager.currentStamina.Value -= jumpStaminaCost;

        jumpDirection = PlayerCamera.instance.cameraObject.transform.forward * PlayerInputManager.instance.verticalInput;
        jumpDirection += PlayerCamera.instance.cameraObject.transform.right * PlayerInputManager.instance.horizontalInput;
        jumpDirection.y = 0;

        if (jumpDirection != Vector3.zero)
        {
            // IF WE ARE SPRINTING, JUMP DIRECTION IS AT FULL DISTANCE
            if (player.playerNetworkManager.isSprinting.Value)
            {
                jumpDirection *= 1;
            }
            // IF WE ARE RUNNING, JUMP DIRECTION IS AT HALF DISTANCE
            else if (PlayerInputManager.instance.moveAmount > 0.5)
            {
                jumpDirection *= 0.5f;
            }
            // IF WE ARE WALKING, JUMP DIRECTION IS AT QUARTER DISTANCE
            else if (PlayerInputManager.instance.moveAmount <= 0.5)
            {
                jumpDirection *= 0.25f;
            }
        } 
    }

    public void ApplyJumpingVelocity()
    {
        // APPLY AN UPWARD VELOCITY
        yVelocity.y = Mathf.Sqrt(jumpHeight * -2 * gravityForce);
    }
}
