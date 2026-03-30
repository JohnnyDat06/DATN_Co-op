using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hitbox cho đòn tấn công. Enable/Disable bằng Animation Event.
/// Detect va chạm với tag "Enemy" hoặc "Player" (tùy game design sau).
/// Chưa gây damage — chỉ detect và ghi nhận. SRS Phase sau sẽ implement IDamageable.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("Hitbox Config")]
    [Tooltip("Layer mask cho đối tượng có thể bị hit")]
    [SerializeField] private LayerMask _hitLayer;

    [Tooltip("Bán kính hitbox (dùng OverlapSphere)")]
    [SerializeField] private float _radius = 0.8f;

    // Track đối tượng đã bị hit trong cùng 1 đòn — tránh hit nhiều lần
    private readonly HashSet<Collider> _hitTargets = new HashSet<Collider>();

    private bool _isActive = false;

    // Event để các system khác lắng nghe (damage system ở phase sau)
    public event System.Action<Collider, int> OnHitDetected; // (target, attackCount)

    [Header("Debug")]
    [SerializeField] private bool _showGizmos = true;

    // ─── Gọi bởi Animation Event ────────────────────────────────

    /// <summary>Gọi từ Animation Event khi bắt đầu active frame của cú đấm.</summary>
    public void EnableHitbox()
    {
        _isActive = true;
        _hitTargets.Clear();
    }

    /// <summary>Gọi từ Animation Event khi kết thúc active frame.</summary>
    public void DisableHitbox()
    {
        _isActive = false;
    }

    // ─── Detect ────────────────────────────────────────────────

    private void FixedUpdate()
    {
        if (!_isActive) return;

        var hits = Physics.OverlapSphere(transform.position, _radius, _hitLayer);
        foreach (var hit in hits)
        {
            if (_hitTargets.Contains(hit)) continue;  // Đã hit trong đòn này
            _hitTargets.Add(hit);

            // Lấy attackCount từ AttackComboController
            var combo  = GetComponentInParent<AttackComboController>();
            int count  = combo != null ? combo.AttackCount : 0;

            Debug.Log($"[AttackHitbox] Hit: {hit.gameObject.name} | AttackCount: {count}");
            OnHitDetected?.Invoke(hit, count);

            // TODO Phase sau: hit.TryGetComponent<IDamageable>(out var dmg) → dmg.TakeDamage(...)
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_showGizmos) return;
        Gizmos.color = _isActive ? Color.red : new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
