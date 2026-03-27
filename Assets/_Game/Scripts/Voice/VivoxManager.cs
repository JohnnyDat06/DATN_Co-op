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
    [Range(1, 200)]
    [SerializeField] private int _audibleDistance = 64;
    [Range(1, 50)]
    [SerializeField] private int _conversationalDistance = 5;
    [Range(0.1f, 4.0f)]
    [SerializeField] private float _rolloff = 1.0f;
    [SerializeField] private AudioFadeModel _distanceModel =
        AudioFadeModel.LinearByDistance;

    [Header("Volume Settings")]
    [Range(-50, 50)]
    [SerializeField] private int _masterVolume = 0;
    [Range(-50, 50)]
    [SerializeField] private int _micGain = 0;
    [Range(0.001f, 0.1f)]
    [SerializeField] private float _vadThreshold = 0.02f;

    public int MasterVolume => _masterVolume;
    public int MicGain => _micGain;
    public float VADThreshold => _vadThreshold;

    private bool _isInitialized;
    private bool _isLoggedIn;
    private string _joinedChannelName;
    private Task _initializeTask;
    private Task _loginTask;

    public bool IsLoggedIn => _isLoggedIn;
    public string JoinedChannelName => _joinedChannelName;
    public string DefaultChannelName => _channelName;

    private void OnValidate()
    {
        if (Application.isPlaying && _isLoggedIn)
        {
            ApplyVolumeSettings();
        }
    }

    private void ApplyVolumeSettings()
    {
        VivoxService.Instance.SetInputDeviceVolume(_micGain);
        VivoxService.Instance.SetOutputDeviceVolume(_masterVolume);
    }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure app runs in background for voice to stay active
        Application.runInBackground = true;

        // Request microphone permission at startup
        await RequestMicrophonePermission();

        if (_autoLogin)
        {
            await InitializeAsync();
        }
    }

    private async Task RequestMicrophonePermission()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            await Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // On Windows, checking Microphone.devices triggers the OS to acknowledge the app's intent to use the mic.
        string[] devices = Microphone.devices;
        if (devices.Length > 0)
        {
            Debug.Log($"[VivoxManager] Microphone devices found: {string.Join(", ", devices)}");
            
            // "Poke" the microphone to ensure the system recognizes the app is using it.
            // This is similar to what Discord does to 'wake up' the audio recorder.
            try
            {
                string device = devices[0];
                Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
                AudioClip tempClip = Microphone.Start(device, false, 1, 44100);
                Microphone.End(device);
                Debug.Log("[VivoxManager] Microphone 'poke' successful.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VivoxManager] Microphone 'poke' failed: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[VivoxManager] No microphone devices found! Please ensure your microphone is plugged in and allowed in Windows Privacy Settings.");
        }

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            var asyncOp = Application.RequestUserAuthorization(UserAuthorization.Microphone);
            while (!asyncOp.isDone) await Task.Yield();
        }
#endif
        await Task.CompletedTask;
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
            
            // Apply initial volume settings
            ApplyVolumeSettings();
            
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

        if (_joinedChannelName == channelName)
        {
            Debug.Log($"[VivoxManager] Already in channel: {channelName}");
            return;
        }

        try
        {
            Debug.Log($"[VivoxManager] Joining channel: {channelName} (Positional: {isPositional})...");
            if (isPositional)
            {
                Channel3DProperties properties = new Channel3DProperties(_audibleDistance, _conversationalDistance, _rolloff, _distanceModel);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            }
            else
            {
                await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            }

            _joinedChannelName = channelName;
            Debug.Log($"[VivoxManager] Successfully joined channel: {channelName}");
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
