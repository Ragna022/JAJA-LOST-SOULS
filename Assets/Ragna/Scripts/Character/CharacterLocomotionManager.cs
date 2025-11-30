using UnityEngine;

public class CharacterLocomotionManager : MonoBehaviour
{
   [HideInInspector] public CharacterManager character;

   [Header("Ground Check & Jumping")]
   [SerializeField] protected float gravityForce = -5.55f;
   [SerializeField] LayerMask groundLayer;
   [SerializeField] float groundCheckSphereRadius = 1;
   [SerializeField] protected Vector3 yVelocity;
   [SerializeField] protected float groundedYVelocity = -20;
   [SerializeField] protected float fallStartYVelocity = -5;
   protected bool fallingVelocityhasBeenSet = false;
   protected float inAirTimer = 0;

   [Header("Flags")]
   public bool isRolling = false;

   protected virtual void Awake()
   {
      character = GetComponent<CharacterManager>();
   }

   protected virtual void Update()
   {
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
      
      if (character.characterController.enabled)
      {
          character.characterController.Move(yVelocity * Time.deltaTime);
      }
   }

   protected void HandleGroundCheck()
   {
      // Don't update grounded state during jumps or rolls
      if (character.characterNetworkManager.isJumping.Value || isRolling)
         return;
         
      character.isGrounded = Physics.CheckSphere(character.transform.position, groundCheckSphereRadius, groundLayer);
   }

   protected void OnDrawGizmosSelected()
   {
      //Gizmos.DrawSphere(character.transform.position, groundCheckSphereRadius);
   }

   public void EnableCanRotate()
   {
        character.canRotate = true;
   }

   public void DisableCanRotate()
   {
        character.canRotate = false;
   }
}