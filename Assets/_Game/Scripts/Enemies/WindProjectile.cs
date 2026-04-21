using Unity.Netcode;
using UnityEngine;

/// <summary>
/// WindProjectile — Xử lý logic di chuyển, va chạm và sát thương của đạn gió.
/// Đảm bảo đồng bộ mượt mà qua mạng (NGO) bằng cách sử dụng Server-authoritative movement.
/// </summary>
public class WindProjectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private LayerMask _hitLayers;

    [Header("VFX Settings")]
    [SerializeField] private GameObject _impactVFX;
    [SerializeField] private GameObject _projectileMesh;

    // Sử dụng NetworkVariable để đồng bộ trạng thái nổ cho cả người chơi mới vào
    private NetworkVariable<bool> _isExplodedSync = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private float _timer;

    public override void OnNetworkSpawn()
    {
        _timer = 0;
        
        // Đăng ký sự kiện thay đổi trạng thái nổ
        _isExplodedSync.OnValueChanged += OnExplodedChanged;
        
        // Trạng thái khởi tạo
        ApplyExplosionState(_isExplodedSync.Value);

        if (!_isExplodedSync.Value)
        {
            ClearVFXBuffers();
            // Tạm ẩn mesh để tránh vệt kéo dài khi vừa spawn (do NetworkTransform đồng bộ chậm 1 frame)
            if (_projectileMesh != null) _projectileMesh.SetActive(false);
            Invoke(nameof(ShowMesh), 0.05f);
        }
    }

    public override void OnNetworkDespawn()
    {
        _isExplodedSync.OnValueChanged -= OnExplodedChanged;
    }

    private void OnExplodedChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            ApplyExplosionState(true);
        }
    }

    private void ApplyExplosionState(bool exploded)
    {
        if (exploded)
        {
            if (_projectileMesh != null) _projectileMesh.SetActive(false);
            if (_impactVFX != null) _impactVFX.SetActive(true);
            
            // Tắt va chạm trên mọi máy khi đã nổ
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        }
        else
        {
            if (_projectileMesh != null) _projectileMesh.SetActive(true);
            if (_impactVFX != null) _impactVFX.SetActive(false);
        }
    }

    private void ShowMesh()
    {
        if (!_isExplodedSync.Value && _projectileMesh != null)
        {
            _projectileMesh.SetActive(true);
        }
    }

    private void Update()
    {
        if (_isExplodedSync.Value) return;

        // Di chuyển: Server di chuyển thực tế, Client di chuyển để dự đoán (Visual only)
        // Nếu dùng NetworkTransform trên Prefab, Client sẽ được nội suy vị trí từ Server
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
        // Chỉ Server xử lý va chạm và sát thương
        if (!IsServer || _isExplodedSync.Value) return;

        if (((1 << other.gameObject.layer) & _hitLayers) != 0)
        {
            _isExplodedSync.Value = true;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }

            // Đợi hiệu ứng chạy xong rồi mới biến mất hoàn toàn trên Server
            StartCoroutine(ServerDespawnTimer(1.5f));
        }
    }

    private System.Collections.IEnumerator ServerDespawnTimer(float delay)
    {
        yield return new WaitForSeconds(delay);
        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        if (IsServer && IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    private void ClearVFXBuffers()
    {
        var trails = GetComponentsInChildren<TrailRenderer>();
        foreach (var trail in trails) trail.Clear();

        var particles = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles) ps.Clear();
    }
}
