using UnityEngine;

/// <summary>
/// PlayerAnimator — Class duy nhất được phép chạm vào Unity Animator.
/// Đọc state từ PlayerStateMachine mỗi frame → set Animator params.
/// Root Motion = OFF. Tất cả parameter dùng StringToHash.
/// SRS §4.1.4
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine _fsm;

    private Animator _animator;
    private PlayerStateType _previousState;

    // Animator Parameter Name Constants — dùng hash để tránh typo, tăng hiệu năng
    private static readonly int SPEED           = Animator.StringToHash("Speed");
    private static readonly int IS_GROUNDED     = Animator.StringToHash("IsGrounded");
    private static readonly int IS_CROUCHING    = Animator.StringToHash("IsCrouching");
    private static readonly int IS_GLIDING      = Animator.StringToHash("IsGliding");
    private static readonly int IS_DEAD         = Animator.StringToHash("IsDead");
    private static readonly int JUMP_TRIGGER    = Animator.StringToHash("JumpTrigger");
    private static readonly int DJUMP_TRIGGER   = Animator.StringToHash("DoubleJumpTrigger");
    private static readonly int WJUMP_TRIGGER   = Animator.StringToHash("WallJumpTrigger");
    private static readonly int SLIDE_TRIGGER   = Animator.StringToHash("SlideTrigger");
    private static readonly int RESPAWN_TRIGGER = Animator.StringToHash("RespawnTrigger");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false; // BẮT BUỘC — vị trí do Rigidbody điều khiển

        if (_fsm == null)
        {
            _fsm = GetComponent<PlayerStateMachine>();
        }

        _previousState = PlayerStateType.Idle;
    }

    private void Update()
    {
        if (_fsm == null || _animator == null) return;

        var currentState = _fsm.CurrentStateType;

        // Luôn update Speed float cho Blend Tree mượt
        _animator.SetFloat(SPEED, StateToSpeed(currentState));

        // Luôn update bools
        _animator.SetBool(IS_GROUNDED,  IsGroundedState(currentState));
        _animator.SetBool(IS_CROUCHING, currentState is PlayerStateType.CrouchIdle or PlayerStateType.CrouchWalk);
        _animator.SetBool(IS_GLIDING,   currentState == PlayerStateType.AirGlide);
        _animator.SetBool(IS_DEAD,      currentState == PlayerStateType.Dead);

        // Triggers — chỉ set khi ENTER state (previous → current)
        if (currentState != _previousState)
        {
            switch (currentState)
            {
                case PlayerStateType.Jump:        _animator.SetTrigger(JUMP_TRIGGER);    break;
                case PlayerStateType.DoubleJump:  _animator.SetTrigger(DJUMP_TRIGGER);   break;
                case PlayerStateType.WallJump:    _animator.SetTrigger(WJUMP_TRIGGER);   break;
                case PlayerStateType.GroundSlide: _animator.SetTrigger(SLIDE_TRIGGER);   break;
                case PlayerStateType.Respawning:  _animator.SetTrigger(RESPAWN_TRIGGER); break;
            }

            _previousState = currentState;
        }
    }

    /// <summary>Chuyển PlayerStateType sang Speed float cho Blend Tree.</summary>
    private float StateToSpeed(PlayerStateType state) => state switch
    {
        PlayerStateType.Walk or PlayerStateType.CrouchWalk => 0.5f,
        PlayerStateType.Run                                => 1.0f,
        _                                                  => 0.0f,
    };

    /// <summary>Kiểm tra state có phải ground state không.</summary>
    private bool IsGroundedState(PlayerStateType state) => state is
        PlayerStateType.Idle or PlayerStateType.Walk or PlayerStateType.Run or
        PlayerStateType.CrouchIdle or PlayerStateType.CrouchWalk or PlayerStateType.GroundSlide;
}
