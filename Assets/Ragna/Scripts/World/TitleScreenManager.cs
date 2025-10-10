using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
{
    public static TitleScreenManager Instance;

    [Header("Menus")]
    [SerializeField] GameObject titleScreenMenu;
    [SerializeField] GameObject titleScreenLoadMenu;

    [Header("Buttons")]
    [SerializeField] Button loadMenuReturnButton;
    [SerializeField] Button mainMenuReturnButton;
    [SerializeField] Button mainMenuNewGameButton;
    [SerializeField] Button deleteCharacterPopUpConfirmButton;

    [Header("Pop Ups")]
    [SerializeField] GameObject noCharacterSlotsPopUp;
    [SerializeField] Button noCharacterSlotsOkayButton;
    [SerializeField] GameObject deleteCharacterSlotPopUp;

    [Header("Character Slots")]
    public CharacterSlot currentSelectedSlot = CharacterSlot.NO_SLOT;



    

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

}
