using UnityEngine;

/// <summary>
/// ScriptableObject cấu hình thông số tấn công tầm xa cho Enemy.
/// Cho phép designer điều chỉnh thông số trong Inspector mà không cần sửa code.
/// </summary>
[CreateAssetMenu(fileName = "NewRangedAttackConfig", menuName = "DATN/Enemy/Ranged Attack Config")]
public class SOEnemyRangedAttackConfig : ScriptableObject
{
    [Header("Ranged Settings")]
    [Tooltip("Prefab đạn (WindProjectile, FireBall, v.v.)")]
    public GameObject projectilePrefab;
}
