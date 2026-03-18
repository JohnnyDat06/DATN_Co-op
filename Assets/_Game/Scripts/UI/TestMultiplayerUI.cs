using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// MÃ TẠM THỜI ĐỂ TEST (XÓA TRƯỚC KHI MERGE).
/// </summary>
public class TestMultiplayerUI : MonoBehaviour
{
    [SerializeField] private AuthManager _authManager;
    [SerializeField] private RelayManager _relayManager;
    [SerializeField] private TMP_Text _joinCodeDisplay;
    [SerializeField] private TMP_Text _statusLog;
    [SerializeField] private TMP_InputField _joinCodeInput;

    [Header("UI Controls")]
    [SerializeField] private GameObject _loginUIPanel;

    private void OnEnable()
    {
        EventBus.OnClientConnected += HandleClientConnected;
    }

    private void OnDisable()
    {
        EventBus.OnClientConnected -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Khi một kết nối mới diễn ra
        if (NetworkManager.Singleton.IsHost)
        {
            // Mình là Host, và có ai đó (hoặc chính mình) vừa kết nối
            if (clientId != 0) // = 0 thường là Host, > 0 là Client
            {
                _statusLog.text = $"Connected! Player {clientId} joined.";
                if (_loginUIPanel != null)
                    _loginUIPanel.SetActive(false);
            }
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // Mình là Client, nếu clientId trùng với ID của mình thì nghĩa là MÌNH vừa join thành công
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                _statusLog.text = "Connected as Client!";
                if (_loginUIPanel != null)
                    _loginUIPanel.SetActive(false);
                if (_joinCodeDisplay != null)
                    _joinCodeDisplay.gameObject.SetActive(false);
            }
        }
    }

    public async void OnHostClicked()
    {
        _statusLog.text = "Initializing...";
        await _authManager.InitializeAsync();
        var code = await _relayManager.CreateRelayAsync();
        
        if (_joinCodeDisplay != null)
            _joinCodeDisplay.text = $"Code: {code}";
        
        _statusLog.text = "Host running. Waiting for client...";
    }

    public async void OnJoinClicked()
    {
        _statusLog.text = "Joining...";
        await _authManager.InitializeAsync();
        await _relayManager.JoinRelayAsync(_joinCodeInput.text);
        
        _statusLog.text = "Authenticating with Host...";
        // Bỏ logic ẩn đi ở đây vì việc join có thể mất tgian, để cho HandleClientConnected lo việc ẩn UI sau khi nhận dc callback từ NGO.
    }
}
