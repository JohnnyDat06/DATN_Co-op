using Unity.Netcode;
using UnityEngine;

/// <summary>
/// RockShooter — Hệ thống bắn đá ngang.
/// Chỉ hoạt động trên Server để đảm bảo tính đồng nhất cho tất cả người chơi.
/// </summary>
public class RockShooter : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject _rockPrefab;
    [SerializeField] private SORockConfig _config;
    [SerializeField] private Transform _spawnPoint;
    
    [Tooltip("Góc quay bù để căn chỉnh hướng viên đá (ví dụ: 90, 0, 0 để đá nằm ngang)")]
    [SerializeField] private Vector3 _rotationOffset; 
    
    [SerializeField] private bool _autoShoot = true;

    private float _nextSpawnTime;

    private void Update()
    {
        // Chỉ Server mới xử lý logic bắn
        if (!IsServer || !_autoShoot) return;

        if (Time.time >= _nextSpawnTime)
        {
            Shoot();
            _nextSpawnTime = Time.time + (1f / _config.SpawnRate);
        }
    }

    [ContextMenu("Shoot Now")]
    public void Shoot()
    {
        if (!IsServer) return;

        if (_rockPrefab == null)
        {
            Debug.LogError("[RockShooter] RockPrefab chưa được gán!");
            return;
        }

        // 1. Xác định vị trí và hướng bắn "thẳng" của máy bắn (dựa trên SpawnPoint)
        Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Quaternion aimRotation = _spawnPoint != null ? _spawnPoint.rotation : transform.rotation;
        Vector3 shootDirection = aimRotation * Vector3.forward; // Đây luôn là hướng "thẳng" theo nòng súng

        // 2. Tính toán góc quay của viên đá (kết hợp hướng bắn và góc quay bù cho đẹp)
        Quaternion rockRotation = aimRotation * Quaternion.Euler(_rotationOffset);

        // 3. Spawn viên đá trên Server
        GameObject rockInstance = Instantiate(_rockPrefab, spawnPos, rockRotation);
        
        // 4. Kích hoạt NetworkObject
        var networkObject = rockInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
            
            // 5. Yêu cầu viên đá bay theo hướng bắn "thẳng" đã tính ở bước 1
            var projectile = rockInstance.GetComponent<RockProjectile>();
            if (projectile != null)
            {
                projectile.Launch(shootDirection * _config.HorizontalSpeed);
            }
        }
        else
        {
            Debug.LogWarning("[RockShooter] RockPrefab thiếu component NetworkObject!");
        }

        Debug.Log("[RockShooter] Rock shot in direction: " + shootDirection);
    }

    public void SetAutoShoot(bool value)
    {
        _autoShoot = value;
    }
}
