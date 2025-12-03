using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileCameraTouch : MonoBehaviour
{
    [Header("Camera Sensitivity")]
    [SerializeField] private float cameraSensitivity = 1f;
    [SerializeField] private bool invertY = false;
    
    [Header("Touch Settings")]
    [SerializeField] private bool ignoreUITouches = true;
    [SerializeField] private float deadZone = 10f; 
    [Tooltip("0.5 means the camera works on the Right Half of the screen.")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float rightSideThreshold = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector2 touchDelta;
    private Vector2 touchStartPosition;
    private Vector2 lastTouchPosition;
    private bool isDragging = false;
    private bool validCameraTouch = false;
    private int activeTouchId = -1;

    private void Update()
    {
        touchDelta = Vector2.zero;

        // 1. If we have touches, process them
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // 2. If no touches at all, reset immediately
        else 
        {
            ResetTouch();
        }

        #if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
        #endif
    }

    private void HandleTouchInput()
    {
        // A. IF WE ALREADY HAVE A CAMERA FINGER
        if (activeTouchId != -1)
        {
            bool foundActive = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId == activeTouchId)
                {
                    ProcessCameraTouch(t);
                    foundActive = true;
                    break;
                }
            }

            // If the finger we were tracking was lifted/lost, reset
            if (!foundActive)
            {
                ResetTouch();
            }
        }
        
        // B. IF WE DON'T HAVE A CAMERA FINGER (This fixes the "2 Finger" bug)
        // We look for ANY finger on the right side, even if it's already "Moved" or "Stationary"
        if (activeTouchId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                // Check 1: Is it on the right side?
                if (!IsTouchOnRightSide(t.position)) 
                    continue;

                // Check 2: Is it hitting a BUTTON? (We ignore Joysticks here)
                if (ignoreUITouches && IsTouchBlockedByUI(t.position))
                    continue;

                // ✅ FOUND A VALID FINGER! Grab it.
                activeTouchId = t.fingerId;
                touchStartPosition = t.position;
                lastTouchPosition = t.position;
                isDragging = true;
                
                // If it's a fresh touch, reset deadzone logic. 
                // If it's an existing touch (Stationary/Moved), we accept it immediately.
                validCameraTouch = (t.phase == TouchPhase.Moved); 

                if (showDebugLogs) Debug.Log($"Grabbed Finger {t.fingerId} at {t.position}");
                break; // Stop looking, we found one
            }
        }
    }

    private void ProcessCameraTouch(Touch touch)
    {
        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            // Deadzone Check
            if (!validCameraTouch)
            {
                if (Vector2.Distance(touch.position, touchStartPosition) > deadZone)
                {
                    validCameraTouch = true;
                }
            }

            // Apply Movement
            if (validCameraTouch)
            {
                Vector2 delta = touch.position - lastTouchPosition;
                touchDelta = delta * cameraSensitivity * 0.15f;
                
                if (invertY) touchDelta.y = -touchDelta.y;
            }

            lastTouchPosition = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            ResetTouch();
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsTouchOnRightSide(Input.mousePosition) && !IsTouchBlockedByUI(Input.mousePosition))
            {
                activeTouchId = 999;
                touchStartPosition = Input.mousePosition;
                lastTouchPosition = Input.mousePosition;
                isDragging = true;
                validCameraTouch = false;
            }
        }
        else if (Input.GetMouseButton(0) && activeTouchId == 999)
        {
            Vector2 pos = Input.mousePosition;
            if (!validCameraTouch && Vector2.Distance(pos, touchStartPosition) > deadZone) validCameraTouch = true;
            
            if (validCameraTouch)
            {
                Vector2 delta = pos - lastTouchPosition;
                touchDelta = delta * cameraSensitivity * 0.15f;
                if (invertY) touchDelta.y = -touchDelta.y;
            }
            lastTouchPosition = pos;
        }
        else if (Input.GetMouseButtonUp(0) && activeTouchId == 999)
        {
            ResetTouch();
        }
    }

    private void ResetTouch()
    {
        isDragging = false;
        validCameraTouch = false;
        activeTouchId = -1;
    }

    private bool IsTouchOnRightSide(Vector2 position)
    {
        return position.x > Screen.width * rightSideThreshold;
    }

    // Renamed for clarity: This only returns true if we hit a BAD UI element (Buttons)
    private bool IsTouchBlockedByUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = position };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult r in results)
        {
            GameObject hit = r.gameObject;
            if (hit == gameObject) continue; // Ignore self

            // Check for Buttons (ALWAYS BLOCK)
            if (hit.GetComponentInParent<UnityEngine.UI.Button>() != null || 
                hit.GetComponentInParent<MobileButton>() != null)
            {
                if (showDebugLogs) Debug.Log($"Blocked by Button: {hit.name}");
                return true; 
            }

            // Check for Joystick
            if (hit.GetComponentInParent<MobileJoystick>() != null)
            {
                // If we hit a joystick, but we are on the Right Side -> IGNORE IT (Don't block)
                if (IsTouchOnRightSide(position))
                {
                    continue; 
                }
                else
                {
                    return true; // Joystick on left side -> Block
                }
            }
            
            // Note: We deliberately ignore generic "Images" or "Panels" here.
            // This ensures invisible containers don't break the camera.
        }

        return false;
    }

    public Vector2 GetCameraInput() => touchDelta;
}