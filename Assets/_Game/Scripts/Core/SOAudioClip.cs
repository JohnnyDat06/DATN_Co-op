using UnityEngine;

/// <summary>
/// SOAudioClip — Wrapper cho AudioClip kèm volume và pitch randomization.
/// SRS §13.3 · §8
/// </summary>
[CreateAssetMenu(fileName = "SOAudioClip", menuName = "CoopGame/Audio/AudioClipConfig")]
public class SOAudioClip : ScriptableObject
{
    [Tooltip("AudioClip gốc")]
    public AudioClip Clip;

    [Range(0f, 1f), Tooltip("Volume cơ bản")]
    public float Volume = 1f;

    [Tooltip("Pitch tối thiểu (random range)")]
    public float PitchMin = 0.9f;

    [Tooltip("Pitch tối đa (random range)")]
    public float PitchMax = 1.1f;
}
