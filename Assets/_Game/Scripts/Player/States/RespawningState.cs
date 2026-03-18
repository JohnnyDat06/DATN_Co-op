/// <summary>
/// RespawningState — Player đang hồi sinh.
/// Không tự chuyển — RespawnManager gọi TransitionTo(Idle) khi xong.
/// </summary>
public class RespawningState : PlayerStateBase
{
    public RespawningState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Không tự chuyển — RespawnManager gọi Machine.TransitionTo(Idle)
    }

    public override void Exit() { }
}
