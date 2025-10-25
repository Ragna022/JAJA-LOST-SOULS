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
        public string description;       // 🌟 Each character's unique description
        public GameObject button;        // UI button
        public GameObject hoverEffect;   // Optional highlight
        public GameObject displayCharacter; // Character standing in the selection scene
        public GameObject prefabToSpawn; // The prefab used in actual gameplay
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

        // 🌟 Update UI Texts
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
        Debug.Log($"Stored {selectedCharacter.name} as selected prefab.");
    }
    #endregion

    #region CAMERA LOGIC
    public void MoveToSelectionView()
    {
        if (inSelectionView) return;
        inSelectionView = true;

        mainCam.transform.DOMove(viewB.position, cameraMoveDuration).SetEase(cameraEase);
        mainCam.transform.DORotateQuaternion(viewB.rotation, cameraMoveDuration).SetEase(cameraEase);

        camSwayScript.enabled = false;
        Debug.Log("Entering Character Selection View...");
    }

    public void ReturnToMainView()
    {
        if (!inSelectionView) return;
        inSelectionView = false;

        foreach (var c in characters)
        {
            if (c.displayCharacter != null)
                c.displayCharacter.SetActive(false);
        }

        mainCam.transform.DOMove(viewA.position, 0.1f).SetEase(cameraEase);
        mainCam.transform.DORotateQuaternion(viewA.rotation, 0.1f).SetEase(cameraEase);

        camSwayScript.enabled = true;
        Debug.Log("Returning to Main View...");
    }
    #endregion
}
