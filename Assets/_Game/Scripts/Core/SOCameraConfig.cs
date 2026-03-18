using Cinemachine;
using UnityEngine;

/// <summary>
/// SOCameraConfig — Config data cho từng Camera preset/mode.
/// Dùng với CameraManager và CinemachineBlend. SRS §4.3 · §4.3.4 · §13.3
/// </summary>
[CreateAssetMenu(fileName = "SOCameraConfig", menuName = "CoopGame/Camera/CameraConfig")]
public class SOCameraConfig : ScriptableObject
{
    [Header("Blend")]
    [Tooltip("Thời gian blend sang chế độ này (giây)")]
    public float BlendTime = 0.5f;

    [Tooltip("Kiểu blend (EaseInOut, Linear...)")]
    public CinemachineBlendDefinition.Style BlendStyle = CinemachineBlendDefinition.Style.EaseInOut;

    [Header("Follow Distance")]
    [Tooltip("Chiều dài Camera Arm (m) — chỉ áp dụng ThirdPerson")]
    public float ArmLength = 5f;

    [Header("Pitch Limits — chỉ áp dụng ThirdPerson")]
    [Tooltip("Góc nhìn tối thiểu (nhìn lên, âm)")]
    public float MinPitch = -30f;

    [Tooltip("Góc nhìn tối đa (nhìn xuống, dương)")]
    public float MaxPitch = 60f;

    [Header("Damping")]
    [Tooltip("Body damping của Cinemachine Transposer")]
    public float BodyDamping = 0.5f;

    [Tooltip("Aim damping của Cinemachine Composer")]
    public float AimDamping  = 0.3f;

    [Header("Collision")]
    [Tooltip("Layer mask cho CinemachineCollider")]
    public LayerMask CollisionLayer;
}
