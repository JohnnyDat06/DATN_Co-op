using Unity.Netcode;
using UnityEngine;
using RayFire;

/// <summary>
/// RockProjectile — Phiên bản tối ưu: Không dùng vật lý thực (Trigger), di chuyển bằng code.
/// Đảm bảo: Không mất tốc độ, không xuyên tường, bay xuyên nhau và đồng bộ tuyệt đối.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class RockProjectile : NetworkBehaviour
{
    [SerializeField] private SORockConfig _config;
    [SerializeField] private RayfireRigid _rayfireRigid;

    private bool _hasCollided = false;
    private Vector3 _currentVelocity;
    private Collider _collider;
    private Rigidbody _rb;

    // Đồng bộ vận tốc từ Server xuống Client
    private NetworkVariable<Vector3> _syncVelocity = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _rayfireRigid = GetComponent<RayfireRigid>();
        
        if (_rayfireRigid == null) _rayfireRigid = GetComponentInChildren<RayfireRigid>();

        // THIẾT LẬP QUAN TRỌNG:
        // Biến viên đá thành Trigger để không bị mất đà khi chạm Player và có thể bay xuyên nhau.
        if (_rb != null) _rb.isKinematic = true;
        if (_collider != null) _collider.isTrigger = true;
    }

    public override void OnNetworkSpawn()
    {
        if (_syncVelocity.Value != Vector3.zero)
        {
            _currentVelocity = _syncVelocity.Value;
        }

        _syncVelocity.OnValueChanged += (oldVal, newVal) => {
            _currentVelocity = newVal;
        };

        if (IsServer)
        {
            Invoke(nameof(DespawnRock), 10f);
        }
    }

    /// <summary>
    /// Gọi từ Server (RockShooter)
    /// </summary>
    public void Launch(Vector3 velocity)
    {
        if (!IsServer) return;
        _syncVelocity.Value = velocity;
        _currentVelocity = velocity;
    }

    private void Update()
    {
        if (_hasCollided || _currentVelocity == Vector3.zero) return;

        // 1. Di chuyển viên đá theo vận tốc đồng bộ
        float step = _currentVelocity.magnitude * Time.deltaTime;
        transform.position += _currentVelocity * Time.deltaTime;

        // 2. RAYCAST KIỂM TRA TƯỜNG (ENVIRONMENT):
        // Đây là cách duy nhất để đảm bảo đá KHÔNG BAO GIỜ xuyên qua tường dù bay nhanh.
        // Kiểm tra một đoạn ngắn phía trước hướng bay.
        int envLayerMask = LayerMask.GetMask(Constants.Layers.ENVIRONMENT);
        if (Physics.Raycast(transform.position, _currentVelocity.normalized, out RaycastHit hit, step + 0.5f, envLayerMask))
        {
            if (IsServer)
            {
                _hasCollided = true;
                ShatterClientRpc(hit.point);
                CancelInvoke(nameof(DespawnRock));
                Invoke(nameof(DespawnRock), 0.1f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Chỉ Server xử lý gây damage và đẩy player
        if (!IsServer || _hasCollided) return;

        // Nếu chạm Player: Gây damage + Đẩy mạnh
        if (other.CompareTag(Constants.Tags.PLAYER))
        {
            HandlePlayerCollision(other.gameObject);
            // Vì là Trigger nên đá KHÔNG bị dừng lại, tiếp tục bay xuyên qua player với vận tốc cũ.
        }
    }

    private void HandlePlayerCollision(GameObject player)
    {
        var playerHealth = player.GetComponent<PlayerHealth>();
        var playerController = player.GetComponent<PlayerController>();

        if (playerHealth != null) playerHealth.TakeDamage(_config.Damage);
        
        if (playerController != null)
        {
            // Lực đẩy cực mạnh: Hướng bay của đá + một chút hướng lên trên
            Vector3 pushDir = _currentVelocity.normalized;
            pushDir.y = 0.2f; 

            // Đẩy Player bay xa (KnockbackForce nên để giá trị lớn trong Config, ví dụ 20-30)
            playerController.ApplyKnockbackClientRpc(pushDir * _config.KnockbackForce);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void ShatterClientRpc(Vector3 shatterPos)
    {
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

    private void DespawnRock()
    {
        if (IsServer && NetworkObject.IsSpawned) NetworkObject.Despawn();
    }
}
