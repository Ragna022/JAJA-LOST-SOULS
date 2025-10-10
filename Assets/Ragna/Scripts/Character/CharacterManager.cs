using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterManager : NetworkBehaviour
{
    [Header("Status")]
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Animator animator;

    [HideInInspector] public CharacterNetworkManager characterNetworkManager;
    [HideInInspector] public CharacterEffectsManager characterEffectsManager;
    [HideInInspector] public CharacterAnimatorManager characterAnimatorManager;

    [Header("Flags")]
    public bool isPerformingAction = false;
    //public bool isJumping = false;
    public bool isGrounded = false;
    public bool applyRootMotion = false;
    public bool canRotate = true;
    public bool canMove = true;

    [Header("Equipment")]
    public DamageCollider damageCollider; // NEW: Add a reference for your weapon's collider

    

    protected virtual void Awake()
    {
        DontDestroyOnLoad(this);

        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        characterNetworkManager = GetComponent<CharacterNetworkManager>();
        characterEffectsManager = GetComponent<CharacterEffectsManager>();
        characterAnimatorManager = GetComponent<CharacterAnimatorManager>();
    }

    protected virtual void Start()
    {
        IgnoreMyColliders();
    }

    protected virtual void Update()
    {
        animator.SetBool("isGrounded", isGrounded);
        if (IsOwner)
        {
            characterNetworkManager.networkPosition.Value = transform.position;
            characterNetworkManager.networkRotation.Value = transform.rotation;
        }
        else
        {
            transform.position = Vector3.SmoothDamp
                (transform.position,
                characterNetworkManager.networkPosition.Value,
                ref characterNetworkManager.networkPositionVelocity,
                characterNetworkManager.networkPositionSmoothTime);

            transform.rotation = Quaternion.Slerp
                (transform.rotation,
                characterNetworkManager.networkRotation.Value,
                characterNetworkManager.networkRotationSmoothTime);
        }
    }

    protected virtual void LateUpdate()
    {

    }

    public virtual IEnumerator ProcessDeathEvent(bool manuallySelectDeathAnimation = false)
    {
        if (IsOwner)
        {
            characterNetworkManager.currentHealth.Value = 0;
            isDead.Value = true;

            // RESET ANY FLAGS HERE THAT NEED TO BE RESET
            // NOTHING YET
            // IF WE ARE NOT GROUNDED, PLAY AN ARIERL DEATH ANIMATION

            if (!manuallySelectDeathAnimation)
            {
                characterAnimatorManager.PlayTargetActionAnimation("Death", true);
            }
        }

        // PLAY SOME DEATH SFX

        yield return new WaitForSeconds(5);

        // AWARD PLAYERS WITH RUNES
        // DISABLE CHARACTER
    }

    public virtual void ReviveCharacter()
    {

    }

    protected virtual void IgnoreMyColliders()
    {
        Collider characterControllerCollider = GetComponent<Collider>();
        Collider[] damageableCharacterColliders = GetComponentsInChildren<Collider>();
        List<Collider> ignoreColliders = new List<Collider>();

        // ADDS ALL OF OUR DAMAGEABLE COLLIDER TO THE LIST THAT WILL BE USED TO IGNORE COLLISIONS
        foreach (var collider in damageableCharacterColliders)
        {
            ignoreColliders.Add(collider);
        }

        // ADDS OUR CHARACTER CONTROLLER COLLIDER TO THE LIST THAT WILL BE USED TO IGNORE COLLISION
        ignoreColliders.Add(characterControllerCollider);

        // GOES THROUGH EVERY COLLIDER ON THE LIST, AND IGNORE COLLISIONS WITH EACH OTHER
        foreach (var collider in ignoreColliders)
        {
            foreach (var otherCollider in ignoreColliders)
            {
                Physics.IgnoreCollision(collider, otherCollider, true);
            }
        }
    }
}
