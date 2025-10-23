using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Collections;
using System;
using TMPro;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;
    
    [Header("UI References")]
    public GameObject lobbyPanel;
    public Button startGameButton;
    public TMP_Text statusText;
    public Button readyButton;
    public Button leaveButton;
    public TMP_Text readyButtonText;
    
    [Header("Player List")]
    public Transform playerListContainer;
    public GameObject playerSlotPrefab;
    
    private NetworkList<LobbyPlayerData> lobbyPlayers;
    private Dictionary<ulong, GameObject> playerSlots = new Dictionary<ulong, GameObject>();
    private bool isReady = false;

    // This list will hold the player data *during* the scene transition
    private List<LobbyPlayerData> persistentLobbyData; // <-- NEW
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        lobbyPlayers = new NetworkList<LobbyPlayerData>();
    }
    
    private void Start()
    {
        Debug.Log("🔄 LobbyManager Start() called");
        ValidateUIPrefabs();
    }
    
    private void ValidateUIPrefabs()
    {
        Debug.Log("🔍 Validating UI prefabs...");
        
        if (playerSlotPrefab != null)
        {
            NetworkObject netObj = playerSlotPrefab.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Debug.LogError($"❌ playerSlotPrefab '{playerSlotPrefab.name}' has NetworkObject component!");
                Debug.LogError("   UI elements should NOT have NetworkObject components!");
            }
            else
            {
                Debug.Log("✅ playerSlotPrefab is properly configured (no NetworkObject)");
            }
        }
    }
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"🎯 LobbyManager.OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, NetworkObjectId: {NetworkObjectId}");
        
        // Server-only setup
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Add host to lobby
            AddPlayerToLobby(NetworkManager.Singleton.LocalClientId, "Host", false);
            Debug.Log("✅ SERVER: Added host to lobby");
        }
        
        // Everyone subscribes to list changes
        lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;
        
        // Setup UI for everyone
        SetupUI();
        UpdatePlayerListUI();
        
        // Clients request their data be added after a short delay
        if (!IsServer)
        {
            StartCoroutine(DelayedSubmitData());
        }
        
        Debug.Log("✅ LobbyManager.OnNetworkSpawn complete");
    }
    
    private System.Collections.IEnumerator DelayedSubmitData()
    {
        yield return new WaitForSeconds(0.5f);
        SubmitPlayerData();
    }
    
    private void SetupUI()
    {
        Debug.Log("🔄 Setting up Lobby UI...");
        
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(IsServer);
            startGameButton.onClick.AddListener(StartGame);
            startGameButton.interactable = false;
        }
        
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReadyStatus);
        }
        
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(LeaveLobby);
        }
        
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "UNREADY" : "READY";
        }
        
        Debug.Log("✅ Lobby UI setup complete");
    }
    
    public void ToggleReadyStatus()
    {
        isReady = !isReady;
        ToggleReadyStatusServerRpc(NetworkManager.Singleton.LocalClientId, isReady);
        
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "UNREADY" : "READY";
        }

        Debug.Log($"🔄 Ready status toggled to: {isReady}");
    }
    
    private void SubmitPlayerData()
    {
        string playerName = "Player_" + NetworkManager.Singleton.LocalClientId;
        if (TitleScreenManager.selectedPlayerPrefab != null)
        {
            playerName = TitleScreenManager.selectedPlayerPrefab.name;
        }
        
        SubmitPlayerDataServerRpc(NetworkManager.Singleton.LocalClientId, playerName, false);
        Debug.Log($"📤 CLIENT: Submitting player data: {playerName}");
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"🎯 SERVER: Client {clientId} connected to lobby");
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"🎯 SERVER: Client {clientId} disconnected from lobby");
        if (IsServer)
        {
            RemovePlayerFromLobby(clientId);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerDataServerRpc(ulong clientId, FixedString64Bytes playerName, bool isReady, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"📥 SERVER: Received player data from {clientId}: {playerName}, Ready: {isReady}");
        AddPlayerToLobby(clientId, playerName.ToString(), isReady);
    }
    
    private void AddPlayerToLobby(ulong clientId, string playerName, bool isReady)
    {
        // Update existing player
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                var updatedPlayer = new LobbyPlayerData
                {
                    clientId = clientId,
                    playerName = new FixedString64Bytes(playerName),
                    isReady = isReady
                };
                lobbyPlayers[i] = updatedPlayer;
                Debug.Log($"🔄 SERVER: Updated player: {playerName} (Ready: {isReady})");
                return;
            }
        }
        
        // Add new player
        lobbyPlayers.Add(new LobbyPlayerData
        {
            clientId = clientId,
            playerName = new FixedString64Bytes(playerName),
            isReady = isReady
        });
        
        Debug.Log($"✅ SERVER: Added player: {playerName} (Ready: {isReady}) - Total: {lobbyPlayers.Count}");
    }
    
    private void RemovePlayerFromLobby(ulong clientId)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                string playerName = lobbyPlayers[i].playerName.ToString();
                lobbyPlayers.RemoveAt(i);
                Debug.Log($"🗑️ SERVER: Removed player: {playerName}");
                break;
            }
        }
        
        if (playerSlots.ContainsKey(clientId))
        {
            Destroy(playerSlots[clientId]);
            playerSlots.Remove(clientId);
        }
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        Debug.Log($"📋 Lobby players changed (Event: {changeEvent.Type}) - Total: {lobbyPlayers.Count}");
        UpdatePlayerListUI();
        
        if (IsServer)
        {
            CheckAllPlayersReady();
        }
    }
    
    private void UpdatePlayerListUI()
    {
        // Clear existing slots
        foreach (var slot in playerSlots.Values)
        {
            if (slot != null) 
            {
                Destroy(slot);
            }
        }
        playerSlots.Clear();
        
        // Create new slots
        foreach (var player in lobbyPlayers)
        {
            if (playerListContainer != null && playerSlotPrefab != null)
            {
                GameObject playerSlot = Instantiate(playerSlotPrefab, playerListContainer);
                playerSlot.name = $"PlayerSlot_{player.clientId}";
                
                SetupPlayerSlotUI(playerSlot, player);
                playerSlots[player.clientId] = playerSlot;
            }
        }
        
        UpdateStatusText();
    }
    
    private void SetupPlayerSlotUI(GameObject playerSlot, LobbyPlayerData playerData)
    {
        LobbyPlayerUI playerUI = playerSlot.GetComponent<LobbyPlayerUI>();

        if (playerUI != null)
        {
            bool isHost = playerData.clientId == NetworkManager.ServerClientId;
            playerUI.SetPlayerData(playerData.playerName.ToString(), playerData.isReady, isHost);
        }
        else
        {
            Debug.LogError($"❌ PlayerSlot prefab missing LobbyPlayerUI component!");
        }
    }
    
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            string status = $"🎮 MULTIPLAYER LOBBY\n";
            status += $"Players: {lobbyPlayers.Count}/4\n\n";
            
            foreach (var player in lobbyPlayers)
            {
                string readyStatus = player.isReady ? "✅ READY" : "❌ NOT READY";
                string hostIndicator = player.clientId == NetworkManager.ServerClientId ? " 👑" : "";
                status += $"{player.playerName}{hostIndicator}\n{readyStatus}\n\n";
            }
            
            statusText.text = status;
        }
    }
    
    private void CheckAllPlayersReady()
    {
        if (lobbyPlayers.Count < 1) 
        {
            if (startGameButton != null)
                startGameButton.interactable = false;
            return;
        }
        
        bool allReady = true;
        foreach (var player in lobbyPlayers)
        {
            if (!player.isReady)
            {
                allReady = false;
                break;
            }
        }
        
        if (startGameButton != null)
        {
            startGameButton.interactable = allReady;
            Debug.Log($"🎮 SERVER: Start button {(allReady ? "ENABLED" : "DISABLED")}");
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyStatusServerRpc(ulong clientId, bool readyStatus, ServerRpcParams rpcParams = default)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                var updatedPlayer = lobbyPlayers[i];
                updatedPlayer.isReady = readyStatus;
                lobbyPlayers[i] = updatedPlayer;
                Debug.Log($"🔄 SERVER: Player {lobbyPlayers[i].playerName} ready: {readyStatus}");
                break;
            }
        }
    }
    
    public void StartGame()
    {
        if (!IsServer) return;
        
        Debug.Log("🎯 SERVER: Starting game!");

        // --- START OF FIX ---
        // 1. Create the persistent list
        persistentLobbyData = new List<LobbyPlayerData>(); // <-- NEW

        // 2. Copy the data from the NetworkList to the normal List
        foreach (var player in lobbyPlayers) // <-- NEW
        {
            persistentLobbyData.Add(player); // <-- NEW
        }
        
        Debug.Log($"📝 Copied {persistentLobbyData.Count} players to persistent list."); // <-- NEW
        // --- END OF FIX ---
        
        // Hide lobby UI
        HideLobbyUIClientRpc();
        
        // Subscribe to scene load completion
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        
        // Load game scene
        NetworkManager.Singleton.SceneManager.LoadScene("World_01", LoadSceneMode.Single);
    }
    
    [ClientRpc]
    private void HideLobbyUIClientRpc()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
            Debug.Log("🖥️ Lobby UI hidden");
        }
    }
    
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"🎯 Scene '{sceneName}' loaded - Clients: {clientsCompleted.Count}, TimedOut: {clientsTimedOut.Count}");
        
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        
        if (IsServer && clientsCompleted.Count > 0)
        {
            SpawnAllPlayers();
        }
    }
    
    private void SpawnAllPlayers()
    {
        // Use the persistent list, as lobbyPlayers has been disposed
        Debug.Log($"🎮 SERVER: Spawning {persistentLobbyData.Count} players..."); // <-- MODIFIED
        
        foreach (var playerData in persistentLobbyData) // <-- MODIFIED
        {
            SpawnPlayerForClient(playerData.clientId);
        }
    }
    
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (TitleScreenManager.selectedPlayerPrefab == null)
        {
            Debug.LogError("❌ No player prefab selected!");
            return;
        }
        
        GameObject playerPrefab = TitleScreenManager.selectedPlayerPrefab;
        NetworkObject playerObject = Instantiate(playerPrefab).GetComponent<NetworkObject>();
        
        if (playerObject != null)
        {
            Vector3 spawnPos = CalculateSpawnPosition(clientId);
            playerObject.transform.position = spawnPos;
            
            playerObject.SpawnAsPlayerObject(clientId, true);
            
            Debug.Log($"🎮 SERVER: Spawned player for client {clientId} at {spawnPos}");
        }
        else
        {
            Debug.LogError($"❌ Player prefab missing NetworkObject: {playerPrefab.name}");
        }
    }
    
    private Vector3 CalculateSpawnPosition(ulong clientId)
    {
        int playerIndex = 0;
        
        // Use the persistent list here as well
        for (int i = 0; i < persistentLobbyData.Count; i++) // <-- MODIFIED
        {
            if (persistentLobbyData[i].clientId == clientId) // <-- MODIFIED
            {
                playerIndex = i;
                break;
            }
        }
        
        float spacing = 3f;
        return new Vector3(
            (playerIndex % 2) * spacing, 
            1f, 
            Mathf.Floor(playerIndex / 2) * spacing
        );
    }
    
    public void LeaveLobby()
    {
        Debug.Log("👋 Leaving lobby...");
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        SceneManager.LoadScene("MainMenu");
    }
    
    public override void OnNetworkDespawn()
    {
        Debug.Log("👋 LobbyManager.OnNetworkDespawn");
        
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        if (lobbyPlayers != null)
        {
            lobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
        }
    }
}