using UnityEngine;
using UnityEngine.UI;

public class UI_StatBar : MonoBehaviour
{
    private Slider slider;
    // VARIABLE TO SCALE BAR SIZE DEPENDING ON STAT (HIGHER STAT = LONGER BAR ACROSS SCREEN)
    // SECONDARY BAR BEHIND FOR POLISH EFFECT (YELLOW BAR THAT SHOWS HOW MUCH AM ACTION/ DAMAGE TAKES AWAY FROM CURRENT STAT)

    protected virtual void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public virtual void SetStat(int newValue)
    {
        slider.value = newValue;
    }

    public virtual void SetMaxStat(int maxValue)
    {
        slider.maxValue = maxValue;
        slider.value = maxValue;
    }
}
