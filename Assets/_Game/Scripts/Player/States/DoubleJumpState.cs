/// <summary>
/// DoubleJumpState — Player đang nhảy lần 2.
/// Chuyển sang Idle khi chạm đất, WallHang khi chạm tường.
/// Physics xử lý bởi PlayerController.
/// </summary>
public class DoubleJumpState : PlayerStateBase
{
    public DoubleJumpState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Transition logic xử lý bởi PlayerController (CheckGrounded, HandleWallClimb)
    }

    public override void Exit() { }
}
