using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerUIPopUpManager : MonoBehaviour
{
    public PlayerUIPopUpManager playerUIPopUpManager;
    
    [Header("Defeat Panel (You Died)")]
    [SerializeField] GameObject defeatPanelGameObject;
    [SerializeField] TextMeshProUGUI defeatBackgroundText;
    [SerializeField] TextMeshProUGUI defeatText;
    [SerializeField] CanvasGroup defeatPanelCanvasGroup;

    [Header("Victory Panel")]
    [SerializeField] GameObject victoryPanelGameObject;
    [SerializeField] TextMeshProUGUI victoryBackgroundText;
    [SerializeField] TextMeshProUGUI victoryText;
    [SerializeField] CanvasGroup victoryPanelCanvasGroup;

    public void SendDefeatPanel()
    {
        defeatPanelGameObject.SetActive(true);
        defeatBackgroundText.characterSpacing = 0;
        StartCoroutine(StretchPopUpTextOverTime(defeatBackgroundText, 8, 19f));
        StartCoroutine(FadeInPopUpOverTime(defeatPanelCanvasGroup, 5));
    }

    public void SendVictoryPanel()
    {
        victoryPanelGameObject.SetActive(true);
        victoryBackgroundText.characterSpacing = 0;
        StartCoroutine(StretchPopUpTextOverTime(victoryBackgroundText, 8, 19f));
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
}