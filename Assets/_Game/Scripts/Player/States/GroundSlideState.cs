/// <summary>
/// GroundSlideState — Player trượt khi đang Run + Crouch.
/// Kết thúc khi timer hết hoặc nhả Crouch.
/// Logic slide timer xử lý bởi PlayerController.
/// </summary>
public class GroundSlideState : PlayerStateBase
{
    public GroundSlideState(PlayerStateMachine machine, PlayerInputHandler input)
        : base(machine, input) { }

    public override void Enter() { }

    public override void Update()
    {
        // Slide logic (timer, force) xử lý bởi PlayerController.HandleGroundSlide()
        // State transition cũng do PlayerController quyết định khi timer hết
    }

    public override void Exit() { }
}
