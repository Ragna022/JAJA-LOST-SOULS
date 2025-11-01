using UnityEngine;
using DG.Tweening;

public class CameraAnimator : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform viewA;
    public Transform viewB;
    public float cameraMoveDuration = 1.2f;
    public Ease cameraEase = Ease.InOutSine;

    [Header("Camera Behavior")]
    public IdleCameraSway camSwayScript;

    private Camera mainCam;
    private bool inSelectionView = false;

    

    private void Awake()
    {
        mainCam = Camera.main;
    }

    #region CAMERA LOGIC (Delegated)
    
    public void MoveToSelectionView()
    {
        if (inSelectionView) return;
        inSelectionView = true;
        
        mainCam.transform.DOMove(viewB.position, cameraMoveDuration).SetEase(cameraEase);
        mainCam.transform.DORotateQuaternion(viewB.rotation, cameraMoveDuration).SetEase(cameraEase);
        camSwayScript.enabled = false; Debug.Log("Entering Character Selection View...");
    }

    public void ReturnToMainView()
    {
        if (!inSelectionView) return;
        inSelectionView = false;

        if (mainCam == null)
        {
            Debug.LogWarning("Main Camera not assigned or found!");
            return;
        }

        camSwayScript.enabled = true;

        mainCam.transform.DOMove(viewA.position, cameraMoveDuration)
            .SetEase(cameraEase);
        mainCam.transform.DORotateQuaternion(viewA.rotation, cameraMoveDuration)
            .SetEase(cameraEase);

        Debug.Log("Returning to Main View");
    }
    #endregion
}


