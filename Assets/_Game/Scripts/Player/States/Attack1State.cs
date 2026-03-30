using UnityEngine;

public class Attack1State : PlayerStateBase
{
    private AttackComboController _combo;
    private float _exitTimer = 0f;
    private const float EXIT_DURATION = 0.7f; // tổng thời gian tối đa của đòn 1

    public Attack1State(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter()
    {
        // Lazy init — đảm bảo component đã tồn tại trên GameObject
        if (_combo == null)
        {
            _combo = Machine.GetComponent<AttackComboController>();
            if (_combo == null)
            {
                Debug.LogError("[Attack1State] AttackComboController không tìm thấy trên Player!");
                Machine.TransitionTo(PlayerStateType.Idle);
                return;
            }
        }

        _combo.OnAttackStart(1);   // Bắt buộc đòn này là 1
        _exitTimer = EXIT_DURATION;

        // Dừng di chuyển khi tấn công
        var rb = Machine.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        Debug.Log("[AttackCombo] Enter Attack1");
    }

    public override void Update()
    {
        _exitTimer -= Time.deltaTime;

        // Cho phép queue trước/trong lúc mở combo
        if (Input.AttackPressed)
        {
            _combo.QueueNextAttack();
        }

        // Nếu combo window đang mở VÀ có ý định đánh tiếp -> Chuyển đòn NGAY
        if (_combo.ComboWindowOpen && _combo.NextAttackQueued)
        {
            Machine.TransitionTo(PlayerStateType.Attack2);
            return;
        }

        // Tự động về Idle nếu hết thời gian thoát (không nhận được attack/combo window đã đóng lại)
        if (_exitTimer <= 0f)
        {
            _combo.ResetCombo();
            Machine.TransitionTo(PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        // Không reset combo ở đây — AttackCount giữ nguyên để Attack2 biết đang ở đòn nào
    }
}
