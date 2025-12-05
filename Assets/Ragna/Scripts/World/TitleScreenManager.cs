using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using System;

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
    [SerializeField] TMP_InputField ipInputField;
    [SerializeField] GameObject connectionStatusPanel;
    [SerializeField] TMP_Text connectionStatusText;

    // NETWORK PREFABS
    [Header("Network Prefabs")]
    public GameObject lobbyManagerPrefab;

    private bool isAttemptingConnection = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 

            // --- SPAWN LOADING SCREEN PREFAB IF MISSING ---
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

    private void Start()
    {
        LoadSelectedCharacterFromPrefs();

        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(defaultCharacterIndex);
        }

        if (hostButton != null) hostButton.onClick.AddListener(HostGame);
        if (joinButton != null) joinButton.onClick.AddListener(() => JoinGame(ipInputField?.text ?? "127.0.0.1"));
        if (ipInputField != null) ipInputField.text = "127.0.0.1";
        if (connectionStatusPanel != null) connectionStatusPanel.SetActive(false);
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
        PlayerManager player = FindObjectOfType<PlayerManager>();
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
    // MULTIPLAYER & LOADING SCREEN LOGIC
    // ==========================================================

    public void HostGame()
    {
        if (isAttemptingConnection) return;
        PrepareForNewGame();

        // Specific Message
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowWithFakeProgress("Initializing Host System...");

        Debug.Log("🎯 HOST: Starting host and loading lobby scene...");
        StartCoroutine(StartHostThenLoadLobby());
    }

    private IEnumerator StartHostThenLoadLobby()
    {
        isAttemptingConnection = true;
        ShowConnectionStatus("Starting Host...", Color.yellow);
        
        // Transparent Update
        if(LoadingScreenManager.Instance != null) 
            LoadingScreenManager.Instance.UpdateLoadingText("Verifying Network Transport...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            ShowConnectionStatus("Network Manager not found!", Color.red);
            if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        DebugNetworkSetup();

        if (!ValidateSelectedCharacterPrefab())
        {
            ShowConnectionStatus("Invalid character configuration!", Color.red);
            if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        RegisterPlayerPrefabs();

        if (!NetworkManager.Singleton.IsListening)
        {
            ShowConnectionStatus("Starting Host...", Color.yellow);
            
            // Transparent Update
            if(LoadingScreenManager.Instance != null) 
                LoadingScreenManager.Instance.UpdateLoadingText("Starting Network Host...");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            bool success = NetworkManager.Singleton.StartHost();

            if (success)
            {
                ShowConnectionStatus("Host Started - Loading Lobby...", Color.green);
                
                // Transparent Update
                if(LoadingScreenManager.Instance != null) 
                    LoadingScreenManager.Instance.UpdateLoadingText("Server Started. Loading Lobby Scene...");
                
                yield return new WaitForSeconds(1f);

                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnHostSceneLoadCompleted;
                var status = NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
                
                if (status != SceneEventProgressStatus.Started)
                {
                    ShowConnectionStatus($"Failed to load Lobby: {status}", Color.red);
                    if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
                    isAttemptingConnection = false;
                    yield break;
                }
            }
            else
            {
                ShowConnectionStatus("Failed to start host!", Color.red);
                if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            }
        }
        else
        {
            // Already listening
            if(LoadingScreenManager.Instance != null) 
                LoadingScreenManager.Instance.UpdateLoadingText("Server Active. Switching to Lobby...");

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnHostSceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }

        isAttemptingConnection = false;
    }

    private void OnHostSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnHostSceneLoadCompleted;
        if (sceneName == "LobbyScene" || sceneName == "Lobby")
        {
            // --- HOST SCENE LOADED: COMPLETE LOADING SCREEN ---
            if (LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Complete();
            SpawnLobbyManagerAsHost();
        }
    }

    private void SpawnLobbyManagerAsHost()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
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

    // =========================================================
    // CLIENT JOIN LOGIC (UPDATED WITH TRANSPARENT MESSAGES)
    // =========================================================

    public void JoinGame(string ipAddress = "127.0.0.1")
    {
        if (isAttemptingConnection) return;
        PrepareForNewGame();

        // Specific Message
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowWithFakeProgress($"Preparing to join {ipAddress}...");

        StartCoroutine(StartClientThenLoadLobby(ipAddress));
    }

    private IEnumerator StartClientThenLoadLobby(string ipAddress)
    {
        isAttemptingConnection = true;
        ShowConnectionStatus($"Connecting to {ipAddress}...", Color.yellow);

        if (NetworkManager.Singleton == null)
        {
            ShowConnectionStatus("Network Manager not found!", Color.red);
            if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        DebugNetworkSetup();
        if (!ValidateSelectedCharacterPrefab())
        {
            ShowConnectionStatus("Invalid character configuration!", Color.red);
            if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            isAttemptingConnection = false;
            yield break;
        }

        RegisterPlayerPrefabs();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null) transport.SetConnectionData(ipAddress, 7777);

        if (!NetworkManager.Singleton.IsListening)
        {
            ShowConnectionStatus("Connecting...", Color.yellow);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            bool startSuccess = NetworkManager.Singleton.StartClient();

            if (startSuccess)
            {
                // CRITICAL FIX: Subscribe to scene load completed BEFORE checking connection loop
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnClientSceneLoadCompleted;

                float timeout = 15f; 
                float timer = 0f;
                int connectionAttempts = 0;

                while (!NetworkManager.Singleton.IsConnectedClient && timer < timeout)
                {
                    timer += Time.deltaTime;
                    connectionAttempts++;
                    
                    // Update user every 0.5s about connection status
                    if (connectionAttempts % 30 == 0)
                    {
                        ShowConnectionStatus($"Connecting... ({timer:F1}s)", Color.yellow);
                        
                        // --- SPECIFIC LOADING TEXT ---
                        if(LoadingScreenManager.Instance != null) 
                            LoadingScreenManager.Instance.UpdateLoadingText($"Negotiating Connection to Host... ({timer:F1}s)");
                    }
                    yield return null;
                }

                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    ShowConnectionStatus("Connected! Syncing Scene...", Color.green);
                    
                    // --- SPECIFIC LOADING TEXT ---
                    if(LoadingScreenManager.Instance != null) 
                        LoadingScreenManager.Instance.UpdateLoadingText("Connection Established. Synchronizing World State...");
                    
                    // We DO NOT wait here artificially. We let OnClientSceneLoadCompleted handle the finish.
                }
                else
                {
                    // Timed out
                    ShowConnectionStatus($"Connection timed out", Color.red);
                    NetworkManager.Singleton.Shutdown();
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnClientSceneLoadCompleted; // Cleanup
                    if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
                }
            }
            else
            {
                ShowConnectionStatus("Failed to start client!", Color.red);
                if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
            }
        }
        isAttemptingConnection = false;
    }

    // --- NEW METHOD: Handles Client Scene Loading Completion ---
    private void OnClientSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Check if WE are in the list of clients who finished loading
        if (clientsCompleted.Contains(NetworkManager.Singleton.LocalClientId))
        {
            Debug.Log($"✅ CLIENT: Scene '{sceneName}' sync complete!");
            
            // Unsubscribe to stop listening
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnClientSceneLoadCompleted;

            ShowConnectionStatus("Connected! Lobby loading...", Color.green);
            
            // --- SPECIFIC LOADING TEXT FOR FINISH ---
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.UpdateLoadingText("Scene Synced. Entering Game...");
                LoadingScreenManager.Instance.Complete();
            }
            
            // Hide connection status UI after a brief moment
            StartCoroutine(HideConnectionStatusDelay());
        }
    }

    private IEnumerator HideConnectionStatusDelay()
    {
        yield return new WaitForSeconds(2f);
        HideConnectionStatus();
    }

    private void OnClientConnected(ulong clientId) { Debug.Log($"🎉 Client connected: {clientId}"); }

    private void OnClientDisconnected(ulong clientId)
    {
        isAttemptingConnection = false;
        ShowConnectionStatus("Disconnected from server", Color.red);
        if(LoadingScreenManager.Instance != null) LoadingScreenManager.Instance.Hide();
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