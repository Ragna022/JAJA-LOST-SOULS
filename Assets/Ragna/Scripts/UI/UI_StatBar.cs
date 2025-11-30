using UnityEngine;
using UnityEngine.UI;

public class UI_StatBar : MonoBehaviour
{
    protected Slider slider;
    protected RectTransform rectTransform;
    protected LayoutElement layoutElement; // Reference to the Layout Element

    private int maxStatValue;

    [Header("Bar Options")]
    [SerializeField] protected bool scaleBarLengthWithStats = true;
    [SerializeField] protected float widthScaleMultiplier = 1;

    protected virtual void Awake()
    {
        slider = GetComponent<Slider>();
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>(); // Get the component
        
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    protected virtual void Start()
    {
        
    }

    public virtual void SetStat(int newValue)
    {
        if (maxStatValue > 0)
        {
            slider.value = (float)newValue / maxStatValue;
        }
        else
        {
            slider.value = 0f;
        }
    }

    public virtual void SetMaxStat(int maxValue)
    {
        maxStatValue = maxValue;
        slider.value = 1f;

        if (scaleBarLengthWithStats)
        {
            // NEW LOGIC: Check if we are inside a Layout Group
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = maxValue * widthScaleMultiplier;
            }
            else 
            {
                // Fallback if not using Layout Group
                rectTransform.sizeDelta = new Vector2(maxValue * widthScaleMultiplier, rectTransform.sizeDelta.y);
            }

            if (PlayerUIManager.instance != null && PlayerUIManager.instance.playerUIHudManager != null)
            {
                PlayerUIManager.instance.playerUIHudManager.RefreshHUD();
            }
        }
    }
}