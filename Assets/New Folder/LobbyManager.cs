using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Collections;
using System;
using TMPro;
using UnityEngine.UI;
using System.Linq;

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
    public TMP_Text joinCodeText;

    [Header("Player List")]
    public Transform playerListContainer;
    public GameObject playerSlotPrefab;

    [Header("Spawning")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";
    [SerializeField] private bool randomizeSpawnOrder = true;

    private NetworkList<LobbyPlayerData> lobbyPlayers;
    private Dictionary<ulong, GameObject> playerSlots = new Dictionary<ulong, GameObject>();
    private bool isReady = false;
    private List<LobbyPlayerData> persistentLobbyData;
    public static List<LobbyPlayerData> PublicPersistentLobbyData;
    private List<Transform> availableSpawnPoints = new List<Transform>();

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

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.Complete();
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            int hostCharacterIndex = 0;
            if (TitleScreenManager.Instance != null)
            {
                hostCharacterIndex = TitleScreenManager.selectedCharacterIndex;
            }
            AddPlayerToLobby(NetworkManager.Singleton.LocalClientId, "Host", false, hostCharacterIndex);
            Debug.Log($"✅ SERVER: Added host to lobby (Index: {hostCharacterIndex})");
        }

        lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;
        SetupUI();
        UpdatePlayerListUI();

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
        if (joinCodeText != null)
        {
            if (TitleScreenManager.Instance != null)
            {
                joinCodeText.text = "" + TitleScreenManager.Instance.currentJoinCode;
            }
            else
            {
                joinCodeText.text = "Join Code: ???";
            }
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
        int characterIndex = 0;
        if (TitleScreenManager.Instance != null)
        {
            if (TitleScreenManager.selectedPlayerPrefab != null)
            {
                playerName = TitleScreenManager.selectedPlayerPrefab.name;
            }
            characterIndex = TitleScreenManager.selectedCharacterIndex;
        }
        else
        {
            Debug.LogWarning("TitleScreenManager.Instance is null! Defaulting to index 0.");
        }
        SubmitPlayerDataServerRpc(NetworkManager.Singleton.LocalClientId, playerName, false, characterIndex);
        Debug.Log($"📤 CLIENT: Submitting player data: {playerName} (Index: {characterIndex})");
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
    private void SubmitPlayerDataServerRpc(ulong clientId, FixedString64Bytes playerName, bool isReady, int characterIndex, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"📥 SERVER: Received player data from {clientId}: {playerName}, Ready: {isReady}, CharIndex: {characterIndex}");
        AddPlayerToLobby(clientId, playerName.ToString(), isReady, characterIndex);
    }

    private void AddPlayerToLobby(ulong clientId, string playerName, bool isReady, int characterIndex)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                var updatedPlayer = new LobbyPlayerData
                {
                    clientId = clientId,
                    playerName = new FixedString64Bytes(playerName),
                    isReady = isReady,
                    characterPrefabIndex = characterIndex
                };
                lobbyPlayers[i] = updatedPlayer;
                Debug.Log($"🔄 SERVER: Updated player: {playerName} (Ready: {isReady}, Index: {characterIndex})");
                return;
            }
        }
        lobbyPlayers.Add(new LobbyPlayerData
        {
            clientId = clientId,
            playerName = new FixedString64Bytes(playerName),
            isReady = isReady,
            characterPrefabIndex = characterIndex
        });
        Debug.Log($"✅ SERVER: Added player: {playerName} (Ready: {isReady}, Index: {characterIndex}) - Total: {lobbyPlayers.Count}");
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
        foreach (var slot in playerSlots.Values)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        playerSlots.Clear();
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
            playerUI.SetPlayerData(playerData.playerName.ToString(), playerData.isReady, isHost, playerData.characterPrefabIndex);
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
            string status = $"";
            status += $"Players: {lobbyPlayers.Count}/4";
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
        persistentLobbyData = new List<LobbyPlayerData>();
        foreach (var player in lobbyPlayers)
        {
            persistentLobbyData.Add(player);
        }
        Debug.Log($"📝 Copied {persistentLobbyData.Count} players to persistent list.");
        PublicPersistentLobbyData = persistentLobbyData;
        HideLobbyUIClientRpc();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        NetworkManager.Singleton.SceneManager.LoadScene("World", LoadSceneMode.Single);
    }

    [ClientRpc]
    private void HideLobbyUIClientRpc()
    {
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowWithFakeProgress("Loading World...");
        }
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
            Debug.Log("🖥️ Lobby UI hidden");
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"🎯 Scene '{sceneName}' loaded - Clients: {clientsCompleted.Count}, TimedOut: {clientsTimedOut.Count}");

        if (sceneName == "World" && LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.Complete();
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        if (IsServer && clientsCompleted.Count > 0)
        {
            FindSpawnPoints();
            SpawnAllPlayers();
        }
    }

    private void FindSpawnPoints()
    {
        availableSpawnPoints.Clear();
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag(spawnPointTag);
        
        foreach (GameObject spawnObj in spawnPointObjects)
        {
            availableSpawnPoints.Add(spawnObj.transform);
        }

        Debug.Log($"🎯 Found {availableSpawnPoints.Count} spawn points with tag '{spawnPointTag}'");

        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogError($"❌ No spawn points found with tag '{spawnPointTag}'! Players will spawn at origin (0,0,0).");
        }
    }

    private void SpawnAllPlayers()
    {
        Debug.Log($"🎮 SERVER: Spawning {persistentLobbyData.Count} players...");
        foreach (var playerData in persistentLobbyData)
        {
            SpawnPlayerForClient(playerData.clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        LobbyPlayerData playerData = persistentLobbyData.FirstOrDefault(p => p.clientId == clientId);
        if (playerData.clientId != clientId && persistentLobbyData.Count > 0)
        {
            Debug.LogWarning($"Could not find persistent data for client {clientId}. Using first player's data as fallback.");
            playerData = persistentLobbyData[0];
        }

        int characterIndex = playerData.characterPrefabIndex;
        if (TitleScreenManager.Instance == null)
        {
            Debug.LogError("❌ TitleScreenManager.Instance is NULL! Cannot access prefab list.");
            return;
        }

        if (characterIndex < 0 || characterIndex >= TitleScreenManager.Instance.availableCharacterPrefabs.Length)
        {
            Debug.LogError($"❌ Invalid character index {characterIndex} for client {clientId}. Defaulting to 0.");
            characterIndex = 0;
        }

        GameObject playerPrefab = TitleScreenManager.Instance.availableCharacterPrefabs[characterIndex];
        if (playerPrefab == null)
        {
            Debug.LogError($"❌ Player prefab at index {characterIndex} is null! Defaulting to prefab 0.");
            playerPrefab = TitleScreenManager.Instance.availableCharacterPrefabs[0];
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Prefab at index 0 is ALSO null! Spawning will fail.");
                return;
            }
        }

        NetworkObject playerObject = Instantiate(playerPrefab).GetComponent<NetworkObject>();
        if (playerObject != null)
        {
            Vector3 spawnPos = GetSpawnPosition();
            
            Debug.Log($"---> [LobbyManager] PRE-SPAWN position for client {clientId} calculated as: {spawnPos}");
            playerObject.transform.position = spawnPos;
            Debug.Log($"---> [LobbyManager] Position for client {clientId} SET TO: {playerObject.transform.position}");
            playerObject.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"---> [LobbyManager] Player for client {clientId} SPAWNED.");
            Debug.Log($"🎮 SERVER: Spawned '{playerPrefab.name}' for client {clientId} at {spawnPos}");
        }
        else
        {
            Debug.LogError($"❌ Player prefab missing NetworkObject: {playerPrefab.name}");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("⚠️ No spawn points available! Spawning at origin (0,0,0).");
            return Vector3.zero;
        }

        if (randomizeSpawnOrder)
        {
            // Pick a random spawn point
            int randomIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
            Vector3 position = availableSpawnPoints[randomIndex].position;
            
            // Remove the spawn point so it's not reused
            availableSpawnPoints.RemoveAt(randomIndex);
            
            Debug.Log($"🎲 Assigned random spawn point at {position} ({availableSpawnPoints.Count} remaining)");
            return position;
        }
        else
        {
            // Use spawn points in order (first available)
            Vector3 position = availableSpawnPoints[0].position;
            availableSpawnPoints.RemoveAt(0);
            
            Debug.Log($"🎯 Assigned sequential spawn point at {position} ({availableSpawnPoints.Count} remaining)");
            return position;
        }
    }

    public void LeaveLobby()
    {
        Debug.Log("👋 Leaving lobby...");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        if (TitleScreenManager.Instance != null)
        {
            Destroy(TitleScreenManager.Instance.gameObject);
        }
        PublicPersistentLobbyData = null;
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