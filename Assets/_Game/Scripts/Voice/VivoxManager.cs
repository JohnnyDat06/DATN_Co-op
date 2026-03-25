using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    [Header("Vivox Settings")]
    [SerializeField] private string _channelName = "MainLobby";
    [SerializeField] private bool _autoLogin = true;

    [Header("3D Audio Settings")]
    [SerializeField] private int _audibleDistance = 32;
    [SerializeField] private int _conversationalDistance = 1;
    [SerializeField] private float _rolloff = 1.0f;
    [SerializeField] private AudioFadeModel _distanceModel =
        AudioFadeModel.InverseByDistance;

    private bool _isInitialized;
    private bool _isLoggedIn;
    private string _joinedChannelName;
    private Task _initializeTask;
    private Task _loginTask;

    public bool IsLoggedIn => _isLoggedIn;
    public string JoinedChannelName => _joinedChannelName;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_autoLogin)
        {
            await InitializeAsync();
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        // If already initializing, wait for that task instead of starting a new one
        if (_initializeTask != null)
        {
            await _initializeTask;
            return;
        }

        _initializeTask = InternalInitializeAsync();
        await _initializeTask;
    }

    private async Task InternalInitializeAsync()
    {
        try
        {
            Debug.Log("[VivoxManager] Starting initialization...");
            await UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await VivoxService.Instance.InitializeAsync();
            _isInitialized = true;
            Debug.Log("[VivoxManager] Initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VivoxManager] Initialization failed: {e.Message}");
            _initializeTask = null; // Allow retrying on failure
        }
    }

    public async Task LoginAsync(string displayName = null)
    {
        if (!_isInitialized) await InitializeAsync();
        if (_isLoggedIn) return;

        if (_loginTask != null)
        {
            await _loginTask;
            return;
        }

        _loginTask = InternalLoginAsync(displayName);
        await _loginTask;
    }

    private async Task InternalLoginAsync(string displayName)
    {
        try
        {
            LoginOptions options = new LoginOptions
            {
                DisplayName = displayName ?? AuthenticationService.Instance.PlayerId,
                ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.TenPerSecond
            };

            await VivoxService.Instance.LoginAsync(options);
            _isLoggedIn = true;
            
            Debug.Log("[VivoxManager] Logged in successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VivoxManager] Login failed: {e.Message}");
            _loginTask = null; // Allow retrying on failure
        }
    }

    public async Task JoinChannelAsync(string channelName, bool isPositional = true)
    {
        if (!_isLoggedIn)
        {
            await LoginAsync();
        }

        if (!_isLoggedIn)
        {
            Debug.LogError("[VivoxManager] Cannot join channel: Login failed.");
            return;
        }

        try
        {
            if (isPositional)
            {
                Channel3DProperties properties = new Channel3DProperties(_audibleDistance, _conversationalDistance, _rolloff, _distanceModel);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            }
            else
            {
                await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            }

            _joinedChannelName = channelName; // Chỉ gán sau khi Join thành công
            Debug.Log($"[VivoxManager] Joined channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VivoxManager] Join channel failed: {e.Message}");
            _joinedChannelName = null;
        }
    }

    public async Task LeaveChannelAsync()
    {
        if (string.IsNullOrEmpty(_joinedChannelName)) return;

        try
        {
            await VivoxService.Instance.LeaveChannelAsync(_joinedChannelName);
            Debug.Log($"[VivoxManager] Left channel: {_joinedChannelName}");
            _joinedChannelName = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[VivoxManager] Leave channel failed: {e.Message}");
        }
    }

    public void SetMicrophoneMute(bool mute)
    {
        if (!_isLoggedIn) return;
        
        if (mute)
            VivoxService.Instance.MuteInputDevice();
        else
            VivoxService.Instance.UnmuteInputDevice();
        
        Debug.Log($"[VivoxManager] Microphone {(mute ? "muted" : "unmuted")}");
    }

    public bool IsMicrophoneMuted()
    {
        return _isLoggedIn && VivoxService.Instance.IsInputDeviceMuted;
    }
}
