using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUIPopUpManager : MonoBehaviour
{
    public PlayerUIPopUpManager playerUIPopUpManager;

    [Header("Defeat Panel (You Died)")]
    [SerializeField] GameObject defeatPanelGameObject;
    [SerializeField] CanvasGroup defeatPanelCanvasGroup;

    [Header("Victory Panel")]
    [SerializeField] GameObject victoryPanelGameObject;
    [SerializeField] CanvasGroup victoryPanelCanvasGroup;

    public static List<LobbyPlayerData> PublicPersistentLobbyData;

    public void SendDefeatPanel()
    {
        defeatPanelGameObject.SetActive(true);
        StartCoroutine(FadeInPopUpOverTime(defeatPanelCanvasGroup, 5));
    }

    public void SendVictoryPanel()
    {
        victoryPanelGameObject.SetActive(true);
        StartCoroutine(FadeInPopUpOverTime(victoryPanelCanvasGroup, 5));
    }

    private IEnumerator StretchPopUpTextOverTime(TextMeshProUGUI text, float duration, float stretchAmount)
    {
        if (duration > 0f)
        {
            text.characterSpacing = 0;
            float timer = 0;

            yield return null;

            while (timer < duration)
            {
                timer = timer + Time.deltaTime;
                text.characterSpacing = Mathf.Lerp(text.characterSpacing, stretchAmount, duration * (Time.deltaTime / 20));
                yield return null;
            }
        }
    }

    private IEnumerator FadeInPopUpOverTime(CanvasGroup canvas, float duration)
    {
        if (duration > 0)
        {
            canvas.alpha = 0;
            float timer = 0;

            yield return null;

            while (timer < duration)
            {
                timer = timer + Time.deltaTime;
                canvas.alpha = Mathf.Lerp(canvas.alpha, 1, duration * Time.deltaTime);
                yield return null;
            }
        }

        canvas.alpha = 1;
        yield return null;
    }

    public void LeaveLobby()
    {
        Debug.Log("👋 Leaving lobby...");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (TitleScreenManager.Instance != null)
        {
            Destroy(TitleScreenManager.Instance.gameObject);
        }

        PublicPersistentLobbyData = null;

        SceneManager.LoadScene("MainMenu");
    }
}