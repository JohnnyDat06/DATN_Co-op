using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Hitbox tấn công cho Enemy. 
/// Kế thừa NetworkBehaviour để đồng bộ IsServer chính xác.
/// Có thể gọi TriggerAttack(duration) từ Animation Event.
/// </summary>
public class EnemyAttackHitbox : NetworkBehaviour
{
    [Header("Hitbox Settings")]
    [Tooltip("Layer chứa Player")]
    [SerializeField] private LayerMask _targetLayer;
    
    [Tooltip("Bán kính vùng quét")]
    [SerializeField] private float _radius = 1.0f;
    
    [Tooltip("Sát thương gây ra")]
    [SerializeField] private int _damage = 20;

    private float _activeTimer = 1.5f;
    private readonly HashSet<Collider> _hitHistory = new HashSet<Collider>();

    /// <summary>
    /// Kích hoạt hitbox trong một khoảng thời gian.
    /// Gọi hàm này từ Animation Event.
    /// </summary>
    /// <param name="duration">Thời gian (giây) hitbox tồn tại.</param>
    public void TriggerAttack(float duration)
    {
        // Chỉ Server mới được phép kích hoạt logic gây sát thương
        if (!IsServer) return;

        _activeTimer = duration;
        _hitHistory.Clear();
    }

    private void Update()
    {
        // Chỉ chạy logic quét mục tiêu trên Server khi timer còn hiệu lực
        if (!IsServer || _activeTimer <= 0) return;

        _activeTimer -= Time.deltaTime;
        CheckForTargets();
    }

    private void CheckForTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _targetLayer);
        foreach (var hit in hits)
        {
            if (_hitHistory.Contains(hit)) continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                // Gọi trực tiếp hàm TakeDamage trên Server
                damageable.TakeDamage(_damage);
                _hitHistory.Add(hit);
                
                Debug.Log($"[EnemyAttackHitbox] Server detected hit on {hit.name} for {_damage} damage.");
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_activeTimer > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
        else
        {
            // Hiển thị vùng quét mờ để dễ debug trong Editor
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
