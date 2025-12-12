using Unity.Netcode;
using UnityEngine;

public class AICharacterNetworkManager : CharacterNetworkManager
{
    [Header("AI Starting Stats")]
    // Assign these in the Inspector. They act as the "Blueprints" for the NetworkVariables.
    [SerializeField] int startingVitality = 10;
    [SerializeField] int startingEndurance = 10;
    [SerializeField] int startingDexterity = 10;
    [SerializeField] int startingIntelligence = 10;

    [Header("AI Base Resources")]
    [SerializeField] int baseHealth = 150;
    [SerializeField] int baseStamina = 100;

    public override void OnNetworkSpawn()
    {
        // Call the base logic (Subscriptions, etc.)
        base.OnNetworkSpawn();

        // Only the Server can write to NetworkVariables (Permissions: Owner/Server)
        if (IsServer)
        {
            // 1. Initialize Stats from Inspector values
            vitality.Value = startingVitality;
            endurance.Value = startingEndurance;
            dexterity.Value = startingDexterity;
            intelligence.Value = startingIntelligence;

            // 2. Initialize Health
            maxHealth.Value = baseHealth;
            currentHealth.Value = baseHealth; // CRITICAL: This prevents it from staying 0 and dying instantly

            // 3. Initialize Stamina
            maxStamina.Value = baseStamina;
            currentStamina.Value = baseStamina;
        }
    }
}