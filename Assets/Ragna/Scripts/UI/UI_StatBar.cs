using UnityEngine;
using UnityEngine.UI;

public class UI_StatBar : MonoBehaviour
{
    private Slider slider;
    private RectTransform rectTransform;

    // VARIABLE TO SCALE BAR SIZE DEPENDING ON STAT (HIGHER STAT = LONGER BAR ACROSS SCREEN)
    [Header("Bar Options")]
    [SerializeField] protected bool scaleBarLengthWithStats = true;
    [SerializeField] protected float widthScaleMultiplier = 1;
    // SECONDARY BAR BEHIND FOR POLISH EFFECT (YELLOW BAR THAT SHOWS HOW MUCH AM ACTION/ DAMAGE TAKES AWAY FROM CURRENT STAT)

    protected virtual void Awake()
    {
        slider = GetComponent<Slider>();
        rectTransform = GetComponent<RectTransform>();
    }

    public virtual void SetStat(int newValue)
    {
        slider.value = newValue;
    }

    public virtual void SetMaxStat(int maxValue)
    {
        slider.maxValue = maxValue;
        slider.value = maxValue;

        if (scaleBarLengthWithStats)
        {
            rectTransform.sizeDelta = new Vector2(maxValue * widthScaleMultiplier, rectTransform.sizeDelta.y);

            PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
        }
    }
}
