using UnityEngine;

public class Attack2State : PlayerStateBase
{
    private AttackComboController _combo;
    private float _exitTimer = 0f;
    private const float EXIT_DURATION = 0.8f;

    public Attack2State(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter()
    {
        if (_combo == null)
        {
            _combo = Machine.GetComponent<AttackComboController>();
            if (_combo == null)
            {
                Debug.LogError("[Attack2State] AttackComboController không tìm thấy trên Player!");
                Machine.TransitionTo(PlayerStateType.Idle);
                return;
            }
        }

        _combo.OnAttackStart(2);   // Bắt buộc đòn này là 2
        _exitTimer = EXIT_DURATION;

        var rb = Machine.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        Debug.Log("[AttackCombo] Enter Attack2");
    }

    public override void Update()
    {
        _exitTimer -= Time.deltaTime;

        if (Input.AttackPressed)
        {
            _combo.QueueNextAttack();
        }

        if (_combo.ComboWindowOpen && _combo.NextAttackQueued)
        {
            Machine.TransitionTo(PlayerStateType.Attack3);
            return;
        }

        if (_exitTimer <= 0f)
        {
            _combo.ResetCombo();
            Machine.TransitionTo(PlayerStateType.Idle);
        }
    }

    public override void Exit() { }
}
