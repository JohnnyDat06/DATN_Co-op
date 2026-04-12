using Unity.Netcode;
using UnityEngine;

/// <summary>
/// WindProjectile — Viên đạn gió gây sát thương cho Player.
/// </summary>
public class WindProjectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private LayerMask _hitLayers;
    [SerializeField] private GameObject _hitEffectPrefab;

    private float _timer;

    private void Update()
    {
        if (!IsServer) return;

        transform.Translate(Vector3.forward * _speed * Time.deltaTime);

        _timer += Time.deltaTime;
        if (_timer >= _lifeTime)
        {
            DespawnProjectile();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Kiểm tra va chạm với các layer cho phép (Player, Tường)
        if (((1 << other.gameObject.layer) & _hitLayers) != 0)
        {
            // Gây sát thương nếu là IDamageable
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }

            SpawnHitEffectClientRpc(transform.position);
            DespawnProjectile();
        }
    }

    private void DespawnProjectile()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [ClientRpc]
    private void SpawnHitEffectClientRpc(Vector3 position)
    {
        if (_hitEffectPrefab != null)
        {
            Instantiate(_hitEffectPrefab, position, Quaternion.identity);
        }
    }
}
