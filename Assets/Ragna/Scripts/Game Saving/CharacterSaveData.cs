using UnityEngine;

[System.Serializable]
// SINCE WE WANT TO REFERENCE THIS DATA FOR EVERY SABE FILE, THIS SCRIPT IS NOT A MONOBEHAVIOUR AND IS INSTEAD SERIALIZABLE
public class CharacterSaveData
{
    [Header("Character Name")]
    public string characterName = "Charater";

    [Header("Time Played")]
    public float secondsPlayed;

    // QUESTION: WHY NOT USE A VECTOR3?
    // ANS: WE CAN ONLY SAVE DATA FROM "BASIC" VARIABLES TYPES (floats, int, string, bool, etc)
    [Header("World Coordinates")]
    public float xPosition;
    public float yPosition;
    public float zPosition;

    [Header("Resources")]
    public int currentHealth;
    public float currentStamina;

    [Header("Stats")]
    public int vitality;
    public int endurance;

    // ADDED: CHARACTER SELECTION DATA
    [Header("Character Selection")]
    public string characterPrefabName = "";
    public int characterPrefabIndex = 0;
}