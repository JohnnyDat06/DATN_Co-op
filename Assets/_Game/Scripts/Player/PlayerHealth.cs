using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerHealth — Quản lý HP player, đồng bộ qua mạng (NGO).
/// Khi HP về 0, chuyển sang DeadState và fire EventBus.OnPlayerDied.
/// </summary>
public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [SerializeField] private SOPlayerConfig _config;
    [SerializeField] private PlayerStateMachine _fsm;

    /// <summary>Biến đồng bộ máu qua mạng. Chỉ Server có quyền ghi.</summary>
    private readonly NetworkVariable<float> _networkCurrentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>Máu hiện tại lấy từ NetworkVariable.</summary>
    public float CurrentHealth => _networkCurrentHealth.Value;

    /// <summary>Máu tối đa từ SOPlayerConfig.</summary>
    public float MaxHealth => _config != null ? _config.MaxHealth : 100f;

    /// <summary>True nếu đã chết.</summary>
    public bool IsDead => _networkCurrentHealth.Value <= 0f;

    /// <summary>Event cục bộ để HUDController lắng nghe.</summary>
    public event Action<float, float> OnHealthChanged; // (current, max)

    private void Awake()
    {
        if (_fsm == null)
        {
            _fsm = GetComponent<PlayerStateMachine>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _networkCurrentHealth.Value = MaxHealth;
        }

        // Đăng ký sự kiện thay đổi máu để cập nhật UI cục bộ
        _networkCurrentHealth.OnValueChanged += HandleHealthChanged;
        
        // Cập nhật lần đầu cho UI
        OnHealthChanged?.Invoke(_networkCurrentHealth.Value, MaxHealth);
    }

    public override void OnNetworkDespawn()
    {
        _networkCurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float oldVal, float newVal)
    {
        OnHealthChanged?.Invoke(newVal, MaxHealth);
        
        if (newVal <= 0f && oldVal > 0f)
        {
            HandleDeath();
        }
    }

    /// <summary>Gây sát thương. Chỉ thực hiện trên Server.</summary>
    public void TakeDamage(float amount)
    {
        if (!IsServer || IsDead || amount <= 0f) return;

        _networkCurrentHealth.Value = Mathf.Max(0f, _networkCurrentHealth.Value - amount);
    }

    /// <summary>Hạ HP về 0 ngay lập tức. Dùng bởi DeathZone.</summary>
    public void InstantKill()
    {
        if (!IsServer || IsDead) return;
        _networkCurrentHealth.Value = 0f;
    }

    /// <summary>Khôi phục HP tối đa. Gọi bởi RespawnManager sau hồi sinh.</summary>
    public void RestoreFullHealth()
    {
        if (IsServer)
        {
            _networkCurrentHealth.Value = MaxHealth;
        }
    }

    private void HandleDeath()
    {
        // Chuyển state máy trạng thái
        if (_fsm != null)
        {
            _fsm.TransitionTo(PlayerStateType.Dead);
        }

        // Thông báo hệ thống (Dùng OwnerClientId để xác định player nào chết)
        EventBus.RaisePlayerDied(OwnerClientId);

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log($"[PlayerHealth] Player {OwnerClientId} died.");
#endif
    }
}
