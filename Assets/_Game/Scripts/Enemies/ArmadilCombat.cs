using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// ArmadilCombat — Hệ thống chiến đấu cận chiến cho quái Armadil.
/// Kế thừa EnemyCombatBase, sử dụng OverlapSphere để phát hiện va chạm
/// và gây sát thương trong vùng bán kính khi animation event kích hoạt.
/// </summary>
public class ArmadilCombat : EnemyCombatBase
{
    #region Config
    [Header("Melee Config (ScriptableObject)")]
    [SerializeField] private SOEnemyMeleeAttackConfig _attackConfig;
    #endregion

    #region VFX
    [Header("VFX Settings")]
    [Tooltip("Gán Particle System của hiệu ứng tấn công vào đây (đối tượng con đã có sẵn trên quái)")]
    [SerializeField] private ParticleSystem _attackVFX;
    #endregion

    #region Internal State
    private float _activeTimer = 0f;
    private readonly HashSet<Collider> _hitHistory = new HashSet<Collider>();
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        
        // Đảm bảo VFX tắt lúc đầu
        if (_attackVFX != null)
        {
            _attackVFX.Stop();
        }
    }
    #endregion

    #region Combat Update (Server-only)
    /// <summary>
    /// Mỗi frame kiểm tra OverlapSphere nếu đang trong thời gian active (sau khi Animation Event gọi TriggerAttack).
    /// </summary>
    protected override void OnCombatUpdate()
    {
        if (_activeTimer > 0)
        {
            _activeTimer -= Time.deltaTime;
            CheckForMeleeTargets();
        }
    }
    #endregion

    #region Animation Event API
    /// <summary>
    /// Gọi từ Animation Event để bắt đầu đòn tấn công cận chiến.
    /// Kích hoạt OverlapSphere check trong khoảng thời gian duration.
    /// </summary>
    /// <param name="duration">Thời gian active frame của đòn đánh (giây)</param>
    public void TriggerAttack(float duration)
    {
        if (!IsServer) return;
        _activeTimer = duration;
        _hitHistory.Clear();

        // Kích hoạt hiệu ứng trên toàn bộ các máy khách
        PlayAttackVFXClientRpc();
    }
    #endregion

    #region VFX Network
    [ClientRpc]
    private void PlayAttackVFXClientRpc()
    {
        if (_attackVFX != null)
        {
            _attackVFX.Stop(); // Đảm bảo restart nếu đang chạy dở
            _attackVFX.Play();
        }

        // Phát âm thanh tấn công
        if (_attackConfig != null && _attackConfig.attackSFX != null)
        {
            AudioManager.Instance.PlaySFX(_attackConfig.attackSFX, transform.position);
        }
    }
    #endregion

    #region Melee Logic
    private void CheckForMeleeTargets()
    {
        if (_attackConfig == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _attackConfig.radius, _attackConfig.targetLayer);
        foreach (var hit in hits)
        {
            if (_hitHistory.Contains(hit)) continue;
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_attackConfig.damage);
                _hitHistory.Add(hit);
            }
        }
    }
    #endregion

    #region Debug Gizmos
    private void OnDrawGizmos()
    {
        if (_activeTimer > 0 && _attackConfig != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackConfig.radius);
        }
    }
    #endregion
}
