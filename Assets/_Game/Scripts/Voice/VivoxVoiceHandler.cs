using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxVoiceHandler : NetworkBehaviour
{
    [Header("Occlusion Settings")]
    [SerializeField] private LayerMask _occlusionLayerMask = 1 << 0; // Default layer
    [SerializeField] private float _occludedVolumeReduction = 0.5f;
    [SerializeField] private float _occlusionCheckInterval = 0.2f;

    private static Dictionary<string, VivoxVoiceHandler> _allHandlers = new Dictionary<string, VivoxVoiceHandler>();

    private string _vivoxId;
    private float _nextOcclusionCheck;

    public override void OnNetworkSpawn()
    {
        _vivoxId = $"Player_{OwnerClientId}";
        _allHandlers[_vivoxId] = this;

        if (IsOwner)
        {
            JoinVoiceChannel();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (_vivoxId != null)
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
        if (VivoxManager.Instance == null) return;

        await VivoxManager.Instance.LoginAsync(_vivoxId);
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
        if (VivoxService.Instance == null || string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName)) return;

        Transform listenerTransform = Camera.main != null ? Camera.main.transform : transform;
        
        VivoxService.Instance.Set3DPosition(
            transform.position,
            listenerTransform.position,
            listenerTransform.forward,
            listenerTransform.up,
            VivoxManager.Instance.JoinedChannelName
        );
    }

    private void HandleOcclusion()
    {
        if (Time.time < _nextOcclusionCheck) return;
        if (VivoxService.Instance == null || string.IsNullOrEmpty(VivoxManager.Instance.JoinedChannelName)) return;
        if (!VivoxService.Instance.ActiveChannels.ContainsKey(VivoxManager.Instance.JoinedChannelName)) return;

        _nextOcclusionCheck = Time.time + _occlusionCheckInterval;

        var channel = VivoxService.Instance.ActiveChannels[VivoxManager.Instance.JoinedChannelName];
        
        foreach (var participant in channel)
        {
            if (participant.IsSelf) continue;

            if (_allHandlers.TryGetValue(participant.PlayerId, out var remoteHandler))
            {
                float volume = 1.0f;
                Vector3 direction = remoteHandler.transform.position - transform.position;
                float distance = direction.magnitude;

                Vector3 startPos = transform.position + Vector3.up * 1.5f;
                Vector3 endPos = remoteHandler.transform.position + Vector3.up * 1.5f;
                Vector3 rayDir = endPos - startPos;

                if (Physics.Raycast(startPos, rayDir.normalized, out RaycastHit hit, distance, _occlusionLayerMask))
                {
                    if (hit.collider.transform != remoteHandler.transform && !hit.collider.transform.IsChildOf(remoteHandler.transform))
                    {
                        volume = _occludedVolumeReduction;
                    }
                }

                // Map 0.0 - 1.0 volume to -50 to 0 range for Vivox 16+ API
                int vivoxVolume = Mathf.Clamp((int)((volume - 1.0f) * 50.0f), -50, 50);
                participant.SetLocalVolume(vivoxVolume);
            }
        }
    }
}
