using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
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
    public static int selectedCharacterIndex = 0; // <-- ADDED

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

    // MULTIPLAYER UI (NEW)
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
            DontDestroyOnLoad(gameObject); // <-- CRITICAL: Makes this object persist

            // Set default character prefab
            if (availableCharacterPrefabs.Length > 0)
            {
                // <-- MODIFIED
                selectedCharacterIndex = defaultCharacterIndex; 
                selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            }

            // Ensure NetworkManager is ready
            SetupNetworkManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Try to load previously selected character
        LoadSelectedCharacterFromPrefs();

        // Initialize with character preview
        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(defaultCharacterIndex);
        }

        // Setup multiplayer buttons
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(HostGame);
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(() => JoinGame(ipInputField?.text ?? "127.0.0.1"));
        }

        // Set default IP if input exists
        if (ipInputField != null)
        {
            ipInputField.text = "127.0.0.1";
        }

        // Hide connection status panel initially
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(false);
        }
    }

    private void SetupNetworkManager()
    {
        // Ensure all player prefabs have NetworkObject components
        ValidatePlayerPrefabs();

        // Register prefabs with NetworkManager
        RegisterPlayerPrefabs();

        // DEBUG: Comprehensive network diagnostics
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
                // SKIP LOBBY/SCENE OBJECTS
                if (prefab.name.Contains("Lobby") || prefab.name.Contains("Manager"))
                {
                    Debug.Log($"⏭️ Skipping validation for '{prefab.name}' - Appears to be a scene object, not a spawnable character");
                    continue;
                }

                NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    Debug.LogError($"❌ Player prefab '{prefab.name}' is missing NetworkObject component!");
                    allValid = false;
                }
                else
                {
                    Debug.Log($"✅ Player prefab '{prefab.name}' has NetworkObject component (InstanceID: {prefab.GetInstanceID()})");
                }
            }
        }

        if (!allValid)
        {
            Debug.LogError("🚨 SOME CHARACTER PREFABS ARE MISSING NETWORKOBJECT COMPONENTS!");
            Debug.LogError("💡 Please open each prefab in Unity Editor and add NetworkObject component to the root GameObject");
        }
        else
        {
            Debug.Log("🎉 All character prefabs are properly configured for networking!");
        }
    }

    private void RegisterPlayerPrefabs()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkManager.Singleton is null - prefabs will be registered when NetworkManager is available");
            return;
        }

        Debug.Log("📝 Registering character prefabs with NetworkManager...");

        int registeredCount = 0;

        foreach (GameObject prefab in availableCharacterPrefabs)
        {
            if (prefab != null)
            {
                // CRITICAL FIX: Skip LobbyManager and other scene objects
                if (prefab.name.Contains("Lobby") || prefab.name.Contains("LobbyManager"))
                {
                    Debug.Log($"⏭️ Skipping '{prefab.name}' - Scene objects should NOT be registered as network prefabs");
                    continue;
                }

                NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    bool alreadyRegistered = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
                        .Any(registeredPrefab => registeredPrefab != null && registeredPrefab.Prefab == prefab);

                    if (!alreadyRegistered)
                    {
                        NetworkManager.Singleton.NetworkConfig.Prefabs.Add(new NetworkPrefab
                        {
                            Prefab = prefab
                        });
                        registeredCount++;
                        Debug.Log($"✅ Registered network prefab: {prefab.name} (InstanceID: {prefab.GetInstanceID()})");
                    }
                    else
                    {
                        Debug.Log($"ℹ️ Network prefab already registered: {prefab.name} (InstanceID: {prefab.GetInstanceID()})");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Cannot register {prefab.name} - missing NetworkObject component!");
                }
            }
        }

        Debug.Log($"📝 Registration complete: {registeredCount} prefabs registered");
    }

    private void DebugNetworkSetup()
    {
        Debug.Log("🐛 DEBUG: Comprehensive Network Setup Analysis...");

        // 1. Check NetworkManager status
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager.Singleton is NULL!");
            return;
        }
        else
        {
            Debug.Log($"✅ NetworkManager exists - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsListening: {NetworkManager.Singleton.IsListening}");
        }

        // 2. Check registered prefabs
        var registeredPrefabs = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;
        Debug.Log($"📋 Total registered prefabs: {registeredPrefabs.Count}");

        int characterPrefabCount = 0;
        int scenePrefabCount = 0;

        foreach (var netPrefab in registeredPrefabs)
        {
            if (netPrefab != null && netPrefab.Prefab != null)
            {
                NetworkObject netObj = netPrefab.Prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // Check if this is a scene object that shouldn't be here
                    if (netPrefab.Prefab.name.Contains("Lobby") || netPrefab.Prefab.name.Contains("Manager"))
                    {
                        Debug.LogWarning($"   ⚠️ {netPrefab.Prefab.name} (InstanceID: {netPrefab.Prefab.GetInstanceID()}) - SCENE OBJECT INCORRECTLY REGISTERED!");
                        scenePrefabCount++;
                    }
                    else
                    {
                        Debug.Log($"   ✅ {netPrefab.Prefab.name} (InstanceID: {netPrefab.Prefab.GetInstanceID()})");
                        characterPrefabCount++;
                    }
                }
                else
                {
                    Debug.LogError($"   ❌ {netPrefab.Prefab.name} - REGISTERED BUT MISSING NETWORKOBJECT!");
                }
            }
            else
            {
                Debug.LogError($"   ❌ NULL prefab in registered list!");
            }
        }

        Debug.Log($"📊 Summary: {characterPrefabCount} character prefabs, {scenePrefabCount} incorrectly registered scene objects");

        // 3. Check transport configuration
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            Debug.Log($"📡 Transport Configuration:");
            Debug.Log($"   - Protocol: {transport.Protocol}");
            Debug.Log($"   - Connection Data: {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
            Debug.Log($"   - Max Connections: {transport.ConnectionData.ServerListenAddress}");
        }
        else
        {
            Debug.LogError("❌ UnityTransport component not found on NetworkManager!");
        }

        // 4. Scan scene for NetworkObjects that might cause issues
        Debug.Log("🔍 Scanning scene for NetworkObjects...");
        NetworkObject[] sceneObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"   Found {sceneObjects.Length} NetworkObjects in scene:");

        foreach (NetworkObject netObj in sceneObjects)
        {
            if (netObj != null)
            {
                bool? isSceneObject = netObj.IsSceneObject;
                bool isSpawned = netObj.IsSpawned;

                string sceneStatus = isSceneObject.HasValue && isSceneObject.Value ? "Scene Object" : "Prefab Instance";
                string spawnedStatus = isSpawned ? "Spawned" : "Not Spawned";

                Debug.Log($"   📍 {netObj.name} - {sceneStatus}, {spawnedStatus}");

                // Check if this is a potential problem object
                if (!isSpawned && !isSceneObject.HasValue)
                {
                    Debug.LogWarning($"   ⚠️ Potential issue: {netObj.name} is not spawned and has null IsSceneObject");
                }
            }
        }
    }

    // LOAD SELECTED CHARACTER FROM PLAYERPREFS
    private void LoadSelectedCharacterFromPrefs()
    {
        string savedCharacterName = PlayerPrefs.GetString("SelectedCharacter", "");
        if (!string.IsNullOrEmpty(savedCharacterName))
        {
            for (int i = 0; i < availableCharacterPrefabs.Length; i++) // <-- MODIFIED
            {
                GameObject prefab = availableCharacterPrefabs[i];
                if (prefab != null && prefab.name == savedCharacterName) // <-- MODIFIED
                {
                    selectedPlayerPrefab = prefab;
                    selectedCharacterIndex = i; // <-- ADDED
                    Debug.Log($"Loaded selected character from prefs: {savedCharacterName} (Index: {i})");
                    break;
                }
            }
        }
    }

    // SAVE SELECTED CHARACTER TO PLAYERPREFS
    private void SaveSelectedCharacterToPrefs()
    {
        if (selectedPlayerPrefab != null)
        {
            PlayerPrefs.SetString("SelectedCharacter", selectedPlayerPrefab.name);
            PlayerPrefs.Save();
            Debug.Log($"Saved selected character to prefs: {selectedPlayerPrefab.name}");
        }
    }

    // PREPARE FOR NEW GAME (CLOSE MENUS, SAVE SELECTION)
    public void PrepareForNewGame()
    {
        // Close all UI menus before starting
        if (titleScreenMenu != null) titleScreenMenu.SetActive(false);
        if (titleScreenCharacterCreationMenu != null) titleScreenCharacterCreationMenu.SetActive(false);
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(false);

        // --- ADD THIS FIX ---
        // Destroy the preview character before loading the next scene
        if (currentPreviewCharacter != null)
        {
            Destroy(currentPreviewCharacter);
            currentPreviewCharacter = null; // Clear the reference
            Debug.Log("🧹 Destroyed character preview object.");
        }
        // --- END FIX ---

        Debug.Log("Preparing for new game...");

        // Make sure we have a character selected and save it
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            selectedCharacterIndex = defaultCharacterIndex; // Ensure index is also set
        }

        // Save the selection
        SaveSelectedCharacterToPrefs();

        Debug.Log($"Selected character saved: {selectedPlayerPrefab?.name ?? "None"}");
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

    // START NEW GAME
    public void StartNewGame()
    {
        // Make sure we have a character selected
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
        }

        // Save the selection
        SaveSelectedCharacterToPrefs();

        // Prepare for new game (close menus, etc.)
        PrepareForNewGame();

        // This will now load the scene and THEN start the host
        WorldSaveGameManager.instance.AttempToCreateNewGame();
    }

    public void OpenLoadGameMenu()
    {
        // CLOSE MAIN MENU
        if (titleScreenMenu != null) titleScreenMenu.SetActive(false);

        // OPEN LOAD MENU
        if (titleScreenLoadMenu != null) titleScreenLoadMenu.SetActive(true);

        // SELECT THE RETURN BUTTON FIRST
        if (loadMenuReturnButton != null) loadMenuReturnButton.Select();
    }

    public void CloseLoadGameMenu()
    {
        // CLOSE LOAD MENU
        if (titleScreenLoadMenu != null) titleScreenLoadMenu.SetActive(false);

        // OPEN MAIN MENU
        if (titleScreenMenu != null) titleScreenMenu.SetActive(true);

        // SELECT THE LOAD BUTTON FIRST
        if (mainMenuReturnButton != null) mainMenuReturnButton.Select();
    }

    // CHARACTER SELECTION METHODS
    public void OpenCharacterSelectionMenu()
    {
        // CLOSE MAIN MENU
        if (titleScreenMenu != null) titleScreenMenu.SetActive(false);

        // OPEN CHARACTER SELECTION MENU
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(true);

        // SELECT THE FIRST CHARACTER BUTTON
        if (characterSelectionButtons.Length > 0 && characterSelectionButtons[0] != null)
        {
            characterSelectionButtons[0].Select();
            characterSelectionButtons[0].OnSelect(null);
        }

        // Create preview for first character
        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(0);
        }
    }

    public void CloseCharacterSelectionMenu()
    {
        // CLOSE CHARACTER SELECTION MENU
        if (titleScreenCharacterSelectionMenu != null) titleScreenCharacterSelectionMenu.SetActive(false);

        // OPEN MAIN MENU
        if (titleScreenMenu != null) titleScreenMenu.SetActive(true);

        // SELECT THE NEW GAME BUTTON
        if (mainMenuNewGameButton != null)
        {
            mainMenuNewGameButton.Select();
            mainMenuNewGameButton.OnSelect(null);
        }
    }

    // SELECT CHARACTER PREFAB
    public void SelectCharacterPrefab(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[characterIndex];
            selectedCharacterIndex = characterIndex; // <-- ADDED
            Debug.Log($"Selected character: {selectedPlayerPrefab.name} (Index: {characterIndex})");

            // Create preview of selected character
            if (characterPreviewSpawnPoint != null)
            {
                CreateCharacterPreview(characterIndex);
            }
        }
        else
        {
            Debug.LogError($"Invalid character index: {characterIndex}");
        }
    }

    // CREATE CHARACTER PREVIEW
    private void CreateCharacterPreview(int characterIndex)
    {
        // Remove current preview
        if (currentPreviewCharacter != null)
        {
            Destroy(currentPreviewCharacter);
        }

        if (characterIndex < 0 || characterIndex >= availableCharacterPrefabs.Length)
            return;

        // Create new preview character
        currentPreviewCharacter = Instantiate(
            availableCharacterPrefabs[characterIndex],
            characterPreviewSpawnPoint.position,
            characterPreviewSpawnPoint.rotation
        );

        // Disable unnecessary components for preview
        SetupPreviewCharacter(currentPreviewCharacter);

        Debug.Log($"Created preview for character: {availableCharacterPrefabs[characterIndex].name}");
    }

    // SETUP PREVIEW CHARACTER (DISABLE GAMEPLAY COMPONENTS)
    private void SetupPreviewCharacter(GameObject previewCharacter)
    {
        // Remove NetworkObject component entirely for preview characters to prevent network issues
        NetworkObject networkObject = previewCharacter.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            DestroyImmediate(networkObject);
            Debug.Log($"🔧 Removed NetworkObject from preview character: {previewCharacter.name}");
        }

        // Disable PlayerManager if present
        PlayerManager playerManager = previewCharacter.GetComponent<PlayerManager>();
        if (playerManager != null)
        {
            playerManager.enabled = false;
        }

        // Disable CharacterController if present
        CharacterController characterController = previewCharacter.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Disable CharacterLocomotionManager if present
        CharacterLocomotionManager locomotionManager = previewCharacter.GetComponent<CharacterLocomotionManager>();
        if (locomotionManager != null)
        {
            locomotionManager.enabled = false;
        }

        // Disable PlayerLocomotionManager if present
        PlayerLocomotionManager playerLocomotion = previewCharacter.GetComponent<PlayerLocomotionManager>();
        if (playerLocomotion != null)
        {
            playerLocomotion.enabled = false;
        }

        // Disable any Rigidbody if present
        Rigidbody rb = previewCharacter.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Disable all colliders to prevent physics interactions
        Collider[] colliders = previewCharacter.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    /// CONFIRM CHARACTER SELECTION AND PROCEED TO CHARACTER CREATION
    public void ConfirmCharacterSelection()
    {
        if (selectedPlayerPrefab != null)
        {
            // Save the selection
            SaveSelectedCharacterToPrefs();
            OpenCharacterCreationMenu();
        }
        else
        {
            Debug.LogWarning("No character selected!");
            // Fallback to default character
            if (availableCharacterPrefabs.Length > 0)
            {
                selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
                selectedCharacterIndex = defaultCharacterIndex; // <-- ADDED
                SaveSelectedCharacterToPrefs();
                OpenCharacterCreationMenu();
            }
        }
    }

    public void OpenCharacterCreationMenu()
    {
        if (titleScreenCharacterCreationMenu != null)
            titleScreenCharacterCreationMenu.SetActive(true);

        // Close character selection menu when opening creation
        if (titleScreenCharacterSelectionMenu != null)
        {
            titleScreenCharacterSelectionMenu.SetActive(false);
        }
    }

    public void CloseCharacterCreationMenu()
    {
        if (titleScreenCharacterCreationMenu != null)
            titleScreenCharacterCreationMenu.SetActive(false);
    }

    public void OpenChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(false);

        if (characterClassMenu != null)
            characterClassMenu.SetActive(true);

        if (characterClassButtons.Length > 0 && characterClassButtons[0] != null)
        {
            characterClassButtons[0].Select();
            characterClassButtons[0].OnSelect(null);
        }
    }

    public void CloseChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(true);

        if (characterClassMenu != null)
            characterClassMenu.SetActive(false);

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

    // CHARACTER SLOTS
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
        if (deleteCharacterSlotPopUp != null)
            deleteCharacterSlotPopUp.SetActive(false);
            
        WorldSaveGameManager.instance.DeleteGame(currentSelectedSlot);

        // WE DISABLE AND ENABLE THE LOAD MENU TO REFRESH THE SLOTS AFTER BEING DELETED
        if (titleScreenLoadMenu != null)
        {
            titleScreenLoadMenu.SetActive(false);
            titleScreenLoadMenu.SetActive(true);
        }
        if (loadMenuReturnButton != null) loadMenuReturnButton.Select();
    }

    public void CloseDeleteCharacterPopUp()
    {
        if (deleteCharacterSlotPopUp != null)
            deleteCharacterSlotPopUp.SetActive(false);
            
        if (loadMenuReturnButton != null) loadMenuReturnButton.Select();
    }

    // CHARACTER CLASS
    public void SelectClass(int classID)
    {
        Debug.Log($"Class selected: {classID}");

        // If we have a player in the scene, apply the class
        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null && startingClasses.Length > classID)
        {
            startingClasses[classID].SetClass(player);
        }

        CloseChooseCharacterClassSubMenu();
    }

    // PREVIEWCLASS TO PREVIEW CHARACTER SELECTION
    public void PreviewClass(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(characterIndex);
            Debug.Log($"Previewing character: {availableCharacterPrefabs[characterIndex].name}");
        }
    }

    public void SetCharacterClass(PlayerManager player, int vitality, int endurance, int strength, int dexterity, int intelligence,
        WeaponItem[] mainHandWeapons, WeaponItem[] offHandWeapons)
    {
        // SET STATS
        player.playerNetworkManager.vitality.Value = vitality;
        player.playerNetworkManager.endurance.Value = endurance;
        player.playerNetworkManager.dexterity.Value = dexterity;
        player.playerNetworkManager.intelligence.Value = intelligence;

        // SET WEAPONS
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

    // CLEAN UP WHEN DESTROYED
    private void OnDestroy()
    {
        if (currentPreviewCharacter != null)
        {
            Destroy(currentPreviewCharacter);
        }
    }

    // LOBBY NETWORK METHODS - UPDATED WITH LOBBYMANAGER SPAWNING
    public void HostGame()
    {
        if (isAttemptingConnection) return;

        PrepareForNewGame();
        Debug.Log("🎯 HOST: Starting host and loading lobby scene...");
        StartCoroutine(StartHostThenLoadLobby());
    }

    private IEnumerator StartHostThenLoadLobby()
    {
        isAttemptingConnection = true;
        ShowConnectionStatus("Starting Host...", Color.yellow);

        // Make sure NetworkManager exists
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            ShowConnectionStatus("Network Manager not found!", Color.red);
            isAttemptingConnection = false;
            yield break;
        }

        // Enhanced debugging for host
        Debug.Log("🔍 HOST: Running comprehensive network diagnostics...");
        DebugNetworkSetup();

        // Validate selected character prefab
        if (!ValidateSelectedCharacterPrefab())
        {
            Debug.LogError("❌ Cannot host game - selected character prefab is not properly configured!");
            ShowConnectionStatus("Invalid character configuration!", Color.red);
            isAttemptingConnection = false;
            yield break;
        }

        // Register prefabs before starting
        RegisterPlayerPrefabs();

        // Start host FIRST
        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.Log("🚀 HOST: Starting NetworkManager as host...");
            ShowConnectionStatus("Starting Host...", Color.yellow);

            // Subscribe to network events for debugging
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            bool success = NetworkManager.Singleton.StartHost();

            if (success)
            {
                Debug.Log("✅ HOST: Host started successfully!");
                ShowConnectionStatus("Host Started - Loading Lobby...", Color.green);
                
                Debug.Log($"📊 HOST Network Status - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");

                // Wait a short time for initialization
                yield return new WaitForSeconds(1f);

                // Check network status again
                Debug.Log($"📊 HOST After wait - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");

                // CRITICAL FIX: Subscribe to scene load event to spawn LobbyManager after scene loads
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnHostSceneLoadCompleted;

                // NOW load the lobby scene using NetworkSceneManager
                Debug.Log("📥 HOST: Loading lobby scene...");
                var status = NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"❌ Failed to load LobbyScene: {status}");
                    ShowConnectionStatus($"Failed to load Lobby: {status}", Color.red);
                    isAttemptingConnection = false;
                    yield break;
                }

                Debug.Log("🎮 LobbyScene load initiated; waiting for scene to load...");
            }
            else
            {
                Debug.LogError("❌ HOST: Failed to start host!");
                ShowConnectionStatus("Failed to start host!", Color.red);
            }
        }
        else
        {
            Debug.Log("HOST: NetworkManager is already listening, loading lobby scene...");
            
            // Subscribe to scene load event
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnHostSceneLoadCompleted;
            
            var status = NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"❌ Failed to load LobbyScene: {status}");
                ShowConnectionStatus($"Failed to load Lobby: {status}", Color.red);
            }
        }

        isAttemptingConnection = false;
    }

    // NEW METHOD: Handle scene load completion for HOST
    private void OnHostSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"🎯 HOST: Scene load completed - {sceneName}");
        
        // Unsubscribe to avoid multiple calls
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnHostSceneLoadCompleted;
        
        if (sceneName == "LobbyScene")
        {
            Debug.Log("🎯 HOST: LobbyScene loaded - ensuring LobbyManager is spawned...");
            
            // As the host, spawn the LobbyManager for all clients
            SpawnLobbyManagerAsHost();
        }
    }

    // NEW METHOD: Spawn LobbyManager as host
    private void SpawnLobbyManagerAsHost()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("❌ Cannot spawn LobbyManager - not the server!");
            return;
        }

        Debug.Log("🎯 HOST: Looking for LobbyManager in scene to spawn...");
        
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        
        if (lobbyManager != null)
        {
            NetworkObject lobbyNetObj = lobbyManager.GetComponent<NetworkObject>();
            
            if (lobbyNetObj != null)
            {
                if (!lobbyNetObj.IsSpawned)
                {
                    Debug.Log("🔄 HOST: Spawning LobbyManager on network for all clients...");
                    lobbyNetObj.Spawn();
                    Debug.Log("✅ HOST: LobbyManager spawned successfully! All clients should see it now.");
                }
                else
                {
                    Debug.Log("ℹ️ HOST: LobbyManager is already spawned.");
                }
            }
            else
            {
                Debug.LogError("❌ HOST: LobbyManager is missing NetworkObject component!");
            }
        }
        else
        {
            Debug.LogError("❌ HOST: LobbyManager not found in LobbyScene!");
            
            // Alternative: Try to spawn from prefab if available
            if (lobbyManagerPrefab != null)
            {
                Debug.Log("🔄 HOST: Attempting to spawn LobbyManager from prefab...");
                GameObject lobbyManagerObj = Instantiate(lobbyManagerPrefab);
                NetworkObject lobbyNetObj = lobbyManagerObj.GetComponent<NetworkObject>();
                
                if (lobbyNetObj != null)
                {
                    lobbyNetObj.Spawn();
                    Debug.Log("✅ HOST: LobbyManager prefab spawned successfully!");
                }
                else
                {
                    Debug.LogError("❌ HOST: LobbyManager prefab is missing NetworkObject component!");
                }
            }
        }
    }

    public void JoinGame(string ipAddress = "127.0.0.1")
    {
        if (isAttemptingConnection)
        {
            Debug.LogWarning("⚠️ Already attempting to connect, please wait...");
            return;
        }

        PrepareForNewGame();
        Debug.Log($"🎯 CLIENT: Joining game at {ipAddress} and loading lobby scene...");
        StartCoroutine(StartClientThenLoadLobby(ipAddress));
    }

    private IEnumerator StartClientThenLoadLobby(string ipAddress)
    {
        isAttemptingConnection = true;
        ShowConnectionStatus($"Connecting to {ipAddress}...", Color.yellow);

        // Make sure NetworkManager exists
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            ShowConnectionStatus("Network Manager not found!", Color.red);
            isAttemptingConnection = false;
            yield break;
        }

        Debug.Log("🔍 CLIENT: Running comprehensive network diagnostics...");
        DebugNetworkSetup();

        // Validate selected character prefab
        if (!ValidateSelectedCharacterPrefab())
        {
            Debug.LogError("❌ Cannot join game - selected character prefab is not properly configured!");
            ShowConnectionStatus("Invalid character configuration!", Color.red);
            isAttemptingConnection = false;
            yield break;
        }

        // FIRST: Register player prefabs
        RegisterPlayerPrefabs();

        // THEN: Set connection data
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, 7777);
            Debug.Log($"🔗 CLIENT: Set connection data: {ipAddress}:7777");

            // Debug transport settings
            Debug.Log($"📡 Transport Settings - Address: {transport.ConnectionData.Address}, Port: {transport.ConnectionData.Port}, ServerListenAddress: {transport.ConnectionData.ServerListenAddress}");
        }
        else
        {
            Debug.LogError("❌ CLIENT: UnityTransport not found on NetworkManager!");
            ShowConnectionStatus("Network transport error!", Color.red);
            isAttemptingConnection = false;
            yield break;
        }

        // Check if we're already connected
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("ℹ️ CLIENT: Already connected to server.");
            ShowConnectionStatus("Already connected!", Color.green);
            isAttemptingConnection = false;
            yield break;
        }

        // FINALLY: Start client
        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.Log("🚀 CLIENT: Starting NetworkManager as client...");
            ShowConnectionStatus("Connecting...", Color.yellow);

            // Subscribe to network events for debugging
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            bool startSuccess = NetworkManager.Singleton.StartClient();

            if (startSuccess)
            {
                Debug.Log("✅ CLIENT: StartClient() returned true");
                Debug.Log($"📊 CLIENT Status - IsListening: {NetworkManager.Singleton.IsListening}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
                Debug.Log("⏳ CLIENT: Waiting for client connection...");

                float timeout = 15f; // Increased timeout
                float timer = 0f;
                int connectionAttempts = 0;

                while (!NetworkManager.Singleton.IsConnectedClient && timer < timeout)
                {
                    timer += Time.deltaTime;
                    connectionAttempts++;

                    // Update status every second
                    if (connectionAttempts % 60 == 0)
                    {
                        ShowConnectionStatus($"Connecting... ({timer:F1}s)", Color.yellow);
                        Debug.Log($"   CLIENT: ... waiting for connection ({timer:F1}s/{timeout}s)");
                    }

                    yield return null;
                }

                if (NetworkManager.Singleton.IsConnectedClient)
                {
                    Debug.Log("🎉 CLIENT: Client connected successfully!");
                    ShowConnectionStatus("Connected! Waiting for host...", Color.green);
                    Debug.Log($"📊 CLIENT Network Status - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");

                    // Wait a moment to ensure everything is synchronized
                    yield return new WaitForSeconds(2f);
                    ShowConnectionStatus("Connected! Lobby loading...", Color.green);
                }
                else
                {
                    Debug.LogError($"❌ CLIENT: Client connection timed out after {timeout} seconds!");
                    ShowConnectionStatus($"Connection timed out after {timeout}s", Color.red);
                    Debug.LogError($"   Final Status - IsListening: {NetworkManager.Singleton.IsListening}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");

                    NetworkManager.Singleton.Shutdown();

                    // Show error to user
                    ShowConnectionError($"Connection timed out after {timeout} seconds. Please check if the server is running at {ipAddress}:7777");
                }
            }
            else
            {
                Debug.LogError("❌ CLIENT: StartClient() returned false - failed to start client!");
                ShowConnectionStatus("Failed to start client!", Color.red);
                ShowConnectionError("Failed to start network client. Please try again.");
            }
        }
        else
        {
            Debug.Log("ℹ️ CLIENT: NetworkManager is already listening, checking connection status...");

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("✅ CLIENT: Already connected.");
                ShowConnectionStatus("Already connected!", Color.green);
            }
            else
            {
                Debug.LogError("❌ CLIENT: NetworkManager is listening but not connected!");
                ShowConnectionStatus("Network manager error!", Color.red);
                ShowConnectionError("Network manager error. Please try again.");
            }
        }

        isAttemptingConnection = false;
        yield return new WaitForSeconds(2f);
        HideConnectionStatus();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"🎉 Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"👋 Client disconnected: {clientId}");
        isAttemptingConnection = false;
        ShowConnectionStatus("Disconnected from server", Color.red);
    }

    private void ShowConnectionStatus(string message, Color color)
    {
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(true);
        }
        
        if (connectionStatusText != null)
        {
            connectionStatusText.text = message;
            connectionStatusText.color = color;
        }
    }

    private void HideConnectionStatus()
    {
        if (connectionStatusPanel != null)
        {
            connectionStatusPanel.SetActive(false);
        }
    }

    private void ShowConnectionError(string message)
    {
        Debug.LogError($"💔 Connection Error: {message}");
        ShowConnectionStatus(message, Color.red);

        // Re-enable the join button
        if (joinButton != null)
        {
            joinButton.interactable = true;
        }
    }

    private bool ValidateSelectedCharacterPrefab()
    {
        if (selectedPlayerPrefab == null)
        {
            Debug.LogError("❌ No character prefab selected!");
            return false;
        }

        // Skip validation for scene objects
        if (selectedPlayerPrefab.name.Contains("Lobby") || selectedPlayerPrefab.name.Contains("Manager"))
        {
            Debug.LogWarning($"⚠️ Selected prefab '{selectedPlayerPrefab.name}' appears to be a scene object, not a character!");
            return false;
        }

        NetworkObject networkObject = selectedPlayerPrefab.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"❌ Selected character '{selectedPlayerPrefab.name}' is missing NetworkObject component!");
            Debug.LogError("   Please open this prefab in Unity Editor and add NetworkObject component");
            return false;
        }

        Debug.Log($"✅ Selected character '{selectedPlayerPrefab.name}' is properly configured for networking (InstanceID: {selectedPlayerPrefab.GetInstanceID()})");
        return true;
    }

    [ContextMenu("Test Server Connection")]
    public void TestServerConnection()
    {
        Debug.Log("🧪 Testing server connection...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager not found!");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            string ip = ipInputField?.text ?? "127.0.0.1";
            Debug.Log($"🔍 Testing connection to {ip}:7777");
            Debug.Log($"📡 Transport configured for: {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
        }

        Debug.Log($"🏠 Network Status - IsServer: {NetworkManager.Singleton.IsServer}, IsClient: {NetworkManager.Singleton.IsClient}, IsListening: {NetworkManager.Singleton.IsListening}");
    }

    [ContextMenu("Find Problematic NetworkObject by Hash")]
    public void FindProblematicObjectByHash()
    {
        uint targetHash = 925896200;
        Debug.Log($"🔍 Searching for NetworkObject with hash: {targetHash}");

        // Helper: try to read GlobalObjectIdHash via reflection, otherwise fall back to a name-based hash
        System.Func<NetworkObject, uint> getHash = (NetworkObject netObj) =>
        {
            if (netObj == null) return 0;
            var prop = netObj.GetType().GetProperty("GlobalObjectIdHash");
            if (prop != null)
            {
                try
                {
                    object val = prop.GetValue(netObj);
                    return System.Convert.ToUInt32(val);
                }
                catch
                {
                    Debug.LogWarning($"⚠️ Unable to read GlobalObjectIdHash via reflection for {netObj.name}, falling back to name hash.");
                }
            }
            // Fallback: use stable name-based hash (not guaranteed to match Netcode hash but useful to search)
            uint fallbackHash = (uint)netObj.name.GetHashCode();
            Debug.Log($"ℹ️ Using fallback name hash for {netObj.name}: {fallbackHash}");
            return fallbackHash;
        };

        // Check registered prefabs in NetworkManager
        if (NetworkManager.Singleton != null)
        {
            var registeredPrefabs = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;
            foreach (var netPrefab in registeredPrefabs)
            {
                if (netPrefab != null && netPrefab.Prefab != null)
                {
                    NetworkObject netObj = netPrefab.Prefab.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        uint prefabHash = getHash(netObj);
                        if (prefabHash == targetHash)
                        {
                            Debug.Log($"✅ FOUND IN REGISTERED PREFABS: {netPrefab.Prefab.name} (InstanceID: {netPrefab.Prefab.GetInstanceID()})");
                            return;
                        }
                    }
                }
            }
        }

        // Check all prefabs in project
        #if UNITY_EDITOR
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in allPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                NetworkObject netObj = prefab.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    uint prefabHash = getHash(netObj);
                    if (prefabHash == targetHash)
                    {
                        Debug.Log($"✅ FOUND IN PROJECT PREFABS: {prefab.name} at path: {path}");
                        Debug.Log($"   InstanceID: {prefab.GetInstanceID()}, ComputedHash: {prefabHash}");
                        return;
                    }
                }
            }
        }
        #endif

        Debug.LogError($"❌ Could not find NetworkObject with hash {targetHash} in registered prefabs or project assets!");
        Debug.Log("💡 This might be a scene object or a dynamically spawned object.");
    }
}