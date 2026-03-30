using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hitbox cho đòn tấn công. Enable/Disable bằng Animation Event.
/// Detect va chạm với tag "Enemy" hoặc "Player" (tùy game design sau).
/// Chưa gây damage — chỉ detect và ghi nhận. SRS Phase sau sẽ implement IDamageableEnemy.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("Hitbox Config")]
    [Tooltip("Layer mask cho đối tượng có thể bị hit (Chọn Enemy)")]
    [SerializeField] private LayerMask _hitLayer;

    [Tooltip("Bán kính hitbox (dùng OverlapSphere)")]
    [SerializeField] private float _radius = 0.8f;

    [Tooltip("Sát thương tương ứng với đòn 1, 2, 3 trong dãy combo")]
    [SerializeField] private int[] _damagePerAttack = new int[] { 15, 25, 45 };

    // Track đối tượng đã bị hit trong cùng 1 đòn — tránh hit nhiều lần
    private readonly HashSet<Collider> _hitTargets = new HashSet<Collider>();

    private bool _isActive = false;

    // Quản lý chủ sở hữu Netcode
    private Unity.Netcode.NetworkObject _playerNetObj;

    // Event để các system khác lắng nghe (nếu cần xử lý thêm UI, Sound local, combo counter)
    public event System.Action<Collider, int> OnHitDetected; // (target, attackCount)

    [Header("Debug")]
    [SerializeField] private bool _showGizmos = true;

    private void Awake()
    {
        _playerNetObj = GetComponentInParent<Unity.Netcode.NetworkObject>();
    }

    // ─── Gọi bởi Animation Event ────────────────────────────────

    /// <summary>Gọi từ Animation Event khi bắt đầu active frame của cú đấm.</summary>
    public void EnableHitbox()
    {
        _isActive = true;
        _hitTargets.Clear();
        Debug.Log("EnableHitbox");
    }

    /// <summary>Gọi từ Animation Event khi kết thúc active frame.</summary>
    public void DisableHitbox()
    {
        _isActive = false;
        Debug.Log("DisableHitbox");
    }

    // ─── Detect ────────────────────────────────────────────────

    private void FixedUpdate()
    {
        if (!_isActive) return;
        
        if (_playerNetObj != null && !_playerNetObj.IsOwner) return;

        var hits = Physics.OverlapSphere(transform.position, _radius, _hitLayer);
        foreach (var hit in hits)
        {
            if (_hitTargets.Contains(hit)) continue;  // Đã hit trong đòn này
            _hitTargets.Add(hit);

            // 1. Phân luồng các đòn tấn công (Sát thương leo thang)
            var combo  = GetComponentInParent<AttackComboController>();
            int count  = combo != null ? combo.AttackCount : 1;
            
            int damageIndex = Mathf.Clamp(count - 1, 0, _damagePerAttack.Length - 1);
            int damage = _damagePerAttack[damageIndex];

            // 2. Tương tác với hệ thống vật lý và Mạng
            if (hit.TryGetComponent<IDamageableEnemy>(out var dmgObj))
            {
                // Lấy điểm tiếp xúc trên bề mặt collider để làm vị trí VFX máu sau này
                Vector3 hitPoint = hit.ClosestPoint(transform.position); 
                ulong clientId = _playerNetObj != null ? _playerNetObj.OwnerClientId : 0;
                
                dmgObj.TakeDamage(damage, hitPoint, transform.forward, clientId);
            }

            Debug.Log($"[AttackHitbox] Chém trúng: {hit.gameObject.name} | Đòn: {count} | Sát thương: {damage}");
            OnHitDetected?.Invoke(hit, count);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_showGizmos) return;
        Gizmos.color = _isActive ? Color.red : new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
