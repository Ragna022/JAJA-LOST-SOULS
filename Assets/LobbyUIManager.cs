using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Button readyButton;
    public Button leaveButton;
    public TMP_Text readyButtonText;
    
    private bool isReady = false;
    
    private void Start()
    {
        Debug.Log("🔄 LobbyUIManager starting...");
        
        // Setup button listeners
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
            Debug.Log("✅ Ready button listener set");
        }
        else
        {
            Debug.LogError("❌ Ready button reference is null!");
        }
            
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveClicked);
            Debug.Log("✅ Leave button listener set");
        }
        else
        {
            Debug.LogError("❌ Leave button reference is null!");
        }
            
        UpdateReadyButton();
    }
    
    private void OnReadyClicked()
    {
        isReady = !isReady;
        Debug.Log($"🔄 Ready button clicked - New state: {isReady}");
        
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ToggleReadyStatus();
        }
        else
        {
            Debug.LogError("❌ LobbyManager.Instance is null!");
        }
        
        UpdateReadyButton();
    }
    
    private void OnLeaveClicked()
    {
        Debug.Log("👋 Leave button clicked");
        
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LeaveLobby();
        }
        else
        {
            Debug.LogError("❌ LobbyManager.Instance is null!");
        }
    }
    
    private void UpdateReadyButton()
    {
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "UNREADY" : "READY";
            Debug.Log($"🔄 Ready button text updated to: {readyButtonText.text}");
        }
        else
        {
            Debug.LogError("❌ Ready button text reference is null!");
        }
    }
}