using UnityEngine;
using UnityEngine.EventSystems;

public class MobileCameraTouch : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Camera Sensitivity")]
    [SerializeField] private float cameraSensitivity = 1f;
    [SerializeField] private bool invertY = false;
    
    [Header("Touch Settings")]
    [SerializeField] private bool ignoreUITouches = true; // Don't rotate camera when touching UI elements
    
    private Vector2 touchDelta;
    private Vector2 lastTouchPosition;
    private bool isDragging = false;
    private int currentFingerId = -1;

    // For detecting if we're over UI
    private bool isOverUI = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Check if we're touching a UI element
        if (ignoreUITouches && IsPointerOverUIElement(eventData))
        {
            isOverUI = true;
            return;
        }

        isOverUI = false;
        isDragging = true;
        currentFingerId = eventData.pointerId;
        lastTouchPosition = eventData.position;
        touchDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Ignore if we started on UI
        if (isOverUI || !isDragging || eventData.pointerId != currentFingerId)
            return;

        // Calculate delta from last position
        Vector2 currentPosition = eventData.position;
        Vector2 delta = currentPosition - lastTouchPosition;
        
        // Apply sensitivity
        touchDelta = delta * cameraSensitivity * 0.1f;
        
        // Invert Y if needed
        if (invertY)
        {
            touchDelta.y = -touchDelta.y;
        }

        lastTouchPosition = currentPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == currentFingerId)
        {
            isDragging = false;
            currentFingerId = -1;
            touchDelta = Vector2.zero;
        }
    }

    // Alternative: Use Unity's Touch Input (works without EventSystem)
    private void Update()
    {
        // If using event system and it's working, we don't need this
        // But keeping it as backup for non-UI touch detection
        if (Input.touchCount > 0 && !isDragging)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                // Check if touch is over UI
                if (ignoreUITouches && IsTouchOverUI(touch))
                    return;
                    
                lastTouchPosition = touch.position;
                isDragging = true;
            }
        }

        if (isDragging && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                touchDelta = delta * cameraSensitivity * 0.1f;
                
                if (invertY)
                {
                    touchDelta.y = -touchDelta.y;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
                touchDelta = Vector2.zero;
            }
        }

        // Mouse support for testing in editor
        #if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (ignoreUITouches && IsMouseOverUI())
                return;
                
            lastTouchPosition = Input.mousePosition;
            isDragging = true;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;
            Vector2 delta = currentPosition - lastTouchPosition;
            touchDelta = delta * cameraSensitivity * 0.1f;
            
            if (invertY)
            {
                touchDelta.y = -touchDelta.y;
            }
            
            lastTouchPosition = currentPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            touchDelta = Vector2.zero;
        }
        #endif
    }

    public Vector2 GetCameraInput()
    {
        return touchDelta;
    }

    public float GetHorizontal()
    {
        return touchDelta.x;
    }

    public float GetVertical()
    {
        return touchDelta.y;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    // Helper method to check if pointer is over UI element
    private bool IsPointerOverUIElement(PointerEventData eventData)
    {
        // Check if we're clicking on a UI element (like the joystick)
        if (eventData.pointerEnter != null)
        {
            // Check if the object we're clicking has any of these components
            if (eventData.pointerEnter.GetComponent<MobileJoystick>() != null ||
                eventData.pointerEnter.GetComponent<MobileButton>() != null ||
                eventData.pointerEnter.GetComponentInParent<MobileJoystick>() != null ||
                eventData.pointerEnter.GetComponentInParent<MobileButton>() != null)
            {
                return true;
            }
        }
        return false;
    }

    // Helper method for touch input
    private bool IsTouchOverUI(Touch touch)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touch.position;
        
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponent<MobileJoystick>() != null ||
                result.gameObject.GetComponent<MobileButton>() != null ||
                result.gameObject.GetComponentInParent<MobileJoystick>() != null ||
                result.gameObject.GetComponentInParent<MobileButton>() != null)
            {
                return true;
            }
        }
        
        return false;
    }

    // Helper for mouse in editor
    private bool IsMouseOverUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponent<MobileJoystick>() != null ||
                result.gameObject.GetComponent<MobileButton>() != null ||
                result.gameObject.GetComponentInParent<MobileJoystick>() != null ||
                result.gameObject.GetComponentInParent<MobileButton>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
}