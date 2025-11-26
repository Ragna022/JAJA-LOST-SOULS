using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.VisualScripting;
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
    public NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> horizontalMovement = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> verticalMovement = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> moveAmount = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Target")]
    public NetworkVariable<ulong> currentTargetNetworkObjectID = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Flags")]
    public NetworkVariable<bool> isLockedOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isSprinting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isChargingAttack = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Game State")]
    public NetworkVariable<bool> hasWon = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

        if (IsClient)
        {
            Debug.Log($"[CharacterNetworkManager] Applying initial EnsureAnimatorSetup() for {gameObject.name}");
            EnsureAnimatorSetup(); 
            StartCoroutine(WaitAndEnsureAnimatorSetup());
        }

        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeathStateChanged;
        hasWon.OnValueChanged += OnVictoryStateChanged;
    }

    private IEnumerator WaitAndEnsureAnimatorSetup()
    {
        yield return null;
        yield return null;

        EnsureAnimatorSetup();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        currentHealth.OnValueChanged -= OnHealthChanged;
        isDead.OnValueChanged -= OnDeathStateChanged;
        hasWon.OnValueChanged -= OnVictoryStateChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        Debug.Log($"[CharacterNetworkManager] Health changed from {oldValue} to {newValue} for {gameObject.name} (LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner})");

        // CRITICAL: Clamp health to 0 minimum - never allow negative health
        if (newValue < 0 && IsOwner)
        {
            Debug.LogWarning($"[CharacterNetworkManager] Health went negative ({newValue}), clamping to 0 for {gameObject.name}");
            currentHealth.Value = 0;
            return;
        }

        // Check for death: health reached 0 or below
        if (oldValue > 0 && newValue <= 0 && IsOwner)
        {
            Debug.Log($"[CharacterNetworkManager] *** DEATH DETECTED *** Setting isDead=true for {gameObject.name} (Owner)");
            currentHealth.Value = 0; // Ensure it's exactly 0
            isDead.Value = true;
            return;
        }

        // Clamp to max health
        if (IsOwner && newValue > maxHealth.Value)
        {
            currentHealth.Value = maxHealth.Value;
        }
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        Debug.Log($"[CharacterNetworkManager] *** DEATH STATE CHANGED *** isDead=true for {gameObject.name} (LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner})");

        // IMMEDIATELY stop all actions and play death animation
        PlayDeathAnimation();

        // AUTO DESTROY AI AFTER DEATH
        if (character is AICharacterManager && IsServer)
        {
            StartCoroutine(AutoDestroyAIAfterDelay());
        }


        // Check if this is a player to show defeat UI
        if (IsOwner)
        {
            PlayerManager player = character as PlayerManager;
            if (player != null)
            {
                PlayerUIManager playerUI = FindFirstObjectByType<PlayerUIManager>();
                if (playerUI != null)
                {
                    playerUI.playerUIPopUpManager.SendDefeatPanel();
                }
            }
        }

        // Check victory condition on server
        if (IsServer)
        {
            StartCoroutine(CheckVictoryNextFrame());
        }
    }
    
    public void OnIsMovingChanged(bool oldStatus, bool newStatus)
    {
        character.animator.SetBool("isMoving", isMoving.Value);
    }

    private IEnumerator CheckVictoryNextFrame()
    {
        yield return new WaitForEndOfFrame();
        CheckForVictoryCondition();
    }

    private void OnVictoryStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        Debug.Log($"[CharacterNetworkManager] hasWon=true! Showing VICTORY panel for {gameObject.name} (LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner})");

        if (IsOwner)
        {
            PlayerManager player = character as PlayerManager;
            if (player != null)
            {
                PlayerUIManager playerUI = FindFirstObjectByType<PlayerUIManager>();
                if (playerUI != null)
                {
                    playerUI.playerUIPopUpManager.SendVictoryPanel();
                }
            }
        }
    }

    private void CheckForVictoryCondition()
    {
        if (!IsServer) return;

        Debug.Log($"[CheckVictory] ===== VICTORY CHECK STARTED ===== Total SpawnedObjects: {NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count}");

        // Track ALIVE status by OwnerClientId for PLAYERS
        Dictionary<ulong, bool> ownerAliveStatus = new Dictionary<ulong, bool>();
        Dictionary<ulong, CharacterManager> ownerToCharacter = new Dictionary<ulong, CharacterManager>();
        
        // Track AI characters
        int totalAICharacters = 0;
        int aliveAICharacters = 0;
        
        foreach (var spawnedObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            CharacterManager charManager = spawnedObj.GetComponent<CharacterManager>();
            if (charManager == null)
                continue;

            PlayerManager playerManager = charManager as PlayerManager;
            AICharacterManager aiManager = charManager as AICharacterManager;
            
            // Handle PLAYER characters
            if (playerManager != null)
            {
                ulong ownerClientId = spawnedObj.OwnerClientId;
                bool isAlive = !charManager.characterNetworkManager.isDead.Value;
                
                Debug.Log($"[CheckVictory] Found PLAYER: {charManager.gameObject.name} (NetworkID: {spawnedObj.NetworkObjectId}, OwnerID: {ownerClientId}), IsAlive: {isAlive}");
                
                // If this owner is dead in ANY of their instances, mark them as dead
                if (!ownerAliveStatus.ContainsKey(ownerClientId))
                {
                    ownerAliveStatus[ownerClientId] = isAlive;
                    ownerToCharacter[ownerClientId] = charManager;
                }
                else
                {
                    // If we find a dead instance for this owner, they're dead
                    if (!isAlive)
                    {
                        ownerAliveStatus[ownerClientId] = false;
                    }
                    // Always keep track of an ALIVE character reference if one exists
                    if (isAlive && !ownerAliveStatus[ownerClientId])
                    {
                        ownerToCharacter[ownerClientId] = charManager;
                        ownerAliveStatus[ownerClientId] = true;
                    }
                }
            }
            // Handle AI characters
            else if (aiManager != null)
            {
                totalAICharacters++;
                bool isAlive = !charManager.characterNetworkManager.isDead.Value;
                
                if (isAlive)
                {
                    aliveAICharacters++;
                }
                
                Debug.Log($"[CheckVictory] Found AI: {charManager.gameObject.name} (NetworkID: {spawnedObj.NetworkObjectId}), IsAlive: {isAlive}");
            }
        }

        // Find alive player owners
        List<ulong> alivePlayerOwners = new List<ulong>();
        foreach (var kvp in ownerAliveStatus)
        {
            Debug.Log($"[CheckVictory] Player OwnerID {kvp.Key}: IsAlive={kvp.Value}");
            if (kvp.Value)
            {
                alivePlayerOwners.Add(kvp.Key);
            }
        }

        Debug.Log($"[CheckVictory] ===== SUMMARY =====");
        Debug.Log($"[CheckVictory] Total unique player owners: {ownerAliveStatus.Count}, Alive player owners: {alivePlayerOwners.Count}");
        Debug.Log($"[CheckVictory] Total AI characters: {totalAICharacters}, Alive AI characters: {aliveAICharacters}");
        Debug.Log($"[CheckVictory] ====================");

        // VICTORY CONDITIONS:
        // 1. Only ONE player alive AND all AI are dead
        // 2. Multiple players can be alive as long as all AI are dead
        
        bool allAIDead = (aliveAICharacters == 0 && totalAICharacters > 0);
        bool anyPlayerAlive = alivePlayerOwners.Count > 0;
        
        if (allAIDead && anyPlayerAlive)
        {
            Debug.Log($"[CheckVictory] *** VICTORY CONDITION MET! *** All AI defeated, {alivePlayerOwners.Count} player(s) alive!");
            
            // Award victory to ALL alive players
            foreach (ulong winnerOwnerId in alivePlayerOwners)
            {
                CharacterManager winner = ownerToCharacter[winnerOwnerId];
                Debug.Log($"[CheckVictory] Awarding VICTORY to OwnerID: {winnerOwnerId}, Character: {winner.gameObject.name} (NetworkID: {winner.NetworkObjectId})");
                winner.characterNetworkManager.hasWon.Value = true;
            }
        }
        else if (alivePlayerOwners.Count == 0)
        {
            Debug.LogWarning("[CheckVictory] All players are dead - DEFEAT!");
        }
        else if (aliveAICharacters > 0)
        {
            Debug.Log($"[CheckVictory] Victory not yet achieved - {aliveAICharacters} AI character(s) still alive");
        }
        else if (totalAICharacters == 0)
        {
            Debug.LogWarning("[CheckVictory] No AI characters found in the scene - cannot determine victory condition");
        }
    }

    private void PlayDeathAnimation()
    {
        if (character == null)
        {
            Debug.LogError($"[CharacterNetworkManager] Character is NULL in PlayDeathAnimation!");
            return;
        }

        if (character.animator == null)
        {
            Debug.LogError($"[CharacterNetworkManager] Animator NULL in PlayDeathAnimation for {gameObject.name}!");
            return;
        }

        Debug.Log($"[CharacterNetworkManager] *** PLAYING DEATH ANIMATION *** for {gameObject.name}");

        // CRITICAL: Set character flags to stop all actions
        character.isPerformingAction = true; // Lock character from performing new actions
        character.canRotate = false;
        character.canMove = false;
        character.isDead = true;
        character.applyRootMotion = true;

        // For AI characters, stop their AI state machine
        AICharacterManager aiCharacter = character as AICharacterManager;
        if (aiCharacter != null)
        {
            Debug.Log($"[CharacterNetworkManager] AI Character detected - stopping AI behavior for {gameObject.name}");
            // The AI state machine will check isDead flag and stop processing
        }

        // STOP all current animations and play death immediately
        int deathHash = Animator.StringToHash("Death");
        int actionLayer = character.animator.GetLayerIndex("Action Override");
        if (actionLayer == -1) actionLayer = 0;

        // Force animator to rebind and update to ensure clean state
        character.animator.Rebind();
        character.animator.Update(0f);
        
        // Play death animation immediately with no crossfade
        character.animator.Play(deathHash, actionLayer, 0f);

        Debug.Log($"[CharacterNetworkManager] Death animation FORCED on layer {actionLayer} for {gameObject.name}");

        // Disable character collision after a short delay to let death animation play
        StartCoroutine(DisableCollisionAfterDeath());
    }

    private IEnumerator DisableCollisionAfterDeath()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (character != null && character.characterController != null)
        {
            character.characterController.enabled = false;
            Debug.Log($"[CharacterNetworkManager] Character controller disabled for {gameObject.name}");
        }
    }

    public void OnLockOnTargetIDChabge(ulong oldID, ulong newID)
    {
        if (IsOwner)
        {
            character.characterCombatManager.currentTarget = NetworkManager.Singleton.SpawnManager.SpawnedObjects[newID].gameObject.GetComponent<CharacterManager>();
        }
    }
    
    public void OnIsLockedOnChanged(bool old, bool isLockedOn)
    {
        if(!isLockedOn)
        {
            character.characterCombatManager.currentTarget = null;
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
        
        character.animator.CrossFade(animationID, 0.2f);
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

    private IEnumerator AutoDestroyAIAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (character != null &&
            character.NetworkObject != null &&
            character.NetworkObject.IsSpawned)
        {
            character.NetworkObject.Despawn(true); // true = destroy GameObject
        }
    }

}