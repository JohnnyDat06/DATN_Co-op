using Unity.Netcode;
using UnityEngine;

/// <summary>
/// CoopInteractable — Tương tác cần 2 người chơi cùng lúc (Co-op).
/// Yêu cầu 2 người đứng đúng vị trí và bấm Interact.
/// Gửi EventBus để Update UI nhấp nháy chờ đồng đội.
/// SRS §4.2.2
/// </summary>
public class CoopInteractable : InteractableBase
{
    // ─── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Coop Settings")]
    [Tooltip("Vị trí đứng cho Player A (Thường là Host)")]
    [SerializeField] private Transform _pointA;

    [Tooltip("Vị trí đứng cho Player B (Thường là Client)")]
    [SerializeField] private Transform _pointB;

    [Tooltip("Khoảng cách khoảng dung sai để xác nhận đứng đúng vị trí.")]
    [SerializeField] private float _validDistance = 2f;

    // ─── Network Variables ────────────────────────────────────────────────────

    private NetworkVariable<bool> _playerAReady = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> _playerBReady = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ─── Lifecycle ────────────────────────────────────────────────────────────

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

    // ─── Tương tác (Client gửi lên) ───────────────────────────────────────────

    public override void Interact(ulong playerId)
    {
        if (IsActivated && !_allowReactivation) return;
        if (!_playersInRange.Contains(playerId)) return;

        AttemptReadyServerRpc(playerId);
    }

    // ─── Logic Server Quyết Định ──────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void AttemptReadyServerRpc(ulong playerId)
    {
        if (IsActivated && !_allowReactivation) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var client)) return;

        Vector3 playerPos = client.PlayerObject.transform.position;
        bool isHost = playerId == NetworkManager.ServerClientId;

        // Xác minh vị trí, nếu Point bị null thì châm chước bỏ qua check khoảng cách 
        bool nearA = _pointA == null || Vector3.Distance(playerPos, _pointA.position) <= _validDistance;
        bool nearB = _pointB == null || Vector3.Distance(playerPos, _pointB.position) <= _validDistance;

        if (isHost && nearA)
        {
            _playerAReady.Value = true;
            Debug.Log($"[CoopInteractable] {_interactableId} - Host (A) đã Sẵn Sàng!");
        }
        else if (!isHost && nearB)
        {
            _playerBReady.Value = true;
            Debug.Log($"[CoopInteractable] {_interactableId} - Client (B) đã Sẵn Sàng!");
        }
        else
        {
            Debug.Log($"[CoopInteractable] {_interactableId} - Player {playerId} bấm nhưng ĐỨNG SAI VỊ TRÍ.");
        }

        CheckActivationConditions();
    }

    private void CheckActivationConditions()
    {
        if (!IsServer) return;

        if (_playerAReady.Value && _playerBReady.Value)
        {
            Debug.Log($"[CoopInteractable] {_interactableId} - Cả 2 đã sẵn sàng -> KÍCH HOẠT!");
            ServerActivate();

            // Nếu nó là dạng dùng đi dùng lại (toggle), reset trạng thái của người chơi sau khi xong.
            if (_allowReactivation)
            {
                _playerAReady.Value = false;
                _playerBReady.Value = false;
            }
        }
    }

    protected override void OnPlayerExited(ulong clientId)
    {
        base.OnPlayerExited(clientId);

        if (!IsServer) return;

        // Nếu một người bỏ đi khỏi vòng Trigger báo hiệu họ hết sẵn sàng
        bool isHost = clientId == NetworkManager.ServerClientId;
        if (isHost && _playerAReady.Value)
        {
            _playerAReady.Value = false;
            Debug.Log($"[CoopInteractable] {_interactableId} - Host rời khỏi vùng -> Bỏ Readiness.");
        }
        else if (!isHost && _playerBReady.Value)
        {
            _playerBReady.Value = false;
            Debug.Log($"[CoopInteractable] {_interactableId} - Client rời khỏi vùng -> Bỏ Readiness.");
        }
    }

    // ─── Cập nhật Giao diện (EventBus) ────────────────────────────────────────

    private void OnPlayerAReadyChanged(bool prev, bool current)
    {
        HandleReadinessUI(NetworkManager.ServerClientId, prev, current);
    }

    private void OnPlayerBReadyChanged(bool prev, bool current)
    {
        // Client ID thường là 1 (trong tình huống 2 player, 0 là host), nhưng để chính xác thì pass ID tượng trưng.
        // EventBus chỉ mang tính chất kích hoạt UI
        HandleReadinessUI(1, prev, current);
    }

    private void HandleReadinessUI(ulong playerId, bool prev, bool current)
    {
        if (current && !prev)
        {
            // Bật tín hiệu Readiness
            EventBus.RaiseCoopInteractablePlayerReady(_interactableId, playerId);
        }
        else if (!current && prev && !IsActivated)
        {
            // Tắt tín hiệu Readiness (do đi khỏi vùng hoặc fail)
            EventBus.RaiseCoopInteractableReset(_interactableId);
        }
    }
}
