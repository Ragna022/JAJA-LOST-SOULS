using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterNetworkManager : NetworkBehaviour
{
    CharacterManager character;

    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController defaultController;
    [SerializeField] private AnimatorOverrideController actionOverrideController;

    [Header("Position")]
    public NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Vector3 networkPositionVelocity;
    public float networkPositionSmoothTime = 0.1f;
    public float networkRotationSmoothTime = 0.1f;

    [Header("Animatior")]
    public NetworkVariable<float> horizontalMovement = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> verticalMovement = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> moveAmount = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Flags")]
    public NetworkVariable<bool> isSprinting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isChargingAttack = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Resources")]
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> maxHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> currentStamina = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> maxStamina = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Stats")]
    public NetworkVariable<int> vitality = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> endurance = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> dexterity = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> intelligence = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected virtual void Awake()
    {
        character = GetComponent<CharacterManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[CharacterNetworkManager] OnNetworkSpawn - ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, OwnerClientId: {OwnerClientId}, Character: {gameObject.name}");

        // Delay animator setup slightly for clients to ensure animator fully initializes
        if (IsClient)
        {
            Debug.Log($"[CharacterNetworkManager] Applying initial EnsureAnimatorSetup() for {gameObject.name}");
            EnsureAnimatorSetup(); 
            
            // Keep coroutine as a delayed double-check for remote clients
            StartCoroutine(WaitAndEnsureAnimatorSetup());
        }

        currentHealth.OnValueChanged += CheckHp;

        Debug.Log($"[CharacterNetworkManager] Subscribed to currentHealth.OnValueChanged for {gameObject.name}");
    }

    private IEnumerator WaitAndEnsureAnimatorSetup()
    {
        // Wait 2 frames to allow Animator to initialize after network spawn
        yield return null;
        yield return null;

        EnsureAnimatorSetup();

        // Confirm that the override contains the "Death" clip (for debugging)
        if (actionOverrideController != null)
        {
            bool foundDeath = false;
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(actionOverrideController.overridesCount);
            actionOverrideController.GetOverrides(overrides);
            foreach (var pair in overrides)
            {
                if (pair.Value != null && pair.Value.name == "Death")
                {
                    foundDeath = true;
                    break;
                }
            }

            Debug.Log(foundDeath
                ? "[CharacterNetworkManager] Client override controller confirmed to include 'Death' clip ✅"
                : "[CharacterNetworkManager] 'Death' clip missing from client override controller ⚠️");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        Debug.Log($"[CharacterNetworkManager] OnNetworkDespawn - Unsubscribing for {gameObject.name}");
        
        currentHealth.OnValueChanged -= CheckHp;
    }

    public void CheckHp(int oldValue, int newValue)
    {
        Debug.Log($"[CharacterNetworkManager] CheckHp CALLED - Character: {gameObject.name}, LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, OwnerClientId: {OwnerClientId}, OldHP: {oldValue}, NewHP: {newValue}, IsDead: {character.isDead.Value}");

        if (character.isDead.Value)
        {
            Debug.Log($"[CharacterNetworkManager] Character already dead, returning early");
            return;
        }

        // Only trigger if health *crossed* zero (prevents recursive calls)
        if (oldValue > 0 && newValue <= 0)
        {
            Debug.Log($"[CharacterNetworkManager] Health <= 0! Setting isDead and starting ProcessDeathEvent for {gameObject.name}");
            
            // CRITICAL: Set isDead IMMEDIATELY to trigger network sync
            if (character.IsOwner)
            {
                character.isDead.Value = true;
                Debug.Log($"[CharacterNetworkManager] isDead.Value set to TRUE immediately for network sync (Owner: {OwnerClientId})");
                
                // BACKUP: Force sync via ClientRpc as a safety net
                if (IsServer)
                {
                    Debug.Log($"[CharacterNetworkManager] Calling ForceDeathSyncClientRpc as backup");
                    ForceDeathSyncClientRpc();
                }
            }
            
            StartCoroutine(character.ProcessDeathEvent());
        }

        if (character.IsOwner)
        {
            if (currentHealth.Value > maxHealth.Value)
            {
                Debug.Log($"[CharacterNetworkManager] Over-healing detected, clamping to max health");
                currentHealth.Value = maxHealth.Value;
            }
        }
    }

    // BACKUP: ClientRpc to force death sync if NetworkVariable sync fails
    [ClientRpc]
    private void ForceDeathSyncClientRpc()
    {
        Debug.Log($"[CharacterNetworkManager] ForceDeathSyncClientRpc RECEIVED on ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, Character: {gameObject.name}");
        
        // Non-owners should receive this and trigger death animation
        if (!character.IsOwner)
        {
            Debug.Log($"[CharacterNetworkManager] Non-owner client checking death status");
            
            // Wait a frame to let OnIsDeadChanged fire first
            StartCoroutine(CheckDeathAfterFrame());
        }
    }

    private System.Collections.IEnumerator CheckDeathAfterFrame()
    {
        // Wait one frame to allow OnIsDeadChanged callback to fire
        yield return null;
        
        Debug.Log($"[CharacterNetworkManager] CheckDeathAfterFrame - isDead: {character.isDead.Value}, deathAnimationPlayed: {character.GetDeathAnimationPlayed()}");
        
        // Only force play if the character is dead but animation hasn't played
        if (character.isDead.Value && !character.GetDeathAnimationPlayed())
        {
            Debug.Log($"[CharacterNetworkManager] NetworkVariable callback didn't fire, forcing death animation via backup ClientRpc");
            character.PlayDeathAnimationDirect();
        }
        else if (character.GetDeathAnimationPlayed())
        {
            Debug.Log($"[CharacterNetworkManager] Death animation already handled by OnIsDeadChanged callback ✅");
        }
    }
    
    public void OnIsChargingAttackChanged(bool oldStatus, bool newStatus)
    {
        character.animator.SetBool("IsChargingAttack", isChargingAttack.Value);
    }

    [ServerRpc]
    public void NotifyTheServerOfActionAnimationServerRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        if (IsServer)
        {
            PlayActionAnimationForAllClientsClientRpc(clientID, animationID, applyRootMotion);
        }
    }

    [ClientRpc]
    public void PlayActionAnimationForAllClientsClientRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        if (clientID != NetworkManager.Singleton.LocalClientId)
        {
            PerformActionAnimationFromServer(animationID, applyRootMotion);
        }
    }

    public void PerformActionAnimationFromServer(string animationID, bool applyRootMotion)
    {
        Debug.Log($"[CharacterNetworkManager] PerformActionAnimationFromServer - Animation: {animationID}, ApplyRootMotion: {applyRootMotion}, Character: {gameObject.name}, LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {character.IsOwner}, OwnerClientId: {OwnerClientId}");
        
        if (character.animator == null)
        {
            Debug.LogError($"[CharacterNetworkManager] ANIMATOR IS NULL for {gameObject.name}!");
            return;
        }
        
        var currentState = character.animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[CharacterNetworkManager] Current Animator State: {currentState.shortNameHash}, IsPlaying: {character.animator.isActiveAndEnabled}");
        
        character.applyRootMotion = applyRootMotion;
        
        if (animationID == "Death")
        {
            Debug.Log($"[CharacterNetworkManager] FORCING DEATH ANIMATION with Play() for {(character.IsOwner ? "OWNER" : "NON-OWNER")}");
            
            // Critical: Stop all ongoing actions and animations
            character.isPerformingAction = false;
            character.canRotate = false;
            character.canMove = false;
            
            character.animator.applyRootMotion = applyRootMotion;
            
            // Get the Action Override layer index
            int actionLayerIndex = character.animator.GetLayerIndex("Action Override");
            if (actionLayerIndex == -1)
            {
                actionLayerIndex = 0;
                Debug.LogWarning("[CharacterNetworkManager] Action Override layer not found, using Base Layer");
            }
            
            int deathHash = Animator.StringToHash("Death");
            Debug.Log($"[CharacterNetworkManager] Death state hash: {deathHash}, Target Layer: {actionLayerIndex}");
            
            // Force animator to reset and play death immediately
            character.animator.Rebind();
            character.animator.Update(0f);
            
            // Play on the correct layer
            character.animator.Play(deathHash, actionLayerIndex, 0f);
            
            Debug.Log($"[CharacterNetworkManager] Death animation Play() executed on layer {actionLayerIndex}");
            
            StartCoroutine(CheckAnimatorStateAfterFrame(animationID));
        }
        else
        {
            character.animator.CrossFade(animationID, 0.2f);
        }
    }

    private System.Collections.IEnumerator CheckAnimatorStateAfterFrame(string expectedAnimation)
    {
        const int maxAttempts = 6;
        const float attemptDelay = 0.08f;
        int attempt = 0;

        if (character == null || character.animator == null)
        {
            Debug.LogError("[CharacterNetworkManager] Character or Animator reference missing during state check!");
            yield break;
        }

        int actionLayerIndex = character.animator.GetLayerIndex("Action Override");
        if (actionLayerIndex == -1)
        {
            actionLayerIndex = 0;
            Debug.LogWarning("[CharacterNetworkManager] 'Action Override' layer not found, defaulting to Base Layer (0)");
        }

        while (attempt < maxAttempts)
        {
            var state = character.animator.GetCurrentAnimatorStateInfo(actionLayerIndex);
            Debug.Log($"[CharacterNetworkManager] After frame check (attempt {attempt + 1}/{maxAttempts}) - Current state hash: {state.shortNameHash}, Expected: {expectedAnimation}, IsPlaying: {character.animator.isActiveAndEnabled}");

            bool matched =
                state.IsName(expectedAnimation) ||
                state.IsName($"Action Override.{expectedAnimation}") ||
                state.IsTag(expectedAnimation);

            if (matched)
            {
                Debug.Log($"[CharacterNetworkManager] '{expectedAnimation}' animation confirmed on '{character.animator.GetLayerName(actionLayerIndex)}' layer ✅");
                yield break;
            }

            attempt++;
            yield return new WaitForSeconds(attemptDelay);
        }

        RuntimeAnimatorController controller = character.animator.runtimeAnimatorController;
        bool foundClip = false;

        if (controller is AnimatorOverrideController overrideController)
        {
            var baseController = overrideController.runtimeAnimatorController;
            if (baseController != null)
            {
                foreach (var clip in baseController.animationClips)
                {
                    if (clip != null && clip.name == expectedAnimation)
                    {
                        foundClip = true;
                        break;
                    }
                }
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);
            foreach (var kv in overrides)
            {
                if (kv.Value != null && kv.Value.name == expectedAnimation)
                {
                    foundClip = true;
                    break;
                }

                if (kv.Key != null && kv.Key.name == expectedAnimation)
                {
                    foundClip = true;
                    break;
                }
            }
        }
        else if (controller != null)
        {
            foreach (var clip in controller.animationClips)
            {
                if (clip != null && clip.name == expectedAnimation)
                {
                    foundClip = true;
                    break;
                }
            }
        }

        if (foundClip)
        {
            Debug.LogWarning($"[CharacterNetworkManager] '{expectedAnimation}' animation exists in controller/overrides but was not observed playing (client delay or transition mismatch).");
        }
        else
        {
            Debug.LogError($"[CharacterNetworkManager] '{expectedAnimation}' animation clip NOT FOUND even in override layers (checked 'Action Override' layer and controller)!");
        }
    }

    [ServerRpc]
    public void NotifyTheServerOfAttackActionAnimationServerRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        if (IsServer)
        {
            PlayAttackActionAnimationForAllClientsClientRpc(clientID, animationID, applyRootMotion);
        }
    }

    [ClientRpc]
    public void PlayAttackActionAnimationForAllClientsClientRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        if (clientID != NetworkManager.Singleton.LocalClientId)
        {
            PerformAttackActionAnimationFromServer(animationID, applyRootMotion);
        }
    }

    private void PerformAttackActionAnimationFromServer(string animationID, bool applyRootMotion)
    {
        character.applyRootMotion = applyRootMotion;
        character.animator.CrossFade(animationID, 0.2f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyTheServerOfCharacterDamageServerRpc(
        ulong damagedCharacterID,
        ulong characterCausingDamageID,
        float physicalDamage,
        float magicDamage,
        float fireDamage,
        float holyDamage,
        float poiseDamage,
        float angleHitFrom,
        float contactPointX,
        float contactPointY,
        float contactPointZ)
    {
        Debug.Log($"[CharacterNetworkManager] NotifyTheServerOfCharacterDamageServerRpc - DamagedID: {damagedCharacterID}, AttackerID: {characterCausingDamageID}, PhysicalDamage: {physicalDamage}");
        
        if (IsServer)
        {
            NotifyTheServerOfCharacterDamageClientRpc(damagedCharacterID, characterCausingDamageID, physicalDamage, magicDamage, fireDamage, holyDamage, poiseDamage, angleHitFrom, contactPointX, contactPointY, contactPointZ);
        }
    }

    [ClientRpc]
    public void NotifyTheServerOfCharacterDamageClientRpc(
        ulong damagedCharacterID,
        ulong characterCausingDamageID,
        float physicalDamage,
        float magicDamage,
        float fireDamage,
        float holyDamage,
        float poiseDamage,
        float angleHitFrom,
        float contactPointX,
        float contactPointY,
        float contactPointZ)
    {
        Debug.Log($"[CharacterNetworkManager] NotifyTheServerOfCharacterDamageClientRpc - ClientID: {NetworkManager.Singleton.LocalClientId}, DamagedID: {damagedCharacterID}, AttackerID: {characterCausingDamageID}");
        
        ProcessCharacterDamageFromServer(damagedCharacterID, characterCausingDamageID, physicalDamage, magicDamage, fireDamage, holyDamage, poiseDamage, angleHitFrom, contactPointX, contactPointY, contactPointZ);
    }

    public void ProcessCharacterDamageFromServer(
        ulong damagedCharacterID,
        ulong characterCausingDamageID,
        float physicalDamage,
        float magicDamage,
        float fireDamage,
        float holyDamage,
        float poiseDamage,
        float angleHitFrom,
        float contactPointX,
        float contactPointY,
        float contactPointZ)
    {
        Debug.Log($"[CharacterNetworkManager] ProcessCharacterDamageFromServer - Looking for damaged character ID: {damagedCharacterID}");

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(damagedCharacterID))
        {
            Debug.LogError($"[CharacterNetworkManager] Damaged character ID {damagedCharacterID} not found in SpawnedObjects!");
            return;
        }
        CharacterManager damageCharcter = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagedCharacterID].gameObject.GetComponent<CharacterManager>();

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(characterCausingDamageID))
        {
            Debug.LogError($"[CharacterNetworkManager] Attacker character ID {characterCausingDamageID} not found in SpawnedObjects!");
            return;
        }
        CharacterManager characterCausingDamage = NetworkManager.Singleton.SpawnManager.SpawnedObjects[characterCausingDamageID].gameObject.GetComponent<CharacterManager>();

        if (damageCharcter == null || characterCausingDamage == null)
        {
            Debug.LogError("[CharacterNetworkManager] Could not find CharacterManager on one of the spawned objects.");
            return;
        }

        Debug.Log($"[CharacterNetworkManager] Found characters - Damaged: {damageCharcter.gameObject.name}, Attacker: {characterCausingDamage.gameObject.name}");

        if (damageCharcter.isDead.Value)
        {
            Debug.Log($"[CharacterNetworkManager] Target already dead, skipping damage processing");
            return;
        }

        if (WorldCharacterEffectsManager.instance == null)
        {
            Debug.LogError("[CharacterNetworkManager] WorldCharacterEffectsManager.instance is NULL!");
            return;
        }
        
        TakeDamageEffect damageEffect = Instantiate(WorldCharacterEffectsManager.instance.takeDamageEffect);

        damageEffect.physicalDamage = physicalDamage;
        damageEffect.magicDamage = magicDamage;
        damageEffect.fireDamage = fireDamage;
        damageEffect.holyDamage = holyDamage;
        damageEffect.poiseDamage = poiseDamage;
        damageEffect.angleHitFrom = angleHitFrom;
        damageEffect.contactPoint = new Vector3(contactPointX, contactPointY, contactPointZ);
        damageEffect.characterCausingDamage = characterCausingDamage;

        Debug.Log($"[CharacterNetworkManager] Processing damage effect on {damageCharcter.gameObject.name}");

        damageCharcter.characterEffectsManager.ProcessInstantEffect(damageEffect);
    }
    
    private void EnsureAnimatorSetup()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("[CharacterNetworkManager] Animator not found on client!");
                return;
            }
        }

        if (animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = defaultController;
            Debug.Log("[CharacterNetworkManager] Default controller re-applied on client.");
        }

        if (actionOverrideController != null)
        {
            if (animator.runtimeAnimatorController != actionOverrideController)
            {
                animator.runtimeAnimatorController = actionOverrideController;
                Debug.Log("[CharacterNetworkManager] Action Override controller re-applied for client ✅");
            }
        }
        else
        {
            Debug.LogWarning("[CharacterNetworkManager] Action Override controller reference is null!");
        }
    }
}