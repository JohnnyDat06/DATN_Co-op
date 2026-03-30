using UnityEngine;

/// <summary>
/// DeathZone — Khu vực giết chết Player ngay lập tức khi chạm vào.
/// Dùng IDamageableEnemy interface — không biết gì về PlayerHealth cụ thể.
/// SRS §11.3
/// </summary>
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Constants.Tags.PLAYER)) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.InstantKill();
        }
    }
}
