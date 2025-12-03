using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UI_Character_HP_Bar : UI_StatBar
{
    private CharacterManager character;
    private AICharacterManager aiCharacter;
    private PlayerManager playerCharacter;

    [SerializeField] bool displayCharacterNameAndDamage = false;
    [SerializeField] float defaultTimeBeforeBarHides = 3;
    [SerializeField] float hideTimer = 0;
    [SerializeField] int currentDamageTaken = 0;
    [SerializeField] TextMeshProUGUI characterName;
    [SerializeField] TextMeshProUGUI characterDamage;
    [HideInInspector] public int oldHealthValue = 0;

    protected override void Awake()
    {
        base.Awake();

        character = GetComponentInParent<CharacterManager>();

        if(character != null)
        {
            playerCharacter = character as PlayerManager;
        }

        if(character != null)
        {
            aiCharacter = character as AICharacterManager;
        }
    }

    protected override void Start()
    {
        base.Start();

        if (character != null)
        {
            if (character.characterUIManager == null)
            {
                CharacterUIManager uiManager = character.gameObject.AddComponent<CharacterUIManager>();
                uiManager.hasFloatingHPBar = true;
                uiManager.characterHPBar = this;
                character.characterUIManager = uiManager; // Assign to the public field if it exists

                // Subscribe to health changes
                character.characterNetworkManager.currentHealth.OnValueChanged += uiManager.OnHPChanged;

                // Initialize with current health
                oldHealthValue = character.characterNetworkManager.currentHealth.Value;
                uiManager.OnHPChanged(oldHealthValue, oldHealthValue);
            }
            else
            {
                //Debug.Log($"[UI_Character_HP_Bar] characterUIManager EXISTS, hasFloatingHPBar: {character.characterUIManager.hasFloatingHPBar}");
            }
        }

        gameObject.SetActive(false);
    }

    public override void SetStat(int newValue)
    {
        //Debug.Log($"[UI_Character_HP_Bar] ===== SetStat CALLED ===== for {(character != null ? character.name : "unknown")}");
       // Debug.Log($"[UI_Character_HP_Bar] newValue: {newValue}, oldHealthValue: {oldHealthValue}");

        if (displayCharacterNameAndDamage)
        {
            if (characterName == null)
            {
                //Debug.LogError("[UI_Character_HP_Bar] characterName TextMeshProUGUI is NULL!");
                return;
            }

            characterName.enabled = true;

            if (playerCharacter != null)
            {
                string playerName = playerCharacter.playerNetworkManager.characterName.Value.ToString();
                characterName.text = playerName;
                //Debug.Log($"[UI_Character_HP_Bar] Set PLAYER name to: {playerName}");
            }
            else if (aiCharacter != null)
            {
                string aiName = aiCharacter.name;
                characterName.text = aiName;
                //Debug.Log($"[UI_Character_HP_Bar] Set AI name to: {aiName}");
            }
        }

        if (character == null)
        {
            //Debug.LogError("[UI_Character_HP_Bar] character is NULL!");
            return;
        }

        slider.maxValue = character.characterNetworkManager.maxHealth.Value;
        //Debug.Log($"[UI_Character_HP_Bar] slider.maxValue set to: {slider.maxValue}");

        currentDamageTaken = Mathf.RoundToInt(currentDamageTaken + (oldHealthValue - newValue));
        //Debug.Log($"[UI_Character_HP_Bar] currentDamageTaken: {currentDamageTaken}");

        if (characterDamage != null)
        {
            if (currentDamageTaken < 0)
            {
                currentDamageTaken = Mathf.Abs(currentDamageTaken);
                characterDamage.text = "+ " + currentDamageTaken.ToString();
            }
            else
            {
                characterDamage.text = "- " + currentDamageTaken.ToString();
            }
        }

        slider.value = newValue;

        if (character.characterNetworkManager.currentHealth.Value != character.characterNetworkManager.maxHealth.Value)
        {
            hideTimer = defaultTimeBeforeBarHides;
            gameObject.SetActive(true);
            //Debug.Log($"[UI_Character_HP_Bar] HP bar activated for {character.name}");
        }
    }

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);

        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
        }
        else
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        //Debug.Log($"[UI_Character_HP_Bar] OnDisable called for {(character != null ? character.name : "unknown")}");
        currentDamageTaken = 0;
    }
}