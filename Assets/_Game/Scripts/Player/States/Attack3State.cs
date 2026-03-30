using UnityEngine;

public class Attack3State : PlayerStateBase
{
    private AttackComboController _combo;
    private float _exitTimer = 0f;
    private const float EXIT_DURATION = 1.3f; // đòn 3 lâu nhất

    public Attack3State(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter()
    {
        if (_combo == null)
        {
            _combo = Machine.GetComponent<AttackComboController>();
            if (_combo == null)
            {
                Debug.LogError("[Attack3State] AttackComboController không tìm thấy trên Player!");
                Machine.TransitionTo(PlayerStateType.Idle);
                return;
            }
        }

        _combo.OnAttackStart(3);   // Bắt buộc đòn này là 3
        _exitTimer = EXIT_DURATION;

        var rb = Machine.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        Debug.Log("[AttackCombo] Enter Attack3");
    }

    public override void Update()
    {
        _exitTimer -= Time.deltaTime;

        // Đòn 3 không có combo tiếp — chỉ chờ kết thúc
        if (_exitTimer <= 0f)
        {
            _combo.ResetCombo();   // Reset AttackCount = 0
            Machine.TransitionTo(PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        _combo.ResetCombo(); // Đảm bảo reset kể cả khi bị interrupt
    }
}
