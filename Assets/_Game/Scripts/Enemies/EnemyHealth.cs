using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Lớp cơ sở (Base Class) chung cho mọi Enemy trong game.
/// Quản lý thanh máu đồng bộ qua mạng (NGO).
/// Áp dụng mẫu thiết kế Template Method: class con tự kiểm soát trạng thái nhưng Health thì base quản lý.
/// </summary>
public abstract class EnemyHealth : NetworkBehaviour, IDamageableEnemy
{
    [Header("Enemy Stats")]
    [SerializeField] protected int _maxHealth = 100;

    // Biến đồng bộ qua mạng. Server có quyền Ghi, mọi Client đều tự động Đọc (Sync).
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // Sự kiện nội bộ trên client để UI thanh máu giật/đỏ lên hoặc AI phản ứng cục bộ
    public event Action<int, int> OnHealthChanged; // (oldValue, newValue)
    public event Action OnDeath;

    public override void OnNetworkSpawn()
    {
        // Khởi tạo máu ban đầu (Chỉ con chủ - Server mới được gán giá trị khởi điểm)
        if (IsServer)
        {
            CurrentHealth.Value = _maxHealth;
        }

        CurrentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int oldVal, int newVal)
    {
        OnHealthChanged?.Invoke(oldVal, newVal);
        
        // Quái hết máu
        if (newVal <= 0 && oldVal > 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kích hoạt từ Hitbox của Player.
    /// Thiết kế theo NGO Client-Authoritative Hit Detection (Client tự dò va chạm -> Request Server trừ máu)
    /// </summary>
    public virtual void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal, ulong instigatorClientId)
    {
        // Tránh quái đã chết vẫn ăn chém
        if (CurrentHealth.Value <= 0) return;

        if (IsServer)
        {
            ApplyDamage(damage, instigatorClientId);
        }
        else
        {
            // Client gọi RPC lên Server báo "Tao chém trúng quái này rồi!"
            // Gửi RequreOwnership = false vì Client KHÔNG LÀ CHỦ của Enemy này
            TakeDamageServerRpc(damage, instigatorClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int damage, ulong instigatorClientId)
    {
        ApplyDamage(damage, instigatorClientId);
    }

    /// <summary>
    /// Cốt lõi logic trừ máu vĩnh viễn (CHỈ CHẠY TRÊN SERVER)
    /// </summary>
    protected virtual void ApplyDamage(int damage, ulong instigatorClientId)
    {
        if (CurrentHealth.Value <= 0) return;

        // Server update state
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damage);
        
        // Cập nhật các AI / Stun / Aggro ở Server
        OnDamagedServerSide(damage, instigatorClientId);
    }

    /// <summary>
    /// Bắt buộc đám Enemy con (Slime, Skeleton, Boss) phải tự override lại mình làm gì khi bị đập.
    /// (Ví dụ: Skeleton thì rụng xương, AI chuyển sang mode Hunt).
    /// </summary>
    protected abstract void OnDamagedServerSide(int damage, ulong instigatorClientId);

    /// <summary>
    /// Xử lý Die.
    /// Bọn đệ có thể Drop Item, phát âm thanh chết, Raggdoll...
    /// </summary>
    protected virtual void Die()
    {
        OnDeath?.Invoke();
        
        if (IsServer) GetComponent<NetworkObject>().Despawn();
    }
}
