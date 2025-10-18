using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    public static TitleScreenManager Instance;
    public static GameObject selectedPlayerPrefab;

    // ADDED: Character Prefab Selection
    [Header("Character Prefabs")]
    [SerializeField] GameObject[] availableCharacterPrefabs;
    [SerializeField] int defaultCharacterIndex = 0;

    // MAIN MENU
    [Header("Main Menu Menus")]
    [SerializeField] GameObject titleScreenMenu;
    [SerializeField] GameObject titleScreenLoadMenu;
    [SerializeField] GameObject titleScreenCharacterCreationMenu;
    [SerializeField] GameObject titleScreenCharacterSelectionMenu; // ADDED

    [Header("Main Menu Buttons")]
    [SerializeField] Button loadMenuReturnButton;
    [SerializeField] Button mainMenuReturnButton;
    [SerializeField] Button mainMenuNewGameButton;
    [SerializeField] Button deleteCharacterPopUpConfirmButton;
    [SerializeField] Button characterSelectionReturnButton; // ADDED

    [Header("Character Selection Buttons")]
    [SerializeField] Button[] characterSelectionButtons; // ADDED

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

    // ADDED: Character Preview
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
        // Initialize with default character preview
        if (availableCharacterPrefabs.Length > 0 && characterPreviewSpawnPoint != null)
        {
            CreateCharacterPreview(defaultCharacterIndex);
        }
    }

    public void StartNetworkAsHost()
    {
        // Code to start the network as host
        Debug.Log("Starting network as host...");

        NetworkManager.Singleton.StartHost();
    }

    // MODIFIED: Now opens character selection instead of direct character creation
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
        // Make sure we have a character selected
        if (selectedPlayerPrefab == null && availableCharacterPrefabs.Length > 0)
        {
            selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
        }
        
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

    // ADDED: Character Selection Methods
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

    // ADDED: Select Character Prefab
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

    // ADDED: Create Character Preview
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

    // ADDED: Setup preview character (disable gameplay components)
    // ADDED: Setup preview character (disable gameplay components)
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
    PreviewCharacterRotator rotator = previewCharacter.AddComponent<PreviewCharacterRotator>();
    rotator.rotationSpeed = 15f;
}

    // ADDED: Confirm character selection and proceed to character creation
    public void ConfirmCharacterSelection()
    {
        if (selectedPlayerPrefab != null)
        {
            OpenCharacterCreationMenu();
        }
        else
        {
            Debug.LogWarning("No character selected!");
            // Fallback to default character
            if (availableCharacterPrefabs.Length > 0)
            {
                selectedPlayerPrefab = availableCharacterPrefabs[defaultCharacterIndex];
                OpenCharacterCreationMenu();
            }
        }
    }

    // YOUR EXISTING METHODS - UNCHANGED
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
        PlayerManager player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerManager>();

        if (startingClasses.Length <= 0)
            return;

        startingClasses[classID].SetClass(player);
        CloseChooseCharacterClassSubMenu();
    }

    // MODIFIED: PreviewClass to preview character selection
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
        //player.playerNetworkManager.mind.Value = mind;
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

    // ADDED: Clean up when destroyed
    private void OnDestroy()
    {
        if (currentPreviewCharacter != null)
        {
            Destroy(currentPreviewCharacter);
        }
    }
}

// ADDED: Helper class for rotating preview characters
public class PreviewCharacterRotator : MonoBehaviour
{
    public float rotationSpeed = 15f;
    
    private void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}