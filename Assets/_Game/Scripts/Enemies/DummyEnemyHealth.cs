using UnityEngine;

public class DummyEnemyHealth : EnemyHealth
{
    protected override void OnDamagedServerSide(int damage, ulong instigatorClientId)
    {
        Debug.Log($"Trời ơi, tôi bị trừ {damage} máu! Máu còn lại: {CurrentHealth.Value}");
    }

    protected override void Die()
    {
        base.Die();
    }
}