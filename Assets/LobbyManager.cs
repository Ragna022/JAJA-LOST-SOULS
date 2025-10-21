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
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        lobbyPlayers = new NetworkList<LobbyPlayerData>();
    }
    
    private void Start()
    {
        Debug.Log("🔄 LobbyManager Start() called");
        
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("❌ LobbyManager is missing NetworkObject component!");
        }
        else
        {
            Debug.Log("✅ LobbyManager has NetworkObject component");
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !netObj.IsSpawned)
            {
                Debug.Log("🔄 Manually spawning LobbyManager on network...");
                netObj.Spawn();
            }
        }
    }
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"🎯 LobbyManager spawned on network - IsServer: {IsServer}, IsClient: {IsClient}, NetworkObjectId: {NetworkObjectId}");
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Add host to lobby
            AddPlayerToLobby(NetworkManager.Singleton.LocalClientId, "Host", false);
        }
        
        lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;
        
        SetupUI();
        UpdatePlayerListUI();
        
        if (!IsServer)
        {
            StartCoroutine(DelayedSubmitData());
        }
    }
    
    private System.Collections.IEnumerator DelayedSubmitData()
    {
        yield return new WaitForSeconds(1f);
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
            Debug.Log($"🎮 Start Game Button - Active: {startGameButton.gameObject.activeSelf}, Interactable: {startGameButton.interactable}");
        }
        
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReadyStatus);
            Debug.Log("✅ Ready button listener added");
        }
        
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(LeaveLobby);
            Debug.Log("✅ Leave button listener added");
        }
        
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Debug.Log("✅ Lobby panel activated");
        }

        // Set initial button text
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "UNREADY" : "READY";
        }
    }
    
    public void ToggleReadyStatus()
    {
        isReady = !isReady;
        ToggleReadyStatusServerRpc(NetworkManager.Singleton.LocalClientId, isReady);
        
        // Update button text
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "UNREADY" : "READY";
        }

        Debug.Log($"🔄 Ready status toggled to: {isReady}");
        
        UpdatePlayerListUI();
    }
    
    private void SubmitPlayerData()
    {
        string playerName = "Player_" + NetworkManager.Singleton.LocalClientId;
        if (TitleScreenManager.selectedPlayerPrefab != null)
        {
            playerName = TitleScreenManager.selectedPlayerPrefab.name;
        }
        
        SubmitPlayerDataServerRpc(NetworkManager.Singleton.LocalClientId, playerName, false);
        Debug.Log($"📤 Submitting player data: {playerName}");
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"🎯 Client {clientId} connected to lobby");
        
        if (IsServer)
        {
            RequestPlayerDataClientRpc(clientId);
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"🎯 Client {clientId} disconnected from lobby");
        if (IsServer)
        {
            RemovePlayerFromLobby(clientId);
        }
    }
    
    [ClientRpc]
    private void RequestPlayerDataClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log($"📥 Server requested our player data");
            SubmitPlayerData();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerDataServerRpc(ulong clientId, FixedString64Bytes playerName, bool isReady)
    {
        Debug.Log($"📥 Received player data from {clientId}: {playerName}, Ready: {isReady}");
        AddPlayerToLobby(clientId, playerName.ToString(), isReady);
    }
    
    private void AddPlayerToLobby(ulong clientId, string playerName, bool isReady)
    {
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
                Debug.Log($"🔄 Updated player in lobby: {playerName} (Ready: {isReady})");
                return;
            }
        }
        
        lobbyPlayers.Add(new LobbyPlayerData
        {
            clientId = clientId,
            playerName = new FixedString64Bytes(playerName),
            isReady = isReady
        });
        
        Debug.Log($"✅ Added player to lobby: {playerName} (Ready: {isReady}) - Total players: {lobbyPlayers.Count}");
    }
    
    private void RemovePlayerFromLobby(ulong clientId)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                string playerName = lobbyPlayers[i].playerName.ToString();
                lobbyPlayers.RemoveAt(i);
                Debug.Log($"🗑️ Removed player from lobby: {playerName}");
                break;
            }
        }
        
        if (playerSlots.ContainsKey(clientId))
        {
            Destroy(playerSlots[clientId]);
            playerSlots.Remove(clientId);
        }
    }

    //
    // --- START OF DEBUG-LOG-HEAVY METHODS ---
    //

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        // This is the most important log. We need to see if this fires.
        Debug.Log($"====== 1. ON LOBBY PLAYERS CHANGED (Event: {changeEvent.Type}) ======");
        UpdatePlayerListUI();
        
        if (IsServer)
        {
            Debug.Log("...is Server, calling CheckAllPlayersReady()");
            CheckAllPlayersReady();
        }
    }
    
    private void UpdatePlayerListUI()
    {
        Debug.Log("====== 2. UPDATING PLAYER LIST UI ======");
        Debug.Log($"... clearing {playerSlots.Count} old UI slots.");
        foreach (var slot in playerSlots.Values)
        {
            if (slot != null) Destroy(slot);
        }
        playerSlots.Clear();
        
        Debug.Log($"... creating {lobbyPlayers.Count} new UI slots.");
        foreach (var player in lobbyPlayers)
        {
            if (playerListContainer != null && playerSlotPrefab != null)
            {
                GameObject playerSlot = Instantiate(playerSlotPrefab, playerListContainer);
                
                // New log to see what data is being used
                Debug.Log($"... setting up UI for Player: {player.playerName}, Ready: {player.isReady}");
                SetupPlayerSlotUI(playerSlot, player);
                playerSlots[player.clientId] = playerSlot;
            }
        }
        
        UpdateStatusText(); // This updates the summary text
        Debug.Log("====== 3. FINISHED UPDATING PLAYER LIST UI ======");
    }
    
    private void SetupPlayerSlotUI(GameObject playerSlot, LobbyPlayerData playerData)
    {
        LobbyPlayerUI playerUI = playerSlot.GetComponent<LobbyPlayerUI>();

        if (playerUI != null)
        {
            bool isHost = playerData.clientId == NetworkManager.ServerClientId;
            // New log to confirm SetPlayerData is called
            Debug.Log($"... calling playerUI.SetPlayerData('{playerData.playerName}', {playerData.isReady}, {isHost})");
            playerUI.SetPlayerData(playerData.playerName.ToString(), playerData.isReady, isHost);
        }
        else
        {
            Debug.LogError($"❌ Prefab 'playerSlotPrefab' is missing the LobbyPlayerUI component!");
        }
    }
    
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            Debug.Log("... updating main LobbyStatus text.");
            string status = $"🎮 MULTIPLAYER LOBBY\n"; // Using emojis might still cause '□' errors if font is not updated
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
        Debug.Log($"====== 4. CHECK ALL PLAYERS READY (IsServer: {IsServer}) ======");
        if (lobbyPlayers.Count < 1) 
        {
            if (startGameButton != null)
                startGameButton.interactable = false;
            Debug.Log("... no players in lobby. Start button disabled.");
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
        
        Debug.Log($"... All players ready? {allReady}");
        if (startGameButton != null)
        {
            startGameButton.interactable = allReady;
            Debug.Log($"... Start Game Button interactable set to: {allReady}");
        }
    }

    //
    // --- END OF DEBUG-LOG-HEAVY METHODS ---
    //
    
    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyStatusServerRpc(ulong clientId, bool readyStatus)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                var updatedPlayer = lobbyPlayers[i];
                updatedPlayer.isReady = readyStatus;
                lobbyPlayers[i] = updatedPlayer;
                Debug.Log($"🔄 Player {lobbyPlayers[i].playerName} ready status: {readyStatus}");
                break;
            }
        }
    }
    
    public void StartGame()
    {
        if (!IsServer) return;
        
        Debug.Log("🎯 Starting game from lobby!");
        
        // Hide lobby UI on all clients
        HideLobbyUIClientRpc();
        
        // Subscribe to load complete event
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        
        // Load the game scene for all clients (replace "GameScene" with your actual scene name)
        NetworkManager.Singleton.SceneManager.LoadScene("World_01", LoadSceneMode.Single);
        Debug.Log("🚀 Loading game scene ('GameScene') for all clients...");
    }
    
    [ClientRpc]
    private void HideLobbyUIClientRpc()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
            Debug.Log("🖥️ Lobby UI hidden on client");
        }
    }
    
    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"🎯 Scene load completed - Scene: {sceneName}, Mode: {loadSceneMode}, Completed: {clientsCompleted.Count}, TimedOut: {clientsTimedOut.Count}");
        
        // Unsubscribe to avoid multiple calls
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        
        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning("⚠️ Some clients timed out during scene load - Not spawning players");
            return;
        }
        
        // Spawn players now that the game scene is loaded for everyone
        SpawnAllPlayers();
        
        Debug.Log("🚀 Game started! Players spawned in game scene.");
    }
    
    private void SpawnAllPlayers()
    {
        foreach (var playerData in lobbyPlayers)
        {
            SpawnPlayerForClient(playerData.clientId);
        }
    }
    
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (TitleScreenManager.selectedPlayerPrefab != null)
        {
            GameObject playerPrefab = TitleScreenManager.selectedPlayerPrefab;
            NetworkObject playerObject = Instantiate(playerPrefab).GetComponent<NetworkObject>();
            
            if (playerObject != null)
            {
                playerObject.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"🎮 Spawned player for client {clientId}: {playerPrefab.name}");
                
                Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
                playerObject.transform.position = spawnPos;
            }
        }
        else
        {
            Debug.LogError("❌ No player prefab selected!");
        }
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
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("🎮 Manual host start from LobbyScene");
        }
        
        if (Input.GetKeyDown(KeyCode.C) && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("🎮 Manual client start from LobbyScene");
        }
    }
    
    public override void OnNetworkDespawn()
    {
        Debug.Log("👋 LobbyManager network despawn");
        
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

// This struct remains the same
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