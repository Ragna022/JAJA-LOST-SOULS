using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUIPopUpManager : MonoBehaviour
{
    // Removing self-reference "playerUIPopUpManager" as it's redundant (this script is the manager)

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
        // Fade in over 5 seconds
        StartCoroutine(FadeInPopUpOverTime(defeatPanelCanvasGroup, 5));
        // Wait 8 seconds (5 for fade + 3 to read) then go to menu
        StartCoroutine(WaitThenLoadMenu(8f)); 
    }

    public void SendVictoryPanel()
    {
        victoryPanelGameObject.SetActive(true);
        // Fade in over 5 seconds
        StartCoroutine(FadeInPopUpOverTime(victoryPanelCanvasGroup, 5));
        // Wait 8 seconds (5 for fade + 3 to read) then go to menu
        StartCoroutine(WaitThenLoadMenu(8f));
    }

    private IEnumerator WaitThenLoadMenu(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Call the manager to load the menu cleanly
        if (PlayerUIManager.instance != null)
        {
            PlayerUIManager.instance.LoadMainMenu();
        }
        else
        {
            // Fallback if UI Manager is missing
            LeaveLobby(); 
        }
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