using UnityEngine;

[System.Serializable]
// SINCE WE WANT TO REFERENCE THIS DATA FOR EVERY SABE FILE, THIS SCRIPT IS NOT A MONOBEHAVIOUR AND IS INSTEAD SERIALIZABLE
public class CharacterSaveData
{
    [Header("Character Name")]
    public string characterName;

    [Header("Time Played")]
    public float secondsPlayed;

    // QUESTION: WHY NOT USE A VECTOR3?
    // ANS: WE CAN ONLY SAVE DATA FROM "BASIC" VARIABLES TYPES (floats, int, string, bool, etc)
    [Header("World Coordinates")]
    public float xPosition;
    public float yPosition;
    public float zPosition;
}
