using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using System;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class TitleScreenManager : MonoBehaviour
{
    public static TitleScreenManager Instance;
    public static GameObject selectedPlayerPrefab;
    public static int selectedCharacterIndex = 0;

    [Header("Loading Screen Settings")]
    [SerializeField] private GameObject loadingScreenPrefab;

    // CHARACTER PREFAB SELECTION
    [Header("Character Prefabs")]
    public GameObject[] availableCharacterPrefabs;
    [SerializeField] int defaultCharacterIndex = 0;

    // MAIN MENU
    [Header("Main Menu Menus")]
    [SerializeField] GameObject titleScreenMenu;
    [SerializeField] GameObject titleScreenLoadMenu;
    [SerializeField] GameObject titleScreenCharacterCreationMenu;
    [SerializeField] GameObject titleScreenCharacterSelectionMenu;

    [Header("Main Menu Buttons")]
    [SerializeField] Button loadMenuReturnButton;
    [SerializeField] Button mainMenuReturnButton;
    [SerializeField] Button mainMenuNewGameButton;
    [SerializeField] Button deleteCharacterPopUpConfirmButton;
    [SerializeField] Button characterSelectionReturnButton;

    [Header("Character Selection Buttons")]
    [SerializeField] Button[] characterSelectionButtons;

    [Header("Main Menu Pop Ups")]
    [SerializeField] GameObject noCharacterSlotsPopUp;
    [SerializeField] Button noCharacterSlotsOkayButton;
    [SerializeField] GameObject deleteCharacterSlotPopUp;

    // CHARACTER CREATION MENU
    [Header("Character Creation Main Panel Buttons")]
    [SerializeField] Button characterNameButton;
    [SerializeField] Button characterClassButton;
    [SerializeField] Button startGameButton;

    [Header("Character Creation Class Panel Buttons")]
    [SerializeField] Button[] characterClassButtons;

    [Header("Character Creation Secondary Panel Menus")]
    [SerializeField] GameObject characterClassMenu;

    [Header("Character Slots")]
    public CharacterSlot currentSelectedSlot = CharacterSlot.NO_SLOT;

    [Header("Classes")]
    public CharacterClass[] startingClasses;

    // CHARACTER PREVIEW
    [Header("Character Preview")]
    [SerializeField] Transform characterPreviewSpawnPoint;
    private GameObject currentPreviewCharacter;

    // MULTIPLAYER UI
    [Header("Multiplayer UI")]
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinCodeInputField; 
    //[SerializeField] GameObject connectionStatusPanel;
    //[SerializeField] TMP_Text connectionStatusText;
    //[SerializeField] TMP_Text joinCodeDisplayText;

    // ERROR POPUP UI
    [Header("Error Popup UI")]
    [SerializeField] GameObject errorPopupPanel;
    [SerializeField] TMP_Text errorPopupTitle;
    [SerializeField] TMP_Text errorPopupMessage;
    [SerializeField] Button errorPopupOkButton;

    // NETWORK PREFABS
    [Header("Network Prefabs")]
    public GameObject lobbyManagerPrefab;

    [Header("Relay Settings")]
    [SerializeField] private int maxConnections = 4;
    [SerializeField] private float connectionTimeout = 10f; // Watchdog timer

    private bool isAttemptingConnection = false;
    private bool unityServicesInitialized = false;
    public string currentJoinCode = "";

    // Error tracking
    private int connectionRetryCount = 0;

    // --- GLOBAL EXCEPTION HANDLING ---
    private void OnEnable() { Application.logMessageReceived += HandleGlobalLog; }
    private void OnDisable() { Application.logMessageReceived -= HandleGlobalLog; }

    private void HandleGlobalLog(string logString, string stackTrace, LogType type)
    {
        // If we hit a critical error while trying to connect, auto-reset to prevent freezing
        if (type == LogType.Exception && isAttemptingConnection)
        {
            Debug.LogError($"🚨 CRITICAL EXCEPTION: {logString}");
            ForceResetState();
        }
    }
    // ---------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (LoadingScreenManager.Instance == null && loadingScreenPrefab != null)
            {
                Instantiate(loadingScreenPrefab);
            }

            if (availableCharacterPrefabs.Length > 0)
            {
                selectedCharacterIndex = defaultCharacterIndex;
                selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            }

            SetupNetworkManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        try
        {
            await InitializeUnityServices();

            if (!unityServicesInitialized) 
                Debug.LogWarning("⚠️ Unity Services failed. Multiplayer disabled.");

            LoadSelectedCharacterFromPrefs();

            if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
            {
                CreateCharacterPreview(defaultCharacterIndex);
            }

            if (hostButton != null) hostButton.onClick.AddListener(HostGame);
            if (joinButton != null) joinButton.onClick.AddListener(() => JoinGame(joinCodeInputField?.text ?? ""));
            if (joinCodeInputField != null) joinCodeInputField.text = "";
            //if (connectionStatusPanel != null) connectionStatusPanel.SetActive(false);
            //if (joinCodeDisplayText != null) joinCodeDisplayText.gameObject.SetActive(false);
            if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
            if (errorPopupOkButton != null) errorPopupOkButton.onClick.AddListener(CloseErrorPopup);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ STARTUP ERROR: {e.Message}");
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
        }
    }

    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        try
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ShowErrorPopup("No Internet", "Check your connection and restart.");
                return;
            }

            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            
            unityServicesInitialized = true;
            Debug.Log($"✅ Unity Services Ready. ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ SERVICES ERROR: {e.Message}");
            ShowErrorPopup("Service Error", "Failed to initialize online services.");
            unityServicesInitialized = false;
        }
    }

    // ==========================================================
    // WATCHDOG / RESET SYSTEM (UPDATED)
    // ==========================================================

    public void ForceResetState()
    {
        Debug.LogWarning("⚠️ EXECUTION: Critical Error encountered. Reloading Title Scene to reset state...");

        // 1. Shutdown Network
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 2. Destroy Singleton Instances to ensure fresh setup on reload
        // We must destroy 'this' (TitleScreenManager) because it holds references to UI elements 
        // that will be destroyed when the scene reloads. A fresh manager is needed.
        if (LoadingScreenManager.Instance != null) Destroy(LoadingScreenManager.Instance.gameObject);
        
        // Remove self from Instance so the new one takes over
        if (Instance == this) Instance = null;
        Destroy(gameObject);

        // 3. Reload Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ==========================================================
    // ERROR POPUP SYSTEM
    // ==========================================================

    private void ShowErrorPopup(string title, string message)
    {
        if (errorPopupPanel != null)
        {
            errorPopupPanel.SetActive(true);
            if (errorPopupTitle != null) errorPopupTitle.text = title;
            if (errorPopupMessage != null) errorPopupMessage.text = message;
            if (errorPopupOkButton != null) errorPopupOkButton.Select();
        }
        else
        {
            Debug.LogWarning($"⚠️ [POPUP MISSING] {title}: {message}");
        }
    }

    private void CloseErrorPopup()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }

    private bool ValidateNetworkConditions()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowErrorPopup("No Internet", "Please check your connection.");
            return false;
        }
        if (!unityServicesInitialized)
        {
            ShowErrorPopup("Services Unavailable", "Please restart the game.");
            return false;
        }
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            ShowErrorPopup("Already Connected", "You are already in a session.");
            return false;
        }
        return true;
    }

    // ==========================================================
    // HOST GAME
    // ==========================================================

    public void HostGame()
    {
        if (isAttemptingConnection) return;
        if (!ValidateNetworkConditions()) return;

        PrepareForNewGame();
        if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.ShowWithFakeProgress("Creating Session...");
        StartCoroutine(StartHostWithRelay());
    }

    private IEnumerator StartHostWithRelay()
    {
        isAttemptingConnection = true;
        ShowConnectionStatus("🔄 Creating Relay...", Color.yellow);

        // --- WATCHDOG 1: Create Allocation ---
        var allocationTask = RelayService.Instance.CreateAllocationAsync(maxConnections - 1);
        float timer = 0f;

        while (!allocationTask.IsCompleted && timer < connectionTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!allocationTask.IsCompleted || allocationTask.IsFaulted)
        {
            HandleError("Allocation Failed", allocationTask.Exception);
            yield break;
        }

        Allocation allocation = allocationTask.Result;

        // --- WATCHDOG 2: Get Join Code ---
        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        timer = 0f;

        while (!joinCodeTask.IsCompleted && timer < connectionTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!joinCodeTask.IsCompleted || joinCodeTask.IsFaulted)
        {
            HandleError("Join Code Failed", joinCodeTask.Exception);
            yield break;
        }

        currentJoinCode = joinCodeTask.Result;
        Debug.Log($"🎫 Join Code: {currentJoinCode}");

        /*if (joinCodeDisplayText != null)
        {
            joinCodeDisplayText.text = $"CODE: {currentJoinCode}";
            joinCodeDisplayText.gameObject.SetActive(true);
        }*/

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport == null)
        {
            ShowErrorPopup("Internal Error", "Transport component missing.");
            ForceResetState();
            yield break;
        }

        transport.SetRelayServerData(new RelayServerData(
            allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.ConnectionData,
            allocation.ConnectionData, allocation.Key, false
        ));

        if (!ValidateSelectedCharacterPrefab())
        {
            ShowErrorPopup("Character Error", "Invalid character selected.");
            ForceResetState();
            yield break;
        }

        RegisterPlayerPrefabs();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;

        if (NetworkManager.Singleton.StartHost())
        {
            ShowConnectionStatus("✅ Hosting...", Color.green);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.UpdateLoadingText("Loading World...");
            
            yield return new WaitForSeconds(1f);

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnHostSceneLoadCompleted;
            var status = NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            
            if(status != SceneEventProgressStatus.Started)
            {
                ShowErrorPopup("Scene Error", "Failed to load Lobby.");
                ForceResetState();
            }
        }
        else
        {
            ShowErrorPopup("Host Failed", "Could not start host.");
            ForceResetState();
        }
    }

    // ==========================================================
    // JOIN GAME
    // ==========================================================

    public void JoinGame(string joinCode)
    {
        if (isAttemptingConnection) return;
        
        joinCode = joinCode?.Trim().ToUpper();
        if (string.IsNullOrEmpty(joinCode) || joinCode.Length != 6)
        {
            ShowErrorPopup("Invalid Code", "Code must be 6 characters.");
            return;
        }

        if (!ValidateNetworkConditions()) return;

        currentJoinCode = joinCode;
        PrepareForNewGame();

        if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.ShowWithFakeProgress($"Joining {joinCode}...");
        StartCoroutine(StartClientWithRelay(currentJoinCode));
    }

    private IEnumerator StartClientWithRelay(string joinCode)
    {
        isAttemptingConnection = true;
        ShowConnectionStatus($"🔄 Joining...", Color.yellow);

        // --- WATCHDOG: Join Allocation ---
        var joinTask = RelayService.Instance.JoinAllocationAsync(joinCode);
        float timer = 0f;

        while (!joinTask.IsCompleted && timer < connectionTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!joinTask.IsCompleted || joinTask.IsFaulted)
        {
            HandleError("Join Failed", joinTask.Exception);
            yield break;
        }

        JoinAllocation joinAllocation = joinTask.Result;

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetRelayServerData(new RelayServerData(
            joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes, joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData, joinAllocation.Key, false
        ));

        if (!ValidateSelectedCharacterPrefab())
        {
            ShowErrorPopup("Character Error", "Invalid character selected.");
            ForceResetState();
            yield break;
        }

        RegisterPlayerPrefabs();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;

        if (NetworkManager.Singleton.StartClient())
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnClientSceneLoadCompleted;

            // Connection Timeout Watchdog
            timer = 0f;
            while (!NetworkManager.Singleton.IsConnectedClient && timer < 15f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                ShowErrorPopup("Timeout", "Could not connect to host.");
                ForceResetState();
            }
        }
        else
        {
            ShowErrorPopup("Client Error", "Failed to start client.");
            ForceResetState();
        }
    }

    // ==========================================================
    // ERROR HANDLER (SUMMARIZED)
    // ==========================================================

    private void HandleError(string title, Exception ex)
    {
        string msg = ex != null ? (ex.InnerException?.Message ?? ex.Message) : "Unknown Error";
        Debug.LogError($"❌ {title}: {msg}");
        
        if (msg.Contains("Timeout")) ShowErrorPopup("Timeout", "Connection timed out.");
        else if (msg.Contains("not found") || msg.Contains("404")) ShowErrorPopup("Invalid Code", "Join code incorrect/expired.");
        else ShowErrorPopup(title, msg); // Fallback for other errors

        ForceResetState();
    }

    private void OnTransportFailure()
    {
        ShowErrorPopup("Connection Lost", "Network transport failed.");
        ForceResetState();
    }

    // ==========================================================
    // CALLBACKS
    // ==========================================================

    private void OnClientConnected(ulong clientId) { Debug.Log($"🎉 Connected: {clientId}"); }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            ShowErrorPopup("Disconnected", "Connection closed.");
            ForceResetState();
        }
    }

    private void OnHostSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnHostSceneLoadCompleted;
        if (sceneName == "LobbyScene" || sceneName == "Lobby")
        {
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Complete();
            SpawnLobbyManagerAsHost();
        }
    }

    private void OnClientSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (clientsCompleted.Contains(NetworkManager.Singleton.LocalClientId))
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnClientSceneLoadCompleted;
            ShowConnectionStatus("✅ Connected!", Color.green);
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.UpdateLoadingText("Entering Game...");
                LoadingScreenManager.Instance.Complete();
            }
            StartCoroutine(HideConnectionStatusDelay());
        }
    }

    private IEnumerator HideConnectionStatusDelay()
    {
        yield return new WaitForSeconds(2f);
        //if (connectionStatusPanel != null) connectionStatusPanel.SetActive(false);
    }

    private void SpawnLobbyManagerAsHost()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        LobbyManager lm = FindFirstObjectByType<LobbyManager>();
        
        if (lm == null && lobbyManagerPrefab != null)
        {
            Instantiate(lobbyManagerPrefab).GetComponent<NetworkObject>()?.Spawn();
        }
        else if (lm != null && !lm.GetComponent<NetworkObject>().IsSpawned)
        {
            lm.GetComponent<NetworkObject>().Spawn();
        }
    }

    // ==========================================================
    // HELPERS & ORIGINAL LOGIC
    // ==========================================================

    private void ShowConnectionStatus(string message, Color color)
    {
        /*if (connectionStatusPanel != null) connectionStatusPanel.SetActive(true);
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.color = color;
        }*/
    }

    private bool ValidateSelectedCharacterPrefab()
    {
        return selectedPlayerPrefab != null && selectedPlayerPrefab.GetComponent<NetworkObject>() != null;
    }

    private void SetupNetworkManager() { RegisterPlayerPrefabs(); }

    private void RegisterPlayerPrefabs()
    {
        if (NetworkManager.Singleton == null) return;
        foreach (GameObject prefab in availableCharacterPrefabs)
        {
            if (prefab != null && !prefab.name.Contains("Lobby"))
            {
                bool registered = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.Any(p => p.Prefab == prefab);
                if (!registered) NetworkManager.Singleton.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
            }
        }
    }

    private void LoadSelectedCharacterFromPrefs()
    {
        string savedName = PlayerPrefs.GetString("SelectedCharacter", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            var found = availableCharacterPrefabs.FirstOrDefault(p => p.name == savedName);
            if (found != null)
            {
                selectedPlayerPrefab = found;
                selectedCharacterIndex = Array.IndexOf(availableCharacterPrefabs, found);
            }
        }
    }

    private void SaveSelectedCharacterToPrefs()
    {
        if (selectedPlayerPrefab != null)
        {
            PlayerPrefs.SetString("SelectedCharacter", selectedPlayerPrefab.name);
            PlayerPrefs.Save();
        }
    }

    public void PrepareForNewGame()
    {
        if (titleScreenMenu != null) titleScreenMenu.SetActive(false);
        if (titleScreenCharacterCreationMenu != null) titleScreenCharacterCreationMenu.SetActive(false);
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(false);
        if (currentPreviewCharacter != null) Destroy(currentPreviewCharacter);

        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            selectedCharacterIndex = defaultCharacterIndex;
        }
        SaveSelectedCharacterToPrefs();
    }

    public void AttemptToCreateNewCharacter()
    {
        if (WorldSaveGameManager.instance.HasFreeCharacterSlots()) OpenCharacterSelectionMenu();
        else if (noCharacterSlotsPopUp != null)
        {
            noCharacterSlotsPopUp.SetActive(true);
            if (noCharacterSlotsOkayButton != null) noCharacterSlotsOkayButton.Select();
        }
    }

    public void StartNewGame()
    {
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
        
        SaveSelectedCharacterToPrefs();
        PrepareForNewGame();
        WorldSaveGameManager.instance.AttempToCreateNewGame();
    }

    public void OpenLoadGameMenu()
    {
        if (titleScreenMenu != null) titleScreenMenu.SetActive(false);
        if (titleScreenLoadMenu != null) titleScreenLoadMenu.SetActive(true);
        if (loadMenuReturnButton != null) loadMenuReturnButton.Select();
    }

    public void CloseLoadGameMenu()
    {
        if (titleScreenLoadMenu != null) titleScreenLoadMenu.SetActive(false);
        if (titleScreenMenu != null) titleScreenMenu.SetActive(true);
        if (mainMenuReturnButton != null) mainMenuReturnButton.Select();
    }

    public void OpenCharacterSelectionMenu()
    {
        if (titleScreenMenu != null) titleScreenMenu.SetActive(false);
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(true);
        if (characterSelectionButtons.Length > 0 && characterSelectionButtons[0] != null) characterSelectionButtons[0].Select();
        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null) CreateCharacterPreview(0);
    }

    public void CloseCharacterSelectionMenu()
    {
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(false);
        if (titleScreenMenu != null) titleScreenMenu.SetActive(true);
        if (mainMenuNewGameButton != null) mainMenuNewGameButton.Select();
    }

    public void SelectCharacterPrefab(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[characterIndex];
            selectedCharacterIndex = characterIndex;
            if (characterPreviewSpawnPoint != null) CreateCharacterPreview(characterIndex);
        }
    }

    private void CreateCharacterPreview(int characterIndex)
    {
        if (currentPreviewCharacter != null) Destroy(currentPreviewCharacter);
        if (characterIndex < 0 || characterIndex >= availableCharacterPrefabs.Length) return;
        
        currentPreviewCharacter = Instantiate(
            availableCharacterPrefabs[characterIndex],
            characterPreviewSpawnPoint.position,
            characterPreviewSpawnPoint.rotation
        );
        SetupPreviewCharacter(currentPreviewCharacter);
    }

    private void SetupPreviewCharacter(GameObject previewCharacter)
    {
        NetworkObject networkObject = previewCharacter.GetComponent<NetworkObject>();
        if (networkObject != null) DestroyImmediate(networkObject);

        var playerManager = previewCharacter.GetComponent<PlayerManager>();
        if (playerManager != null) playerManager.enabled = false;

        var rb = previewCharacter.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
    }

    public void ConfirmCharacterSelection()
    {
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            selectedCharacterIndex = defaultCharacterIndex;
        }
        SaveSelectedCharacterToPrefs();
        OpenCharacterCreationMenu();
    }

    public void OpenCharacterCreationMenu()
    {
        if (titleScreenCharacterCreationMenu != null) titleScreenCharacterCreationMenu.SetActive(true);
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(false);
    }

    public void CloseCharacterCreationMenu()
    {
        if (titleScreenCharacterCreationMenu != null) titleScreenCharacterCreationMenu.SetActive(false);
    }

    public void OpenChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(false);
        if (characterClassMenu != null) characterClassMenu.SetActive(true);
        if (characterClassButtons.Length > 0 && characterClassButtons[0] != null) characterClassButtons[0].Select();
    }

    public void CloseChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(true);
        if (characterClassMenu != null) characterClassMenu.SetActive(false);
        if (characterClassButton != null) characterClassButton.Select();
    }

    private void ToogleCharacterCreationScreenMainMenuButtons(bool status)
    {
        if (characterNameButton != null) characterNameButton.enabled = status;
        if (characterClassButton != null) characterClassButton.enabled = status;
        if (startGameButton != null) startGameButton.enabled = status;
    }

    public void CloseNoFreeCharacterSlotsPopUp()
    {
        if (noCharacterSlotsPopUp != null)
        {
            noCharacterSlotsPopUp.SetActive(false);
            if (mainMenuNewGameButton != null) mainMenuNewGameButton.Select();
        }
    }

    public void SelectCharcterSlot(CharacterSlot characterSlot) { currentSelectedSlot = characterSlot; }
    public void SelectNoSlot() { currentSelectedSlot = CharacterSlot.NO_SLOT; }

    public void AttemptTodeleteCharacaterSlot()
    {
        if (currentSelectedSlot != CharacterSlot.NO_SLOT && deleteCharacterSlotPopUp != null)
        {
            deleteCharacterSlotPopUp.SetActive(true);
            if (deleteCharacterPopUpConfirmButton != null) deleteCharacterPopUpConfirmButton.Select();
        }
    }

    public void DeleteCharacterSlot()
    {
        if (deleteCharacterSlotPopUp != null) deleteCharacterSlotPopUp.SetActive(false);
        WorldSaveGameManager.instance.DeleteGame(currentSelectedSlot);
        if (titleScreenLoadMenu != null) { titleScreenLoadMenu.SetActive(false); titleScreenLoadMenu.SetActive(true); }
        if (loadMenuReturnButton != null) loadMenuReturnButton.Select();
    }

    public void CloseDeleteCharacterPopUp()
    {
        if (deleteCharacterSlotPopUp != null) deleteCharacterSlotPopUp.SetActive(false);
        if (loadMenuReturnButton != null) loadMenuReturnButton.Select();
    }

    public void SelectClass(int classID)
    {
        PlayerManager player = FindFirstObjectByType<PlayerManager>();
        if (player != null && startingClasses.Length > classID) startingClasses[classID].SetClass(player);
        CloseChooseCharacterClassSubMenu();
    }

    public void PreviewClass(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length && characterPreviewSpawnPoint != null)
            CreateCharacterPreview(characterIndex);
    }

    public void SetCharacterClass(PlayerManager player, int vitality, int endurance, int strength, int dexterity, int intelligence,
        WeaponItem[] mainHandWeapons, WeaponItem[] offHandWeapons)
    {
        player.playerNetworkManager.vitality.Value = vitality;
        player.playerNetworkManager.endurance.Value = endurance;
        player.playerNetworkManager.dexterity.Value = dexterity;
        player.playerNetworkManager.intelligence.Value = intelligence;

        player.playerInventoryManager.weaponsInRightHandSlots[0] = Instantiate(mainHandWeapons[0]);
        player.playerInventoryManager.weaponsInRightHandSlots[1] = Instantiate(mainHandWeapons[1]);
        player.playerInventoryManager.weaponsInRightHandSlots[2] = Instantiate(mainHandWeapons[2]);
        player.playerInventoryManager.currentRightHandWeapon = player.playerInventoryManager.weaponsInRightHandSlots[0];
        player.playerNetworkManager.currentRightHandWeaponID.Value = player.playerInventoryManager.weaponsInRightHandSlots[0].itemID;

        player.playerInventoryManager.weaponsInLeftHandSlots[0] = Instantiate(offHandWeapons[0]);
        player.playerInventoryManager.weaponsInLeftHandSlots[1] = Instantiate(offHandWeapons[1]);
        player.playerInventoryManager.weaponsInLeftHandSlots[2] = Instantiate(offHandWeapons[2]);
        player.playerInventoryManager.currentLeftHandWeapon = player.playerInventoryManager.weaponsInLeftHandSlots[0];
        player.playerNetworkManager.currentLeftHandWeaponID.Value = player.playerInventoryManager.weaponsInLeftHandSlots[0].itemID;
    }

    private void OnDestroy()
    {
        if (currentPreviewCharacter != null) Destroy(currentPreviewCharacter);
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
        }
    }
}