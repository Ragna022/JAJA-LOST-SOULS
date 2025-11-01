using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Character Database (same order as selector)")]
    public CharacterSelector.CharacterOption[] allCharacters;

    void Start()
    {
        string savedName = PlayerPrefs.GetString("SelectedCharacter", string.Empty);
        if (string.IsNullOrEmpty(savedName))
        {
            Debug.LogWarning("No saved character found!");
            return;
        }

        CharacterSelector.CharacterOption chosen = null;

        foreach (var c in allCharacters)
        {
            if (c.name == savedName)
            {
                chosen = c;
                break;
            }
        }

        if (chosen != null && chosen.prefabToSpawn != null)
        {
            Instantiate(chosen.prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            Debug.Log($"Spawned saved character: {savedName}");
        }
        else
        {
            Debug.LogWarning($"Prefab for '{savedName}' not found!");
        }
    }
}