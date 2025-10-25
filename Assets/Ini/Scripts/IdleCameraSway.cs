using UnityEngine;
using DG.Tweening;

public class IdleCameraSway : MonoBehaviour
{
    [Header("Camera Views")]
    [SerializeField] private Transform viewA; // e.g. Main Menu view
    [SerializeField] private Transform viewB; // e.g. Game view
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    [Header("Sway Settings")]
    [Tooltip("How far the camera moves in local space")]
    public float positionAmplitude = 0.05f;
    [Tooltip("How much the camera tilts (in degrees)")]
    public float rotationAmplitude = 0.3f;
    [Tooltip("Speed of the position sway")]
    public float positionSpeed = 0.3f;
    [Tooltip("Speed of the rotation sway")]
    public float rotationSpeed = 0.2f;

    private bool isAtViewA = true;
    private bool isSwaying = true;

    private Transform cam;
    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        cam = Camera.main.transform;

        // Optional: start camera at viewA
        if (viewA != null)
        {
            cam.position = viewA.position;
            cam.rotation = viewA.rotation;
        }

        startPos = cam.localPosition;
        startRot = cam.localRotation;
    }

    void Update()
    {
        if (!isSwaying) return;

        float t = Time.time;

        // Gentle movement
        float x = Mathf.Sin(t * positionSpeed) * positionAmplitude;
        float y = Mathf.Cos(t * positionSpeed * 0.8f) * positionAmplitude * 0.5f;
        float z = Mathf.Sin(t * positionSpeed * 0.5f) * positionAmplitude * 0.3f;

        cam.localPosition = startPos + new Vector3(x, y, z);

        // Subtle tilt
        float rotX = Mathf.Sin(t * rotationSpeed) * rotationAmplitude;
        float rotY = Mathf.Cos(t * rotationSpeed * 0.7f) * rotationAmplitude;
        cam.localRotation = startRot * Quaternion.Euler(rotX, rotY, 0f);
    }
}


