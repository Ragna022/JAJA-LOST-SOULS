using System.Collections.Generic;
using UnityEngine;

public class WorldGameSessionManager : MonoBehaviour
{
    public static WorldGameSessionManager instance;

    [Header("Active Players In Session")]
    public List<PlayerManager> players = new List<PlayerManager>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddPlayerToActivePlayerList(PlayerManager player)
    {
        // CHECK THE LIST, IF IT DOES NOT ALREADY CONTAIN THE PALYER, ADD THEM
        if (!players.Contains(player))
        {
            players.Add(player);
        }

        // CHECK THE LIST FOR NULL SLOTS, AND REMOVE THE NULL SLOTS
        for (int i = players.Count - 1; i > -1; i--)
        {
            if (players[i] == null)
            {
                players.RemoveAt(i);
            }
        }
    }
    
    public void RemovePlayerFromActivePlayerList(PlayerManager player)
    {
        // CHECK THE LIST, IF IT DOES CONTAIN THE PALYER, REMOVE THEM
        if (players.Contains(player))
        {
            players.Remove(player);
        }

        // CHECK THE LIST FOR NULL SLOTS, AND REMOVE THE NULL SLOTS
        for (int i = players.Count - 1; i > -1; i--)
        {
            if(players[i] == null)
            {
                players.RemoveAt(i);
            }
        }
    }
}
