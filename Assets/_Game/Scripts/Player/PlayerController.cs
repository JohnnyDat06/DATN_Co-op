using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerController — Xử lý toàn bộ di chuyển và vật lý Player bằng Rigidbody3D.
/// Đọc state từ PlayerStateMachine, input từ PlayerInputHandler, config từ SOPlayerConfig.
/// KHÔNG tự đọc Input, KHÔNG tự quản lý state — chỉ react.
/// Camera-relative movement: hướng di chuyển dựa trên hướng camera.
/// SRS §4.1.2 · §4.3.3 · §3.1
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SOPlayerConfig _config;
    [SerializeField] private Transform      _cameraLookTarget; // child empty transform

    private Rigidbody          _rb;
    private CapsuleCollider    _capsule;
    private PlayerStateMachine _fsm;
    private PlayerInputHandler _input;

    // Runtime state
    private int     _jumpCount;
    private bool    _isGrounded;
    private bool    _glideUsed;          // true sau khi dùng glide 1 lần, reset khi chạm đất
    private bool    _isCrouching;
    private float   _slideTimer;
    private bool    _isTouchingWall;
    private Vector3 _wallNormal;

    // Capsule original values (để restore sau Crouch)
    private float   _originalCapsuleHeight;
    private Vector3 _originalCapsuleCenter;

    private void Awake()
    {
        _rb     = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _fsm    = GetComponent<PlayerStateMachine>();
        _input  = GetComponent<PlayerInputHandler>();

        if (_config == null)
        {
            Debug.LogError("[PlayerController] SOPlayerConfig chưa được gán trong Inspector!");
            enabled = false;
            return;
        }

        // Lưu capsule gốc
        _originalCapsuleHeight = _capsule.height;
        _originalCapsuleCenter = _capsule.center;

        // Rigidbody setup
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned) return;

        // Guard: skip khi Dead/Respawning
        if (_fsm.CurrentStateType is PlayerStateType.Dead or PlayerStateType.Respawning) return;

        // Cả Host và Client đều tính toán Ground và Wall để phục vụ Animation local được siêu mượt
        CheckGrounded();
        CheckWall();

        // Từ đoạn này trở xuống: Liên quan đến tác động vật lý (thay đổi vận tốc, ép lực), CHỈ OWNER được làm:
        if (!IsOwner) return;
        HandleCrouch();
        HandleGroundSlide();
        HandleJump();
        HandleAirGlide();
        HandleWallClimb();
        HandleMovement(); // Luôn cuối cùng — override velocity sau các force
    }

    private void CheckWall()
    {
        _isTouchingWall = false;

        if (_input.MoveInput.sqrMagnitude < 0.01f) return;

        var cam = Camera.main;
        if (cam == null) return;

        var camForward = cam.transform.forward;
        var camRight   = cam.transform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * _input.MoveInput.y + camRight * _input.MoveInput.x).normalized;
        Vector3 origin = transform.position + Vector3.up * (_capsule.height * 0.5f);
        
        // Dùng SphereCast quét theo đúng hướng nhân vật đang cố đi tới
        if (Physics.SphereCast(origin, _capsule.radius * 0.9f, moveDir, out var hit, 0.3f,
            LayerMask.GetMask(Constants.Layers.ENVIRONMENT, "Default")))
        {
            _isTouchingWall = true;
            _wallNormal = hit.normal;
        }
    }

    // ─── HandleMovement — Camera-relative movement ───────────────────────────

    private void HandleMovement()
    {
        if (_fsm.CurrentStateType == PlayerStateType.WallHang) return;

        if (_input.MoveInput.sqrMagnitude < 0.01f)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }

        var cam = Camera.main;
        if (cam == null) return;

        var camForward = cam.transform.forward;
        var camRight   = cam.transform.right;

        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Tính hướng di chuyển từ input
        var moveDir = camForward * _input.MoveInput.y + camRight * _input.MoveInput.x;

        // Tính tốc độ theo state hiện tại
        float speed = _fsm.CurrentStateType switch
        {
            PlayerStateType.Run        => _config.MoveSpeed * _config.SprintMultiplier,
            PlayerStateType.CrouchWalk => _config.MoveSpeed * _config.CrouchSpeedMultiplier,
            PlayerStateType.GroundSlide => 0f,           // slide dùng quán tính, không drive
            _                          => _config.MoveSpeed,
        };

        var targetVel = moveDir * speed;

        // Xử lý chống trượt: NẾU ĐANG CHẠM TƯỜNG & HƯỚNG ĐI ĐÂM VÀO TƯỜNG -> CHIẾU (SLIDE) VẬN TỐC THEO MẶT TƯỜNG
        if (_isTouchingWall && Vector3.Dot(moveDir, _wallNormal) < 0f)
        {
            targetVel = Vector3.ProjectOnPlane(targetVel, _wallNormal);
            // Chuẩn hóa lại tốc độ để không bị giảm tốc độ dọc theo mặt phẳng tường
            if (targetVel.sqrMagnitude > 0.01f)
            {
                targetVel = targetVel.normalized * speed;
            }
        }

        // Apply velocity ngang — giữ velocity Y hiện tại
        _rb.linearVelocity = new Vector3(targetVel.x, _rb.linearVelocity.y, targetVel.z);

        // Xoay Player về hướng di chuyển (Slerp — KHÔNG xoay theo camera)
        if (moveDir.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    // ─── HandleJump ──────────────────────────────────────────────────────────

    private void HandleJump()
    {
        if (!_input.JumpPressed) return;

        bool canJump = _isGrounded && _jumpCount == 0;
        bool canDoubleJump = !_isGrounded && _jumpCount < _config.MaxJumpCount;

        if (canJump || canDoubleJump)
        {
            _input.ConsumeJumpPressed();

            // Reset Y velocity trước khi nhảy để lực nhất quán
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            bool isFirstJump = _jumpCount == 0;

            // Xác định lực nhảy dựa trên jump count
            float force = isFirstJump ? _config.JumpForce : _config.DoubleJumpForce;
            _rb.AddForce(Vector3.up * force, ForceMode.Impulse);
            
            _jumpCount++;
            
            // Đảm bảo không còn ở mặt đất sau khi nhảy
            _isGrounded = false;

            var newState = isFirstJump ? PlayerStateType.Jump : PlayerStateType.DoubleJump;
            _fsm.TransitionTo(newState);
        }
    }

    // ─── HandleAirGlide ──────────────────────────────────────────────────────

    private void HandleAirGlide()
    {
        bool isFalling = _rb.linearVelocity.y < 0f;
        bool canGlide  = _input.JumpHeld && isFalling && !_glideUsed && !_isGrounded;

        if (canGlide && _fsm.CurrentStateType is PlayerStateType.Jump or PlayerStateType.DoubleJump)
        {
            _rb.linearVelocity = new Vector3(
                _rb.linearVelocity.x,
                Mathf.Max(_rb.linearVelocity.y, -2f), // giới hạn tốc độ rơi
                _rb.linearVelocity.z
            );
            _rb.useGravity = false;
            _rb.AddForce(Vector3.up * (Physics.gravity.magnitude * (1f - _config.GlideGravityScale)), ForceMode.Acceleration);
            _glideUsed = true;
            _fsm.TransitionTo(PlayerStateType.AirGlide);
        }

        // Khi không giữ Jump nữa hoặc chạm đất → tắt glide
        if (_fsm.CurrentStateType == PlayerStateType.AirGlide && !_input.JumpHeld)
        {
            _rb.useGravity = true;
            _fsm.TransitionTo(PlayerStateType.Jump);
        }
    }

    // ─── HandleCrouch ────────────────────────────────────────────────────────

    private void HandleCrouch()
    {
        if (_input.IsCrouching && !_isCrouching)
        {
            // Thu nhỏ capsule
            _capsule.height = _originalCapsuleHeight * 0.5f;
            _capsule.center = new Vector3(0f, _capsule.height * 0.5f, 0f);
            _isCrouching = true;
        }
        else if (!_input.IsCrouching && _isCrouching)
        {
            // Kiểm tra có đủ chỗ đứng dậy không (raycast lên)
            if (!Physics.Raycast(transform.position, Vector3.up, _originalCapsuleHeight))
            {
                _capsule.height = _originalCapsuleHeight;
                _capsule.center = _originalCapsuleCenter;
                _isCrouching = false;
            }
        }
    }

    // ─── HandleGroundSlide ───────────────────────────────────────────────────

    private void HandleGroundSlide()
    {
        if (_fsm.CurrentStateType == PlayerStateType.GroundSlide)
        {
            _slideTimer -= Time.deltaTime;
            if (_slideTimer <= 0f || !_input.IsCrouching)
            {
                // Kết thúc slide
                _capsule.height = _originalCapsuleHeight;
                _capsule.center = _originalCapsuleCenter;
                _isCrouching = false;
                _fsm.TransitionTo(_input.IsMoving ? PlayerStateType.Run : PlayerStateType.Idle);
            }
            return;
        }

        // Bắt đầu slide: đang Run + Crouch pressed + isGrounded
        if (_fsm.CurrentStateType == PlayerStateType.Run && _input.IsCrouching && _isGrounded)
        {
            _slideTimer = _config.GroundSlideDuration;
            _capsule.height = _originalCapsuleHeight * 0.5f;
            _capsule.center = new Vector3(0f, _capsule.height * 0.5f, 0f);
            _isCrouching = true;

            // Lực quán tính ban đầu theo hướng đang chạy
            _rb.AddForce(transform.forward * _config.GroundSlideForce, ForceMode.Impulse);
            _fsm.TransitionTo(PlayerStateType.GroundSlide);
        }
    }

    // ─── HandleWallClimb — chỉ active khi WallClimbEnabled ──────────────────

    private void HandleWallClimb()
    {
        if (!_fsm.WallClimbEnabled) return;

        // Raycast tìm tường xung quanh khi đang không IsGrounded
        _isTouchingWall = false;
        var dirs = new[] { transform.forward, -transform.forward, transform.right, -transform.right };
        foreach (var dir in dirs)
        {
            if (Physics.Raycast(transform.position + Vector3.up, dir, out var hit, 0.6f,
                LayerMask.GetMask(Constants.Layers.ENVIRONMENT)))
            {
                _isTouchingWall = true;
                _wallNormal = hit.normal;
                break;
            }
        }

        // Bắt đầu WallHang
        if (_isTouchingWall && !_isGrounded
            && _fsm.CurrentStateType is PlayerStateType.Jump or PlayerStateType.DoubleJump or PlayerStateType.AirGlide)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.useGravity = false;
            _fsm.TransitionTo(PlayerStateType.WallHang);
        }

        // WallJump
        if (_fsm.CurrentStateType == PlayerStateType.WallHang && _input.JumpPressed)
        {
            _input.ConsumeJumpPressed();
            _rb.useGravity = true;
            var jumpDir = (_wallNormal + Vector3.up).normalized;
            _rb.AddForce(jumpDir * _config.WallJumpVerticalForce
                         + _wallNormal * _config.WallJumpHorizontalForce, ForceMode.Impulse);
            _jumpCount = 1; // reset để có thể double jump sau wall jump
            _glideUsed = false;
            _fsm.TransitionTo(PlayerStateType.WallJump);
        }

        // Rơi nếu không còn chạm tường
        if (_fsm.CurrentStateType == PlayerStateType.WallHang && !_isTouchingWall)
        {
            _rb.useGravity = true;
            _fsm.TransitionTo(PlayerStateType.Jump);
        }
    }

    // ─── CheckGrounded ───────────────────────────────────────────────────────

    private void CheckGrounded()
    {
        // Khi đang nhảy lên (Velocity Y > 0), tạm thời coi như không chạm đất để tránh reset state xuống Idle/Walk ngay lập tức
        if (_rb.linearVelocity.y > 0.1f)
        {
            _isGrounded = false;
            return;
        }

        // Bắt đầu SphereCast từ vị trí ngang tâm capsule để tránh collider chồng chéo ngay lúc đầu
        float radius  = _capsule.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (_capsule.height * 0.5f);
        float castDistance = (_capsule.height * 0.5f) - radius + 0.2f;

        _isGrounded = Physics.SphereCast(
            origin, 
            radius, 
            Vector3.down, 
            out _, 
            castDistance,
            LayerMask.GetMask(Constants.Layers.ENVIRONMENT, "Default"));

        if (_isGrounded)
        {
            _jumpCount  = 0;
            _glideUsed  = false;
            _rb.useGravity = true;

            // Reset về ground state nếu đang ở air state
            if (_fsm.CurrentStateType is PlayerStateType.Jump
                or PlayerStateType.DoubleJump
                or PlayerStateType.AirGlide
                or PlayerStateType.WallJump)
            {
                _fsm.TransitionTo(_input.IsMoving
                    ? (_input.IsSprinting ? PlayerStateType.Run : PlayerStateType.Walk)
                    : PlayerStateType.Idle);
            }
        }
    }

    // ─── Public Helpers ──────────────────────────────────────────────────────

    /// <summary>Expose IsGrounded cho các class khác (ví dụ PlayerAnimator).</summary>
    public bool IsGrounded => _isGrounded;
}
