/// <summary>
/// DeadState — Player đã chết.
/// Không tự chuyển state — chờ RespawnManager gọi TransitionTo(Respawning).
/// </summary>
public class DeadState : PlayerStateBase
{
    public DeadState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Không tự chuyển — RespawnManager gọi Machine.TransitionTo(Respawning)
    }

    public override void Exit() { }
}
