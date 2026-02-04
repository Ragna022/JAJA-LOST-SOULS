using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

        if (IsClient)
        {
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
        if (newValue < 0 && IsOwner)
        {
            currentHealth.Value = 0;
            return;
        }

        if (oldValue > 0 && newValue <= 0 && IsOwner)
        {
            currentHealth.Value = 0;
            isDead.Value = true;
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

        Debug.Log($"[CharacterNetworkManager] *** DEATH DETECTED *** for {gameObject.name}");

        PlayDeathAnimation();

        // Server handles AI cleanup
        if (character is AICharacterManager && IsServer)
        {
            StartCoroutine(AutoDestroyAIAfterDelay());
        }

        // --- FIX: ONLY DISABLE CONTROLS IF THIS IS A PLAYER ---
        // We check (character is PlayerManager) to ensure we don't disable controls 
        // when the Host kills an AI (which the Host technically "Owns")
        if (IsOwner && character is PlayerManager)
        {
            Debug.Log($"[CharacterNetworkManager] Local PLAYER Died: {gameObject.name}. Triggering Defeat.");

            // 1. Disable Mobile Controls
            if (MobileInputManager.instance != null)
            {
                MobileInputManager.instance.SetMobileControls(false);
            }

            // 2. Show Defeat UI
            PlayerUIManager playerUI = FindFirstObjectByType<PlayerUIManager>();
            if (playerUI != null)
            {
                playerUI.playerUIPopUpManager.SendDefeatPanel();
            }
        }
        // -----------------------------------------------------

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

        Debug.Log($"[CharacterNetworkManager] VICTORY DETECTED for {gameObject.name}");

        // --- FIX: ONLY DISABLE CONTROLS IF THIS IS A PLAYER ---
        if (IsOwner && character is PlayerManager)
        {
            if (MobileInputManager.instance != null)
            {
                MobileInputManager.instance.SetMobileControls(false);
            }

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

        List<CharacterManager> alivePlayers = new List<CharacterManager>();
        int aliveAICharacters = 0;

        Debug.Log("--- VICTORY CHECK START ---");

        foreach (var spawnedObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (spawnedObj == null || spawnedObj.gameObject == null) continue;

            CharacterManager charManager = spawnedObj.GetComponent<CharacterManager>();
            if (charManager == null) continue;

            // CHECK PLAYERS
            if (charManager is PlayerManager)
            {
                if (!charManager.characterNetworkManager.isDead.Value)
                {
                    alivePlayers.Add(charManager);
                    // LOG THE NAME SO WE KNOW WHO THIS IS
                    Debug.Log($"✅ Found ALIVE Player: {charManager.gameObject.name} (NetID: {charManager.NetworkObjectId})");
                }
                else
                {
                    Debug.Log($"💀 Found DEAD Player: {charManager.gameObject.name}");
                }
            }
            // CHECK AI
            else if (charManager is AICharacterManager)
            {
                if (!charManager.characterNetworkManager.isDead.Value)
                {
                    aliveAICharacters++;
                }
            }
        }

        Debug.Log($"[Summary] AI Left: {aliveAICharacters} | Players Left: {alivePlayers.Count}");

        if (aliveAICharacters > 0) 
        {
            Debug.Log("❌ Victory Failed: AI still alive.");
            return; 
        }

        if (alivePlayers.Count == 1)
        {
            CharacterManager winner = alivePlayers[0];
            Debug.Log($"👑 VICTORY TRIGGERED! Winner: {winner.name}");
            winner.characterNetworkManager.hasWon.Value = true;
        }
        else
        {
            // THIS WILL SHOW IN YOUR LOGS
            Debug.Log($"❌ Victory Failed: Player count is {alivePlayers.Count}. It must be EXACTLY 1.");
        }
        
        Debug.Log("--- VICTORY CHECK END ---");
    }

    private void PlayDeathAnimation()
    {
        if (character == null || character.animator == null) return;

        character.isPerformingAction = true;
        character.canRotate = false;
        character.canMove = false;
        character.isDead = true;
        character.applyRootMotion = true;

        int deathHash = Animator.StringToHash("Death");
        int actionLayer = character.animator.GetLayerIndex("Action Override");
        if (actionLayer == -1) actionLayer = 0;

        character.animator.Rebind();
        character.animator.Update(0f);
        character.animator.Play(deathHash, actionLayer, 0f);

        StartCoroutine(DisableCollisionAfterDeath());
    }

    private IEnumerator DisableCollisionAfterDeath()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (character != null && character.characterController != null)
        {
            character.characterController.enabled = false;
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
        if (character.animator == null) return;
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
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(damagedCharacterID)) return;
        CharacterManager damageCharcter = NetworkManager.Singleton.SpawnManager.SpawnedObjects[damagedCharacterID].gameObject.GetComponent<CharacterManager>();

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(characterCausingDamageID)) return;
        CharacterManager characterCausingDamage = NetworkManager.Singleton.SpawnManager.SpawnedObjects[characterCausingDamageID].gameObject.GetComponent<CharacterManager>();

        if (damageCharcter == null || characterCausingDamage == null) return;

        if (WorldCharacterEffectsManager.instance == null) return;
        
        TakeDamageEffect damageEffect = Instantiate(WorldCharacterEffectsManager.instance.takeDamageEffect);

        damageEffect.physicalDamage = physicalDamage;
        damageEffect.magicDamage = magicDamage;
        damageEffect.fireDamage = fireDamage;
        damageEffect.holyDamage = holyDamage;
        damageEffect.poiseDamage = poiseDamage;
        damageEffect.angleHitFrom = angleHitFrom;
        damageEffect.contactPoint = new Vector3(contactPointX, contactPointY, contactPointZ);
        damageEffect.characterCausingDamage = characterCausingDamage;

        damageCharcter.characterEffectsManager.ProcessInstantEffect(damageEffect);
    }
    
    private void EnsureAnimatorSetup()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = defaultController;
        }

        if (actionOverrideController != null)
        {
            if (animator.runtimeAnimatorController != actionOverrideController)
            {
                animator.runtimeAnimatorController = actionOverrideController;
            }
        }
    }

    private IEnumerator AutoDestroyAIAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (character != null && character.NetworkObject != null && character.NetworkObject.IsSpawned)
        {
            character.NetworkObject.Despawn(true);
        }
    }
}