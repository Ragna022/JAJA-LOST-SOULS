using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterLocomotionManager : MonoBehaviour
{
   [HideInInspector] public CharacterManager character;

   [Header("Ground Check & Jumping")]
   [SerializeField] protected float gravityForce = -5.55f;
   [SerializeField] LayerMask groundLayer;
   [SerializeField] float groundCheckSphereRadius = 1;
   [SerializeField] protected Vector3 yVelocity; // THE FORCE AT WHICH OUR CHARACTER I SPULLED UP OR DOWN (JUMPING OR FALLING)
   [SerializeField] protected float groundedYVelocity = -20; ///THE FORCE AT WHICH OUR CHARACTER IS STICKING TOT THE GROUND WHILST THEY ARE GROUNDED
   [SerializeField] protected float fallStartYVelocity = -5; // THE FORCE AT WHICH OUR CHARACTER BEGINS TO FALL WHEN THEY BECOME UNGROUNDED (RISES AS THEY FALL LONGER)
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
         // IF WE ARE NOT ATTEMPTING TO JUMP OR MOVE UPWARD
         if (yVelocity.y < 0)
         {
            inAirTimer = 0;
            fallingVelocityhasBeenSet = false;
            yVelocity.y = groundedYVelocity;
         }
      }
      else
      {
         // IF WE ARE NOT JUMPING, AND OUR FALLING VELOCITY HAS NOT BEEN SET
         if (!character.characterNetworkManager.isJumping.Value && !fallingVelocityhasBeenSet)
         {
            fallingVelocityhasBeenSet = true;
            yVelocity.y = fallStartYVelocity;
         }

         inAirTimer = inAirTimer + Time.deltaTime;
         character.animator.SetFloat("inAirTimer", inAirTimer);
         yVelocity.y += gravityForce * Time.deltaTime;
      }
      
      // THERE SHOULD ALWAY BE SOME FORCE APPLIED TO THE Y VELOCITY 
      character.characterController.Move(yVelocity * Time.deltaTime);
   }

   protected void HandleGroundCheck()
   {
      character.isGrounded = Physics.CheckSphere(character.transform.position, groundCheckSphereRadius, groundLayer);
   }

   // DRAWS OUR GROUNDED CHECK SPHERE IN SCENE VIEW
   protected void OnDrawGizmosSelected()
   {
      //Gizmos.DrawSphere(character.transform.position, groundCheckSphereRadius);
   }
}
