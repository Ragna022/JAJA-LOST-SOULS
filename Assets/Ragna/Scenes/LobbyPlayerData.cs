using Unity.Netcode;
using System;
using Unity.Collections;

[System.Serializable]
public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong clientId;
    public FixedString64Bytes playerName;
    public bool isReady;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref isReady);
    }
    
    public bool Equals(LobbyPlayerData other)
    {
        return clientId == other.clientId &&
               playerName.Equals(other.playerName) &&
               isReady == other.isReady;
    }
    
    public override bool Equals(object obj)
    {
        return obj is LobbyPlayerData other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(clientId, playerName, isReady);
    }

    public static bool operator ==(LobbyPlayerData a, LobbyPlayerData b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(LobbyPlayerData a, LobbyPlayerData b)
    {
        return !a.Equals(b);
    }
}