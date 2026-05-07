using UnityEngine;
using Unity.Netcode;

/// <summary>
/// OwlCombat — Hệ thống chiến đấu tầm xa cho quái Owl.
/// Kế thừa EnemyCombatBase, bắn projectile (WindProjectile) về phía mục tiêu.
/// </summary>
public class OwlCombat : EnemyCombatBase
{
    #region Config
    [Header("Ranged Config (ScriptableObject)")]
    [SerializeField] private SOEnemyRangedAttackConfig _attackConfig;
    #endregion

    #region Fire Point
    [Header("Fire Point")]
    [Tooltip("Vị trí spawn đạn (nếu để trống sẽ dùng transform.position + offset)")]
    [SerializeField] private Transform _firePoint;
    #endregion

    #region Animation Event API
    /// <summary>
    /// Gọi từ Animation Event để bắn đạn tầm xa.
    /// Server spawn projectile NetworkObject và hướng về phía Target.
    /// </summary>
    public void FireProjectile()
    {
        if (!IsServer) return;

        // Re-check target từ blackboard lần cuối trước khi bắn
        UpdateTargetFromBlackboard();

        if (_attackConfig == null || _attackConfig.projectilePrefab == null || Target == null) return;

        Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position + transform.forward + Vector3.up * 1.5f;
        Vector3 targetPos = Target.transform.position + Vector3.up * 1.5f;
        Vector3 fireDir = (targetPos - spawnPos).normalized;

        GameObject projectile = Object.Instantiate(_attackConfig.projectilePrefab, spawnPos, Quaternion.LookRotation(fireDir));
        if (projectile.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
    }
    #endregion
}
