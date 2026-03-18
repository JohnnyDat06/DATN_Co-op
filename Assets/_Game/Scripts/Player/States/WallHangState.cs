/// <summary>
/// WallHangState — Player bám tường (chỉ active khi WallClimbEnabled = true).
/// Chuyển sang WallJump khi nhấn Jump, Jump khi rời tường.
/// Physics xử lý bởi PlayerController.
/// </summary>
public class WallHangState : PlayerStateBase
{
    public WallHangState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Transition logic xử lý bởi PlayerController (HandleWallClimb)
    }

    public override void Exit() { }
}
