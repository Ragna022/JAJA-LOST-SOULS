using TMPro;
using UnityEngine;

public class LobbyPlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text playerNameText;
    public TMP_Text readyStatusText;
    
    [Header("Host State GameObjects")]
    public GameObject hostReadyObject;
    public GameObject hostNotReadyObject;
    
    [Header("Client State GameObjects")]
    public GameObject clientReadyObject;
    public GameObject clientNotReadyObject;

    [Header("Character Visuals")]
    public GameObject[] characterVisuals;

    private bool _isHost;
    private int _characterIndex = -1;

    public void SetPlayerData(string playerName, bool isReady, bool isHost = false, int characterIndex = 0)
    {
        Debug.Log($"🎨 Setting UI for {playerName} - Ready: {isReady}, Host: {isHost}, CharacterIndex: {characterIndex}");
        
        // Store host status and character index
        _isHost = isHost;
        _characterIndex = characterIndex;
        
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        
        // Set ready status text
        if (readyStatusText != null)
        {
            readyStatusText.text = isReady ? "READY" : "NOT READY";
        }
        
        // Update state GameObjects based on host status and ready status
        UpdateStateGameObjects(isReady);
        
        // Update character visual
        UpdateCharacterVisual();
    }
    
    // Method to update ready status without changing other data
    public void UpdateReadyStatus(bool isReady)
    {
        if (readyStatusText != null)
        {
            readyStatusText.text = isReady ? "READY" : "NOT READY";
        }
        
        // Update state GameObjects
        UpdateStateGameObjects(isReady);
    }
    
    private void UpdateStateGameObjects(bool isReady)
    {
        // First, disable all state objects
        DisableAllStateObjects();
        
        // Then enable the appropriate ones based on host and ready status
        if (_isHost)
        {
            if (isReady && hostReadyObject != null)
                hostReadyObject.SetActive(true);
            else if (hostNotReadyObject != null)
                hostNotReadyObject.SetActive(true);
        }
        else // Client
        {
            if (isReady && clientReadyObject != null)
                clientReadyObject.SetActive(true);
            else if (clientNotReadyObject != null)
                clientNotReadyObject.SetActive(true);
        }
    }
    
    private void DisableAllStateObjects()
    {
        if (hostReadyObject != null) hostReadyObject.SetActive(false);
        if (hostNotReadyObject != null) hostNotReadyObject.SetActive(false);
        if (clientReadyObject != null) clientReadyObject.SetActive(false);
        if (clientNotReadyObject != null) clientNotReadyObject.SetActive(false);
    }

    private void UpdateCharacterVisual()
    {
        // Disable all character visuals
        DisableAllCharacterVisuals();
        
        // Enable the selected one if valid
        if (_characterIndex >= 0 && _characterIndex < characterVisuals.Length && characterVisuals[_characterIndex] != null)
        {
            characterVisuals[_characterIndex].SetActive(true);
        }
    }

    private void DisableAllCharacterVisuals()
    {
        foreach (var visual in characterVisuals)
        {
            if (visual != null)
            {
                visual.SetActive(false);
            }
        }
    }
}