using UnityEngine;

/// <summary>
/// SOMushroomConfig — Config data cho môi trường nấm bật (MushroomBounce).
/// SRS §13.3 · §7.3
/// </summary>
[CreateAssetMenu(fileName = "SOMushroomConfig", menuName = "CoopGame/Environment/MushroomConfig")]
public class SOMushroomConfig : ScriptableObject
{
    [Tooltip("Lực bật lên khi player nhảy trên nấm")]
    public float BounceForce = 15f;

    [Tooltip("Thời gian cooldown sau khi nảy (tránh nảy liên tục)")]
    public float CooldownTime = 0.3f;

    [Tooltip("Tên Animator trigger cho animation nấm bị nén")]
    public string AnimationTrigger = "Bounce";

    [Tooltip("Prefab VFX phát tán bào tử khi nảy")]
    public GameObject VFXPrefab;
}
