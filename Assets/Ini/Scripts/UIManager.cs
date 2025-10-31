using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI Screens")]
    [SerializeField] private RectTransform[] UIScreens;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup splashUICG, loadingUICG, menuUICG, charSelectionUICG, gameModeUICG, hostingUI, gameIDUI, lobbyUI;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicAS;
    [SerializeField] private AudioSource sfxAS;
    [SerializeField] private AudioClip buttonClick;

    public GameObject[] playerImageBg;

    private CameraAnimator camAnim;

    public TextMeshProUGUI readyText;
    public CharacterSelector charView;

    [Header("Audio Settings")]
    [SerializeField] private Slider musicSlider, sfxSlider;

    private bool isMenu;

    private void Start()
    {
        if (camAnim == null)
        {
            camAnim = FindFirstObjectByType<CameraAnimator>(FindObjectsInactive.Include);

            if (camAnim == null)
                Debug.LogError("UIManager: CharacterSelector (camAnim) not found in scene!");
        }

        StartCoroutine(CloseSplashThenLoadMain());

        // Load saved prefs (defaults if not set)
        float savedMusicVol = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float savedSFXVol = PlayerPrefs.GetFloat("SFXVol", 0.75f);

        // Apply to sliders
        musicSlider.value = savedMusicVol;
        sfxSlider.value = savedSFXVol;

        // Hook listeners
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Apply saved settings immediately
        SetMusicVolume(savedMusicVol);
        SetSFXVolume(savedSFXVol);
    }

    private IEnumerator CloseSplashThenLoadMain()
    {
        yield return new WaitForSeconds(Random.Range(4.5f, 8f));

        splashUICG.DOFade(0, fadeDuration);
        SetCanvasGroupInActive(splashUICG);
        yield return new WaitForSeconds(fadeDuration);

        yield return StartCoroutine(ShowLoadingUI(menuUICG));
    }

    public void MenuToSettings()
    {
        isMenu = true;
            SetCanvasGroupInActive(menuUICG);
                ShowSettingsUI();
    }
    
    public void BackToMenuUI()
    {
        StartCoroutine(ShowLoadingUI(menuUICG));
        SetCanvasGroupInActive(charSelectionUICG);
    }
    
    public IEnumerator ShowLoadingUI(CanvasGroup targetUI)
    {
        if (loadingUICG == null || progressBar == null || progressText == null)
        {
            Debug.LogWarning("UIManager: Missing references in the inspector!");
            yield break;
        }

        loadingUICG.gameObject.SetActive(true);
        loadingUICG.alpha = 0;
        progressBar.value = 0f;
        progressText.text = "0%";

        yield return loadingUICG.DOFade(1, fadeDuration).WaitForCompletion();

        float displayProgress = 0f;
        float targetProgress = 0f;

        while (displayProgress < 0.99f)
        {
            targetProgress = Mathf.Min(targetProgress + Time.deltaTime * Random.Range(0.3f, 0.6f), 1f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime * 2f);

            progressBar.value = displayProgress;
            progressText.text = Mathf.RoundToInt(displayProgress * 100) + "%";

            yield return null;
        }

        displayProgress = 1f;
        progressBar.value = 1f;
        progressText.text = "100%";

        float briefWaitSimulation = Random.Range(1f, 3);
        yield return new WaitForSeconds(briefWaitSimulation);

        yield return loadingUICG.DOFade(0, fadeDuration).WaitForCompletion();
        loadingUICG.gameObject.SetActive(false);

        if (targetUI != null)
        {
            targetUI.gameObject.SetActive(true);
            targetUI.alpha = 0;
            targetUI.DOFade(1, fadeDuration);
            targetUI.interactable = true;
            targetUI.blocksRaycasts = true;
        }
    }

    public void StartGame()
    {       
        StartCoroutine(startGame());
    }

    private IEnumerator startGame()
    {
        SetCanvasGroupInActive(menuUICG);
        yield return StartCoroutine(ShowLoadingUI(charSelectionUICG));
        yield return new WaitForSeconds(0.1f); // tiny delay buffer
        charView.InitializeDefaultCharacter();
        camAnim.MoveToSelectionView();

    }

    public void CloseCharacterSelectionUI()
    {
            SetCanvasGroupInActive(charSelectionUICG);
        SetCanvasGroupActive(menuUICG);
    }

    public void loadGameSelectionUI()
    {
        StartCoroutine(loadOutToGameMode());

        // TODO: Ragna the selected character will be registered in this method
    }

    public IEnumerator loadOutToGameMode()
    {
        SetCanvasGroupInActive(charSelectionUICG);
            yield return StartCoroutine(ShowLoadingUI(gameModeUICG));
                UIScreens[1].DOScale (Vector2.one, 0.25f).SetEase(Ease.OutBack);
    }

    public void GameModeToMenu()
    {
        StartCoroutine(returnToGameMode());
    }

    public IEnumerator returnToGameMode()
    {
        yield return UIScreens[1].DOScale (Vector2.zero, 0.20f).SetEase(Ease.OutBack);
            SetCanvasGroupInActive(gameModeUICG);
                StartCoroutine(ShowLoadingUI(charSelectionUICG));
    }

    public void GameModeToHosting()
    {
        StartCoroutine(OpenHosting());
    }

    public IEnumerator OpenHosting()
    {
        yield return UIScreens[1].DOScale (Vector2.zero, 0.20f).SetEase(Ease.OutBack);
            SetCanvasGroupInActive(gameModeUICG);
                SetCanvasGroupActive(hostingUI);
    }

    public void HostingToGameMode()
    {
        StartCoroutine(OpenGameMode());
    }

    public IEnumerator OpenGameMode()
    {
        SetCanvasGroupInActive(hostingUI);
            SetCanvasGroupActive(gameModeUICG);
                yield return UIScreens[1].DOScale (Vector2.one, 0.20f).SetEase(Ease.OutBack);
    }

    public void FadeInGameIDInputField()
    {
        SetCanvasGroupActive(gameIDUI);
    }

    public void FadeOutGameIDInputField()
    {
        SetCanvasGroupInActive(gameIDUI);
    }

    public void FadeInLobby()
    {
        SetCanvasGroupActive(lobbyUI);
        SetCanvasGroupInActive(hostingUI);
    }

    public void FadeOutLobby()
    {
        SetCanvasGroupActive(hostingUI);
        SetCanvasGroupInActive(lobbyUI);
    }

    public void PlayerReady()
    {
        readyText.text = "Ready...";

        // TODO: Add other functionality to show player is ready, like enabling background colour

        // foreach(var bg in playerImageBg)
        // {
        //     bg.gameObject.SetActive(false);
        // }

        // playerImageBg[selectedBgInt].SetActive(true);

    }

    #region SETTINGS UI
    private void ShowSettingsUI()
    {
        UIScreens[0].gameObject.SetActive(true);
            UIScreens[0].DOScale(Vector2.one, 0.25f).SetEase(Ease.OutBack);
    }
    
    private IEnumerator undoSettings()
    {
        yield return UIScreens[0].DOScale(Vector2.zero, 0.25f).SetEase(Ease.InBack);
            UIScreens[0].gameObject.SetActive(false);
    }

    public void CloseSettingsUI()
    {
        if (isMenu)
        {
            isMenu = false;
                SetCanvasGroupActive(menuUICG);
        }

        StartCoroutine(undoSettings());
    }
    #endregion

    private void SetCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.gameObject.SetActive(true);
        tUI.alpha = 0;
        tUI.DOFade(1, fadeDuration);
        tUI.interactable = true;
        tUI.blocksRaycasts = true;
    }

    private void SetCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.gameObject.SetActive(false);
        tUI.alpha = 0;
        tUI.DOFade(0, fadeDuration);
        tUI.interactable = false;
        tUI.blocksRaycasts = false;
    }

    #region AUDIO
    public void SetMusicVolume(float value)
    {
        if (musicAS != null)
            musicAS.volume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        if (sfxAS != null)
            sfxAS.volume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat("SFXVol", value);
    }

    public void PlayClickSound()
    {
        if (sfxAS != null && buttonClick != null)
            sfxAS.PlayOneShot(buttonClick, sfxAS.volume);
    }
    #endregion

    public void Quit() => Application.Quit();  // Quit application ASAP ASAP

    public void LoadGame()
    {
        Debug.Log("Load Game!");
        // This will load the gameScene using scene manager
    }
}