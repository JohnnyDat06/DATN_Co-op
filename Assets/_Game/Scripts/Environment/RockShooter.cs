using Unity.Netcode;
using UnityEngine;

/// <summary>
/// RockShooter — Hệ thống bắn đá ngang.
/// Đã được sửa đổi để tự đồng bộ hóa mà không cần đăng ký NetworkPrefab.
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
        // Chỉ Server mới điều khiển nhịp bắn
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

        // 1. Tính toán thông số bắn trên Server
        Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Quaternion aimRotation = _spawnPoint != null ? _spawnPoint.rotation : transform.rotation;
        Vector3 shootDirection = aimRotation * Vector3.forward;
        Vector3 velocity = shootDirection * _config.HorizontalSpeed;
        Quaternion rockRotation = aimRotation * Quaternion.Euler(_rotationOffset);
        double serverTime = NetworkManager.Singleton.ServerTime.Time;

        // 2. Gửi lệnh cho toàn bộ Client sinh đá (bao gồm cả Host)
        SpawnRockClientRpc(spawnPos, rockRotation, velocity, serverTime);
    }

    [ClientRpc]
    private void SpawnRockClientRpc(Vector3 pos, Quaternion rot, Vector3 vel, double spawnTime)
    {
        // Sinh đá cục bộ trên mỗi máy. Vì sinh cục bộ nên không cần đăng ký NetworkPrefab.
        GameObject rockInstance = Instantiate(_rockPrefab, pos, rot);
        
        var projectile = rockInstance.GetComponent<RockProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(vel, spawnTime, pos);
        }
        else
        {
            Debug.LogWarning("[RockShooter] RockPrefab thiếu component RockProjectile!");
        }
    }

    public void SetAutoShoot(bool value)
    {
        _autoShoot = value;
    }
}
