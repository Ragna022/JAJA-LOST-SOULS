using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text playerNameText;
    public Image readyStatusImage;
    public TMP_Text statusText;
    
    [Header("Colors")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.red;
    
    public void SetPlayerData(string playerName, bool isReady, bool isHost = false)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
            if (isHost)
                playerNameText.text += " 👑";
        }
            
        if (readyStatusImage != null)
            readyStatusImage.color = isReady ? readyColor : notReadyColor;
            
        if (statusText != null)
            statusText.text = isReady ? "READY" : "NOT READY";
    }
}