using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterManager : NetworkBehaviour
{
    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Animator animator;

    [HideInInspector] public CharacterNetworkManager characterNetworkManager;
    [HideInInspector] public CharacterEffectsManager characterEffectsManager;
    [HideInInspector] public CharacterAnimatorManager characterAnimatorManager;
    [HideInInspector] public CharacterCombatManager characterCombatManager;
    [HideInInspector] public CharacterSoundFXManager characterSoundFXManager;
    [HideInInspector] public CharacterUIManager characterUIManager;
    [HideInInspector] public CharacterLocomotionManager characterLocomotionManager;

    [Header("Character Group")]
    public CharacterGroup characterGroup;

    [Header("Flags")]
    public bool isPerformingAction = false;
    public bool isGrounded = false;
    public bool applyRootMotion = false;
    public bool canRotate = true;
    public bool canMove = true;
    public bool isDead = false;

    [Header("Equipment")]
    public DamageCollider damageCollider;

    protected virtual void Awake()
    {
        DontDestroyOnLoad(this);

        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        characterNetworkManager = GetComponent<CharacterNetworkManager>();
        characterEffectsManager = GetComponent<CharacterEffectsManager>();
        characterAnimatorManager = GetComponent<CharacterAnimatorManager>();
        characterCombatManager = GetComponent<CharacterCombatManager>();
        characterSoundFXManager = GetComponent<CharacterSoundFXManager>();
        characterUIManager = GetComponent<CharacterUIManager>();
        characterLocomotionManager = GetComponent<CharacterLocomotionManager>();
    }

    protected virtual void Start()
    {
        IgnoreMyColliders();
    }

    protected virtual void Update()
    {
        if (IsOwner)
        {
            animator.SetBool("isGrounded", isGrounded);

            // Always update network position for ragdoll sync
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

    protected virtual void FixedUpdate()
    {

    }

    protected virtual void LateUpdate()
    {

    }

    protected virtual void OnEnable()
    {
        /*if(characterUIManager.hasFloatingHPBar)
            characterNetworkManager.currentHealth.OnValueChanged += characterUIManager.OnHPChanged;*/
    }
    
    protected virtual void OnDisable()
    {
        /*if(characterUIManager.hasFloatingHPBar)
            characterNetworkManager.currentHealth.OnValueChanged -= characterUIManager.OnHPChanged;*/
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[CharacterManager] OnNetworkSpawn - Character: {gameObject.name}, LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, OwnerClientId: {OwnerClientId}, NetworkObjectId: {NetworkObjectId}");

        characterNetworkManager.isMoving.OnValueChanged += characterNetworkManager.OnIsMovingChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Debug.Log($"[CharacterManager] OnNetworkDespawn - Unsubscribing for {gameObject.name}");

        characterNetworkManager.isMoving.OnValueChanged -= characterNetworkManager.OnIsMovingChanged;

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
        if (characterControllerCollider != null)
        {
            ignoreColliders.Add(characterControllerCollider);
        }

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