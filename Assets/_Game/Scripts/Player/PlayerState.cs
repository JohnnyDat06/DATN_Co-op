using System;

/// <summary>
/// Enum chứa tất cả trạng thái có thể có của Player.
/// Dùng chung bởi PlayerStateMachine, PlayerController, PlayerAnimator.
/// SRS §4.1.2
/// </summary>
public enum PlayerStateType
{
    Idle,
    Walk,
    Run,
    CrouchIdle,
    CrouchWalk,
    GroundSlide,
    Jump,
    DoubleJump,
    AirGlide,
    WallHang,
    WallJump,
    Dead,
    Respawning,
    DashInAir,
    DashOnGround,
    Attack1,
    Attack2,
    Attack3,
    Knockback
}

/// <summary>
/// Base class cho mọi Player state. Dùng Strategy Pattern.
/// Mỗi state là 1 class riêng kế thừa PlayerStateBase.
/// SRS §3.2
/// </summary>
public abstract class PlayerStateBase
{
    /// <summary>Reference tới PlayerStateMachine để gọi TransitionTo.</summary>
    protected PlayerStateMachine Machine { get; }

    /// <summary>Reference tới PlayerInputHandler để đọc input.</summary>
    protected PlayerInputHandler Input { get; }

    protected PlayerStateBase(PlayerStateMachine machine, PlayerInputHandler input)
    {
        Machine = machine;
        Input   = input;
    }

    /// <summary>Gọi khi Enter state. Setup logic, animation trigger.</summary>
    public abstract void Enter();

    /// <summary>Gọi mỗi frame khi đang ở state này. Kiểm tra điều kiện chuyển state.</summary>
    public abstract void Update();

    /// <summary>Gọi khi Exit state. Cleanup logic.</summary>
    public abstract void Exit();
}
