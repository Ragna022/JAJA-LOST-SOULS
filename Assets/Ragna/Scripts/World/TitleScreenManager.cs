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
    [SerializeField] GameObject connectionStatusPanel;
    [SerializeField] TMP_Text connectionStatusText;
    [SerializeField] TMP_Text joinCodeDisplayText; 

    // NETWORK PREFABS
    [Header("Network Prefabs")]
    public GameObject lobbyManagerPrefab;

    [Header("Relay Settings")]
    [SerializeField] private int maxConnections = 4; // Maximum players including host

    private bool isAttemptingConnection = false;
    
    // CHANGED TO PUBLIC SO LOBBY MANAGER CAN ACCESS IT
    public string currentJoinCode = ""; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Spawn Loading Screen Prefab if missing
            if (LoadingScreenManager.Instance == null)
            {
                if (loadingScreenPrefab != null)
                {
                    Instantiate(loadingScreenPrefab);
                    Debug.Log("✅ TitleScreenManager: Auto-spawned LoadingScreenManager prefab.");
                }
                else
                {
                    Debug.LogError("❌ TitleScreenManager: You forgot to assign the Loading Screen Prefab in the Inspector!");
                }
            }

            // Set default character prefab
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
        // Initialize Unity Services (REQUIRED for Relay)
        await InitializeUnityServices();

        LoadSelectedCharacterFromPrefs();

        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(defaultCharacterIndex);
        }

        if (hostButton != null) hostButton.onClick.AddListener(HostGame);
        if (joinButton != null) joinButton.onClick.AddListener(() => JoinGame(joinCodeInputField?.text ?? ""));
        if (joinCodeInputField != null) joinCodeInputField.text = "";
        if (connectionStatusPanel != null) connectionStatusPanel.SetActive(false);
        if (joinCodeDisplayText != null) joinCodeDisplayText.gameObject.SetActive(false);
    }

    // ==========================================================
    // UNITY SERVICES INITIALIZATION
    // ==========================================================

    private async System.Threading.Tasks.Task InitializeUnityServices()
    {
        try
        {
            ShowConnectionStatus("Initializing Unity Services...", Color.yellow);
            
            await UnityServices.InitializeAsync();
            
            // Sign in anonymously
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"✅ Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
            
            HideConnectionStatus();
            Debug.Log("✅ Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to initialize Unity Services: {e.Message}");
            ShowConnectionStatus("Failed to connect to Unity Services", Color.red);
        }
    }

    private void SetupNetworkManager()
    {
        ValidatePlayerPrefabs();
        RegisterPlayerPrefabs();
        DebugNetworkSetup();
    }

    private void ValidatePlayerPrefabs()
    {
        Debug.Log("🔍 Validating Character Prefabs for Networking...");
        bool allValid = true;

        foreach (GameObject prefab in availableCharacterPrefabs)
        {
            if (prefab != null)
            {
                if (prefab.name.Contains("Lobby") || prefab.name.Contains("Manager"))
                    continue;

                NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    Debug.LogError($"❌ Player prefab '{prefab.name}' is missing NetworkObject component!");
                    allValid = false;
                }
            }
        }

        if (!allValid) Debug.LogError("🚨 SOME CHARACTER PREFABS ARE MISSING NETWORKOBJECT COMPONENTS!");
    }

    private void RegisterPlayerPrefabs()
    {
        if (NetworkManager.Singleton == null) return;

        Debug.Log("📝 Registering character prefabs with NetworkManager...");
        foreach (GameObject prefab in availableCharacterPrefabs)
        {
            if (prefab != null)
            {
                if (prefab.name.Contains("Lobby") || prefab.name.Contains("LobbyManager")) continue;

                NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    bool alreadyRegistered = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
                        .Any(registeredPrefab => registeredPrefab != null && registeredPrefab.Prefab == prefab);

                    if (!alreadyRegistered)
                    {
                        NetworkManager.Singleton.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
                    }
                }
            }
        }
    }

    private void DebugNetworkSetup()
    {
        if (NetworkManager.Singleton == null) return;
        Debug.Log($"✅ NetworkManager exists - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}");
    }

    private void LoadSelectedCharacterFromPrefs()
    {
        string savedCharacterName = PlayerPrefs.GetString("SelectedCharacter", "");
        if (!string.IsNullOrEmpty(savedCharacterName))
        {
            for (int i = 0; i < availableCharacterPrefabs.Length; i++)
            {
                GameObject prefab = availableCharacterPrefabs[i];
                if (prefab != null && prefab.name == savedCharacterName)
                {
                    selectedPlayerPrefab = prefab;
                    selectedCharacterIndex = i;
                    break;
                }
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

        if (currentPreviewCharacter != null)
        {
            Destroy(currentPreviewCharacter);
            currentPreviewCharacter = null;
        }

        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            selectedCharacterIndex = defaultCharacterIndex;
        }
        SaveSelectedCharacterToPrefs();
    }

    // ==========================================================
    // CHARACTER SELECTION & CREATION
    // ==========================================================

    public void AttemptToCreateNewCharacter()
    {
        if (WorldSaveGameManager.instance.HasFreeCharacterSlots())
        {
            OpenCharacterSelectionMenu();
        }
        else
        {
            DisplayNoFreeCharacterSlotsPopUp();
        }
    }

    public void StartNewGame()
    {
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
        }
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
        if (characterSelectionButtons.Length > 0 && characterSelectionButtons[0] != null)
        {
            characterSelectionButtons[0].Select();
            characterSelectionButtons[0].OnSelect(null);
        }
        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
            CreateCharacterPreview(0);
    }

    public void CloseCharacterSelectionMenu()
    {
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(false);
        if (titleScreenMenu != null) titleScreenMenu.SetActive(true);
        if (mainMenuNewGameButton != null)
        {
            mainMenuNewGameButton.Select();
            mainMenuNewGameButton.OnSelect(null);
        }
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

        PlayerManager playerManager = previewCharacter.GetComponent<PlayerManager>();
        if (playerManager != null) playerManager.enabled = false;

        CharacterController characterController = previewCharacter.GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;

        CharacterLocomotionManager locomotionManager = previewCharacter.GetComponent<CharacterLocomotionManager>();
        if (locomotionManager != null) locomotionManager.enabled = false;

        PlayerLocomotionManager playerLocomotion = previewCharacter.GetComponent<PlayerLocomotionManager>();
        if (playerLocomotion != null) playerLocomotion.enabled = false;

        Rigidbody rb = previewCharacter.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Collider[] colliders = previewCharacter.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders) collider.enabled = false;
    }

    public void ConfirmCharacterSelection()
    {
        if (selectedPlayerPrefab != null)
        {
            SaveSelectedCharacterToPrefs();
            OpenCharacterCreationMenu();
        }
        else
        {
            if (availableCharacterPrefabs.Length > 0)
            {
                selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
                selectedCharacterIndex = defaultCharacterIndex;
                SaveSelectedCharacterToPrefs();
                OpenCharacterCreationMenu();
            }
        }
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
        if (characterClassButtons.Length > 0 && characterClassButtons[0] != null)
        {
            characterClassButtons[0].Select();
            characterClassButtons[0].OnSelect(null);
        }
    }

    public void CloseChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(true);
        if (characterClassMenu != null) characterClassMenu.SetActive(false);
        if (characterClassButton != null)
        {
            characterClassButton.Select();
            characterClassButton.OnSelect(null);
        }
    }

    private void ToogleCharacterCreationScreenMainMenuButtons(bool status)
    {
        if (characterNameButton != null) characterNameButton.enabled = status;
        if (characterClassButton != null) characterClassButton.enabled = status;
        if (startGameButton != null) startGameButton.enabled = status;
    }

    public void DisplayNoFreeCharacterSlotsPopUp()
    {
        if (noCharacterSlotsPopUp != null)
        {
            noCharacterSlotsPopUp.SetActive(true);
            if (noCharacterSlotsOkayButton != null) noCharacterSlotsOkayButton.Select();
        }
    }

    public void CloseNoFreeCharacterSlotsPopUp()
    {
        if (noCharacterSlotsPopUp != null)
        {
            noCharacterSlotsPopUp.SetActive(false);
            if (mainMenuNewGameButton != null) mainMenuNewGameButton.Select();
        }
    }

    public void SelectCharcterSlot(CharacterSlot characterSlot)
    {
        currentSelectedSlot = characterSlot;
    }

    public void SelectNoSlot()
    {
        currentSelectedSlot = CharacterSlot.NO_SLOT;
    }

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
        if (titleScreenLoadMenu != null)
        {
            titleScreenLoadMenu.SetActive(false);
            titleScreenLoadMenu.SetActive(true);
        }
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
        if (player != null && startingClasses.Length > classID)
        {
            startingClasses[classID].SetClass(player);
        }
        CloseChooseCharacterClassSubMenu();
    }

    public void PreviewClass(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(characterIndex);
        }
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
    }

    // ==========================================================
    // MULTIPLAYER WITH RELAY & JOIN CODES
    // ==========================================================

    public void HostGame()
    {
        if (isAttemptingConnection) return;
        PrepareForNewGame();

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowWithFakeProgress("Creating Game Session...");

        Debug.Log("🎯 HOST: Creating Relay allocation and starting host...");
        StartCoroutine(StartHostWithRelay());
    }

    private IEnumerator StartHostWithRelay()
    {
        isAttemptingConnection = true;
        ShowConnectionStatus("Creating game session...", Color.yellow);

        // Create Relay allocation
        var allocationTask = RelayService.Instance.CreateAllocationAsync(maxConnections - 1); // -1 because host counts as one
        yield return new WaitUntil(() => allocationTask.IsCompleted);

        if (allocationTask.IsFaulted)
        {
            Debug.LogError($"❌ Failed to create Relay allocation: {allocationTask.Exception}");
            ShowConnectionStatus("Failed to create game session", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        Allocation allocation = allocationTask.Result;
        Debug.Log($"✅ Relay allocation created with ID: {allocation.AllocationId}");

        // Get join code
        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);

        if (joinCodeTask.IsFaulted)
        {
            Debug.LogError($"❌ Failed to get join code: {joinCodeTask.Exception}");
            ShowConnectionStatus("Failed to generate join code", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        currentJoinCode = joinCodeTask.Result;
        Debug.Log($"🎫 Join Code: {currentJoinCode}");

        // Display join code to host
        if (joinCodeDisplayText != null)
        {
            joinCodeDisplayText.text = $"JOIN CODE: {currentJoinCode}";
            joinCodeDisplayText.gameObject.SetActive(true);
        }

        // Configure Unity Transport with Relay
        // We cast the Transport linked in the Inspector to UnityTransport
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        if (transport == null)
        {
            Debug.LogError($"❌ Still Null! The NetworkManager object named '{NetworkManager.Singleton.name}' does not have a UnityTransport component linked!");
            yield break;
        }

        // --- FIXED RELAY SERVER DATA CONSTRUCTION ---
        var relayServerData = new RelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData, 
            allocation.Key,
            false // false means UDP (not secure DTLS)
        );

        transport.SetRelayServerData(relayServerData);

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.UpdateLoadingText("Starting Network Host...");

        if (!ValidateSelectedCharacterPrefab())
        {
            ShowConnectionStatus("Invalid character configuration!", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        RegisterPlayerPrefabs();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            ShowConnectionStatus($"Hosting - Code: {currentJoinCode}", Color.green);

            if (LoadingScreenManager.Instance != null)
                LoadingScreenManager.Instance.UpdateLoadingText("Loading Lobby Scene...");

            yield return new WaitForSeconds(1f);

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnHostSceneLoadCompleted;
            var status = NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);

            if (status != SceneEventProgressStatus.Started)
            {
                ShowConnectionStatus($"Failed to load Lobby: {status}", Color.red);
                if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            }
        }
        else
        {
            ShowConnectionStatus("Failed to start host!", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
        }

        isAttemptingConnection = false;
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

    private void SpawnLobbyManagerAsHost()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        LobbyManager lobbyManager = FindFirstObjectByType<LobbyManager>();
        if (lobbyManager != null)
        {
            NetworkObject lobbyNetObj = lobbyManager.GetComponent<NetworkObject>();
            if (lobbyNetObj != null && !lobbyNetObj.IsSpawned) lobbyNetObj.Spawn();
        }
        else
        {
            if (lobbyManagerPrefab != null)
            {
                GameObject lobbyManagerObj = Instantiate(lobbyManagerPrefab);
                NetworkObject lobbyNetObj = lobbyManagerObj.GetComponent<NetworkObject>();
                if (lobbyNetObj != null) lobbyNetObj.Spawn();
            }
        }
    }

    // ==========================================================
    // CLIENT JOIN WITH JOIN CODE
    // ==========================================================

    public void JoinGame(string joinCode)
    {
        if (isAttemptingConnection) return;

        if (string.IsNullOrEmpty(joinCode))
        {
            ShowConnectionStatus("Please enter a join code", Color.red);
            return;
        }

        // SAVE CODE SO CLIENT CAN SEE IT IN LOBBY TOO
        currentJoinCode = joinCode;

        PrepareForNewGame();

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowWithFakeProgress($"Joining game with code: {joinCode}...");

        StartCoroutine(StartClientWithRelay(joinCode));
    }

    private IEnumerator StartClientWithRelay(string joinCode)
    {
        isAttemptingConnection = true;
        ShowConnectionStatus($"Joining with code: {joinCode}...", Color.yellow);

        // Join Relay allocation using join code
        var joinTask = RelayService.Instance.JoinAllocationAsync(joinCode);
        yield return new WaitUntil(() => joinTask.IsCompleted);

        if (joinTask.IsFaulted)
        {
            Debug.LogError($"❌ Failed to join Relay: {joinTask.Exception}");
            ShowConnectionStatus("Invalid join code or connection failed", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        JoinAllocation joinAllocation = joinTask.Result;
        Debug.Log($"✅ Joined Relay allocation");

        // We cast the Transport linked in the Inspector to UnityTransport
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        if (transport == null)
        {
            Debug.LogError($"❌ Still Null! The NetworkManager object named '{NetworkManager.Singleton.name}' does not have a UnityTransport component linked!");
            yield break;
        }
        
        // --- FIXED RELAY SERVER DATA CONSTRUCTION ---
        var relayServerData = new RelayServerData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData, 
            joinAllocation.Key,
            false // false means UDP (not secure DTLS)
        );

        transport.SetRelayServerData(relayServerData);

        if (!ValidateSelectedCharacterPrefab())
        {
            ShowConnectionStatus("Invalid character configuration!", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        RegisterPlayerPrefabs();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        // -----------------------------------------------------------------------
        // CRITICAL FIX: DO NOT SUBSCRIBE TO SCENEMANAGER BEFORE STARTING CLIENT
        // -----------------------------------------------------------------------

        bool startSuccess = NetworkManager.Singleton.StartClient();

        if (startSuccess)
        {
            // ✅ SUBSCRIBE HERE, NOW THAT THE CLIENT IS RUNNING AND SCENEMANAGER EXISTS
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnClientSceneLoadCompleted;

            float timeout = 15f;
            float timer = 0f;
            int connectionAttempts = 0;

            while (!NetworkManager.Singleton.IsConnectedClient && timer < timeout)
            {
                timer += Time.deltaTime;
                connectionAttempts++;

                if (connectionAttempts % 30 == 0)
                {
                    ShowConnectionStatus($"Connecting... ({timer:F1}s)", Color.yellow);

                    if (LoadingScreenManager.Instance != null)
                        LoadingScreenManager.Instance.UpdateLoadingText($"Establishing Connection... ({timer:F1}s)");
                }
                yield return null;
            }

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                ShowConnectionStatus("Connected! Syncing Scene...", Color.green);

                if (LoadingScreenManager.Instance != null)
                    LoadingScreenManager.Instance.UpdateLoadingText("Connection Established. Synchronizing World State...");
            }
            else
            {
                // Timed out
                ShowConnectionStatus($"Connection timed out", Color.red);
                NetworkManager.Singleton.Shutdown();
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnClientSceneLoadCompleted;
                if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            }
        }
        else
        {
            ShowConnectionStatus("Failed to start client!", Color.red);
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
        }

        isAttemptingConnection = false;
    }

    private void OnClientSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (clientsCompleted.Contains(NetworkManager.Singleton.LocalClientId))
        {
            Debug.Log($"✅ CLIENT: Scene '{sceneName}' sync complete!");

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnClientSceneLoadCompleted;

            ShowConnectionStatus("Connected! Lobby loading...", Color.green);

            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.UpdateLoadingText("Scene Synced. Entering Game...");
                LoadingScreenManager.Instance.Complete();
            }

            StartCoroutine(HideConnectionStatusDelay());
        }
    }

    private IEnumerator HideConnectionStatusDelay()
    {
        yield return new WaitForSeconds(2f);
        HideConnectionStatus();
    }

    private void OnClientConnected(ulong clientId) 
    { 
        Debug.Log($"🎉 Client connected: {clientId}"); 
    }

    private void OnClientDisconnected(ulong clientId)
    {
        isAttemptingConnection = false;
        ShowConnectionStatus("Disconnected from server", Color.red);
        if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
    }

    private void ShowConnectionStatus(string message, Color color)
    {
        if (connectionStatusPanel != null) connectionStatusPanel.SetActive(true);
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.color = color;
        }
    }

    private void HideConnectionStatus()
    {
        if (connectionStatusPanel != null) connectionStatusPanel.SetActive(false);
    }

    private bool ValidateSelectedCharacterPrefab()
    {
        if (selectedPlayerPrefab == null) return false;
        if (selectedPlayerPrefab.name.Contains("Lobby") || selectedPlayerPrefab.name.Contains("Manager")) return false;
        if (selectedPlayerPrefab.GetComponent<NetworkObject>() == null) return false;
        return true;
    }
}