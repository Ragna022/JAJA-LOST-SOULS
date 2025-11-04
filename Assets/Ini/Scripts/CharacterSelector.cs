using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public class CharacterOption
    {
        public string name;
        [TextArea(2, 5)]
        public string description;
        public GameObject button;
        public GameObject hoverEffect;
        public GameObject displayCharacter;
        public GameObject prefabToSpawn; // Prefab used in actual gameplay
    }

    #region VARIABLES
    [Header("Character Setup")]
    public List<CharacterOption> characters = new List<CharacterOption>();

    [Header("UI")]
    public Button selectButton;
    public Button startGameButton; // <-- NEW
    public TextMeshProUGUI charNameText;
    public TextMeshProUGUI charDescriptionText;

    [Header("Scene References")]
    public Transform spawnPoint;

    [Header("External References")]
    public CameraAnimator cameraController;

    private GameObject currentCharacter;
    private CharacterOption selectedCharacter;
    private bool inSelectionView = false;
    #endregion

    #region UNITY METHODS
    void Start()
    {
        // Initialize buttons and hide all characters/hover effects
        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);

            if (c.hoverEffect != null)
                c.hoverEffect.SetActive(false);

            if (c.button != null)
            {
                Button btn = c.button.GetComponent<Button>();
                btn.onClick.AddListener(() => OnCharacterClicked(c));
            }
        }

        // Hook up buttons
        if (selectButton != null)
            selectButton.onClick.AddListener(SaveSelectedCharacter);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        // Load saved selection if it exists
        LoadSelectedCharacter();
    }
    #endregion

    #region CHARACTER LOGIC
    private void OnCharacterClicked(CharacterOption clicked)
    {
        // Disable all other displays and hover effects
        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);
            if (c.hoverEffect != null)
                c.hoverEffect.SetActive(false);
        }

        // Enable the selected one
        if (clicked.displayCharacter != null)
            clicked.displayCharacter.SetActive(true);
        if (clicked.hoverEffect != null)
            clicked.hoverEffect.SetActive(true);

        selectedCharacter = clicked;
        currentCharacter = clicked.displayCharacter;

        // Update UI
        charNameText.text = clicked.name;
        charDescriptionText.text = clicked.description;

        Debug.Log($"Selected Character: {selectedCharacter.name}");
    }

    private void SaveSelectedCharacter()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("No character selected!");
            return;
        }

        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter.name);
        PlayerPrefs.Save();

        Debug.Log($"Saved {selectedCharacter.name} as selected character.");
    }
    #endregion

    #region INITIALIZATION
    public void InitializeDefaultCharacter()
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("No characters found to initialize!");
            return;
        }

        CharacterOption charToShow = selectedCharacter != null ? selectedCharacter : characters[0];

        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);
            if (c.hoverEffect != null)
                c.hoverEffect.SetActive(false);
        }

        if (charToShow.displayCharacter != null)
            charToShow.displayCharacter.SetActive(true);
        if (charToShow.hoverEffect != null)
            charToShow.hoverEffect.SetActive(true);

        charNameText.text = charToShow.name;
        charDescriptionText.text = charToShow.description;

        selectedCharacter = charToShow;
        currentCharacter = charToShow.displayCharacter;

        Debug.Log($"Default Character Initialized: {charToShow.name}");
    }

    private void LoadSelectedCharacter()
    {
        string savedName = PlayerPrefs.GetString("SelectedCharacter", string.Empty);
        if (string.IsNullOrEmpty(savedName))
        {
            selectedCharacter = null;
            return;
        }

        CharacterOption found = characters.Find(c => c.name == savedName);
        if (found != null)
        {
            selectedCharacter = found;
            Debug.Log($"Loaded saved character: {savedName}");
        }
        else
        {
            Debug.LogWarning($"Saved character '{savedName}' not found in list!");
        }
    }
    #endregion

    #region SCENE LOGIC
    public void StartGame()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("No character selected! Cannot start game.");
            return;
        }

        // Save before loading next scene
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter.name);
        PlayerPrefs.Save();

        Debug.Log($"Starting game with {selectedCharacter.name}...");
        SceneManager.LoadScene("GameScene"); // Change this to your gameplay scene name
    }
    #endregion
}