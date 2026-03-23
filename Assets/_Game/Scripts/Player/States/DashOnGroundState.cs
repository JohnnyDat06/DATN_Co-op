/// <summary>
/// DashOnGroundState — Player thực hiện dash khi đang ở mặt đất.
/// Physics xử lý bởi PlayerController.HandleDash().
/// </summary>
public class DashOnGroundState : PlayerStateBase
{
    public DashOnGroundState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }
    public override void Update() { }
    public override void Exit() { }
}
