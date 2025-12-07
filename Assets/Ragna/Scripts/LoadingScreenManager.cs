using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider loadingSlider; 
    [SerializeField] private TMP_Text loadingText; 
    [SerializeField] private TMP_Text percentageText; 
    
    [Header("Loading Bar Settings")]
    [SerializeField] private float fakeProgressSpeed = 0.3f;
    [SerializeField] private bool showPercentage = true;

    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private Coroutine loadingCoroutine;
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private void Update()
    {
        if (isShowing)
        {
            // Smoothly move currentProgress towards target
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 5f);
            
            if (loadingSlider != null)
            {
                loadingSlider.value = currentProgress;
            }
            
            UpdatePercentageDisplay();
        }
    }

    public void Show(string message = "Loading...")
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);

        isShowing = true;
        currentProgress = 0f;
        targetProgress = 0f;

        UpdateLoadingText(message);
        UpdatePercentageDisplay();

        if (loadingSlider != null) loadingSlider.value = 0f;

        Debug.Log($"🔄 Loading Screen: SHOWN - {message}");
    }

    public void Hide()
    {
        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        if (loadingPanel != null) loadingPanel.SetActive(false);

        isShowing = false;
        currentProgress = 0f;
        targetProgress = 0f;

        Debug.Log("✅ Loading Screen: HIDDEN");
    }

    public void UpdateLoadingText(string message)
    {
        if (loadingText != null) loadingText.text = message;
    }

    // Kept for backward compatibility if other scripts call it, but it just calls UpdateLoadingText
    public void UpdateStatusText(string status)
    {
        UpdateLoadingText(status);
    }

    private void UpdatePercentageDisplay()
    {
        if (percentageText != null && showPercentage)
        {
            int percentage = Mathf.RoundToInt(currentProgress * 100f);
            percentage = Mathf.Clamp(percentage, 0, 100);
            percentageText.text = $"{percentage}%";
        }
    }

    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    public void ShowWithFakeProgress(string message = "Loading...")
    {
        Show(message);
        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(FakeProgressCoroutine());
    }

    public void Complete()
    {
        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(CompleteLoadingCoroutine());
    }

    private IEnumerator FakeProgressCoroutine()
    {
        // Slowly creep up to 90% while waiting for actual network events
        while (targetProgress < 0.9f)
        {
            float speed = fakeProgressSpeed;
            
            // Slow down the closer we get to 90%
            if (targetProgress > 0.6f) speed *= 0.5f;
            if (targetProgress > 0.8f) speed *= 0.25f;

            targetProgress += speed * Time.deltaTime;
            yield return null;
        }
        targetProgress = 0.9f;
    }

    private IEnumerator CompleteLoadingCoroutine()
    {
        targetProgress = 1f;
        
        // Wait until the visual slider is basically full (0.99)
        while (currentProgress < 0.99f)
        {
            yield return null;
        }
        
        if (percentageText != null) percentageText.text = "100%";

        yield return new WaitForSeconds(0.5f);
        Hide();
    }
}