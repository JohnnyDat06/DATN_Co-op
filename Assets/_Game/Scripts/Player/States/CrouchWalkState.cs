/// <summary>
/// CrouchWalkState — Player đang ngồi xổm và di chuyển.
/// Chuyển sang CrouchIdle khi hết input, Walk khi nhả Crouch.
/// </summary>
public class CrouchWalkState : PlayerStateBase
{
    public CrouchWalkState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Stand up
        if (!Input.IsCrouching)
        {
            Machine.TransitionTo(PlayerStateType.Walk);
            return;
        }

        // Stop moving
        if (!Input.IsMoving)
        {
            Machine.TransitionTo(PlayerStateType.CrouchIdle);
            return;
        }
    }

    public override void Exit() { }
}
