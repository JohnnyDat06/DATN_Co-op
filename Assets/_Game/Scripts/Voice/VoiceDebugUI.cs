using System.Collections.Generic;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceDebugUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Image _energyBar;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _energyValueText;

    private bool _isVisible = true;

    private void Start()
    {
        // Setup initial UI state
        if (_panel == null)
        {
            // If not assigned, try to find a child panel
            _panel = transform.GetChild(0).gameObject;
        }
    }

    private void Update()
    {
        // Toggle UI visibility with 'V' key
        if (Input.GetKeyDown(KeyCode.V))
        {
            _isVisible = !_isVisible;
            _panel.SetActive(_isVisible);
        }

        if (!_isVisible) return;

        UpdateStatus();
        UpdateEnergy();
    }

    private void UpdateStatus()
    {
        if (VivoxManager.Instance == null)
        {
            if (_statusText != null) _statusText.text = "Status: VivoxManager Missing";
            return;
        }

        string status = VivoxManager.Instance.IsLoggedIn ? "Logged In" : "Not Logged In";
        string channel = !string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName) 
            ? VivoxManager.Instance.JoinedChannelName 
            : "None";
        
        if (_statusText != null) _statusText.text = $"Status: {status}\nChannel: {channel}";
    }

    private void UpdateEnergy()
    {
        if (VivoxService.Instance == null || string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName))
        {
            if (_energyBar != null) _energyBar.fillAmount = 0;
            if (_energyValueText != null) _energyValueText.text = "0%";
            return;
        }

        // Find local participant energy
        double energy = 0;
        if (VivoxService.Instance.ActiveChannels.TryGetValue(VivoxManager.Instance.JoinedChannelName, out var participants))
        {
            foreach (var participant in participants)
            {
                if (participant.IsSelf)
                {
                    energy = participant.AudioEnergy;
                    break;
                }
            }
        }

        if (_energyBar != null)
        {
            _energyBar.fillAmount = (float)energy;
            // Color feedback: Green for low, Red for peaking
            _energyBar.color = Color.Lerp(Color.green, Color.red, (float)energy);
        }
        
        if (_energyValueText != null) _energyValueText.text = $"{(energy * 100):F0}%";
    }
}
