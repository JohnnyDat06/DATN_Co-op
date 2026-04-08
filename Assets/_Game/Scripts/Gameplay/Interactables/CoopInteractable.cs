using Unity.Netcode;
using UnityEngine;

public class CoopInteractable : InteractableBase
{
    [Header("Coop Settings")]
    [SerializeField] private Transform _pointA;
    [SerializeField] private Transform _pointB;
    [SerializeField] private float _validDistance = 2f;

    private readonly NetworkVariable<bool> _playerAReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _playerBReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _playerAReady.OnValueChanged += OnPlayerAReadyChanged;
        _playerBReady.OnValueChanged += OnPlayerBReadyChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _playerAReady.OnValueChanged -= OnPlayerAReadyChanged;
        _playerBReady.OnValueChanged -= OnPlayerBReadyChanged;
    }

    public override void Interact(ulong playerId)
    {
        if (!CanInteract) return;

        AttemptReadyServerRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttemptReadyServerRpc(ulong playerId)
    {
        if (!CanInteract) return;
        if (!TryGetPlayerObject(playerId, out NetworkObject playerObject)) return;

        Vector3 playerPos = playerObject.transform.position;
        bool isHost = playerId == NetworkManager.ServerClientId;
        bool nearA = _pointA == null || Vector3.Distance(playerPos, _pointA.position) <= _validDistance;
        bool nearB = _pointB == null || Vector3.Distance(playerPos, _pointB.position) <= _validDistance;

        if (isHost && nearA)
        {
            _playerAReady.Value = true;
            Debug.Log($"[CoopInteractable] {_interactableId} - Host (A) da san sang.");
        }
        else if (!isHost && nearB)
        {
            _playerBReady.Value = true;
            Debug.Log($"[CoopInteractable] {_interactableId} - Client (B) da san sang.");
        }
        else
        {
            Debug.Log($"[CoopInteractable] {_interactableId} - Player {playerId} bam nhung dung sai vi tri.");
        }

        CheckActivationConditions();
    }

    private void Update()
    {
        if (!IsServer) return;
        if (IsActivated && !_allowReactivation) return;

        // THAY ĐỔI CỐT LÕI: Update CHỈ ĐƯỢC PHÉP dùng để HỦY (Tắt) trạng thái ready 
        // nếu người chơi bỏ đi rông quá xa Point A / Point B. 
        // Tuyệt đối KHÔNG được tự động Bật (gán True) ở đây, Bật (True) chỉ được làm khi bấm [E].
        if (_playerAReady.Value)
        {
            if (!IsPlayerNear(NetworkManager.ServerClientId, true))
            {
                _playerAReady.Value = false;
                Debug.Log($"[CoopInteractable] {_interactableId} - Host roi khoi vi tri -> Huy San Sang.");
            }
        }

        if (_playerBReady.Value)
        {
            if (!IsPlayerNear(1, false)) // ID = 1 tam thoi cho p2
            {
                _playerBReady.Value = false;
                Debug.Log($"[CoopInteractable] {_interactableId} - Client roi khoi vi tri -> Huy San Sang.");
            }
        }
    }

    private bool IsPlayerNear(ulong playerId, bool isHost)
    {
        if (!TryGetPlayerObject(playerId, out NetworkObject playerObject)) return false;

        Vector3 playerPos = playerObject.transform.position;
        Transform targetPoint = isHost ? _pointA : _pointB;
        
        return targetPoint == null || Vector3.Distance(playerPos, targetPoint.position) <= _validDistance;
    }

    private void CheckActivationConditions()
    {
        if (!IsServer) return;

        if (_playerAReady.Value && _playerBReady.Value)
        {
            Debug.Log($"[CoopInteractable] {_interactableId} - Ca 2 da san sang -> KICH HOAT.");
            ServerActivate();

            if (_allowReactivation)
            {
                _playerAReady.Value = false;
                _playerBReady.Value = false;
            }
        }
    }

    public override void ResetInteractable()
    {
        if (!IsServer) return;
        base.ResetInteractable();
        _playerAReady.Value = false;
        _playerBReady.Value = false;
    }

    private void OnPlayerAReadyChanged(bool previousValue, bool newValue)
    {
        HandleReadinessUI(NetworkManager.ServerClientId, previousValue, newValue);
    }

    private void OnPlayerBReadyChanged(bool previousValue, bool newValue)
    {
        HandleReadinessUI(1, previousValue, newValue);
    }

    private void HandleReadinessUI(ulong playerId, bool previousValue, bool newValue)
    {
        if (newValue && !previousValue)
        {
            EventBus.RaiseCoopInteractablePlayerReady(_interactableId, playerId);
        }
        else if (!newValue && previousValue && !IsActivated)
        {
            EventBus.RaiseCoopInteractableReset(_interactableId);
        }
    }
}
