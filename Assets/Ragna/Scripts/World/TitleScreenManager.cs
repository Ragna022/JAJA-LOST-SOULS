using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    public static TitleScreenManager Instance;
    public static GameObject selectedPlayerPrefab;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Set default character prefab
            if (availableCharacterPrefabs.Length > 0)
            {
                selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
            }
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
    }

    // LOAD SELECTED CHARACTER FROM PLAYERPREFS
    private void LoadSelectedCharacterFromPrefs()
    {
        string savedCharacterName = PlayerPrefs.GetString("SelectedCharacter", "");
        if (!string.IsNullOrEmpty(savedCharacterName))
        {
            foreach (var prefab in availableCharacterPrefabs)
            {
                if (prefab.name == savedCharacterName)
                {
                    selectedPlayerPrefab = prefab;
                    Debug.Log($"Loaded selected character from prefs: {savedCharacterName}");
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
        titleScreenMenu.SetActive(false);
        titleScreenCharacterCreationMenu.SetActive(false);
        titleScreenCharacterSelectionMenu.SetActive(false);
        
        Debug.Log("Preparing for new game...");
        
        // Make sure we have a character selected and save it
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
        }
        
        // Save the selection
        SaveSelectedCharacterToPrefs();
        
        Debug.Log($"Selected character saved: {selectedPlayerPrefab.name}");
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
        titleScreenMenu.SetActive(false);

        // OPEN LOAD MENU
        titleScreenLoadMenu.SetActive(true);

        // SELECT THE RETURN BUTTON FIRST
        loadMenuReturnButton.Select();

        //FIND THE FIRST LOAD SLOT AND AUTO SELECT IT
    }

    public void CloseLoadGameMenu()
    {
        // CLOSE LOAD MENU
        titleScreenLoadMenu.SetActive(false);

        // OPEN MAIN MENU
        titleScreenMenu.SetActive(true);

        // SELECT THE LOAD BUTTON FIRST
        mainMenuReturnButton.Select();

        //FIND THE FIRST LOAD SLOT AND AUTO SELECT IT
    }

    // CHARACTER SELECTION METHODS
    public void OpenCharacterSelectionMenu()
    {
        // CLOSE MAIN MENU
        titleScreenMenu.SetActive(false);

        // OPEN CHARACTER SELECTION MENU
        titleScreenCharacterSelectionMenu.SetActive(true);

        // SELECT THE FIRST CHARACTER BUTTON
        if (characterSelectionButtons.Length > 0)
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
        titleScreenCharacterSelectionMenu.SetActive(false);

        // OPEN MAIN MENU
        titleScreenMenu.SetActive(true);

        // SELECT THE NEW GAME BUTTON
        mainMenuNewGameButton.Select();
        mainMenuNewGameButton.OnSelect(null);
    }

    // SELECT CHARACTER PREFAB
    public void SelectCharacterPrefab(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[characterIndex];
            Debug.Log($"Selected character: {selectedPlayerPrefab.name}");
            
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
        // Disable NetworkObject if present
        NetworkObject networkObject = previewCharacter.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.enabled = false;
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

        // Add rotation script for nice visual effect
        //PreviewCharacterRotator rotator = previewCharacter.AddComponent<PreviewCharacterRotator>();
        //rotator.rotationSpeed = 15f;
    }

    // CONFIRM CHARACTER SELECTION AND PROCEED TO CHARACTER CREATION
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
                SaveSelectedCharacterToPrefs();
                OpenCharacterCreationMenu();
            }
        }
    }

    public void OpenCharacterCreationMenu()
    {
        titleScreenCharacterCreationMenu.SetActive(true);
        
        // Close character selection menu when opening creation
        if (titleScreenCharacterSelectionMenu != null)
        {
            titleScreenCharacterSelectionMenu.SetActive(false);
        }
    }

    public void CloseCharacterCreationMenu()
    {
        titleScreenCharacterCreationMenu.SetActive(false);
    }

    public void OpenChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(false);

        characterClassMenu.SetActive(true);

        if(characterClassButtons.Length > 0)
        {
            characterClassButtons[0].Select();
            characterClassButtons[0].OnSelect(null);
        }
    }

    public void CloseChooseCharacterClassSubMenu()
    {
        ToogleCharacterCreationScreenMainMenuButtons(true);

        characterClassMenu.SetActive(false);

        characterClassButton.Select();
        characterClassButton.OnSelect(null);
    }
    
    private void ToogleCharacterCreationScreenMainMenuButtons(bool status)
    {
        characterNameButton.enabled = status;
        characterClassButton.enabled = status;
        startGameButton.enabled = status;
    }

    public void DisplayNoFreeCharacterSlotsPopUp()
    {
        noCharacterSlotsPopUp.SetActive(true);
        noCharacterSlotsOkayButton.Select();
    }

    public void CloseNoFreeCharacterSlotsPopUp()
    {
        noCharacterSlotsPopUp.SetActive(false);
        mainMenuNewGameButton.Select();
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
        if (currentSelectedSlot != CharacterSlot.NO_SLOT)
        {
            deleteCharacterSlotPopUp.SetActive(true);
            deleteCharacterPopUpConfirmButton.Select();
        }
    }

    public void DeleteCharacterSlot()
    {
        deleteCharacterSlotPopUp.SetActive(false);
        WorldSaveGameManager.instance.DeleteGame(currentSelectedSlot);

        // WE DISABLE AND ENABLE THE LOAD MENU TO REFRESH THE SLOTS AFTER BEING DELETED
        titleScreenLoadMenu.SetActive(false);
        titleScreenLoadMenu.SetActive(true);
        loadMenuReturnButton.Select();
    }

    public void CloseDeleteCharacterPopUp()
    {
        deleteCharacterSlotPopUp.SetActive(false);
        loadMenuReturnButton.Select();
    }

    // CHARACTER CLASS
    public void SelectClass(int classID)
    {
        // This would be called when you actually start the game with a class
        // For now, we'll handle class selection differently
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
        // Use this method to preview character when hovering/selecting buttons
        if (characterIndex >= 0 && characterIndex < availableCharacterPrefabs.Length && characterPreviewSpawnPoint != null)
        {
            // Create preview of the character
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

    // LOBBY NETWORK METHODS
   // Add these methods to your existing TitleScreenManager class:

public void HostGame()
{
    PrepareForNewGame();
    
    Debug.Log("Starting host and loading lobby scene...");
    
    // Start the host FIRST, then load the scene
    StartCoroutine(StartHostThenLoadLobby());
}

private System.Collections.IEnumerator StartHostThenLoadLobby()
{
    // Make sure NetworkManager exists
    if (NetworkManager.Singleton == null)
    {
        Debug.LogError("NetworkManager not found!");
        yield break;
    }
    
    // Start host FIRST
    if (!NetworkManager.Singleton.IsListening)
    {
        Debug.Log("🚀 Starting NetworkManager as host...");
        bool success = NetworkManager.Singleton.StartHost();
        
        if (success)
        {
            Debug.Log("✅ Host started successfully!");
            
            // Wait for network to initialize
            yield return new WaitForSeconds(1f);
            
            // NOW load the lobby scene
            Debug.Log("📥 Loading lobby scene...");
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.LogError("❌ Failed to start host!");
        }
    }
    else
    {
        Debug.Log("NetworkManager is already listening, loading lobby scene...");
        SceneManager.LoadScene("LobbyScene");
    }
}

public void JoinGame(string ipAddress = "127.0.0.1")
{
    PrepareForNewGame();
    
    Debug.Log("Joining game and loading lobby scene...");
    
    // Start the client FIRST, then load the scene
    StartCoroutine(StartClientThenLoadLobby(ipAddress));
}

private System.Collections.IEnumerator StartClientThenLoadLobby(string ipAddress)
{
    // Make sure NetworkManager exists
    if (NetworkManager.Singleton == null)
    {
        Debug.LogError("NetworkManager not found!");
        yield break;
    }

    // Set connection data
    var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
    if (transport != null)
    {
        transport.SetConnectionData(ipAddress, 7777);
        Debug.Log($"🔗 Set connection data: {ipAddress}:7777");
    }

    // Start client FIRST
    if (!NetworkManager.Singleton.IsListening)
    {
        Debug.Log("🚀 Starting NetworkManager as client...");
        bool success = NetworkManager.Singleton.StartClient();
        
        if (success)
        {
            Debug.Log("✅ Client started successfully!");
            
            // Wait for network to initialize
            yield return new WaitForSeconds(1f);
            
            // NOW load the lobby scene
            Debug.Log("📥 Loading lobby scene...");
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.LogError("❌ Failed to start client!");
        }
    }
    else
    {
        Debug.Log("NetworkManager is already listening, loading lobby scene...");
        SceneManager.LoadScene("LobbyScene");
    }
}
}