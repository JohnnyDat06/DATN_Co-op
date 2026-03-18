using UnityEngine;

/// <summary>
/// SOWindConfig — Config data cho vùng gió (WindZone).
/// SRS §13.3 · §7.3
/// </summary>
[CreateAssetMenu(fileName = "SOWindConfig", menuName = "CoopGame/Environment/WindConfig")]
public class SOWindConfig : ScriptableObject
{
    [Tooltip("Lực đẩy lên theo Vector3.up")]
    public float UpwardForce = 20f;

    [Tooltip("Thời gian áp lực gió (giây, 0 = liên tục trong trigger)")]
    public float Duration = 0f;

    [Tooltip("Prefab VFX cột bụi cát xoáy")]
    public GameObject VFXPrefab;
}
