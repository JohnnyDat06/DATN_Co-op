using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using Unity.Collections;

public class VivoxVoiceHandler : NetworkBehaviour
{
    [Header("Occlusion Settings")]
    [SerializeField] private LayerMask _occlusionLayerMask = 1 << 0; // Default layer
    [Range(0f, 1f)]
    [SerializeField] private float _occludedVolumeReduction = 0.5f;
    [Range(0.05f, 1f)]
    [SerializeField] private float _occlusionCheckInterval = 0.2f;

    private static Dictionary<string, VivoxVoiceHandler> _allHandlers = new Dictionary<string, VivoxVoiceHandler>();

    private NetworkVariable<FixedString64Bytes> _syncedVivoxId = new NetworkVariable<FixedString64Bytes>(
        writePerm: NetworkVariableWritePermission.Owner);
    
    private string _vivoxId;
    private float _nextOcclusionCheck;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Kiểm tra an toàn xem Services đã được khởi tạo chưa trước khi truy cập Instance
            if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized)
            {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    _syncedVivoxId.Value = AuthenticationService.Instance.PlayerId;
                }
            }
            
            // JoinVoiceChannel đã có sẵn logic đợi khởi tạo bên trong
            JoinVoiceChannel();
        }

        // Register current ID if already set
        if (!_syncedVivoxId.Value.IsEmpty)
        {
            RegisterHandler(_syncedVivoxId.Value.ToString());
        }

        _syncedVivoxId.OnValueChanged += (prev, current) => 
        {
            if (!prev.IsEmpty) _allHandlers.Remove(prev.ToString());
            if (!current.IsEmpty) RegisterHandler(current.ToString());
        };
    }

    private void RegisterHandler(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        
        _vivoxId = id;
        _allHandlers[id] = this;
        Debug.Log($"[VivoxVoiceHandler] Registered handler for PlayerId: {id} (Owner: {OwnerClientId}, IsOwner: {IsOwner})");
    }

    public override void OnNetworkDespawn()
    {
        if (!string.IsNullOrEmpty(_vivoxId))
        {
            if (_allHandlers.TryGetValue(_vivoxId, out var handler) && handler == this)
            {
                _allHandlers.Remove(_vivoxId);
            }
        }

        if (IsOwner && VivoxManager.Instance != null)
        {
            _ = VivoxManager.Instance.LeaveChannelAsync();
        }
    }

    private async void JoinVoiceChannel()
    {
        if (VivoxManager.Instance == null)
        {
            Debug.LogError("[VivoxVoiceHandler] VivoxManager instance is null!");
            return;
        }

        // Đợi một chút để đảm bảo NetworkId và Ownership đã ổn định
        await System.Threading.Tasks.Task.Delay(1000);

        int retries = 0;
        while (retries < 10) // Tăng số lần thử lại
        {
            try
            {
                Debug.Log($"[VivoxVoiceHandler] Login attempt {retries + 1}...");
                await VivoxManager.Instance.LoginAsync();
                
                if (VivoxManager.Instance.IsLoggedIn)
                {
                    if (IsOwner && AuthenticationService.Instance.IsSignedIn)
                    {
                        string currentId = AuthenticationService.Instance.PlayerId;
                        
                        // Cập nhật NetworkVariable để các máy khác biết PlayerId của mình
                        if (_syncedVivoxId.Value.ToString() != currentId)
                        {
                            _syncedVivoxId.Value = currentId;
                        }
                        
                        // Đăng ký local handler
                        RegisterHandler(currentId);
                    }

                    // Lấy channel name từ RelayManager (thông qua DefaultChannelName)
                    string channelToJoin = VivoxManager.Instance.DefaultChannelName;
                    
                    if (string.IsNullOrEmpty(channelToJoin) || channelToJoin == "MainLobby")
                    {
                        // Nếu vẫn là mặc định, đợi thêm chút nữa (có thể Relay chưa kịp set JoinCode)
                        Debug.Log("[VivoxVoiceHandler] Channel name is still default/empty, waiting for Relay JoinCode...");
                        await System.Threading.Tasks.Task.Delay(2000);
                        channelToJoin = VivoxManager.Instance.DefaultChannelName;
                    }

                    Debug.Log($"[VivoxVoiceHandler] Attempting to join channel: {channelToJoin}");
                    await VivoxManager.Instance.JoinChannelAsync(channelToJoin, true);
                    
                    if (!string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName))
                    {
                        Debug.Log($"[VivoxVoiceHandler] Successfully joined voice channel: {VivoxManager.Instance.JoinedChannelName}");
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[VivoxVoiceHandler] Login failed at attempt {retries + 1}, retrying...");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VivoxVoiceHandler] Join attempt {retries + 1} failed with error: {e.Message}");
            }

            retries++;
            await System.Threading.Tasks.Task.Delay(3000); // Tăng thời gian delay giữa các lần retry
        }

        if (!VivoxManager.Instance.IsLoggedIn)
        {
            Debug.LogError("[VivoxVoiceHandler] Failed to login to Vivox after all retries.");
        }
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            UpdatePositions();
            HandleOcclusion();
        }
    }

    private void UpdatePositions()
    {
        if (VivoxManager.Instance == null || string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName)) return;
        if (!VivoxManager.Instance.IsLoggedIn) return;

        // Vị trí của người nghe (Listener) luôn là Camera hoặc chính mình
        Transform listenerTransform = Camera.main != null ? Camera.main.transform : transform;
        
        try 
        {
            // Set3DPosition cho local player: cập nhật cả vị trí nói (speaker) và vị trí nghe (listener)
            VivoxService.Instance.Set3DPosition(
                transform.position,             // Vị trí miệng người nói
                listenerTransform.position,      // Vị trí tai người nghe
                listenerTransform.forward,       // Hướng nhìn
                listenerTransform.up,            // Hướng lên
                VivoxManager.Instance.JoinedChannelName
            );
        }
        catch (System.Exception) { }
    }

    private void HandleOcclusion()
    {
        if (Time.time < _nextOcclusionCheck) return;
        if (VivoxManager.Instance == null || string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName)) return;
        
        if (!VivoxService.Instance.ActiveChannels.TryGetValue(VivoxManager.Instance.JoinedChannelName, out var channel)) return;

        _nextOcclusionCheck = Time.time + _occlusionCheckInterval;

        foreach (var participant in channel)
        {
            if (participant.IsSelf) continue;

            // Tìm remote handler dựa trên PlayerId
            VivoxVoiceHandler remoteHandler = null;
            string participantId = participant.PlayerId;

            // Log để debug ID (chỉ log khi có sự thay đổi hoặc theo chu kỳ dài để tránh spam)
            // Debug.Log($"[VivoxVoiceHandler] Checking participant: {participantId}");

            foreach (var kvp in _allHandlers)
            {
                // Logic so khớp ID cải tiến:
                // Vivox ID thường có format "f:envId:playerId"
                // kvp.Key là "playerId" thuần túy từ AuthenticationService
                if (participantId.EndsWith(kvp.Key) || participantId.Contains(kvp.Key))
                {
                    remoteHandler = kvp.Value;
                    break;
                }
            }

            float occlusionVolume = 1.0f;
            if (remoteHandler != null)
            {
                Vector3 startPos = transform.position + Vector3.up * 1.5f;
                Vector3 endPos = remoteHandler.transform.position + Vector3.up * 1.5f;
                Vector3 direction = endPos - startPos;
                float distance = direction.magnitude;

                // Kiểm tra vật cản giữa 2 player
                if (Physics.Raycast(startPos, direction.normalized, out RaycastHit hit, distance, _occlusionLayerMask))
                {
                    // Nếu trúng vật gì đó không phải là chính player đó
                    if (hit.collider.transform != remoteHandler.transform && !hit.collider.transform.IsChildOf(remoteHandler.transform))
                    {
                        occlusionVolume = _occludedVolumeReduction; 
                    }
                }
            }
            else
            {
                // Nếu không tìm thấy handler cho participant này, có thể do chưa đồng bộ xong ID
                // Chúng ta vẫn cho nghe nhưng có thể giới hạn volume nhẹ
                // Debug.LogWarning($"[VivoxVoiceHandler] No handler found for participant: {participantId}");
            }

            // Áp dụng volume (Vivox volume từ -50 đến 50)
            int targetVolume = (occlusionVolume < 1.0f) ? -15 : 0; 
            
            // Chỉ áp dụng volume nếu participant đang thực sự nói (AudioEnergy > 0)
            if (participant.AudioEnergy > 1e-5) 
            {
                participant.SetLocalVolume(targetVolume);
            }
            else
            {
                // Giảm nhiễu nền khi không nói
                participant.SetLocalVolume(-50);
            }
        }
    }
}
