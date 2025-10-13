using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterNetworkManager : NetworkBehaviour
{
    CharacterManager character;

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
    

    protected virtual void Awake()
    {
        character = GetComponent<CharacterManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"[CharacterNetworkManager] OnNetworkSpawn - ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, Character: {gameObject.name}");
        
        // Subscribe to health changes on ALL clients
        currentHealth.OnValueChanged += CheckHp;
        
        Debug.Log($"[CharacterNetworkManager] Subscribed to currentHealth.OnValueChanged for {gameObject.name}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        Debug.Log($"[CharacterNetworkManager] OnNetworkDespawn - Unsubscribing for {gameObject.name}");
        
        // Unsubscribe to prevent memory leaks
        currentHealth.OnValueChanged -= CheckHp;
    }

    public void CheckHp(int oldValue, int newValue)
    {
        Debug.Log($"[CharacterNetworkManager] CheckHp CALLED - Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}, OldHP: {oldValue}, NewHP: {newValue}, IsDead: {character.isDead.Value}");

        if (character.isDead.Value)
        {
            Debug.Log($"[CharacterNetworkManager] Character already dead, returning early");
            return;  // Prevent re-triggering
        }

        if (newValue <= 0)
        {
            Debug.Log($"[CharacterNetworkManager] Health <= 0! Starting ProcessDeathEvent for {gameObject.name}");
            StartCoroutine(character.ProcessDeathEvent());
        }

        // PREVENTS US FROM OVER HEALING
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

    // A SERVER RPC IS A FUNCTION THAT IS CALLED FROM A CLIENT, TO THE SERVER (IN OUR CASE THE HOST )
    [ServerRpc]
    public void NotifyTheServerOfActionAnimationServerRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        if (IsServer)
        {
            // IF THIS CHARACTER IS THE SERVER / HOST, THEN ACTIVATE THE CLIENT RPC TO TELL ALL THE OTHER CLIENTS TO PLAY THE ANIMATION
            PlayActionAnimationForAllClientsClientRpc(clientID, animationID, applyRootMotion);
        }
    }

    // PLAY ACTION ANIMATION FOR ALL CLIENTS PRESENT, FROM THE SERVER / HOST
    [ClientRpc]
    public void PlayActionAnimationForAllClientsClientRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        // WE MAKE SURE TO NOT RUN THE ANIMATION/FUNCTION ON THE CHARACTER WHO SENT IT (SO WE DON'T PLAY THE ANIMATION TWICE)
        if (clientID != NetworkManager.Singleton.LocalClientId)
        {
            PerformActionAnimationFromServer(animationID, applyRootMotion);
        }
    }

    // Made public for use in death sync callback
    public void PerformActionAnimationFromServer(string animationID, bool applyRootMotion)
    {
        Debug.Log($"[CharacterNetworkManager] PerformActionAnimationFromServer - Animation: {animationID}, ApplyRootMotion: {applyRootMotion}, Character: {gameObject.name}, ClientID: {NetworkManager.Singleton.LocalClientId}, IsOwner: {character.IsOwner}");
        
        // Check if animator exists
        if (character.animator == null)
        {
            Debug.LogError($"[CharacterNetworkManager] ANIMATOR IS NULL for {gameObject.name}!");
            return;
        }
        
        // Check current animator state
        var currentState = character.animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[CharacterNetworkManager] Current Animator State: {currentState.shortNameHash}, IsPlaying: {character.animator.isActiveAndEnabled}");
        
        character.applyRootMotion = applyRootMotion;
        
        // CRITICAL FIX: Use Play for death to force immediate switch
        if (animationID == "Death")
        {
            Debug.Log($"[CharacterNetworkManager] FORCING DEATH ANIMATION with Play()");
            
            // Stop all current actions
            character.isPerformingAction = false;
            
            // Disable root motion temporarily to prevent movement issues
            character.animator.applyRootMotion = false;
            
            // Try multiple methods to force the death animation
            // Method 1: Try with hash (more reliable)
            int deathHash = Animator.StringToHash("Death");
            Debug.Log($"[CharacterNetworkManager] Death state hash: {deathHash}");
            
            // Method 2: Reset animator first to clear any blocking states
            character.animator.Rebind();
            character.animator.Update(0f);
            
            // Method 3: Force play with hash
            character.animator.Play(deathHash, 0, 0f);
            
            // Method 4: Also try layer -1 (all layers)
            character.animator.Play("Death", -1, 0f);
            
            Debug.Log($"[CharacterNetworkManager] Death animation Play() called with multiple methods");
            
            // Re-enable root motion after a frame
            character.applyRootMotion = applyRootMotion;
            
            // Check if it actually changed
            StartCoroutine(CheckAnimatorStateAfterFrame(animationID));
        }
        else
        {
            character.animator.CrossFade(animationID, 0.2f);
        }
    }
    
    private System.Collections.IEnumerator CheckAnimatorStateAfterFrame(string expectedAnimation)
    {
        yield return null; // Wait one frame
        var newState = character.animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[CharacterNetworkManager] After frame check - Current state hash: {newState.shortNameHash}, Expected: {expectedAnimation}, IsPlaying: {character.animator.isActiveAndEnabled}");
        
        // Also check if the animator parameter/state exists
        bool hasDeathState = false;
        foreach (var clip in character.animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == expectedAnimation)
            {
                hasDeathState = true;
                Debug.Log($"[CharacterNetworkManager] Found '{expectedAnimation}' animation clip in animator");
                break;
            }
        }
        
        if (!hasDeathState)
        {
            Debug.LogError($"[CharacterNetworkManager] '{expectedAnimation}' animation clip NOT FOUND in animator!");
        }
    }

    // ATTACK ANIMATIONS

    // A SERVER RPC IS A FUNCTION THAT IS CALLED FROM A CLIENT, TO THE SERVER (IN OUR CASE THE HOST )
    [ServerRpc]
    public void NotifyTheServerOfAttackActionAnimationServerRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        if (IsServer)
        {
            // IF THIS CHARACTER IS THE SERVER / HOST, THEN ACTIVATE THE CLIENT RPC TO TELL ALL THE OTHER CLIENTS TO PLAY THE ANIMATION
            PlayAttackActionAnimationForAllClientsClientRpc(clientID, animationID, applyRootMotion);
        }
    }

    // PLAY ACTION ANIMATION FOR ALL CLIENTS PRESENT, FROM THE SERVER / HOST
    [ClientRpc]
    public void PlayAttackActionAnimationForAllClientsClientRpc(ulong clientID, string animationID, bool applyRootMotion)
    {
        // WE MAKE SURE TO NOT RUN THE ANIMATION/FUNCTION ON THE CHARACTER WHO SENT IT (SO WE DON'T PLAY THE ANIMATION TWICE)
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
        
        if(IsServer)
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
        
        // CRITICAL FIX: Don't process damage if already dead
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
}