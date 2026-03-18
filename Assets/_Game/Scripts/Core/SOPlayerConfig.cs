using UnityEngine;

/// <summary>
/// SOPlayerConfig — Config data cho Player character.
/// SRS §4.1.3 · §13.3
/// </summary>
[CreateAssetMenu(fileName = "SOPlayerConfig", menuName = "CoopGame/Player/PlayerConfig")]
public class SOPlayerConfig : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Máu tối đa của player")]
    public float MaxHealth = 100f;

    [Header("Movement")]
    [Tooltip("Tốc độ đi bộ cơ bản (m/s)")]
    public float MoveSpeed = 5f;

    [Tooltip("Nhân tốc độ khi Sprint")]
    public float SprintMultiplier = 1.6f;

    [Tooltip("Tốc độ di chuyển khi Crouch (nhân với MoveSpeed)")]
    public float CrouchSpeedMultiplier = 0.5f;

    [Header("Jump")]
    [Tooltip("Lực nhảy (áp lên Rigidbody AddForce)")]
    public float JumpForce = 7f;

    [Tooltip("Số lần nhảy tối đa (1 = không double jump, 2 = có double jump)")]
    public int MaxJumpCount = 2;

    [Header("Glide")]
    [Tooltip("gravityScale khi đang Glide (< 1 = rơi chậm)")]
    public float GlideGravityScale = 0.2f;

    [Header("Wall Jump")]
    [Tooltip("Lực ngang khi WallJump")]
    public float WallJumpHorizontalForce = 5f;

    [Tooltip("Lực dọc khi WallJump")]
    public float WallJumpVerticalForce = 8f;

    [Header("Slide")]
    [Tooltip("Thời gian tối đa của GroundSlide (giây)")]
    public float GroundSlideDuration = 0.8f;

    [Tooltip("Lực quán tính ban đầu của Slide")]
    public float GroundSlideForce = 12f;
}
