/// <summary>
/// JumpState — Player đang nhảy lần 1.
/// Chuyển sang DoubleJump/AirGlide/WallHang/Idle tùy điều kiện.
/// Physics xử lý bởi PlayerController.
/// </summary>
public class JumpState : PlayerStateBase
{
    public JumpState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Transition logic xử lý bởi PlayerController (CheckGrounded, HandleJump, HandleAirGlide, HandleWallClimb)
        // vì cần physics data (isGrounded, velocity, raycast) mà state không có
    }

    public override void Exit() { }
}
