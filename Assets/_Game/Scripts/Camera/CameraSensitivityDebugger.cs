using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Script hỗ trợ test nhanh tốc độ chuột ngay trong Inspector khi đang Play.
/// Gắn script này vào cùng chỗ với CameraSettingsService (thường là PlayerCamera).
/// </summary>
public class CameraSensitivityDebugger : MonoBehaviour
{
    [Header("Quick Test Sensitivity (Drag Sliders in Play Mode)")]
    [Range(0.01f, 20f)]
    public float testSensitivityX = 1f;

    [Range(0.01f, 20f)]
    public float testSensitivityY = 1f;

    private CameraSettingsService _service;
    private CinemachineInputAxisController _axisController;

    private float _lastAppliedX = -1f;
    private float _lastAppliedY = -1f;

    private void Start()
    {
        ResolveTargets();

        if (_service != null)
        {
            testSensitivityX = _service.SensitivityX;
            testSensitivityY = _service.SensitivityY;
        }

        _lastAppliedX = testSensitivityX;
        _lastAppliedY = testSensitivityY;
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!Mathf.Approximately(testSensitivityX, _lastAppliedX) || !Mathf.Approximately(testSensitivityY, _lastAppliedY))
        {
            ApplySensitivity();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ApplySensitivity();
    }

    private void ResolveTargets()
    {
        if (_service == null)
        {
            _service = GetComponent<CameraSettingsService>();
            if (_service == null)
            {
                _service = FindFirstObjectByType<CameraSettingsService>();
            }
        }

        if (_axisController == null)
        {
            if (CameraManager.Instance != null && CameraManager.Instance.VcamThirdPerson != null)
            {
                _axisController = CameraManager.Instance.VcamThirdPerson.GetComponent<CinemachineInputAxisController>();
            }

            if (_axisController == null)
            {
                _axisController = FindFirstObjectByType<CinemachineInputAxisController>();
            }
        }
    }

    private void ApplySensitivity()
    {
        ResolveTargets();

        if (_service != null)
        {
            _service.SetSensitivityX(testSensitivityX);
            _service.SetSensitivityY(testSensitivityY);
        }
        else if (_axisController != null)
        {
            foreach (var controller in _axisController.Controllers)
            {
                var lowerName = controller.Name.ToLower();

                if (lowerName.Contains("orbit x") || lowerName.Contains("horizontal") || lowerName == "x")
                {
                    controller.Input.Gain = testSensitivityX;
                }
                else if (lowerName.Contains("orbit y") || lowerName.Contains("vertical") || lowerName == "y")
                {
                    // Giữ dấu hiện tại; nếu chưa có dấu thì mặc định non-invert là âm.
                    float sign = Mathf.Sign(controller.Input.Gain);
                    controller.Input.Gain = (sign == 0f ? -1f : sign) * testSensitivityY;
                }
            }
        }
        else
        {
            Debug.LogWarning("[CamDebugger] Khong tim thay CameraSettingsService hoac CinemachineInputAxisController de apply sensitivity.");
            return;
        }

        _lastAppliedX = testSensitivityX;
        _lastAppliedY = testSensitivityY;

        Debug.Log($"[CamDebugger] Sensitivity Updated: X={testSensitivityX}, Y={testSensitivityY}");
    }
}
