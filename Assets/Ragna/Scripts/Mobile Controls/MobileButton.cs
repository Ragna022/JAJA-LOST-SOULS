using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Button Settings")]
    [SerializeField] private string buttonName; // e.g., "Jump", "Dodge", "Attack"
    
    public event Action OnButtonPressed;
    public event Action OnButtonReleased;
    
    private bool isPressed = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        OnButtonPressed?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        OnButtonReleased?.Invoke();
    }

    public bool IsPressed()
    {
        return isPressed;
    }

    public string GetButtonName()
    {
        return buttonName;
    }
}