using UnityEngine;

/// <summary>
/// SORockConfig — Config data cho FallingRock (đá rơi).
/// Dùng với Object Pool. SRS §13.3 · §7.5
/// </summary>
[CreateAssetMenu(fileName = "SORockConfig", menuName = "CoopGame/Environment/RockConfig")]
public class SORockConfig : ScriptableObject
{
    [Tooltip("Số đá spawn mỗi giây (giá trị ban đầu, tăng theo tiến trình)")]
    public float SpawnRate = 0.5f;

    [Tooltip("Sát thương gây ra khi trúng player")]
    public float Damage = 25f;

    [Tooltip("Tốc độ rơi tối thiểu")]
    public float SpeedMin = 8f;

    [Tooltip("Tốc độ rơi tối đa")]
    public float SpeedMax = 14f;

    [Tooltip("Offset bóng shadow indicator so với vị trí đá")]
    public float ShadowOffset = 0.1f;

    [Tooltip("Prefab VFX khi đá va chạm nền")]
    public GameObject VFXImpactPrefab;

    [Tooltip("Kích thước pool ban đầu")]
    public int PoolSize = 20;
}
