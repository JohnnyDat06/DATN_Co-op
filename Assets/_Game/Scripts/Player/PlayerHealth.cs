using System;
using UnityEngine;

/// <summary>
/// PlayerHealth — Quản lý HP player, implement IDamageable.
/// Khi HP về 0, fire EventBus.OnPlayerDied — không tự respawn.
/// SRS §4.1.3
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private SOPlayerConfig _config;
    [SerializeField] private PlayerStateMachine _fsm;

    /// <summary>Máu hiện tại.</summary>
    public float CurrentHealth { get; private set; }

    /// <summary>Máu tối đa từ SOPlayerConfig.</summary>
    public float MaxHealth => _config.MaxHealth;

    /// <summary>True nếu đã chết.</summary>
    public bool IsDead => CurrentHealth <= 0f;

    /// <summary>Event cục bộ để HUDController lắng nghe (không qua EventBus vì có dữ liệu HP).</summary>
    public event Action<float, float> OnHealthChanged; // (current, max)

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError("[PlayerHealth] SOPlayerConfig chưa được gán trong Inspector!");
        }
        if (_fsm == null)
        {
            _fsm = GetComponent<PlayerStateMachine>();
        }
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>Gây sát thương. Không có hiệu ứng nếu đã dead hoặc amount <= 0.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (IsDead) HandleDeath();
    }

    /// <summary>Hạ HP về 0 ngay lập tức. Dùng bởi DeathZone.</summary>
    public void InstantKill()
    {
        if (IsDead) return;
        CurrentHealth = 0f;
        OnHealthChanged?.Invoke(0f, MaxHealth);
        HandleDeath();
    }

    /// <summary>Khôi phục HP tối đa. Gọi bởi RespawnManager sau hồi sinh.</summary>
    public void RestoreFullHealth()
    {
        CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void HandleDeath()
    {
        // Lấy NetworkObjectId nếu có (NGO), fallback sang GetInstanceID
        ulong clientId = TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj)
            ? netObj.OwnerClientId
            : (ulong)GetInstanceID();

        _fsm.TransitionTo(PlayerStateType.Dead);
        EventBus.RaisePlayerDied(clientId);

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log($"[PlayerHealth] Player {clientId} died.");
#endif
    }
}
