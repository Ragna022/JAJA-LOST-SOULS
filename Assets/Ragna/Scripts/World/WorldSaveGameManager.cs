using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSaveGameManager : MonoBehaviour
{
    public static WorldSaveGameManager instance;

    public PlayerManager player;

    [Header("SAVE/LOAD")]
    [SerializeField] bool saveGame;
    [SerializeField] bool loadGame;


    [Header("World Scene Index")]
    [SerializeField] int worldSceneIndex = 1;

    [Header("Save Data Writer")]
    private SaveFileDataWriter saveFileDataWriter;

    [Header("Current Character Data")]
    public CharacterSlot currentCharacterSlotBeingUsed;
    public CharacterSaveData currentCharacterData;
    private string saveFileName;

    [Header("Character Slots")]
    public CharacterSaveData characterSlot01;
    public CharacterSaveData characterSlot02;
    public CharacterSaveData characterSlot03;
    public CharacterSaveData characterSlot04;
    public CharacterSaveData characterSlot05;
    public CharacterSaveData characterSlot06;
    public CharacterSaveData characterSlot07;
    public CharacterSaveData characterSlot08;
    public CharacterSaveData characterSlot09;
    public CharacterSaveData characterSlot10;
    

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        LoadAllCharacterProfiles();
    }

    private void Update()
    {
        if (saveGame)
        {
            saveGame = false;
            SaveGame();
        }

        if (loadGame)
        {
            loadGame = false;
            LoadGame();
        }
    }

    public bool HasFreeCharacterSlots()
    {
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        // CHECK TO SEE IF WE CAN CREATE A NEW SAVE FILE (CHECK FOR OTHER EXISTING FILES FIRST)
        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_01);
        
        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_02);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_03);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_04);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_05);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_06);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_07);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_08);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_09);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_10);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            return true;
        }

        return false;
    }

    public string DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot characterSlot)
    {
        string fileName = "";
        switch (characterSlot)
        {
            case CharacterSlot.CharacterSlot_01:
                fileName = "characterSlot_01";
                break;
            case CharacterSlot.CharacterSlot_02:
                fileName = "characterSlot_02";
                break;
            case CharacterSlot.CharacterSlot_03:
                fileName = "characterSlot_03";
                break;
            case CharacterSlot.CharacterSlot_04:
                fileName = "characterSlot_04";
                break;
            case CharacterSlot.CharacterSlot_05:
                fileName = "characterSlot_05";
                break;
            case CharacterSlot.CharacterSlot_06:
                fileName = "characterSlot_06";
                break;
            case CharacterSlot.CharacterSlot_07:
                fileName = "characterSlot_07";
                break;
            case CharacterSlot.CharacterSlot_08:
                fileName = "characterSlot_08";
                break;
            case CharacterSlot.CharacterSlot_09:
                fileName = "characterSlot_09";
                break;
            case CharacterSlot.CharacterSlot_10:
                fileName = "characterSlot_10";
                break;
            default:
                break;
        }

        return fileName;
    }

    public void AttempToCreateNewGame()
    {
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        // CHECK TO SEE IF WE CAN CREATE A NEW SAVE FILE (CHECK FOR OTHER EXISTING FILES FIRST)
        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_01);
        
        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_01;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_02);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_02;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_03);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_03;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_04);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_04;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_05);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_05;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_06);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_06;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_07);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_07;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_08);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_08;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_09);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_09;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_10);

        if (!saveFileDataWriter.CheckToSeeIfFileExist())
        {
            // IF THIS PROFILE SLOT IS NOT TAKEN, MAKE A NEW ONE USING THIS SLOT
            currentCharacterSlotBeingUsed = CharacterSlot.CharacterSlot_10;
            currentCharacterData = new CharacterSaveData();
            NewGame();
            return;
        }

        TitleScreenManager.Instance.DisplayNoFreeCharacterSlotsPopUp();
    }

    private void NewGame()
    {
        player.playerNetworkManager.vitality.Value = 15;
        player.playerNetworkManager.endurance.Value = 10;
        
        SaveGame();
        StartCoroutine(LoadWorldScene());
    }

    public void LoadGame()
    {

        // LOAD A PREVIOUS FILE, WITH A FILE NAME DEPENDING ON WHICH SLOT YOU ARE USING 
        saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(currentCharacterSlotBeingUsed);

        saveFileDataWriter = new SaveFileDataWriter();
        // GENERALLY WORKS ON MULTIPLE MACHINE TYPES (Aplication.persistenceDataPath)
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        saveFileDataWriter.saveFileName = saveFileName;
        currentCharacterData = saveFileDataWriter.LoadSaveFile();

        StartCoroutine(LoadWorldScene());
    }

    public void SaveGame()
    {
        // SAVE THE CURRENT FILE NAME DEPENDING ON WHICH SLOT WE ARE USING
        saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(currentCharacterSlotBeingUsed);

        saveFileDataWriter = new SaveFileDataWriter();
        // GENERALLY WORKS ON MULTIPLE MACHINE TYPES (Aplication.persistenceDataPath)
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        saveFileDataWriter.saveFileName = saveFileName;

        // PASS THE PLAYERS INFO, FROM GAME, TO THIER SAVE FILE
        player.SaveGameDataToCurrentCharacterData(ref currentCharacterData);
        
        // WRITE THAT INFO INTO A JSON FILE, SAVED TO THIS MACHINE
        saveFileDataWriter.CreateNewCharacterSaveFile(currentCharacterData);

    }

    public void DeleteGame(CharacterSlot characterSlot)
    {
        // CHOOSE FILE TO DELETE
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        //saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(characterSlot);
        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(characterSlot);
        //saveFileDataWriter.saveFileName = saveFileName;
        saveFileDataWriter.DeleteSaveFile();
    }

    // LOAD ALL CHARACTER PROFILES ON DEVICE WHEN STARTING GAME
    private void LoadAllCharacterProfiles()
    {
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_01);
        characterSlot01 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_02);
        characterSlot02 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_03);
        characterSlot03 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_04);
        characterSlot04 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_05);
        characterSlot05 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_06);
        characterSlot06 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_07);
        characterSlot07 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_08);
        characterSlot08 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_09);
        characterSlot09 = saveFileDataWriter.LoadSaveFile();

        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot.CharacterSlot_10);
        characterSlot10 = saveFileDataWriter.LoadSaveFile();
    }

    public IEnumerator LoadWorldScene()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(worldSceneIndex);

        player.LoadGameDataFromCurrentCharacterData(ref currentCharacterData);

        yield return null;
    }
    
    public int GetWorldSceneIndex()
    {
        return worldSceneIndex;
    }
}
