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
    [HideInInspector] public CharacterCombatManager characterCombatManager;
    [HideInInspector] public CharacterSoundFXManager characterSoundFXManager;

    [Header("Flags")]
    public bool isPerformingAction = false;
    public bool isGrounded = false;
    public bool applyRootMotion = false;
    public bool canRotate = true;
    public bool canMove = true;

    [Header("Equipment")]
    public DamageCollider damageCollider;

    // Track if death animation has been played
    private bool deathAnimationPlayed = false;

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

            // CRITICAL FIX: Check isDead status every frame for non-owners
            if (isDead.Value && !deathAnimationPlayed)
            {
                Debug.Log($"[CharacterManager] Update detected isDead=true without callback! Playing death animation for {gameObject.name}");
                PlayDeathAnimation();
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        
    }

    protected virtual void LateUpdate()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[CharacterManager] OnNetworkSpawn - Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, NetworkObjectId: {NetworkObjectId}, isDead: {isDead.Value}");

        // Subscribe to isDead changes for sync
        isDead.OnValueChanged += OnIsDeadChanged;
        
        Debug.Log($"[CharacterManager] Subscribed to isDead.OnValueChanged for {gameObject.name}");
        
        // If the character is already dead when we spawn (late join scenario), play the death animation
        if (isDead.Value && !deathAnimationPlayed)
        {
            Debug.Log($"[CharacterManager] Character spawned already dead, playing death animation immediately");
            PlayDeathAnimation();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Debug.Log($"[CharacterManager] OnNetworkDespawn - Unsubscribing for {gameObject.name}");
        
        // Unsubscribe to avoid leaks
        isDead.OnValueChanged -= OnIsDeadChanged;
    }

    // Callback to play death animation on ALL clients when isDead syncs
    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[CharacterManager] OnIsDeadChanged TRIGGERED - Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, NetworkObjectId: {NetworkObjectId}, OldValue: {oldValue}, NewValue: {newValue}");
        
        if (newValue && !deathAnimationPlayed)
        {
            Debug.Log($"[CharacterManager] isDead changed to TRUE! Playing death animation for {gameObject.name}");
            PlayDeathAnimation();
        }
    }

    private void PlayDeathAnimation()
    {
        if (deathAnimationPlayed)
        {
            Debug.Log($"[CharacterManager] Death animation already played for {gameObject.name}, skipping");
            return;
        }

        deathAnimationPlayed = true;
        Debug.Log($"[CharacterManager] PlayDeathAnimation called for {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}");
        Debug.Log($"[CharacterManager] Animator enabled: {animator.enabled}, GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"[CharacterManager] isPerformingAction: {isPerformingAction}, applyRootMotion: {applyRootMotion}");
        
        // Play death animation locally on this instance
        // The method on CharacterNetworkManager is not public; use reflection to invoke it safely.
        try
        {
            var managerType = characterNetworkManager.GetType();

            // Prefer public method if it exists
            var method = managerType.GetMethod("PerformActionAnimationFromServer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (method == null)
            {
                // Fallback to non-public method
                method = managerType.GetMethod("PerformActionAnimationFromServer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            if (method != null)
            {
                method.Invoke(characterNetworkManager, new object[] { "Death", true });
            }
            else
            {
                Debug.LogWarning($"[CharacterManager] Could not find method PerformActionAnimationFromServer on {managerType.FullName}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterManager] Error invoking PerformActionAnimationFromServer: {ex}");
        }
    }

    public virtual IEnumerator ProcessDeathEvent(bool manuallySelectDeathAnimation = false)
    {
        Debug.Log($"[CharacterManager] ProcessDeathEvent STARTED - Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, NetworkObjectId: {NetworkObjectId}");
        
        if (IsOwner)
        {
            Debug.Log($"[CharacterManager] Setting health to 0 and isDead to true for {gameObject.name}");
            
            characterNetworkManager.currentHealth.Value = 0;
            isDead.Value = true;

            Debug.Log($"[CharacterManager] isDead.Value set to: {isDead.Value}");
        }
        else
        {
            Debug.Log($"[CharacterManager] ProcessDeathEvent called on non-owner, waiting for network sync");
        }

        // PLAY SOME DEATH SFX (add here if needed)

        yield return new WaitForSeconds(5);

        Debug.Log($"[CharacterManager] ProcessDeathEvent completed 5 second wait for {gameObject.name}");
        
        // AWARD PLAYERS WITH RUNES
        // DISABLE CHARACTER
    }

    public virtual void ReviveCharacter()
    {
        deathAnimationPlayed = false; // Reset flag on revive
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