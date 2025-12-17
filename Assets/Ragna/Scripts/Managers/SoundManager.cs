using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicAS;
    [SerializeField] private AudioSource sfxAS;
    [SerializeField] private AudioClip buttonClick;

    [Header("UI Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // PlayerPrefs Keys
    private const string MusicKey = "MusicVol";
    private const string SFXKey = "SFXVol";

    private void Awake()
    {
        // Singleton logic
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Load saved values (with safe defaults)
        float savedMusicVol = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.75f));
        float savedSFXVol   = Mathf.Clamp01(PlayerPrefs.GetFloat(SFXKey, 0.75f));

        // Prevent listener stacking
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.value = savedMusicVol;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.value = savedSFXVol;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // Apply volumes immediately
        SetMusicVolume(savedMusicVol);
        SetSFXVolume(savedSFXVol);
    }

    #region AUDIO CONTROL

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (musicAS != null)
            musicAS.volume = value;

        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (sfxAS != null)
            sfxAS.volume = value;

        PlayerPrefs.SetFloat(SFXKey, value);
        PlayerPrefs.Save();
    }

    public void PlayClickSound()
    {
        if (sfxAS != null && buttonClick != null)
            sfxAS.PlayOneShot(buttonClick, sfxAS.volume);
    }

    #endregion
}
