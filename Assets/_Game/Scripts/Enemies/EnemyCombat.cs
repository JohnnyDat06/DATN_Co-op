using UnityEngine;
using Unity.Netcode;
using Unity.Behavior;
using System.Collections.Generic;
using System;

/// <summary>
/// EnemyCombat — Hệ thống chiến đấu tổng hợp cho Enemy.
/// Hỗ trợ cả tấn công cận chiến (Hitbox) và tầm xa (Projectile).
/// Đảm bảo quái vật luôn xoay mặt về phía người chơi khi đang combat.
/// </summary>
public class EnemyCombat : NetworkBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Mục tiêu hiện tại (Có thể gán thủ công hoặc qua AI Action)")]
    public GameObject Target;
    
    [Tooltip("Trạng thái phát hiện đồng bộ mạng")]
    public NetworkVariable<bool> IsDetected = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Blackboard References (Optional)")]
    [Tooltip("Dùng để debug hoặc tự động lấy nếu gán trong Inspector")]
    [SerializeReference] public BlackboardVariable<GameObject> BlackboardTarget;
    [SerializeReference] public BlackboardVariable<bool> BlackboardIsDetected;

    [Header("Combat Settings")]
    [SerializeField] private float _rotationSpeed = 360f;

    [Header("Melee Settings")]
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _radius = 1.0f;
    [SerializeField] private int _damage = 20;

    [Header("Ranged Settings")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    private float _activeTimer = 0f;
    private readonly HashSet<Collider> _hitHistory = new HashSet<Collider>();

    private void Update()
    {
        if (!IsServer) return;

        // Ưu tiên lấy từ Blackboard nếu Target đang null
        UpdateTargetFromBlackboard();

        if (IsDetected.Value && Target != null)
        {
            RotateTowardsTarget(Target);
        }

        if (_activeTimer > 0)
        {
            _activeTimer -= Time.deltaTime;
            CheckForMeleeTargets();
        }
    }

    private void UpdateTargetFromBlackboard()
    {
        if (BlackboardTarget != null && BlackboardTarget.Value != null)
        {
            Target = BlackboardTarget.Value;
        }
        
        if (BlackboardIsDetected != null)
        {
            // Chỉ gán khi giá trị thay đổi để tối ưu băng thông mạng và đảm bảo trigger OnValueChanged
            if (IsDetected.Value != BlackboardIsDetected.Value)
            {
                IsDetected.Value = BlackboardIsDetected.Value;
                Debug.Log($"[EnemyCombat] Detection state changed to: {IsDetected.Value}");
            }
        }
    }

    /// <summary>
    /// Gán mục tiêu chiến đấu trực tiếp (Dùng bởi AI Action).
    /// </summary>
    public void SetCombatTarget(GameObject target, bool detected)
    {
        Target = target;
        IsDetected.Value = detected;
    }

    private void RotateTowardsTarget(GameObject target)
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    #region Melee
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
            }
        }
    }
    #endregion

    #region Ranged
    public void FireProjectile()
    {
        if (!IsServer) return;

        // Re-check target từ blackboard lần cuối trước khi bắn
        UpdateTargetFromBlackboard();

        Debug.Log($"[EnemyCombat] FireProjectile. Target: {(Target != null ? Target.name : "NULL")}");

        if (_projectilePrefab == null || Target == null) return;

        Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position + transform.forward + Vector3.up * 1.5f;
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
    }
}
