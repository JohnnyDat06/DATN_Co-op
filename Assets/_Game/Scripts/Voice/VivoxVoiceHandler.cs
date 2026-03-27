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
            // Initial attempt, might be empty if services not ready
            if (AuthenticationService.Instance.IsSignedIn)
            {
                _syncedVivoxId.Value = AuthenticationService.Instance.PlayerId;
            }
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
        if (VivoxManager.Instance == null) return;

        int retries = 0;
        while (retries < 5)
        {
            try
            {
                await VivoxManager.Instance.LoginAsync();
                
                if (IsOwner && AuthenticationService.Instance.IsSignedIn)
                {
                    string currentId = AuthenticationService.Instance.PlayerId;
                    if (_syncedVivoxId.Value.ToString() != currentId)
                    {
                        _syncedVivoxId.Value = currentId;
                        RegisterHandler(currentId);
                    }
                }

                // Luôn cố gắng join channel mặc định
                await VivoxManager.Instance.JoinChannelAsync(VivoxManager.Instance.DefaultChannelName, true);
                
                if (!string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName))
                {
                    break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VivoxVoiceHandler] Join attempt failed: {e.Message}");
            }

            retries++;
            await System.Threading.Tasks.Task.Delay(1000);
        }
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            UpdatePositions();
        }
        
        // Luôn chạy Occlusion/VAD cho local player để xử lý âm lượng của người khác
        if (IsOwner)
        {
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

            // Tìm remote handler dựa trên PlayerId (Vivox ID thường chứa PlayerId bên trong)
            VivoxVoiceHandler remoteHandler = null;
            foreach (var kvp in _allHandlers)
            {
                // So khớp linh hoạt hơn vì ID Vivox có thể có prefix
                if (participant.PlayerId.Contains(kvp.Key) || kvp.Key.Contains(participant.PlayerId))
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

                if (Physics.Raycast(startPos, direction.normalized, out RaycastHit hit, distance, _occlusionLayerMask))
                {
                    if (hit.collider.transform != remoteHandler.transform && !hit.collider.transform.IsChildOf(remoteHandler.transform))
                    {
                        occlusionVolume = _occludedVolumeReduction; 
                    }
                }
            }

            // Logic VAD & Volume
            // Nếu không tìm thấy Handler, vẫn để volume = 0 (bình thường) thay vì mute
            int vivoxVolume = (occlusionVolume < 1.0f) ? -15 : 0; 
            
            // Chỉ mute hoàn toàn nếu năng lượng quá thấp (tránh nhiễu nền)
            if (participant.AudioEnergy < 0.001f) // Ngưỡng rất thấp
            {
                participant.SetLocalVolume(-50);
            }
            else
            {
                participant.SetLocalVolume(vivoxVolume);
            }
        }
    }
}
