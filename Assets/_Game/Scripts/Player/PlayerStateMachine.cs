using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerStateMachine — quản lý trạng thái Player.
/// Đồng bộ State qua NetworkVariable để proxy/client có thể chạy Animation theo Host.
/// SRS §4.1.2 · §3.2
/// </summary>
public class PlayerStateMachine : NetworkBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;

    private Dictionary<PlayerStateType, PlayerStateBase> _states;
    private PlayerStateBase _currentState;

    // Biến đồng bộ trạng thái qua mạng
    private NetworkVariable<PlayerStateType> _netState = new NetworkVariable<PlayerStateType>(
        PlayerStateType.Idle,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    /// <summary>Trạng thái hiện tại của Player (read-only).</summary>
    public PlayerStateType CurrentStateType => _netState.Value;

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
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _netState.OnValueChanged += OnStateValueChanged;
        
        // Khởi tạo state đầu tiên
        _currentState = _states[CurrentStateType];
        _currentState.Enter();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _netState.OnValueChanged -= OnStateValueChanged;
    }

    private void OnStateValueChanged(PlayerStateType oldState, PlayerStateType newState)
    {
        // Chạy trên mọi máy (kể cả proxy) khi NetworkVariable thay đổi
        _currentState?.Exit();
        _currentState = _states[newState];
        _currentState.Enter();
        OnStateChanged?.Invoke(oldState, newState);

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log($"[PlayerFSM] {oldState} → {newState} (Sync)");
#endif
    }

    private void Update()
    {
        if (!IsSpawned) return;
        
        // Chỉ owner mới chạy logic Update của State (để tự thân nó đổi State qua TransitionTo)
        if (IsOwner)
        {
            _currentState?.Update();
        }
    }

    /// <summary>
    /// Chuyển sang state mới. Gọi Exit() state cũ, Enter() state mới.
    /// Guard: không cho transition vào chính state đang có.
    /// </summary>
    public void TransitionTo(PlayerStateType newState)
    {
        if (!IsOwner) return; // Chỉ owner mới được đổi state
        if (newState == CurrentStateType) return;

        // Thay đổi giá trị qua mạng, nó sẽ tự động kích hoạt OnStateValueChanged trên tất cả các Client (BAO GỒM CẢ BẢN THÂN OWNER)
        _netState.Value = newState;
    }
}
