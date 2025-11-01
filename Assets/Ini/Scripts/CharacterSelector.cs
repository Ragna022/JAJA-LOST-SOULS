using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class CharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public class CharacterOption
    {
        public string name;
        [TextArea(2, 5)]
        public string description;         // Each character's unique description
        public GameObject button;          // UI button
        public GameObject hoverEffect;     // Optional highlight
        public GameObject displayCharacter; // Character standing in the selection scene
        public GameObject prefabToSpawn;   // The prefab used in actual gameplay
    }

    #region VARIABLES
    [Header("Character Setup")]
    public List<CharacterOption> characters = new List<CharacterOption>();

    [Header("UI")]
    public Button selectButton;
    public Button backButton;
    public TextMeshProUGUI charNameText;
    public TextMeshProUGUI charDescriptionText;

    [Header("Spawn/Scene Settings")]
    public Transform spawnPoint;

    [Header("Camera Settings")]
    public Transform viewA;
    public Transform viewB;
    public float cameraMoveDuration = 1.2f;
    public Ease cameraEase = Ease.InOutSine;

    private GameObject currentCharacter;
    private CharacterOption selectedCharacter;
    private bool inSelectionView = false;
    private Camera mainCam;

    public IdleCameraSway camSwayScript;
    #endregion

    #region UNITY METHODS
    void Start()
    {
        mainCam = Camera.main;

        // Initialize all characters off
        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);

            if (c.hoverEffect != null)
                c.hoverEffect.SetActive(false);

            Button btn = c.button.GetComponent<Button>();
            btn.onClick.AddListener(() => OnCharacterClicked(c));
        }

        if (backButton != null)
            backButton.onClick.AddListener(ReturnToMainView);

        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectButtonPressed);

        // Try to load previously selected character
        LoadSelectedCharacter();
    }
    #endregion

    #region CHARACTER LOGIC
    private void OnCharacterClicked(CharacterOption clicked)
    {
        // Disable all others
        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);

            if (c.hoverEffect != null)
                c.hoverEffect.SetActive(false);
        }

        // Activate clicked
        if (clicked.displayCharacter != null)
            clicked.displayCharacter.SetActive(true);

        if (clicked.hoverEffect != null)
            clicked.hoverEffect.SetActive(true);

        selectedCharacter = clicked;
        currentCharacter = clicked.displayCharacter;

        // Update UI Texts
        charNameText.text = clicked.name;
        charDescriptionText.text = clicked.description;

        Debug.Log($"Selected: {selectedCharacter.name}");
    }

    private void OnSelectButtonPressed()
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

    #region CAMERA LOGIC
    public void MoveToSelectionView()
    {
        if (inSelectionView) return;
        inSelectionView = true;

        // Re-enable current or default character
        InitializeDefaultCharacter();

        mainCam.transform.DOMove(viewB.position, cameraMoveDuration).SetEase(cameraEase);
        mainCam.transform.DORotateQuaternion(viewB.rotation, cameraMoveDuration).SetEase(cameraEase);

        camSwayScript.enabled = false;
        Debug.Log("Entering Character Selection View...");
    }

    public void ReturnToMainView()
    {
        if (!inSelectionView) return;
        inSelectionView = false;

        // Disable currently active character display
        if (currentCharacter != null)
            currentCharacter.SetActive(false);

        mainCam.transform.DOMove(viewA.position, 0.1f).SetEase(cameraEase);
        mainCam.transform.DORotateQuaternion(viewA.rotation, 0.1f).SetEase(cameraEase);

        camSwayScript.enabled = true;
        Debug.Log("Returning to Main View...");
    }
    #endregion

    #region INITIALIZATION
    public void InitializeDefaultCharacter()
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("No characters available to initialize!");
            return;
        }

        // If a previously selected character exists, show that instead of first
        CharacterOption charToShow = selectedCharacter != null ? selectedCharacter : characters[0];

        // Disable all first
        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);
            if (c.hoverEffect != null)
                c.hoverEffect.SetActive(false);
        }

        // Activate the one to show
        if (charToShow.displayCharacter != null)
            charToShow.displayCharacter.SetActive(true);
        if (charToShow.hoverEffect != null)
            charToShow.hoverEffect.SetActive(true);

        // Update text
        charNameText.text = charToShow.name;
        charDescriptionText.text = charToShow.description;

        // Cache selection
        selectedCharacter = charToShow;
        currentCharacter = charToShow.displayCharacter;

        Debug.Log($"Default character initialized: {charToShow.name}");
    }

    private void LoadSelectedCharacter()
    {
        string savedName = PlayerPrefs.GetString("SelectedCharacter", string.Empty);
        if (string.IsNullOrEmpty(savedName))
        {
            selectedCharacter = null; // fallback to default later
            return;
        }

        // Find the saved character
        CharacterOption found = characters.Find(c => c.name == savedName);
        if (found != null)
        {
            selectedCharacter = found;
            Debug.Log($"Loaded saved character: {savedName}");
        }
        else
        {
            Debug.LogWarning($"Saved character '{savedName}' not found in character list!");
        }
    }
    #endregion
}
