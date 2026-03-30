using System;
using UnityEngine;

/// <summary>
/// Quản lý combo window và attack count.
/// Gắn trên Player prefab, được inject vào Attack States.
/// SRP: AttackStates chỉ gọi methods ở đây — không tự track timing.
/// </summary>
public class AttackComboController : MonoBehaviour
{
    [Header("Timing Config")]
    [Tooltip("Thời gian combo window mở sau khi animation bắt đầu (giây)")]
    [SerializeField] private float _comboWindowDuration = 0.6f;

    // Runtime state
    public int  AttackCount     { get; private set; } = 0;  // 0=không attack, 1/2/3=đòn hiện tại
    public bool ComboWindowOpen { get; private set; } = false;
    public bool NextAttackQueued { get; private set; } = false; // nhấn trong window

    private float _comboWindowTimer = 0f;
    //private bool  _isInCooldown     = false;

    public const int MAX_COMBO = 3;

    private void Update()
    {
        if (ComboWindowOpen)
        {
            _comboWindowTimer -= Time.deltaTime;
            if (_comboWindowTimer <= 0f)
            {
                CloseComboWindow();
            }
        }
    }

    /// <summary>
    /// Gọi khi bắt đầu 1 đòn attack. Thiết lập AttackCount, đóng ComboWindow.
    /// </summary>
    public void OnAttackStart(int comboIndex)
    {
        AttackCount      = Mathf.Clamp(comboIndex, 1, MAX_COMBO);
        NextAttackQueued = false;
        ComboWindowOpen  = false; // đóng window cũ, sẽ mở lại sau
    }

    /// <summary>
    /// Gọi từ Animation Event tại thời điểm animation cho phép combo.
    /// Mở cửa sổ nhận input tiếp theo.
    /// </summary>
    public void OpenComboWindow()
    {
        if (AttackCount >= MAX_COMBO) return; // đòn 3 không có window tiếp

        ComboWindowOpen  = true;
        _comboWindowTimer = _comboWindowDuration;
        // KHÔNG clear NextAttackQueued ở đây, cho phép người chơi ấn sớm trước khi window mở
    }

    /// <summary>
    /// Gọi khi player nhấn Attack trong combo window.
    /// Queue đòn tiếp theo — State sẽ xử lý transition.
    /// </summary>
    public void QueueNextAttack()
    {
        // Bỏ điều kiện if (!ComboWindowOpen) 
        // để hỗ trợ pre-queue (nhấp sớm vẫn ghi nhận)
        NextAttackQueued = true;
    }

    /// <summary>
    /// Gọi khi Attack State kết thúc và chuyển về Idle.
    /// Reset AttackCount về 0.
    /// </summary>
    public void ResetCombo()
    {
        AttackCount      = 0;
        ComboWindowOpen  = false;
        NextAttackQueued = false;
        _comboWindowTimer = 0f;
    }

    private void CloseComboWindow()
    {
        ComboWindowOpen   = false;
        _comboWindowTimer = 0f;
        // Nếu không có input queue → PlayerFSM về Idle (Attack State tự check)
    }
}
