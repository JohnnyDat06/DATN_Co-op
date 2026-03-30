using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bridge nhận Animation Events từ Animator (trên Player root)
/// và dispatch xuống các component con.
/// Gắn trên cùng GameObject với Animator.
/// SRS §4.1.2 (T1-7)
/// </summary>
public class PlayerAnimationEventReceiver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Fallback nếu cần bind tay một hitbox cụ thể trong Inspector")]
    [SerializeField] private AttackHitbox _attackHitbox;

    [Tooltip("Gắn vào AttackComboController trên Player root")]
    [SerializeField] private AttackComboController _comboController;

    private readonly List<AttackHitbox> _attackHitboxes = new();

    private void Awake()
    {
        CacheAttackHitboxes();

        if (_comboController == null)
            _comboController = GetComponent<AttackComboController>();

        if (_attackHitboxes.Count == 0)
            Debug.LogWarning("[AnimEventReceiver] AttackHitbox không tìm thấy trong children!");

        if (_comboController == null)
            Debug.LogWarning("[AnimEventReceiver] AttackComboController không tìm thấy!");
    }

    private void CacheAttackHitboxes()
    {
        _attackHitboxes.Clear();

        // Include inactive children vì một trong hai model sẽ bị tắt theo role.
        var hitboxes = GetComponentsInChildren<AttackHitbox>(true);
        foreach (var hitbox in hitboxes)
        {
            if (hitbox != null && !_attackHitboxes.Contains(hitbox))
                _attackHitboxes.Add(hitbox);
        }

        if (_attackHitbox != null && !_attackHitboxes.Contains(_attackHitbox))
            _attackHitboxes.Add(_attackHitbox);

        if (_attackHitboxes.Count > 0)
            _attackHitbox = _attackHitboxes[0];
    }

    // ─── Gọi bởi Animation Event trên Attack Clips ───────────────

    /// <summary>
    /// Kích hoạt hitbox — gọi tại frame cú đấm bắt đầu tiếp xúc.
    /// Animation Event → Function: "EnableHitbox"
    /// </summary>
    public void EnableHitbox()
    {
        for (int i = 0; i < _attackHitboxes.Count; i++)
        {
            _attackHitboxes[i]?.EnableHitbox();
        }
    }

    /// <summary>
    /// Tắt hitbox — gọi tại frame cú đấm kết thúc tiếp xúc.
    /// Animation Event → Function: "DisableHitbox"
    /// </summary>
    public void DisableHitbox()
    {
        for (int i = 0; i < _attackHitboxes.Count; i++)
        {
            _attackHitboxes[i]?.DisableHitbox();
        }
    }

    /// <summary>
    /// Mở combo window — gọi tại 60-70% thời lượng clip Attack1 và Attack2.
    /// Animation Event → Function: "OpenComboWindow"
    /// </summary>
    public void OpenComboWindow()
    {
        _comboController?.OpenComboWindow();
    }
}
