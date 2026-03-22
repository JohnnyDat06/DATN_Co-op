using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// CameraSettingsService - Xử lý việc lưu trữ và áp dụng các thiết lập Camera (Sensitivity, FOV, v.v.)
/// </summary>
public class CameraSettingsService : MonoBehaviour
{
    private CinemachineOrbitalFollow _orbitalFollow;
    private CinemachineInputAxisController _axisController;

    public float SensitivityX { get; private set; }
    public float SensitivityY { get; private set; }
    public bool InvertY { get; private set; }
    public float ArmLength { get; private set; }

    private void Start()
    {
        InitializeReferences();
        LoadFromPlayerPrefs();
        ApplySettings();
    }

    private void OnEnable()
    {
        EventBus.OnSettingsChanged += ApplySettings;
    }

    private void OnDisable()
    {
        EventBus.OnSettingsChanged -= ApplySettings;
    }

    private void InitializeReferences()
    {
        if (CameraManager.Instance != null && CameraManager.Instance.VcamThirdPerson != null)
        {
            var vcam = CameraManager.Instance.VcamThirdPerson;
            _orbitalFollow = vcam.GetComponent<CinemachineOrbitalFollow>();
            _axisController = vcam.GetComponent<CinemachineInputAxisController>();
        }
    }

    private void LoadFromPlayerPrefs()
    {
        SensitivityX = PlayerPrefs.GetFloat(Constants.PlayerPrefsKeys.CAM_SENSITIVITY_X, Constants.Camera.DEFAULT_SENSITIVITY_X);
        SensitivityY = PlayerPrefs.GetFloat(Constants.PlayerPrefsKeys.CAM_SENSITIVITY_Y, Constants.Camera.DEFAULT_SENSITIVITY_Y);
        InvertY = PlayerPrefs.GetInt(Constants.PlayerPrefsKeys.CAM_INVERT_Y, 0) == 1;
        ArmLength = PlayerPrefs.GetFloat(Constants.PlayerPrefsKeys.CAM_DISTANCE, Constants.Camera.DEFAULT_ARM_LENGTH);
    }

    public void ApplySettings()
    {
        if (_orbitalFollow == null || _axisController == null)
        {
            InitializeReferences();
        }

        LoadFromPlayerPrefs();

        if (_orbitalFollow != null)
        {
            _orbitalFollow.Radius = Mathf.Clamp(ArmLength, Constants.Camera.MIN_ARM_LENGTH, Constants.Camera.MAX_ARM_LENGTH);
        }

        if (_axisController != null)
        {
            foreach (var controller in _axisController.Controllers)
            {
                string axisName = controller.Name;
                string lowerName = axisName.ToLower();
                
                Debug.Log($"[CameraSettings] Found Axis: {axisName}");

                if (lowerName.Contains("orbit x") || lowerName.Contains("pan") || lowerName.Contains("horizontal") || lowerName == "x")
                {
                    controller.Input.Gain = SensitivityX;
                    Debug.Log($"[CameraSettings] Applied SensitivityX {SensitivityX} to {axisName}");
                }
                else if (lowerName.Contains("orbit y") || lowerName.Contains("tilt") || lowerName.Contains("vertical") || lowerName == "y")
                {
                    // Cinemachine orbit Y thường cần gain âm cho hướng look "bình thường".
                    controller.Input.Gain = InvertY ? SensitivityY : -SensitivityY;
                    Debug.Log($"[CameraSettings] Applied SensitivityY {SensitivityY} (invert={InvertY}) to {axisName}");
                }
            }
        }

        EventBus.RaiseCameraSettingsChanged();
    }

    public void SetSensitivityX(float value)
    {
        SensitivityX = Mathf.Clamp(value, Constants.Camera.MIN_SENSITIVITY, Constants.Camera.MAX_SENSITIVITY);
        PlayerPrefs.SetFloat(Constants.PlayerPrefsKeys.CAM_SENSITIVITY_X, SensitivityX);
        ApplySettings();
    }

    public void SetSensitivityY(float value)
    {
        SensitivityY = Mathf.Clamp(value, Constants.Camera.MIN_SENSITIVITY, Constants.Camera.MAX_SENSITIVITY);
        PlayerPrefs.SetFloat(Constants.PlayerPrefsKeys.CAM_SENSITIVITY_Y, SensitivityY);
        ApplySettings();
    }

    public void SetInvertY(bool invert)
    {
        InvertY = invert;
        PlayerPrefs.SetInt(Constants.PlayerPrefsKeys.CAM_INVERT_Y, invert ? 1 : 0);
        ApplySettings();
    }

    public void SetArmLength(float length)
    {
        ArmLength = Mathf.Clamp(length, Constants.Camera.MIN_ARM_LENGTH, Constants.Camera.MAX_ARM_LENGTH);
        PlayerPrefs.SetFloat(Constants.PlayerPrefsKeys.CAM_DISTANCE, ArmLength);
        ApplySettings();
    }
}
