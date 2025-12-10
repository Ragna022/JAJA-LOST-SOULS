using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
// Define aliases to avoid conflicts with the old system
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

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

    // ESSENTIAL: The New Input System's "Enhanced Touch" must be enabled to work like the old array
    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        touchDelta = Vector2.zero;

        // 1. If we have touches, process them using EnhancedTouch
        if (Touch.activeTouches.Count > 0)
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
            // Iterate through the New Input System's touch list
            foreach (var t in Touch.activeTouches)
            {
                if (t.finger.index == activeTouchId)
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
        
        // B. IF WE DON'T HAVE A CAMERA FINGER
        if (activeTouchId == -1)
        {
            foreach (var t in Touch.activeTouches)
            {
                // Check 1: Is it on the right side?
                if (!IsTouchOnRightSide(t.screenPosition)) 
                    continue;

                // Check 2: Is it hitting a BUTTON?
                if (ignoreUITouches && IsTouchBlockedByUI(t.screenPosition))
                    continue;

                // ✅ FOUND A VALID FINGER! Grab it.
                activeTouchId = t.finger.index;
                touchStartPosition = t.screenPosition;
                lastTouchPosition = t.screenPosition;
                isDragging = true;
                
                // If it's an existing touch (Moved), accept immediately
                validCameraTouch = (t.phase == TouchPhase.Moved); 

                if (showDebugLogs) Debug.Log($"Grabbed Finger {t.finger.index} at {t.screenPosition}");
                break; 
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
                if (Vector2.Distance(touch.screenPosition, touchStartPosition) > deadZone)
                {
                    validCameraTouch = true;
                }
            }

            // Apply Movement
            if (validCameraTouch)
            {
                Vector2 delta = touch.screenPosition - lastTouchPosition;
                touchDelta = delta * cameraSensitivity * 0.15f;
                
                if (invertY) touchDelta.y = -touchDelta.y;
            }

            lastTouchPosition = touch.screenPosition;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            ResetTouch();
        }
    }

    private void HandleMouseInput()
    {
        // Check if Mouse exists (prevent errors on devices without mice)
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (IsTouchOnRightSide(mousePos) && !IsTouchBlockedByUI(mousePos))
            {
                activeTouchId = 999;
                touchStartPosition = mousePos;
                lastTouchPosition = mousePos;
                isDragging = true;
                validCameraTouch = false;
            }
        }
        else if (Mouse.current.leftButton.isPressed && activeTouchId == 999)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            if (!validCameraTouch && Vector2.Distance(pos, touchStartPosition) > deadZone) validCameraTouch = true;
            
            if (validCameraTouch)
            {
                Vector2 delta = pos - lastTouchPosition;
                touchDelta = delta * cameraSensitivity * 0.15f;
                if (invertY) touchDelta.y = -touchDelta.y;
            }
            lastTouchPosition = pos;
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && activeTouchId == 999)
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

    private bool IsTouchBlockedByUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = position };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult r in results)
        {
            GameObject hit = r.gameObject;
            if (hit == gameObject) continue; 

            // Check for Buttons
            if (hit.GetComponentInParent<UnityEngine.UI.Button>() != null || 
                hit.GetComponentInParent<MobileButton>() != null)
            {
                if (showDebugLogs) Debug.Log($"Blocked by Button: {hit.name}");
                return true; 
            }

            // Check for Joystick
            if (hit.GetComponentInParent<MobileJoystick>() != null)
            {
                if (IsTouchOnRightSide(position))
                {
                    continue; 
                }
                else
                {
                    return true; 
                }
            }
        }

        return false;
    }

    public Vector2 GetCameraInput() => touchDelta;
}