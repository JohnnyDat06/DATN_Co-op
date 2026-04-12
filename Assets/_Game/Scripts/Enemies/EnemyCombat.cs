using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// EnemyCombat — Hệ thống chiến đấu tổng hợp cho Enemy.
/// Hỗ trợ cả tấn công cận chiến (Hitbox) và tầm xa (Projectile).
/// Đảm bảo quái vật luôn xoay mặt về phía người chơi khi đang combat.
/// </summary>
public class EnemyCombat : NetworkBehaviour
{
    [Header("Detection & Strategy")]
    [Tooltip("Trạng thái phát hiện mục tiêu")]
    public bool IsDetected = false;
    
    [Tooltip("Mục tiêu hiện tại (Player)")]
    public GameObject Target;

    [Tooltip("Tốc độ xoay khi combat")]
    [SerializeField] private float _rotationSpeed = 360f;

    [Header("Melee Settings (Cận chiến)")]
    [Tooltip("Layer chứa Player")]
    [SerializeField] private LayerMask _targetLayer;
    
    [Tooltip("Bán kính vùng quét của Hitbox")]
    [SerializeField] private float _radius = 1.0f;
    
    [Tooltip("Sát thương cận chiến")]
    [SerializeField] private int _damage = 20;

    [Header("Ranged Settings (Bắn xa)")]
    [Tooltip("Prefab của viên đạn")]
    [SerializeField] private GameObject _projectilePrefab;
    
    [Tooltip("Điểm bắn đạn")]
    [SerializeField] private Transform _firePoint;

    private float _activeTimer = 0f;
    private readonly HashSet<Collider> _hitHistory = new HashSet<Collider>();

    private void Update()
    {
        if (!IsServer) return;

        // 1. Luôn xoay mặt về phía Target nếu bị phát hiện
        if (IsDetected && Target != null)
        {
            RotateTowardsTarget();
        }

        // 2. Logic quét Hitbox cận chiến khi timer còn hiệu lực
        if (_activeTimer > 0)
        {
            _activeTimer -= Time.deltaTime;
            CheckForMeleeTargets();
        }
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;
        direction.y = 0; // Chỉ xoay quanh trục Y

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    #region Melee Logic
    /// <summary>
    /// Kích hoạt hitbox cận chiến trong một khoảng thời gian.
    /// Gọi hàm này từ Animation Event.
    /// </summary>
    public void TriggerAttack(float duration)
    {
        if (!IsServer) return;
        _activeTimer = duration;
        _hitHistory.Clear();
    }

    private void CheckForMeleeTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _targetLayer);
        foreach (var hit in hits)
        {
            if (_hitHistory.Contains(hit)) continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
                _hitHistory.Add(hit);
                Debug.Log($"[EnemyCombat] Melee hit on {hit.name} for {_damage} damage.");
            }
        }
    }
    #endregion

    #region Ranged Logic
    /// <summary>
    /// Bắn đạn tầm xa hướng về phía Target.
    /// Gọi hàm này từ Animation Event.
    /// </summary>
    public void FireProjectile()
    {
        // Kiểm tra an toàn để không làm hỏng quái chỉ có cận chiến
        if (!IsServer || _projectilePrefab == null || Target == null) return;

        Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position + transform.forward + Vector3.up * 1.5f;
        
        // Bắn vào phần thân Player (Y + 1.5m)
        Vector3 targetPos = Target.transform.position + Vector3.up * 1.5f;
        Vector3 fireDir = (targetPos - spawnPos).normalized;

        GameObject projectile = Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(fireDir));
        
        if (projectile.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (_activeTimer > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
        else
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
