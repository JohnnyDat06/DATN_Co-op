using UnityEngine;

/// <summary>
/// SOScreenShakeConfig — Config data cho Screen Shake events.
/// Dùng với Cinemachine Perlin Noise. SRS §14.2 · §14.3.2
/// </summary>
[CreateAssetMenu(fileName = "SOScreenShakeConfig", menuName = "CoopGame/Camera/ScreenShakeConfig")]
public class SOScreenShakeConfig : ScriptableObject
{
    [Tooltip("Cường độ rung (Cinemachine Perlin Amplitude)")]
    public float Amplitude = 0.5f;

    [Tooltip("Tần số rung (Cinemachine Perlin Frequency)")]
    public float Frequency = 1.5f;

    [Tooltip("Thời gian rung (giây)")]
    public float Duration = 0.3f;
}
