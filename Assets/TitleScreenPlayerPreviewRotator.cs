using UnityEngine;

public class TitleScreenPlayerPreviewRotator : MonoBehaviour
{
    PlayerControls playerControls;

    [Header("Camera Input")]
    [SerializeField] private Vector2 cameraInput;
    [SerializeField] private float horizontalInput;

    [Header("Rotation")]
    [SerializeField] private float lookAngle;
    [SerializeField] private float rotationSpeed = 5;

    private void OnEnable()
    {
        if (playerControls == null)
        {
            playerControls = new PlayerControls();

            playerControls.PlayerCamera.Movement.performed += i => cameraInput = i.ReadValue<Vector2>();
        }

        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        horizontalInput = cameraInput.x;

        lookAngle += (horizontalInput * rotationSpeed) * Time.deltaTime;
        Vector3 CameraRotation = Vector3.zero;
        CameraRotation.y = lookAngle;
        //Quaternion targetRotation = Quaternion.Euler(CameraRotation);
        //transform.rotation = targetRotation;
    }
}
