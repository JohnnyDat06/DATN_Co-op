using UnityEngine;

/// <summary>
/// SOVFXConfig — Config data cho VFX (Visual Effects).
/// Dùng với Object Pool. SRS §13.3
/// </summary>
[CreateAssetMenu(fileName = "SOVFXConfig", menuName = "CoopGame/VFX/VFXConfig")]
public class SOVFXConfig : ScriptableObject
{
    [Tooltip("Prefab ParticleSystem")]
    public GameObject VFXPrefab;

    [Tooltip("Scale tối thiểu khi spawn")]
    public float ScaleMin = 0.8f;

    [Tooltip("Scale tối đa khi spawn")]
    public float ScaleMax = 1.2f;

    [Tooltip("Thời gian sống của VFX (giây) trước khi trả về pool")]
    public float Lifetime = 2f;

    [Tooltip("Số lượng object trong pool ban đầu")]
    public int PoolSize = 10;
}
