/// <summary>
/// CrouchIdleState — Player đang ngồi xổm, không di chuyển.
/// Chuyển sang Idle khi nhả Crouch, CrouchWalk khi di chuyển.
/// </summary>
public class CrouchIdleState : PlayerStateBase
{
    public CrouchIdleState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Stand up
        if (!Input.IsCrouching)
        {
            Machine.TransitionTo(PlayerStateType.Idle);
            return;
        }

        // Move while crouching
        if (Input.IsMoving)
        {
            Machine.TransitionTo(PlayerStateType.CrouchWalk);
            return;
        }
    }

    public override void Exit() { }
}
