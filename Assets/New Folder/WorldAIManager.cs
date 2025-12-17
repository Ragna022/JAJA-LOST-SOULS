using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldAIManager : MonoBehaviour
{
    public static WorldAIManager instance;

    [Header("Debug")]
    [SerializeField] bool despawnCharacters = false;
    [SerializeField] bool respawnCharacters = false;

    [Header("Characters")]
    [SerializeField] GameObject[] aiCharacters;
    [SerializeField] List<GameObject> spawnedInCharacters;

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
        if (NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(WaitForSceneToLoadThenSpawnCharacters());
        }
    }
    
    private void Update()
    {
        if (respawnCharacters)
        {
            respawnCharacters = false;
            SpawnAllCharacters();
        }

        if(despawnCharacters)
        {
            despawnCharacters = false;
            DespawnAllCharacters();
        }
    }

    private IEnumerator WaitForSceneToLoadThenSpawnCharacters()
    {
        while (!SceneManager.GetActiveScene().isLoaded)
        {
            yield return null;
        }

        SpawnAllCharacters();
    }

    private void SpawnAllCharacters()
    {
        // 1. Find all GameObjects with the tag "AI_Spawn"
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("AI_Spawn");

        // 2. Create a temporary list to track available spots (so we don't spawn two AI on top of each other)
        List<Transform> availableSpawnPositions = new List<Transform>();
        foreach (GameObject sp in spawnPoints)
        {
            availableSpawnPositions.Add(sp.transform);
        }

        // Safety Check: Do we have points?
        if (availableSpawnPositions.Count == 0)
        {
            Debug.LogError("WorldAIManager: No objects found with tag 'AI_Spawn'. Characters spawned at Vector3.zero.");
        }

        foreach (var character in aiCharacters)
        {
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            // 3. If we have valid points, pick a random one
            if (availableSpawnPositions.Count > 0)
            {
                int randomIndex = Random.Range(0, availableSpawnPositions.Count);
                Transform selectedPoint = availableSpawnPositions[randomIndex];
                
                spawnPos = selectedPoint.position;
                spawnRot = selectedPoint.rotation;

                // Remove this point from the list so another AI doesn't spawn here
                availableSpawnPositions.RemoveAt(randomIndex);
            }

            // 4. Instantiate at the chosen position and rotation
            GameObject instantiatedCharacter = Instantiate(character, spawnPos, spawnRot);
            
            // 5. Spawn across the network
            instantiatedCharacter.GetComponent<NetworkObject>().Spawn();
            spawnedInCharacters.Add(instantiatedCharacter);
        }
    }

    private void DespawnAllCharacters()
    {
        foreach (var character in spawnedInCharacters)
        {
            if (character != null)
            {
                character.GetComponent<NetworkObject>().Despawn();
            }
        }
        // Clear list after despawning to avoid null reference errors later
        spawnedInCharacters.Clear(); 
    }
    
    private void DisableAllCharacters()
    {
        // TODO: DISABLE CHARACTER GAMEOBJECTS, SYNC DISABLED STATUS ON NETWORK
    }
}