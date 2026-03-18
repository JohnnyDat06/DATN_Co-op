using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerStateMachine — quản lý trạng thái Player.
/// Không Singleton — gắn trực tiếp lên Player prefab.
/// Entry point duy nhất để đổi state: TransitionTo().
/// SRS §4.1.2 · §3.2
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;

    private Dictionary<PlayerStateType, PlayerStateBase> _states;
    private PlayerStateBase _currentState;

    /// <summary>Trạng thái hiện tại của Player (read-only).</summary>
    public PlayerStateType CurrentStateType { get; private set; }

    /// <summary>
    /// Chỉ active khi Player đang trong MountainPlatformerZone (Màn 3).
    /// Default = false. CameraZoneTrigger set giá trị này.
    /// </summary>
    public bool WallClimbEnabled { get; set; } = false;

    /// <summary>Đăng ký để nhận callback khi state thay đổi.</summary>
    public event Action<PlayerStateType, PlayerStateType> OnStateChanged; // (from, to)

    private void Awake()
    {
        if (_inputHandler == null)
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
        }

        // Khởi tạo tất cả 13 state — inject this + inputHandler
        _states = new Dictionary<PlayerStateType, PlayerStateBase>
        {
            { PlayerStateType.Idle,         new IdleState(this, _inputHandler) },
            { PlayerStateType.Walk,         new WalkState(this, _inputHandler) },
            { PlayerStateType.Run,          new RunState(this, _inputHandler) },
            { PlayerStateType.CrouchIdle,   new CrouchIdleState(this, _inputHandler) },
            { PlayerStateType.CrouchWalk,   new CrouchWalkState(this, _inputHandler) },
            { PlayerStateType.GroundSlide,  new GroundSlideState(this, _inputHandler) },
            { PlayerStateType.Jump,         new JumpState(this, _inputHandler) },
            { PlayerStateType.DoubleJump,   new DoubleJumpState(this, _inputHandler) },
            { PlayerStateType.AirGlide,     new AirGlideState(this, _inputHandler) },
            { PlayerStateType.WallHang,     new WallHangState(this, _inputHandler) },
            { PlayerStateType.WallJump,     new WallJumpState(this, _inputHandler) },
            { PlayerStateType.Dead,         new DeadState(this, _inputHandler) },
            { PlayerStateType.Respawning,   new RespawningState(this, _inputHandler) },
        };

        CurrentStateType = PlayerStateType.Idle;
        _currentState = _states[PlayerStateType.Idle];
        _currentState.Enter();
    }

    private void Update()
    {
        _currentState?.Update();
    }

    /// <summary>
    /// Chuyển sang state mới. Gọi Exit() state cũ, Enter() state mới.
    /// Guard: không cho transition vào chính state đang có.
    /// </summary>
    public void TransitionTo(PlayerStateType newState)
    {
        if (newState == CurrentStateType) return;

        var from = CurrentStateType;
        _currentState.Exit();
        CurrentStateType = newState;
        _currentState = _states[newState];
        _currentState.Enter();
        OnStateChanged?.Invoke(from, newState);

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log($"[PlayerFSM] {from} → {newState}");
#endif
    }
}
