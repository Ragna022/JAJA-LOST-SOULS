using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text playerNameText;
    public TMP_Text readyStatusText;
    public Image backgroundImage;
    public GameObject hostIndicator;
    
    [Header("Colors")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.red;
    public Color hostColor = Color.yellow;
    
    public void SetPlayerData(string playerName, bool isReady, bool isHost = false)
    {
        Debug.Log($"🎨 Setting UI for {playerName} - Ready: {isReady}, Host: {isHost}");
        
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        
        // Set ready status
        if (readyStatusText != null)
        {
            readyStatusText.text = isReady ? "READY" : "NOT READY";
            readyStatusText.color = isReady ? Color.green : Color.red;
        }
        
        // Set background color
        if (backgroundImage != null)
        {
            if (isHost)
            {
                backgroundImage.color = hostColor;
            }
            else
            {
                backgroundImage.color = isReady ? readyColor : notReadyColor;
            }
        }
        
        // Show host indicator
        if (hostIndicator != null)
        {
            hostIndicator.SetActive(isHost);
        }
    }
    
    // Method to update ready status without changing other data
    public void UpdateReadyStatus(bool isReady)
    {
        if (readyStatusText != null)
        {
            readyStatusText.text = isReady ? "READY" : "NOT READY";
            readyStatusText.color = isReady ? Color.green : Color.red;
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = isReady ? readyColor : notReadyColor;
        }
    }
}