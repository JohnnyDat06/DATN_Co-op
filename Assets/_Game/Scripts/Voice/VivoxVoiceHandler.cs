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
    [SerializeField] private float _occludedVolumeReduction = 0.5f;
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
            _syncedVivoxId.Value = AuthenticationService.Instance.PlayerId;
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
        _vivoxId = id;
        _allHandlers[id] = this;
        Debug.Log($"[VivoxVoiceHandler] Registered handler for PlayerId: {id} (Owner: {OwnerClientId})");
    }

    public override void OnNetworkDespawn()
    {
        if (!string.IsNullOrEmpty(_vivoxId))
        {
            _allHandlers.Remove(_vivoxId);
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
            Debug.LogError("[VivoxVoiceHandler] VivoxManager instance not found!");
            return;
        }

        Debug.Log($"[VivoxVoiceHandler] Player {OwnerClientId} joining voice channel...");
        await VivoxManager.Instance.LoginAsync();
        await VivoxManager.Instance.JoinChannelAsync("MainLobby", true);
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

        // Ensure we are logged in and in a channel
        if (!VivoxManager.Instance.IsLoggedIn) return;

        Transform listenerTransform = Camera.main != null ? Camera.main.transform : transform;
        
        try 
        {
            VivoxService.Instance.Set3DPosition(
                transform.position,
                listenerTransform.position,
                listenerTransform.forward,
                listenerTransform.up,
                VivoxManager.Instance.JoinedChannelName
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VivoxVoiceHandler] Failed to set 3D position: {e.Message}");
        }
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

            if (_allHandlers.TryGetValue(participant.PlayerId, out var remoteHandler))
            {
                float volume = 1.0f;
                Vector3 startPos = transform.position + Vector3.up * 1.5f;
                Vector3 endPos = remoteHandler.transform.position + Vector3.up * 1.5f;
                Vector3 direction = endPos - startPos;
                float distance = direction.magnitude;

                if (Physics.Raycast(startPos, direction.normalized, out RaycastHit hit, distance, _occlusionLayerMask))
                {
                    if (hit.collider.transform != remoteHandler.transform && !hit.collider.transform.IsChildOf(remoteHandler.transform))
                    {
                        volume = _occludedVolumeReduction;
                    }
                }

                // Map 0.0 - 1.0 volume to -50 to 50 range for Vivox 16+ API
                // Note: SetLocalVolume(0) is original volume, negative is quieter, positive is louder.
                int vivoxVolume = Mathf.Clamp((int)((volume - 1.0f) * 50.0f), -50, 50);
                participant.SetLocalVolume(vivoxVolume);
            }
        }
    }
}
