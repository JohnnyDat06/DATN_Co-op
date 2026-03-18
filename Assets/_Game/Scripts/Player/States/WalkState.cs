/// <summary>
/// WalkState — Player đang đi bộ.
/// Chuyển sang Idle/Run/CrouchWalk/Jump tùy input.
/// </summary>
public class WalkState : PlayerStateBase
{
    public WalkState(PlayerStateMachine machine, PlayerInputHandler input)
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

        // Crouch while walking
        if (Input.IsCrouching)
        {
            Machine.TransitionTo(PlayerStateType.CrouchWalk);
            return;
        }

        // Sprint
        if (Input.IsSprinting)
        {
            Machine.TransitionTo(PlayerStateType.Run);
            return;
        }

        // Stop
        if (!Input.IsMoving)
        {
            Machine.TransitionTo(PlayerStateType.Idle);
            return;
        }
    }

    public override void Exit() { }
}
