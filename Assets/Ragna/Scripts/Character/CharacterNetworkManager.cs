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

    public void CheckHp(int oldValue, int newValue)  // FIXED: Param names to match OnValueChanged delegate (was newVlue)
    {
        if (currentHealth.Value <= 0)
        {
            StartCoroutine(character.ProcessDeathEvent());
        }

        // PREVENTS US FROM OVER HEALING
        if (character.IsOwner)
        {
            if (currentHealth.Value > maxHealth.Value)
            {
                currentHealth.Value = maxHealth.Value;
            }
        }
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

    private void PerformActionAnimationFromServer(string animationID, bool applyRootMotion)
    {
        character.applyRootMotion = applyRootMotion;
        character.animator.CrossFade(animationID, 0.2f);
    }

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
}

