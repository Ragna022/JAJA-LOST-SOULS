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

    // Delay animator setup slightly for clients to ensure animator fully initializes
    if (IsClient)
    {
        StartCoroutine(WaitAndEnsureAnimatorSetup());
    }

    Debug.Log($"[CharacterNetworkManager] OnNetworkSpawn - ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, Character: {gameObject.name}");

    currentHealth.OnValueChanged += CheckHp;

    Debug.Log($"[CharacterNetworkManager] Subscribed to currentHealth.OnValueChanged for {gameObject.name}");
}

// 🕓 NEW helper coroutine — ensures Action Override is fully applied on clients after spawn
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
        Debug.Log($"[CharacterNetworkManager] CheckHp CALLED - Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, OldHP: {oldValue}, NewHP: {newValue}, IsDead: {character.isDead.Value}");

        if (character.isDead.Value)
        {
            Debug.Log($"[CharacterNetworkManager] Character already dead, returning early");
            return;
        }

        if (newValue <= 0)
        {
            Debug.Log($"[CharacterNetworkManager] Health <= 0! Starting ProcessDeathEvent for {gameObject.name}");
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
        Debug.Log($"[CharacterNetworkManager] PerformActionAnimationFromServer - Animation: {animationID}, ApplyRootMotion: {applyRootMotion}, Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {character.IsOwner}");
        
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
            Debug.Log($"[CharacterNetworkManager] FORCING DEATH ANIMATION with Play()");
            
            character.isPerformingAction = false;
            character.animator.applyRootMotion = false;
            
            int deathHash = Animator.StringToHash("Death");
            Debug.Log($"[CharacterNetworkManager] Death state hash: {deathHash}");
            
            character.animator.Rebind();
            character.animator.Update(0f);
            character.animator.Play(deathHash, 0, 0f);
            character.animator.Play("Death", -1, 0f);
            
            Debug.Log($"[CharacterNetworkManager] Death animation Play() called with multiple methods");
            
            character.applyRootMotion = applyRootMotion;
            
            StartCoroutine(CheckAnimatorStateAfterFrame(animationID));
        }
        else
        {
            character.animator.CrossFade(animationID, 0.2f);
        }
    }

    // ============================
    // REPLACED: more robust, retrying, layer-aware checker
    // ============================
    private System.Collections.IEnumerator CheckAnimatorStateAfterFrame(string expectedAnimation)
    {
        // We will attempt up to N retries (small waits) so clients have time to receive override data.
        const int maxAttempts = 6;
        const float attemptDelay = 0.08f; // 80ms between attempts (total ~480ms max)
        int attempt = 0;

        if (character == null || character.animator == null)
        {
            Debug.LogError("[CharacterNetworkManager] Character or Animator reference missing during state check!");
            yield break;
        }

        // Prefer checking the Action Override layer, fallback to layer 0 if missing
        int actionLayerIndex = character.animator.GetLayerIndex("Action Override");
        if (actionLayerIndex == -1)
        {
            actionLayerIndex = 0;
            Debug.LogWarning("[CharacterNetworkManager] 'Action Override' layer not found, defaulting to Base Layer (0)");
        }

        // Retry loop: check state, then wait, then check again (gives clients time to load overrides)
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

            // small delay before next attempt
            attempt++;
            yield return new WaitForSeconds(attemptDelay);
        }

        // After retries, still not matched — inspect controllers/overrides for presence of the clip (for debugging)
        RuntimeAnimatorController controller = character.animator.runtimeAnimatorController;
        bool foundClip = false;

        if (controller is AnimatorOverrideController overrideController)
        {
            // Check base controller clips (the runtimeAnimatorController the override wraps)
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

            // Check override mappings (the actual clips assigned by the override controller)
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);
            foreach (var kv in overrides)
            {
                // kv.Key = original clip, kv.Value = override clip (may be null)
                if (kv.Value != null && kv.Value.name == expectedAnimation)
                {
                    foundClip = true;
                    break;
                }

                // As a fallback, sometimes the original clip name matches
                if (kv.Key != null && kv.Key.name == expectedAnimation)
                {
                    foundClip = true;
                    break;
                }
            }
        }
        else
        {
            // Regular controller case
            foreach (var clip in controller.animationClips)
            {
                if (clip != null && clip.name == expectedAnimation)
                {
                    foundClip = true;
                    break;
                }
            }
        }

        // Final log depending on whether the clip exists anywhere
        if (foundClip)
        {
            // Clip exists in controller/overrides but the client didn't transition to it in time.
            Debug.LogWarning($"[CharacterNetworkManager] '{expectedAnimation}' animation exists in controller/overrides but was not observed playing (client delay or transition mismatch).");
        }
        else
        {
            // Clip truly not found anywhere the script could inspect
            Debug.LogError($"[CharacterNetworkManager] '{expectedAnimation}' animation clip NOT FOUND even in override layers (checked 'Action Override' layer and controller)!");
        }
    }
    // ============================
    // END replaced method
    // ============================


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

        CharacterManager damageCharcter = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagedCharacterID].gameObject.GetComponent<CharacterManager>();
        CharacterManager characterCausingDamage = NetworkManager.Singleton.SpawnManager.SpawnedObjects[characterCausingDamageID].gameObject.GetComponent<CharacterManager>();

        Debug.Log($"[CharacterNetworkManager] Found characters - Damaged: {damageCharcter.gameObject.name}, Attacker: {characterCausingDamage.gameObject.name}");

        if (damageCharcter.isDead.Value)
        {
            Debug.Log($"[CharacterNetworkManager] Target already dead, skipping damage processing");
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
        // Re-apply Action Override layer if missing or not set correctly
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
