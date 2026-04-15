using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// CameraManager - Quản lý việc chuyển đổi giữa các Preset Camera và thiết lập Target cho Cinemachine.
/// Hoạt động như một Singleton cục bộ trên mỗi Client/Host.
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Virtual Cameras - Cinemachine 3.x")]
    [SerializeField] private CinemachineCamera _vcamThirdPerson;
    [SerializeField] private CinemachineCamera _vcamSandSlide;
    [SerializeField] private CinemachineCamera _vcamPlatformer;
    [SerializeField] private CinemachineCamera _vcamFlyDown;
    [SerializeField] private CinemachineCamera _vcamTopDownController; // Mới
    [SerializeField] private CinemachineCamera _vcamTopDownObserver;   // Mới

    [Header("Configuration Assets (SO)")]
    [SerializeField] private SOCameraConfig _configThirdPerson;
    [SerializeField] private SOCameraConfig _configSandSlide;
    [SerializeField] private SOCameraConfig _configPlatformer;
    [SerializeField] private SOCameraConfig _configFlyDown;
    [SerializeField] private SOCameraConfig _configCutscene;
    [SerializeField] private SOCameraConfig _configTopDown; // Mới (Dùng chung)

    private PlayerInputHandler _inputHandler;
    private CameraPreset _currentPreset = CameraPreset.ThirdPerson;
    
    private Dictionary<CameraPreset, CinemachineCamera> _vcamMap;
    private Dictionary<CameraPreset, SOCameraConfig> _configMap;

    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 0;

    public CameraPreset CurrentPreset => _currentPreset;

    public CinemachineCamera VcamThirdPerson => _vcamThirdPerson;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-add để tránh trường hợp quên gắn service trong scene.
        if (GetComponent<CameraSettingsService>() == null)
        {
            gameObject.AddComponent<CameraSettingsService>();
        }

        InitializeMaps();
        SetAllPriorities(PRIORITY_INACTIVE);

        if (_vcamThirdPerson != null)
        {
            _vcamThirdPerson.Priority.Value = PRIORITY_ACTIVE;
        }
    }

    private void OnEnable()
    {
        EventBus.OnGamePaused += HandleGamePaused;
        EventBus.OnGameResumed += HandleGameResumed;
        EventBus.OnCutSceneStarted += HandleCutSceneStarted;
        EventBus.OnCutSceneEnded += HandleCutSceneEnded;
        EventBus.OnPlayerRespawned += HandlePlayerRespawned;
    }

    private void OnDisable()
    {
        EventBus.OnGamePaused -= HandleGamePaused;
        EventBus.OnGameResumed -= HandleGameResumed;
        EventBus.OnCutSceneStarted -= HandleCutSceneStarted;
        EventBus.OnCutSceneEnded -= HandleCutSceneEnded;
        EventBus.OnPlayerRespawned -= HandlePlayerRespawned;
    }

    private void InitializeMaps()
    {
        _vcamMap = new Dictionary<CameraPreset, CinemachineCamera>
        {
            { CameraPreset.ThirdPerson, _vcamThirdPerson },
            { CameraPreset.SandSlide, _vcamSandSlide },
            { CameraPreset.Platformer, _vcamPlatformer },
            { CameraPreset.FlyDown, _vcamFlyDown },
            { CameraPreset.TopDownController, _vcamTopDownController },
            { CameraPreset.TopDownObserver, _vcamTopDownObserver }
        };

        _configMap = new Dictionary<CameraPreset, SOCameraConfig>
        {
            { CameraPreset.ThirdPerson, _configThirdPerson },
            { CameraPreset.SandSlide, _configSandSlide },
            { CameraPreset.Platformer, _configPlatformer },
            { CameraPreset.FlyDown, _configFlyDown },
            { CameraPreset.Cutscene, _configCutscene },
            { CameraPreset.TopDownController, _configTopDown },
            { CameraPreset.TopDownObserver, _configTopDown }
        };
    }

    public void SetTarget(Transform target)
    {
        SetPlayerTarget(target, target);
    }

    public void SetPlayerTarget(Transform followTarget, Transform lookAtTarget)
    {
        if (followTarget == null)
        {
            Debug.LogWarning("[CameraManager] SetPlayerTarget được gọi với followTarget null!");
            return;
        }

        foreach (var kvp in _vcamMap)
        {
            var vcam = kvp.Value;
            if (vcam == null) continue;
            
            vcam.Target.TrackingTarget = followTarget;
            
            if (lookAtTarget != null)
            {
                vcam.Target.LookAtTarget = lookAtTarget;
            }
            
            Debug.Log($"[CameraManager] Đã gán Target cho {kvp.Key}: Follow={followTarget.name}");
        }
    }

    public void SwitchCamera(CameraPreset preset)
    {
        if (preset == _currentPreset || !_vcamMap.ContainsKey(preset)) return;

        _currentPreset = preset;
        SetAllPriorities(PRIORITY_INACTIVE);

        if (_vcamMap.TryGetValue(preset, out CinemachineCamera target) && target != null)
        {
            target.Priority.Value = PRIORITY_ACTIVE;

            // ÉP THÔNG SỐ TỰ ĐỘNG (Sửa lỗi cái to cái nhỏ)
            if (preset == CameraPreset.TopDownController || preset == CameraPreset.TopDownObserver)
            {
                // 1. Ép FOV giống hệt nhau
                var lens = target.Lens;
                lens.FieldOfView = 60f; 
                target.Lens = lens;

                // 2. Ép góc nhìn thẳng xuống 90 độ
                target.transform.rotation = Quaternion.Euler(90f, target.transform.eulerAngles.y, 0f);

                // 3. Chỉnh độ cao (Distance) tự động
                // Tìm component Follow của Cinemachine để chỉnh khoảng cách
                var follow = target.GetComponent<CinemachineFollow>();
                if (follow != null)
                {
                    float distance = (preset == CameraPreset.TopDownController) ? 12f : 22f;
                    follow.FollowOffset = new Vector3(0, distance, 0);
                }
            }
        }

        ApplyBlendConfig(preset);
        UpdateInputState(preset);
        UpdateCursorState(preset);

        EventBus.RaiseCameraPresetChanged(preset);
    }

    private void SetAllPriorities(int priority)
    {
        foreach (var vcam in _vcamMap.Values)
        {
            if (vcam != null) vcam.Priority.Value = priority;
        }
    }

    private void ApplyBlendConfig(CameraPreset preset)
    {
        if (!_configMap.TryGetValue(preset, out SOCameraConfig config) || config == null) return;

        CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(config.BlendStyle, config.BlendTime);
        }
    }

    private void UpdateInputState(CameraPreset preset)
    {
        bool lockMouse = preset is CameraPreset.SandSlide or CameraPreset.Platformer or CameraPreset.FlyDown or CameraPreset.Cutscene or CameraPreset.TopDownController or CameraPreset.TopDownObserver;
        
        ResolvePlayerInputIfNeeded();

        if (_inputHandler != null)
        {
            if (lockMouse) _inputHandler.DisableCameraLook();
            else _inputHandler.EnableCameraLook();
        }
    }

    private void UpdateCursorState(CameraPreset preset)
    {
        bool showCursor = preset == CameraPreset.Cutscene || preset == CameraPreset.TopDownController || preset == CameraPreset.TopDownObserver;
        Cursor.lockState = showCursor ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = showCursor;
    }

    private void HandleGamePaused()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        _inputHandler?.DisableCameraLook();
    }

    private void HandleGameResumed()
    {
        if (_currentPreset == CameraPreset.ThirdPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _inputHandler?.EnableCameraLook();
        }
    }

    private void HandleCutSceneStarted() => SwitchCamera(CameraPreset.Cutscene);
    private void HandleCutSceneEnded() => SwitchCamera(CameraPreset.ThirdPerson);

    private void HandlePlayerRespawned(ulong clientId, Vector3 spawnPosition)
    {
        // Khi respawn có thể cần cập nhật lại input handler nếu nó bị thay đổi
        ResolvePlayerInputIfNeeded();
    }

    private void ResolvePlayerInputIfNeeded()
    {
        if (_inputHandler != null)
        {
            NetworkObject netObj = _inputHandler.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner) return;
        }

        foreach (var handler in FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None))
        {
            var netObj = handler.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                _inputHandler = handler;
                return;
            }
        }
    }
}
