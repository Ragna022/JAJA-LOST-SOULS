using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    public static TitleScreenManager Instance;
    public static GameObject selectedPlayerPrefab;

    // MAIN MENU
    [Header("Main Menu Menus")]
    [SerializeField] GameObject titleScreenMenu;
    [SerializeField] GameObject titleScreenLoadMenu;
    [SerializeField] GameObject titleScreenCharacterCreationMenu;

    [Header("Main Menu Buttons")]
    [SerializeField] Button loadMenuReturnButton;
    [SerializeField] Button mainMenuReturnButton;
    [SerializeField] Button mainMenuNewGameButton;
    [SerializeField] Button deleteCharacterPopUpConfirmButton;

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



    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void StartNetworkAsHost()
    {
        // Code to start the network as host
        Debug.Log("Starting network as host...");

        NetworkManager.Singleton.StartHost();
    }

    public void AttemptToCreateNewCharacter()
    {
        if (WorldSaveGameManager.instance.HasFreeCharacterSlots())
        {
            OpenCharacterCreationMenu();
        }
        else
        {
            DisplayNoFreeCharacterSlotsPopUp();
        }
    }

    public void StartNewGame()
    {
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

    public void OpenCharacterCreationMenu()
    {
        titleScreenCharacterCreationMenu.SetActive(true);
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

    public void PreviewClass(int classID)
    {
        PlayerManager previewPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerManager>();

    if (startingClasses.Length <= 0)
        return;
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

}
