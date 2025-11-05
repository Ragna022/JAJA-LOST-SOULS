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

        if (oldValue > 0 && newValue <= 0 && IsOwner)
        {
            Debug.Log($"[CharacterNetworkManager] *** DEATH DETECTED *** Setting isDead=true for {gameObject.name} (Owner)");
            isDead.Value = true;
            currentHealth.Value = 0;
            return;
        }

        if (IsOwner && newValue > maxHealth.Value)
        {
            currentHealth.Value = maxHealth.Value;
        }
    }

    private void OnDeathStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        Debug.Log($"[CharacterNetworkManager] isDead=true! Playing DEATH animation on {gameObject.name} (LocalClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner})");

        PlayDeathAnimation();

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

        if (IsServer)
        {
            StartCoroutine(CheckVictoryNextFrame());
        }
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

        Debug.Log($"[CheckVictory] Total SpawnedObjects: {NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count}");

        // Track ALIVE status by OwnerClientId
        Dictionary<ulong, bool> ownerAliveStatus = new Dictionary<ulong, bool>();
        Dictionary<ulong, CharacterManager> ownerToCharacter = new Dictionary<ulong, CharacterManager>();
        
        foreach (var spawnedObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            CharacterManager charManager = spawnedObj.GetComponent<CharacterManager>();
            if (charManager == null)
                continue;

            PlayerManager playerManager = charManager as PlayerManager;
            
            if (playerManager != null)
            {
                ulong ownerClientId = spawnedObj.OwnerClientId;
                bool isAlive = !charManager.characterNetworkManager.isDead.Value;
                
                Debug.Log($"[CheckVictory] Found Player: {charManager.gameObject.name} (NetworkID: {spawnedObj.NetworkObjectId}, OwnerID: {ownerClientId}), IsAlive: {isAlive}");
                
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
        }

        // Find alive owners
        List<ulong> aliveOwners = new List<ulong>();
        foreach (var kvp in ownerAliveStatus)
        {
            Debug.Log($"[CheckVictory] OwnerID {kvp.Key}: IsAlive={kvp.Value}");
            if (kvp.Value)
            {
                aliveOwners.Add(kvp.Key);
            }
        }

        Debug.Log($"[CheckVictory] Total unique owners: {ownerAliveStatus.Count}, Alive owners: {aliveOwners.Count}");

        if (aliveOwners.Count == 1)
        {
            ulong winnerOwnerId = aliveOwners[0];
            CharacterManager winner = ownerToCharacter[winnerOwnerId];
            
            Debug.Log($"[CheckVictory] *** VICTORY! *** Winner OwnerID: {winnerOwnerId}, Character: {winner.gameObject.name} (NetworkID: {winner.NetworkObjectId})");
            
            winner.characterNetworkManager.hasWon.Value = true;
        }
        else if (aliveOwners.Count == 0)
        {
            Debug.LogWarning("[CheckVictory] No players alive - draw or all died simultaneously!");
        }
    }

    private void PlayDeathAnimation()
    {
        if (character.animator == null)
        {
            Debug.LogError($"[CharacterNetworkManager] Animator NULL in PlayDeathAnimation for {gameObject.name}!");
            return;
        }

        character.isPerformingAction = false;
        character.canRotate = false;
        character.canMove = false;
        character.applyRootMotion = true;

        int deathHash = Animator.StringToHash("Death");
        int actionLayer = character.animator.GetLayerIndex("Action Override");
        if (actionLayer == -1) actionLayer = 0;

        character.animator.Rebind();
        character.animator.Update(0f);
        character.animator.Play(deathHash, actionLayer, 0f);

        Debug.Log($"[CharacterNetworkManager] Death animation PLAYED on layer {actionLayer} for {gameObject.name}");
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
}