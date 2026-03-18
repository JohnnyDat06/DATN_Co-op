/// <summary>
/// Interface cho mọi object có thể nhận sát thương.
/// Implement bởi PlayerHealth.
/// SRS §4.1.3 · §11.3
/// </summary>
public interface IDamageable
{
    /// <summary>Máu hiện tại.</summary>
    float CurrentHealth { get; }

    /// <summary>Máu tối đa.</summary>
    float MaxHealth { get; }

    /// <summary>True nếu đã chết (HP <= 0).</summary>
    bool IsDead { get; }

    /// <summary>Gây sát thương. Không có hiệu ứng nếu đã dead.</summary>
    void TakeDamage(float amount);

    /// <summary>Hạ HP về 0 ngay lập tức. Dùng bởi DeathZone.</summary>
    void InstantKill();

    /// <summary>Khôi phục HP tối đa. Gọi bởi RespawnManager sau hồi sinh.</summary>
    void RestoreFullHealth();
}
