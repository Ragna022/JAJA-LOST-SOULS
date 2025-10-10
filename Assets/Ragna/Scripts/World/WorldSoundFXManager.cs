using UnityEngine;

public class WorldSoundFXManager : MonoBehaviour
{
    public static WorldSoundFXManager instance;

    [Header("Action FX")]
    public AudioClip rollSFX;

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
        DontDestroyOnLoad(gameObject);
    }
}
