using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerInputHandler — layer duy nhất được phép đọc input.
/// Các class khác chỉ hỏi Handler, KHÔNG BAO GIỜ gọi InputSystem trực tiếp.
/// Yêu cầu: CHỈ CHẠY trên máy khách là LẤY QUYỀN SỞ HỮU (IsOwner) của bản sao Player này.
/// SRS §4.1.5
/// </summary>
public class PlayerInputHandler : NetworkBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField, Min(0f)] private float _jumpBufferDuration = 0.12f;

    // Cached InputActions
    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _crouchAction;
    private InputAction _interactAction;
    private InputAction _pauseAction;
    private InputAction _cameraLookAction;

    private bool _inputLocked;
    private float _jumpBufferTimer;

    // ─── Movement Properties (read-only cho class khác) ──────────────────────

    /// <summary>Raw WASD/Arrow input.</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>True khi MoveInput.magnitude > 0.1f.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>True khi Sprint held.</summary>
    public bool IsSprinting { get; private set; }

    /// <summary>True khi Crouch held.</summary>
    public bool IsCrouching { get; private set; }

    // ─── Action Properties (consumed once per press — reset sau khi đọc) ─────

    /// <summary>Jump down this frame.</summary>
    public bool JumpPressed { get; private set; }

    /// <summary>Jump held.</summary>
    public bool JumpHeld { get; private set; }

    /// <summary>Interact down this frame.</summary>
    public bool InteractPressed { get; private set; }

    /// <summary>Pause down this frame.</summary>
    public bool PausePressed { get; private set; }

    // ─── Camera Properties ───────────────────────────────────────────────────

    /// <summary>Mouse delta.</summary>
    public Vector2 CameraLookDelta { get; private set; }

    /// <summary>Tắt khi camera đặc biệt.</summary>
    public bool CameraLookEnabled { get; private set; } = true;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_inputActions == null)
        {
            Debug.LogError("[PlayerInputHandler] InputActionAsset chưa được gán trong Inspector!");
            return;
        }

        var playerMap = _inputActions.FindActionMap("Player");
        if (playerMap == null)
        {
            Debug.LogError("[PlayerInputHandler] Không tìm thấy ActionMap 'Player' trong InputActionAsset!");
            return;
        }

        _moveAction       = playerMap.FindAction("Move");
        _sprintAction     = playerMap.FindAction("Sprint");
        _jumpAction       = playerMap.FindAction("Jump");
        _crouchAction     = playerMap.FindAction("Crouch");
        _interactAction   = playerMap.FindAction("Interact");
        _pauseAction      = playerMap.FindAction("Pause");
        _cameraLookAction = playerMap.FindAction("CameraLook");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            EnableInputActions();
            EventBus.OnCutSceneStarted += LockAllInput;
            EventBus.OnCutSceneEnded   += UnlockAllInput;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {
            EventBus.OnCutSceneStarted -= LockAllInput;
            EventBus.OnCutSceneEnded   -= UnlockAllInput;
            DisableInputActions();
        }
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        if (_inputLocked)
        {
            ClearAllInput();
            return;
        }

        ReadInput();
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner) return;

        // Reset consumed properties sau mỗi frame
        InteractPressed = false;
        PausePressed    = false;
    }

    // ─── Input Reading ───────────────────────────────────────────────────────

    private void ReadInput()
    {
        // Movement
        MoveInput   = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        IsMoving    = MoveInput.sqrMagnitude > 0.01f;
        IsSprinting = _sprintAction?.IsPressed() ?? false;
        IsCrouching = _crouchAction?.IsPressed() ?? false;

        // Actions (consumed — chỉ true 1 frame)
        if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
        {
            _jumpBufferTimer = _jumpBufferDuration;
        }

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.deltaTime;
        }
        JumpPressed = _jumpBufferTimer > 0f;

        JumpHeld = _jumpAction?.IsPressed() ?? false;

        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            InteractPressed = true;

        if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
            PausePressed = true;

        // Camera
        CameraLookDelta = CameraLookEnabled
            ? (_cameraLookAction?.ReadValue<Vector2>() ?? Vector2.zero)
            : Vector2.zero;
    }

    private void ClearAllInput()
    {
        MoveInput       = Vector2.zero;
        IsMoving        = false;
        IsSprinting     = false;
        IsCrouching     = false;
        JumpPressed     = false;
        JumpHeld        = false;
        _jumpBufferTimer = 0f;
        InteractPressed = false;
        PausePressed    = false;
        CameraLookDelta = Vector2.zero;
    }

    // ─── Public Methods ──────────────────────────────────────────────────────

    /// <summary>Tắt CameraLook input. Gọi bởi CameraManager khi switch sang camera đặc biệt.</summary>
    public void DisableCameraLook()
    {
        CameraLookEnabled = false;
    }

    /// <summary>Bật lại CameraLook input. Gọi bởi CameraManager khi về ThirdPerson.</summary>
    public void EnableCameraLook()
    {
        CameraLookEnabled = true;
    }

    /// <summary>Lock toàn bộ input. Gọi khi CutScene bắt đầu.</summary>
    public void LockAllInput()
    {
        _inputLocked = true;
        ClearAllInput();
    }

    /// <summary>Unlock input. Gọi khi CutScene kết thúc.</summary>
    public void UnlockAllInput()
    {
        _inputLocked = false;
    }

    /// <summary>
    /// Dùng cho code vật lý (FixedUpdate): đọc và consume jump buffered.
    /// </summary>
    public bool ConsumeJumpPressed()
    {
        if (_jumpBufferTimer <= 0f)
        {
            return false;
        }

        _jumpBufferTimer = 0f;
        JumpPressed = false;
        return true;
    }

    // ─── Enable/Disable Actions ──────────────────────────────────────────────

    private void EnableInputActions()
    {
        _inputActions?.Enable();
    }

    private void DisableInputActions()
    {
        _inputActions?.Disable();
    }
}
