using UnityEngine;

/// <summary>
/// ScriptableObject cấu hình thông số tấn công cận chiến cho Enemy.
/// Cho phép designer điều chỉnh thông số trong Inspector mà không cần sửa code.
/// </summary>
[CreateAssetMenu(fileName = "NewMeleeAttackConfig", menuName = "DATN/Enemy/Melee Attack Config")]
public class SOEnemyMeleeAttackConfig : ScriptableObject
{
    [Header("Melee Settings")]
    [Tooltip("Layer mask cho đối tượng có thể bị hit (Chọn Player)")]
    public LayerMask targetLayer;

    [Tooltip("Bán kính vùng tấn công (OverlapSphere)")]
    public float radius = 1.0f;

    [Tooltip("Sát thương mỗi đòn")]
    public int damage = 20;

    [Header("Audio Settings")]
    [Tooltip("Âm thanh khi thực hiện đòn tấn công")]
    public SOAudioClip attackSFX;
}
