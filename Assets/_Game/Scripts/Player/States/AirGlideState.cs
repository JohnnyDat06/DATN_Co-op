/// <summary>
/// AirGlideState — Player đang lướt trên không (giữ Jump khi rơi).
/// Chỉ được dùng 1 lần mỗi lần rời đất.
/// Chuyển sang Jump khi nhả Jump, Idle khi chạm đất.
/// Physics xử lý bởi PlayerController.
/// </summary>
public class AirGlideState : PlayerStateBase
{
    public AirGlideState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Transition logic xử lý bởi PlayerController (HandleAirGlide, CheckGrounded)
    }

    public override void Exit() { }
}
