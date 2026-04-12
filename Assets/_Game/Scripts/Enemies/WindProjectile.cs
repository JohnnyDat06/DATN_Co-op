using Unity.Netcode;
using UnityEngine;

/// <summary>
/// WindProjectile — Xử lý logic di chuyển, va chạm và sát thương của đạn gió.
/// Đảm bảo đồng bộ mượt mà qua mạng (NGO).
/// </summary>
public class WindProjectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private LayerMask _hitLayers;

    [Header("VFX Settings")]
    [Tooltip("Object hiệu ứng nổ có sẵn trong Prefab")]
    [SerializeField] private GameObject _impactVFX;
    
    [Tooltip("Mesh hoặc các thành phần chính của đạn để ẩn đi khi nổ")]
    [SerializeField] private GameObject _projectileMesh;

    private float _timer;
    private bool _isExploded = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _timer = 0;
        _isExploded = false;
        if (_impactVFX != null) _impactVFX.SetActive(false);
        
        // Tạm ẩn mesh trong frame đầu tiên để tránh vệt kéo dài
        if (_projectileMesh != null) _projectileMesh.SetActive(false);
        Invoke(nameof(ShowMesh), 0.05f); // Bật lại sau một khoảng thời gian rất ngắn
        
        ClearVFXBuffers();
    }

    private void ShowMesh()
    {
        if (!_isExploded && _projectileMesh != null)
        {
            _projectileMesh.SetActive(true);
        }
    }

    private void Update()
    {
        if (_isExploded) return;

        // Cả Client và Server đều tự di chuyển
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);

        if (!IsServer) return;

        _timer += Time.deltaTime;
        if (_timer >= _lifeTime)
        {
            DespawnProjectile();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || _isExploded) return;

        // Kiểm tra xem có va chạm với layer mục tiêu (Player, Environment) không
        if (((1 << other.gameObject.layer) & _hitLayers) != 0)
        {
            _isExploded = true;

            // Nếu trúng Player (IDamageable), gây sát thương
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }

            // Kích hoạt hiệu ứng nổ trên tất cả Client (bao gồm cả Host)
            ExplodeClientRpc(transform.position);

            // Phá hủy đạn sau va chạm
            Invoke(nameof(DespawnProjectile), 1.5f);
        }
    }

    private void DespawnProjectile()
    {
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    /// <summary>
    /// Đồng bộ việc kích hoạt hiệu ứng nổ cho mọi Client.
    /// </summary>
    [ClientRpc]
    private void ExplodeClientRpc(Vector3 impactPosition)
    {
        _isExploded = true;
        
        // Đưa về vị trí nổ chuẩn của Server
        transform.position = impactPosition;
        
        // Ẩn mesh đạn và bật hiệu ứng nổ
        if (_projectileMesh != null) _projectileMesh.SetActive(false);
        if (_impactVFX != null) _impactVFX.SetActive(true);
        
        Debug.Log($"[WindProjectile] Explosion at {impactPosition}");
    }

    private void ClearVFXBuffers()
    {
        var trails = GetComponentsInChildren<TrailRenderer>();
        foreach (var trail in trails) trail.Clear();

        var particles = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles) ps.Clear();
    }
}
