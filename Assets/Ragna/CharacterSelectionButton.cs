using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CharacterSelectionButton : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [SerializeField] private int characterIndex;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private Button selectionButton;

    [Header("Character Info")]
    [SerializeField] private string characterName;
    [SerializeField] private Sprite characterSprite;

    private void Start()
    {
        // Set up character info
        if (characterNameText != null)
            characterNameText.text = characterName;

        if (characterImage != null && characterSprite != null)
            characterImage.sprite = characterSprite;

        // Get button component if not assigned
        if (selectionButton == null)
            selectionButton = GetComponent<Button>();

        if (selectionButton != null)
        {
            selectionButton.onClick.AddListener(OnCharacterSelected);
        }
    }

    public void OnCharacterSelected()
    {
        TitleScreenManager.Instance.SelectCharacterPrefab(characterIndex);
        
        // Visual feedback
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(true);
        }

        // You could add sound feedback here
        Debug.Log($"Selected character: {characterName} (Index: {characterIndex})");
    }

    // ADDED: Preview when hovering with mouse
    public void OnPointerEnter(PointerEventData eventData)
    {
        TitleScreenManager.Instance.PreviewClass(characterIndex);
    }

    // ADDED: Preview when selecting with keyboard/gamepad
    public void OnSelect(BaseEventData eventData)
    {
        TitleScreenManager.Instance.PreviewClass(characterIndex);
        
        // Visual feedback for selection
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(true);
        }
    }

    public void Deselect()
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
    }
}