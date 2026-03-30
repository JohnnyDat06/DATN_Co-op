/// <summary>
/// IdleState — Player đứng yên, không di chuyển.
/// Chuyển sang Walk/Run/CrouchIdle/Jump tùy input.
/// </summary>
public class IdleState : PlayerStateBase
{
    public IdleState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Jump
        if (Input.JumpPressed)
        {
            Machine.TransitionTo(PlayerStateType.Jump);
            return;
        }

        // Crouch
        if (Input.IsCrouching)
        {
            Machine.TransitionTo(PlayerStateType.CrouchIdle);
            return;
        }

        // Movement
        if (Input.IsMoving)
        {
            Machine.TransitionTo(Input.IsSprinting
                ? PlayerStateType.Run
                : PlayerStateType.Walk);
            return;
        }

        // Attack
        if (Input.AttackPressed && Machine.GetComponent<PlayerController>().IsGrounded)
        {
            Machine.TransitionTo(PlayerStateType.Attack1);
            return;
        }
    }

    public override void Exit() { }
}
