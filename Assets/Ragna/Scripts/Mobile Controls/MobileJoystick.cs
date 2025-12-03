using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private bool dynamicJoystick = false; // If true, joystick appears where you touch
    
    private Vector2 inputVector;
    private Vector2 joystickStartPosition;
    private Canvas canvas;
    private Camera cam;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MobileJoystick must be a child of a Canvas!");
            return;
        }

        // Get camera for screen space calculations
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cam = canvas.worldCamera;
        }

        // Store the initial position for dynamic joystick
        joystickStartPosition = joystickBackground.anchoredPosition;

        // Validate references
        if (joystickBackground == null)
        {
            Debug.LogError($"MobileJoystick '{gameObject.name}': Joystick Background is not assigned!");
        }
        if (joystickHandle == null)
        {
            Debug.LogError($"MobileJoystick '{gameObject.name}': Joystick Handle is not assigned!");
        }

        Debug.Log($"MobileJoystick '{gameObject.name}' initialized successfully");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (dynamicJoystick)
        {
            // Move joystick to touch position
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground.parent as RectTransform,
                eventData.position,
                cam,
                out localPoint
            );
            joystickBackground.anchoredPosition = localPoint;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        
        // Convert screen point to local point in the joystick's rect transform
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            cam,
            out position))
        {
            // Normalize position based on joystick background size
            position.x = (position.x / joystickBackground.sizeDelta.x);
            position.y = (position.y / joystickBackground.sizeDelta.y);

            // Clamp to circular area
            inputVector = new Vector2(position.x * 2, position.y * 2);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Move handle
            joystickHandle.anchoredPosition = new Vector2(
                inputVector.x * handleRange,
                inputVector.y * handleRange
            );

            // Debug log to verify joystick is working
            if (inputVector.magnitude > 0.1f)
            {
                Debug.Log($"Joystick '{gameObject.name}' Input: {inputVector}");
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset joystick
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;

        // Reset position if dynamic
        if (dynamicJoystick)
        {
            joystickBackground.anchoredPosition = joystickStartPosition;
        }
    }

    public Vector2 GetInputVector()
    {
        return inputVector;
    }

    public float GetHorizontal()
    {
        return inputVector.x;
    }

    public float GetVertical()
    {
        return inputVector.y;
    }
}