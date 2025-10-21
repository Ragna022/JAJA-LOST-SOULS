using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class WorldSaveGameManager : MonoBehaviour
{
    public static WorldSaveGameManager instance;

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

    private bool isCreatingNewGame = false;

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
        
        // Check all slots
        for (int i = 1; i <= 10; i++)
        {
            CharacterSlot slot = (CharacterSlot)i;
            saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(slot);
            
            if (!saveFileDataWriter.CheckToSeeIfFileExist())
            {
                return true;
            }
        }

        return false;
    }

    public string DecideCharacterNameBasedOnCharactersSlotBeingUsed(CharacterSlot characterSlot)
    {
        string fileName = "";
        switch (characterSlot)
        {
            case CharacterSlot.CharacterSlot_01: fileName = "characterSlot_01"; break;
            case CharacterSlot.CharacterSlot_02: fileName = "characterSlot_02"; break;
            case CharacterSlot.CharacterSlot_03: fileName = "characterSlot_03"; break;
            case CharacterSlot.CharacterSlot_04: fileName = "characterSlot_04"; break;
            case CharacterSlot.CharacterSlot_05: fileName = "characterSlot_05"; break;
            case CharacterSlot.CharacterSlot_06: fileName = "characterSlot_06"; break;
            case CharacterSlot.CharacterSlot_07: fileName = "characterSlot_07"; break;
            case CharacterSlot.CharacterSlot_08: fileName = "characterSlot_08"; break;
            case CharacterSlot.CharacterSlot_09: fileName = "characterSlot_09"; break;
            case CharacterSlot.CharacterSlot_10: fileName = "characterSlot_10"; break;
            default: break;
        }
        return fileName;
    }

    // ATTEMPT TO CREATE NEW GAME
    public void AttempToCreateNewGame()
    {
        Debug.Log("Attempting to create new game...");

        // Find first available slot
        currentCharacterSlotBeingUsed = FindFirstAvailableSlot();
        if (currentCharacterSlotBeingUsed == CharacterSlot.NO_SLOT)
        {
            TitleScreenManager.Instance.DisplayNoFreeCharacterSlotsPopUp();
            return;
        }

        currentCharacterData = new CharacterSaveData();
        StoreCharacterSelectionInSaveData();
        isCreatingNewGame = true;
        
        // Save initial game data
        SaveInitialGameData();
        
        // Load the world scene - we'll start the host AFTER the scene loads
        StartCoroutine(LoadWorldSceneAndStartHost());
    }

    // FIND FIRST AVAILABLE SLOT
    private CharacterSlot FindFirstAvailableSlot()
    {
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        
        for (int i = 1; i <= 10; i++)
        {
            CharacterSlot slot = (CharacterSlot)i;
            saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(slot);
            
            if (!saveFileDataWriter.CheckToSeeIfFileExist())
            {
                Debug.Log($"Found available slot: {slot}");
                return slot;
            }
        }
        return CharacterSlot.NO_SLOT;
    }

    // STORE CHARACTER SELECTION IN SAVE DATA
    private void StoreCharacterSelectionInSaveData()
    {
        if (currentCharacterData != null && TitleScreenManager.selectedPlayerPrefab != null)
        {
            currentCharacterData.characterPrefabName = TitleScreenManager.selectedPlayerPrefab.name;
            
            // Find the index of the selected prefab
            if (TitleScreenManager.Instance != null)
            {
                for (int i = 0; i < TitleScreenManager.Instance.availableCharacterPrefabs.Length; i++)
                {
                    if (TitleScreenManager.Instance.availableCharacterPrefabs[i] == TitleScreenManager.selectedPlayerPrefab)
                    {
                        currentCharacterData.characterPrefabIndex = i;
                        break;
                    }
                }
            }
            Debug.Log($"Stored character selection in save data: {TitleScreenManager.selectedPlayerPrefab.name}");
        }
        else
        {
            Debug.LogWarning("No character prefab selected for save data!");
        }
    }

    // SAVE INITIAL GAME DATA WITHOUT REQUIRING PLAYER REFERENCE
    private void SaveInitialGameData()
    {
        // Set initial stats in the save data directly
        currentCharacterData.vitality = 15;
        currentCharacterData.endurance = 10;
        currentCharacterData.currentHealth = 100;
        currentCharacterData.currentStamina = 100f;
        
        // Set default position
        currentCharacterData.xPosition = 0f;
        currentCharacterData.yPosition = 0f;
        currentCharacterData.zPosition = 0f;
        
        // Set character name if not set
        if (string.IsNullOrEmpty(currentCharacterData.characterName))
        {
            currentCharacterData.characterName = "Character";
        }

        // SAVE THE CURRENT FILE NAME DEPENDING ON WHICH SLOT WE ARE USING
        saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(currentCharacterSlotBeingUsed);

        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        saveFileDataWriter.saveFileName = saveFileName;

        // WRITE THE INITIAL SAVE FILE
        saveFileDataWriter.CreateNewCharacterSaveFile(currentCharacterData);
        Debug.Log($"Created initial save file for character: {currentCharacterData.characterPrefabName}");
    }

    // LOAD WORLD SCENE AND START HOST
    public IEnumerator LoadWorldSceneAndStartHost()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(worldSceneIndex);

        // Wait for the scene to load
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        Debug.Log("World scene loaded successfully, configuring network...");
        
        // Wait one more frame to ensure everything is initialized
        yield return null;
        
        // NOW configure the network for the selected character
        ConfigureNetworkForSelectedCharacter();
        
        // Start the host AFTER the scene is loaded and network is configured
        StartCoroutine(StartHostAfterDelay());
    }

    // START HOST AFTER DELAY
    private IEnumerator StartHostAfterDelay()
    {
        // Wait one more frame to ensure everything is initialized
        yield return null;
        
        Debug.Log("Starting network as host...");
        
        // Make sure NetworkManager exists and is not already running
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log($"Host started with character: {TitleScreenManager.selectedPlayerPrefab?.name}");
        }
        else if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("NetworkManager is already running!");
        }
        else
        {
            Debug.LogError("NetworkManager not found or not initialized!");
        }
        
        // Reset the flag
        isCreatingNewGame = false;
    }

    // CONFIGURE NETWORK FOR SELECTED CHARACTER
    private void ConfigureNetworkForSelectedCharacter()
    {
        // Load the selected character from prefs if needed
        if (TitleScreenManager.selectedPlayerPrefab == null)
        {
            string savedCharacterName = PlayerPrefs.GetString("SelectedCharacter", "");
            if (!string.IsNullOrEmpty(savedCharacterName) && TitleScreenManager.Instance != null)
            {
                foreach (var prefab in TitleScreenManager.Instance.availableCharacterPrefabs)
                {
                    if (prefab.name == savedCharacterName)
                    {
                        TitleScreenManager.selectedPlayerPrefab = prefab;
                        Debug.Log($"Loaded character from prefs: {savedCharacterName}");
                        break;
                    }
                }
            }
        }

        if (TitleScreenManager.selectedPlayerPrefab != null && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = TitleScreenManager.selectedPlayerPrefab;
            Debug.Log($"Network configured to use character prefab: {TitleScreenManager.selectedPlayerPrefab.name}");
        }
        else
        {
            Debug.LogWarning("No character prefab selected or NetworkManager not found, using default");
        }
    }

    public void LoadGame()
    {
        // LOAD A PREVIOUS FILE, WITH A FILE NAME DEPENDING ON WHICH SLOT YOU ARE USING 
        saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(currentCharacterSlotBeingUsed);

        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        saveFileDataWriter.saveFileName = saveFileName;
        currentCharacterData = saveFileDataWriter.LoadSaveFile();

        // CONFIGURE NETWORK FOR LOADED CHARACTER
        ConfigureNetworkForLoadedCharacter();

        isCreatingNewGame = false;
        StartCoroutine(LoadWorldSceneAndStartHost());
    }

    // CONFIGURE NETWORK FOR LOADED CHARACTER
    private void ConfigureNetworkForLoadedCharacter()
    {
        if (currentCharacterData != null && !string.IsNullOrEmpty(currentCharacterData.characterPrefabName))
        {
            // Find the character prefab by name from save data
            if (TitleScreenManager.Instance != null)
            {
                GameObject[] availablePrefabs = TitleScreenManager.Instance.availableCharacterPrefabs;
                for (int i = 0; i < availablePrefabs.Length; i++)
                {
                    if (availablePrefabs[i].name == currentCharacterData.characterPrefabName)
                    {
                        TitleScreenManager.selectedPlayerPrefab = availablePrefabs[i];
                        Debug.Log($"Loaded character selection from save: {TitleScreenManager.selectedPlayerPrefab.name}");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No character data found in save, using default character");
        }
    }

    public void SaveGame()
    {
        // Don't try to save if we're in the middle of creating a new game
        if (isCreatingNewGame)
        {
            Debug.Log("Skipping save during new game creation");
            return;
        }

        // SAVE THE CURRENT FILE NAME DEPENDING ON WHICH SLOT WE ARE USING
        saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(currentCharacterSlotBeingUsed);

        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        saveFileDataWriter.saveFileName = saveFileName;

        // UPDATE CHARACTER SELECTION IN SAVE DATA BEFORE SAVING
        if (TitleScreenManager.selectedPlayerPrefab != null)
        {
            currentCharacterData.characterPrefabName = TitleScreenManager.selectedPlayerPrefab.name;
            // Find the index
            if (TitleScreenManager.Instance != null)
            {
                for (int i = 0; i < TitleScreenManager.Instance.availableCharacterPrefabs.Length; i++)
                {
                    if (TitleScreenManager.Instance.availableCharacterPrefabs[i] == TitleScreenManager.selectedPlayerPrefab)
                    {
                        currentCharacterData.characterPrefabIndex = i;
                        break;
                    }
                }
            }
        }

        // Only try to save player data if we have a valid player reference
        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.SaveGameDataToCurrentCharacterData(ref currentCharacterData);
            Debug.Log("Game saved successfully");
        }
        else
        {
            Debug.LogWarning("No player found to save game data");
        }

        // WRITE THAT INFO INTO A JSON FILE, SAVED TO THIS MACHINE
        saveFileDataWriter.CreateNewCharacterSaveFile(currentCharacterData);
    }

    public void DeleteGame(CharacterSlot characterSlot)
    {
        // CHOOSE FILE TO DELETE
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;
        saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(characterSlot);
        saveFileDataWriter.DeleteSaveFile();
    }

    // LOAD ALL CHARACTER PROFILES ON DEVICE WHEN STARTING GAME
    private void LoadAllCharacterProfiles()
    {
        saveFileDataWriter = new SaveFileDataWriter();
        saveFileDataWriter.saveDataDirectoryPath = Application.persistentDataPath;

        // Load all character slots
        for (int i = 1; i <= 10; i++)
        {
            CharacterSlot slot = (CharacterSlot)i;
            saveFileDataWriter.saveFileName = DecideCharacterNameBasedOnCharactersSlotBeingUsed(slot);
            
            CharacterSaveData saveData = saveFileDataWriter.LoadSaveFile();
            
            switch (slot)
            {
                case CharacterSlot.CharacterSlot_01: characterSlot01 = saveData; break;
                case CharacterSlot.CharacterSlot_02: characterSlot02 = saveData; break;
                case CharacterSlot.CharacterSlot_03: characterSlot03 = saveData; break;
                case CharacterSlot.CharacterSlot_04: characterSlot04 = saveData; break;
                case CharacterSlot.CharacterSlot_05: characterSlot05 = saveData; break;
                case CharacterSlot.CharacterSlot_06: characterSlot06 = saveData; break;
                case CharacterSlot.CharacterSlot_07: characterSlot07 = saveData; break;
                case CharacterSlot.CharacterSlot_08: characterSlot08 = saveData; break;
                case CharacterSlot.CharacterSlot_09: characterSlot09 = saveData; break;
                case CharacterSlot.CharacterSlot_10: characterSlot10 = saveData; break;
            }
        }
    }
    
    public int GetWorldSceneIndex()
    {
        return worldSceneIndex;
    }
}