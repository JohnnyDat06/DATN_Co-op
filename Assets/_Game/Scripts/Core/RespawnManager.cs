using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// RespawnManager — Xử lý hồi sinh Player khi nhân vật chết và lưu điểm Checkpoint.
/// Luôn luôn lắng nghe EventBus trên Singleton (để trên Scene Game).
/// Đã được cập nhật để kế thừa NetworkBehaviour và sử dụng NetworkVariable để đồng bộ vị trí Checkpoint qua mạng.
/// </summary>
public class RespawnManager : NetworkBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _respawnDelay = 3f;

    // Sử dụng NetworkVariable để đồng bộ tọa độ hồi sinh từ Server xuống tất cả Client
    // Điều này đảm bảo khi Client hồi sinh, họ sẽ lấy đúng vị trí mới nhất mà Server đã lưu.
    private readonly NetworkVariable<Vector3> _currentHostSpawnPos = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3> _currentClientSpawnPos = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Chờ một chút để Server kết nối và Player tự load ra xong lúc bắt đầu màn.
        Invoke(nameof(SetInitialSpawnPoints), 2f);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Lắng nghe sự kiện từ EventBus
        EventBus.OnCheckpointReached += HandleCheckpointReached;
        EventBus.OnPlayerDied += HandlePlayerDied;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        EventBus.OnCheckpointReached -= HandleCheckpointReached;
        EventBus.OnPlayerDied -= HandlePlayerDied;
    }

    private void SetInitialSpawnPoints()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                if (client.ClientId == NetworkManager.ServerClientId)
                    _currentHostSpawnPos.Value = client.PlayerObject.transform.position;
                else
                    _currentClientSpawnPos.Value = client.PlayerObject.transform.position;
            }
        }
        Debug.Log($"[RespawnManager] Khởi tạo vị trí vạch xuất phát: Host ({_currentHostSpawnPos.Value}) | Client ({_currentClientSpawnPos.Value})");
    }

    /// <summary>
    /// Lưu điểm checkpoint mới nhất. Chỉ Server mới thực hiện ghi vào NetworkVariable.
    /// </summary>
    private void HandleCheckpointReached(string checkpointId, Vector3 hostSpawnPos, Vector3 clientSpawnPos)
    {
        if (IsServer)
        {
            _currentHostSpawnPos.Value = hostSpawnPos;
            _currentClientSpawnPos.Value = clientSpawnPos;
            Debug.Log($"<color=green>[RespawnManager] SERVER LƯU CHECKPOINT THÀNH CÔNG!</color> Trạm: {checkpointId} | Vị trí Host: {hostSpawnPos} | Vị trí Client: {clientSpawnPos}");
        }
    }

    /// <summary>
    /// Khi nhân vật bị chết, bắt đầu chạy routine delay để hồi sinh.
    /// Sự kiện này được bắn từ PlayerHealth qua ClientRpc nên sẽ chạy trên cả Host và Client.
    /// </summary>
    private void HandlePlayerDied(ulong clientId)
    {
        Debug.Log($"<color=red>[RespawnManager] Phát hiện Player {clientId} đã chết! Bắt đầu đếm ngược {_respawnDelay} giây...</color>");
        StartCoroutine(RespawnRoutine(clientId));
    }

    private IEnumerator RespawnRoutine(ulong clientId)
    {
        // 1. Chờ vài giây để player kịp load xong hiệu ứng chết
        yield return new WaitForSeconds(_respawnDelay);

        Debug.Log($"[RespawnManager] Đã ngâm xác đủ thời gian! Đang lôi {clientId} dậy...");

        // 2. NGO Kiểm tra quyền Owner.
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            NetworkObject netObj = client.PlayerObject;
            if (netObj != null)
            {
                // CHỈ OWNER MỚI ĐƯỢC PHÉP DI CHUYỂN TRANSFORM CỦA CHÍNH MÌNH 
                // (Vì dự án đang dùng ClientNetworkTransform hoặc logic tương tự)
                if (netObj.IsOwner)
                {
                    var fsm = netObj.GetComponent<PlayerStateMachine>();
                    var health = netObj.GetComponent<PlayerHealth>();
                    var rb = netObj.GetComponent<Rigidbody>();

                    // Chọn tọa độ Spawn theo chủ Owner từ NetworkVariable đã đồng bộ
                    bool isHost = clientId == NetworkManager.ServerClientId;
                    Vector3 spawnPos = isHost ? _currentHostSpawnPos.Value : _currentClientSpawnPos.Value;

                    Debug.Log($"[RespawnManager] HỒI SINH OWNER {clientId}! Đẩy về vị trí Checkpoint: {spawnPos}");

                    // Tắt vật lý tạm -> Di chuyển tọa độ qua checkpoint
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.MovePosition(spawnPos);
                        netObj.transform.position = spawnPos;
                    }
                    else
                    {
                        netObj.transform.position = spawnPos;
                    }

                    // Phát Event cục bộ để Camera hoặc UI xử lý
                    EventBus.RaisePlayerRespawned(clientId, spawnPos);
                    
                    // Trả HP và Chuyển State
                    health.RestoreFullHealth();
                    fsm.TransitionTo(PlayerStateType.Respawning);
                    
                    yield return new WaitForSeconds(0.5f);
                    fsm.TransitionTo(PlayerStateType.Idle);
                }
                else
                {
                    Debug.Log($"[RespawnManager] Bỏ qua vì máy này không phải là Owner của nhân vật {clientId}.");
                }
            }
            else
            {
                Debug.LogError($"[RespawnManager] Lỗi: PlayerObject của Client {clientId} bị rỗng!");
            }
        }
    }
}
