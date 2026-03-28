using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// RespawnManager — Xử lý hồi sinh Player khi nhân vật chết và lưu điểm Checkpoint.
/// Luôn luôn lắng nghe EventBus trên Singleton (để trên Scene Game).
/// </summary>
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _respawnDelay = 3f;

    // Lưu trữ tọa độ hồi sinh hiện tại
    private Vector3 _currentHostSpawnPos;
    private Vector3 _currentClientSpawnPos;

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

    private void OnEnable()
    {
        EventBus.OnCheckpointReached += HandleCheckpointReached;
        EventBus.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        EventBus.OnCheckpointReached -= HandleCheckpointReached;
        EventBus.OnPlayerDied -= HandlePlayerDied;
    }

    private void SetInitialSpawnPoints()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                if (client.ClientId == NetworkManager.ServerClientId)
                    _currentHostSpawnPos = client.PlayerObject.transform.position;
                else
                    _currentClientSpawnPos = client.PlayerObject.transform.position;
            }
        }
        Debug.Log($"[RespawnManager] Khởi tạo vị trí vạch xuất phát: Host ({_currentHostSpawnPos}) | Client ({_currentClientSpawnPos})");
    }

    /// <summary>
    /// Lưu điểm checkpoint mới nhất. Hàm này được trigger trên tất cả các máy tính nhận được EventBus.
    /// </summary>
    private void HandleCheckpointReached(string checkpointId, Vector3 hostSpawnPos, Vector3 clientSpawnPos)
    {
        _currentHostSpawnPos = hostSpawnPos;
        _currentClientSpawnPos = clientSpawnPos;
        Debug.Log($"<color=green>[RespawnManager] LƯU CHECKPOINT THÀNH CÔNG!</color> Trạm: {checkpointId} | Vị trí Host: {hostSpawnPos} | Vị trí Client: {clientSpawnPos}");
    }

    /// <summary>
    /// Khi nhân vật bị chết (HP = 0, rơi trúng gai, vực sâu), bắt đầu chạy routine delay để hồi sinh.
    /// </summary>
    private void HandlePlayerDied(ulong clientId)
    {
        Debug.Log($"<color=red>[RespawnManager] Phát hiện Player {clientId} đã chếttt! Bắt đầu đếm ngược {_respawnDelay} giây...</color>");
        StartCoroutine(RespawnRoutine(clientId));
    }

    private IEnumerator RespawnRoutine(ulong clientId)
    {
        // 1. Chờ vài giây để player kịp load xong hiệu ứng chết (chờ màn hình đen...)
        yield return new WaitForSeconds(_respawnDelay);

        Debug.Log($"[RespawnManager] Đã ngâm xác đủ thời gian! Đang lôi {clientId} dậy...");

        // 2. NGO Kiểm tra quyền Owner. Phải là Owner thì mới tự đổi Transform của chính mình được.
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            NetworkObject netObj = client.PlayerObject;
            if (netObj != null)
            {
                Debug.Log($"[RespawnManager] Tìm thấy Data mạng của thân xác {clientId}!");

                // Chỉ ai là Owner của nhân vật đó thì mới được thực thi phần Move Position khôi phục này.
                if (netObj.IsOwner)
                {
                    var fsm = netObj.GetComponent<PlayerStateMachine>();
                    var health = netObj.GetComponent<PlayerHealth>();
                    var rb = netObj.GetComponent<Rigidbody>();

                    // Chọn tọa độ Spawn theo chủ Owner
                    bool isHost = clientId == NetworkManager.ServerClientId;
                    Vector3 spawnPos = isHost ? _currentHostSpawnPos : _currentClientSpawnPos;

                    Debug.Log($"[RespawnManager] KẾT THÚC HỒI SINH! Đẩy {clientId} (IsHost: {isHost}) về x: {spawnPos.x}, y: {spawnPos.y}");

                    // Tắt vật lý tạm -> Di chuyển tọa độ qua checkpoint -> Đặt lại
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.MovePosition(spawnPos);
                        netObj.transform.position = spawnPos; // Khẳng định triệt để
                    }

                    // Phát Event
                    EventBus.RaisePlayerRespawned(clientId, spawnPos);
                    
                    // Trả HP và Chuyển State (Chuyển nhanh sang Respawning nếu có, rồi gạt cờ Idle)
                    health.RestoreFullHealth();
                    fsm.TransitionTo(PlayerStateType.Respawning);
                    
                    yield return new WaitForSeconds(0.5f);
                    fsm.TransitionTo(PlayerStateType.Idle);
                }
                else
                {
                    Debug.Log($"[RespawnManager] Bỏ qua vì máy này không phải là Owner của nhân vật {clientId} (Tránh giật giật kéo 2 bên).");
                }
            }
            else
            {
                Debug.LogError($"[RespawnManager] Lỗi: PlayerObject của Client {clientId} bị rỗng (null)!");
            }
        }
        else
        {
            Debug.LogError($"[RespawnManager] Lỗi nghiêm trọng: Không tìm thấy Client {clientId} trong mạng!");
        }
    }
}
