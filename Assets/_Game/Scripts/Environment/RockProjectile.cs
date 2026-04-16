using UnityEngine;
using RayFire;
using Unity.Netcode;

/// <summary>
/// RockProjectile — Phiên bản đồng bộ thủ công: Không dùng NetworkObject để tránh lỗi Prefab registration.
/// Tự đồng bộ vị trí dựa trên ServerTime.
/// </summary>
public class RockProjectile : MonoBehaviour
{
    [SerializeField] private SORockConfig _config;
    [SerializeField] private RayfireRigid _rayfireRigid;

    private bool _hasCollided = false;
    private Vector3 _currentVelocity;
    private Collider _collider;
    private Rigidbody _rb;

    private double _spawnServerTime;
    private Vector3 _spawnPosition;
    private bool _isInitialized = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _rayfireRigid = GetComponent<RayfireRigid>();
        
        if (_rayfireRigid == null) _rayfireRigid = GetComponentInChildren<RayfireRigid>();

        if (_rb != null) _rb.isKinematic = true;
        if (_collider != null) _collider.isTrigger = true;
    }

    /// <summary>
    /// Khởi tạo viên đá với các thông số đồng bộ
    /// </summary>
    public void Initialize(Vector3 velocity, double spawnTime, Vector3 spawnPos)
    {
        _currentVelocity = velocity;
        _spawnServerTime = spawnTime;
        _spawnPosition = spawnPos;
        transform.position = spawnPos;
        _isInitialized = true;

        // Tự hủy sau 10s nếu không va chạm
        Destroy(gameObject, 10f);
    }

    private void Update()
    {
        if (!_isInitialized || _hasCollided || _currentVelocity == Vector3.zero) return;

        // Đồng bộ vị trí dựa trên thời gian thực của Server
        double timePassed = NetworkManager.Singleton.ServerTime.Time - _spawnServerTime;
        Vector3 targetPos = _spawnPosition + _currentVelocity * (float)timePassed;

        // Di chuyển mượt mà tới vị trí mục tiêu
        float step = _currentVelocity.magnitude * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step * 1.1f);

        // Kiểm tra va chạm tường (Cả Server và Client đều tự kiểm tra để tối ưu hình ảnh)
        int envLayerMask = LayerMask.GetMask(Constants.Layers.ENVIRONMENT);
        if (Physics.Raycast(transform.position, _currentVelocity.normalized, out RaycastHit hit, step + 0.5f, envLayerMask))
        {
            ShatterLocal(hit.point);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // CHỈ SERVER mới xử lý gây damage để tránh cheat hoặc lỗi lặp damage
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _hasCollided) return;

        if (other.CompareTag(Constants.Tags.PLAYER))
        {
            HandlePlayerCollision(other.gameObject);
        }
    }

    private void HandlePlayerCollision(GameObject player)
    {
        var playerHealth = player.GetComponent<PlayerHealth>();
        var playerController = player.GetComponent<PlayerController>();

        if (playerHealth != null) playerHealth.TakeDamage(_config.Damage);
        
        if (playerController != null)
        {
            Vector3 pushDir = _currentVelocity.normalized;
            pushDir.y = 0.2f; 
            playerController.ApplyKnockbackClientRpc(pushDir * _config.KnockbackForce);
        }
    }

    public void ShatterLocal(Vector3 shatterPos)
    {
        if (_hasCollided) return;
        _hasCollided = true;
        transform.position = shatterPos;

        if (_config.VFXImpactPrefab != null)
        {
            Instantiate(_config.VFXImpactPrefab, shatterPos, Quaternion.identity);
        }

        if (_rayfireRigid != null)
        {
            if (_config.FragmentMaterial != null)
            {
                _rayfireRigid.materials.iMat = _config.FragmentMaterial;
                var renderer = GetComponent<MeshRenderer>() ?? GetComponentInChildren<MeshRenderer>();
                if (renderer != null) renderer.material = _config.FragmentMaterial;
            }
            _rayfireRigid.Demolish();
            Invoke(nameof(ForceBlackFragments), 0.05f);
        }
        
        // Xóa object sau khi vỡ (Rayfire sẽ tạo ra các mảnh vụn riêng)
        Destroy(gameObject, 0.1f);
    }

    private void ForceBlackFragments()
    {
        if (_config.FragmentMaterial == null) return;
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            Material[] blackmats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < blackmats.Length; i++) blackmats[i] = _config.FragmentMaterial;
            r.materials = blackmats;
        }
    }
}
