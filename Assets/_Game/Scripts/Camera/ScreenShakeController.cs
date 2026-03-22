using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class ScreenShakeController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _vcamThirdPerson;

    private CinemachineBasicMultiChannelPerlin _perlin;
    private float _currentAmplitude;
    private int _activeShakeCount;
    private bool _shakeEnabled = true;

    private void Awake()
    {
        if (_vcamThirdPerson != null)
        {
            _perlin = _vcamThirdPerson.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        if (_perlin == null)
        {
            Debug.LogError("[ScreenShakeController] Missing CinemachineBasicMultiChannelPerlin on ThirdPerson VCam.");
        }
    }

    private void OnEnable()
    {
        EventBus.OnScreenShakeRequested += HandleShakeRequested;
        EventBus.OnAccessibilityChanged += LoadAccessibilitySettings;
    }

    private void OnDisable()
    {
        EventBus.OnScreenShakeRequested -= HandleShakeRequested;
        EventBus.OnAccessibilityChanged -= LoadAccessibilitySettings;
    }

    private void Start()
    {
        LoadAccessibilitySettings();
    }

    private void LoadAccessibilitySettings()
    {
        _shakeEnabled = PlayerPrefs.GetInt(Constants.PlayerPrefsKeys.ACCESSIBILITY_CAMERA_SHAKE, 1) == 1;
        if (!_shakeEnabled)
        {
            StopAllShakes();
        }
    }

    private void HandleShakeRequested(SOScreenShakeConfig config)
    {
        if (!_shakeEnabled || config == null)
        {
            return;
        }

        Shake(config);
    }

    public void Shake(SOScreenShakeConfig config)
    {
        if (!_shakeEnabled || config == null || _perlin == null)
        {
            return;
        }

        StartCoroutine(ShakeCoroutine(config));
    }

    private IEnumerator ShakeCoroutine(SOScreenShakeConfig config)
    {
        _activeShakeCount++;
        _currentAmplitude += config.Amplitude;
        _perlin.AmplitudeGain = _currentAmplitude;
        _perlin.FrequencyGain = config.Frequency;

        yield return new WaitForSeconds(config.Duration);

        _currentAmplitude -= config.Amplitude;
        _currentAmplitude = Mathf.Max(0f, _currentAmplitude);
        _activeShakeCount--;

        if (_activeShakeCount <= 0)
        {
            _perlin.AmplitudeGain = 0f;
            _perlin.FrequencyGain = 0f;
            _activeShakeCount = 0;
        }
        else
        {
            _perlin.AmplitudeGain = _currentAmplitude;
        }
    }

    public void StopAllShakes()
    {
        StopAllCoroutines();
        _currentAmplitude = 0f;
        _activeShakeCount = 0;

        if (_perlin != null)
        {
            _perlin.AmplitudeGain = 0f;
            _perlin.FrequencyGain = 0f;
        }
    }
}
