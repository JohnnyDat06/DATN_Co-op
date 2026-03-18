/// <summary>
/// WallJumpState — Player nhảy từ tường.
/// Chuyển sang Idle khi chạm đất, Jump khi timer hết.
/// Physics xử lý bởi PlayerController.
/// </summary>
public class WallJumpState : PlayerStateBase
{
    public WallJumpState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Transition logic xử lý bởi PlayerController (CheckGrounded)
    }

    public override void Exit() { }
}
