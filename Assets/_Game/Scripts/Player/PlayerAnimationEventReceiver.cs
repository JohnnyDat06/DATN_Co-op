using UnityEngine;

/// <summary>
/// Bridge nhận Animation Events từ Animator (trên Player root)
/// và dispatch xuống các component con.
/// Gắn trên cùng GameObject với Animator.
/// SRS §4.1.2 (T1-7)
/// </summary>
public class PlayerAnimationEventReceiver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Gắn vào AttackHitbox trên child AttackHitboxRoot")]
    [SerializeField] private AttackHitbox _attackHitbox;

    [Tooltip("Gắn vào AttackComboController trên Player root")]
    [SerializeField] private AttackComboController _comboController;

    private void Awake()
    {
        // Auto-find nếu chưa gán trong Inspector
        if (_attackHitbox == null)
            _attackHitbox = GetComponentInChildren<AttackHitbox>();

        if (_comboController == null)
            _comboController = GetComponent<AttackComboController>();

        if (_attackHitbox == null)
            Debug.LogWarning("[AnimEventReceiver] AttackHitbox không tìm thấy trong children!");

        if (_comboController == null)
            Debug.LogWarning("[AnimEventReceiver] AttackComboController không tìm thấy!");
    }

    // ─── Gọi bởi Animation Event trên Attack Clips ───────────────

    /// <summary>
    /// Kích hoạt hitbox — gọi tại frame cú đấm bắt đầu tiếp xúc.
    /// Animation Event → Function: "EnableHitbox"
    /// </summary>
    public void EnableHitbox()
    {
        _attackHitbox?.EnableHitbox();
    }

    /// <summary>
    /// Tắt hitbox — gọi tại frame cú đấm kết thúc tiếp xúc.
    /// Animation Event → Function: "DisableHitbox"
    /// </summary>
    public void DisableHitbox()
    {
        _attackHitbox?.DisableHitbox();
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
