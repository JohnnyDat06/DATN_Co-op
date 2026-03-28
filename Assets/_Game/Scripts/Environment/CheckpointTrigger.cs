using Unity.Netcode;
using UnityEngine;

/// <summary>
/// CheckpointTrigger — Kích hoạt Checkpoint khi 1 player đi qua.
/// Bắn sự kiện kèm theo vị trí Host và Client để RespawnManager lưu lại.
/// Mặc định chỉ Server (Host) mới kích hoạt trigger để tránh trùng lặp.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    private string _checkpointId;
    [Header("Spawn Points")]
    [SerializeField] private Transform _hostSpawnPoint;
    [SerializeField] private Transform _clientSpawnPoint;
    
    // Đảm bảo chỉ trigger 1 lần duy nhất cho mỗi điểm
    private bool _isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;
        
        // Chỉ Server mới xác nhận Checkpoint để tránh gọi sự kiện nhiều lần rác
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

        if (other.CompareTag("Player"))
        {
            _isTriggered = true;
            
            // Nếu designer quên gắn Transform vào inspector, lấy biến GameObject hiện tại làm gốc
            Vector3 hostPos = _hostSpawnPoint != null ? _hostSpawnPoint.position : transform.position + Vector3.right;
            Vector3 clientPos = _clientSpawnPoint != null ? _clientSpawnPoint.position : transform.position + Vector3.left;

            // Bắn event đi khắp toàn bộ Server (và có thể trigger Client)
            EventBus.RaiseCheckpointReached(_checkpointId, hostPos, clientPos);
            
            // Có thể play animation hoặc fx đánh dấu đã Checkpoint ở đây (tùy game)
            Debug.Log($"[CheckpointTrigger] Reached: {_checkpointId}");
        }
    }
}
