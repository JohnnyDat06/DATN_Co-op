/// <summary>
/// DashInAirState — Player thực hiện dash khi đang ở trên không.
/// Physics xử lý bởi PlayerController.HandleDashInAir().
/// </summary>
public class DashInAirState : PlayerStateBase
{
    public DashInAirState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Transition logic xử lý bởi PlayerController (CheckGrounded, HandleDashInAir)
    }

    public override void Exit() { }
}
