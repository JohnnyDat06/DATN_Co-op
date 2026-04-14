using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Đồng bộ player theo mô hình owner-authoritative của NGO 2.x.
/// Owner mô phỏng input + state + physics cục bộ.
/// Remote instance chỉ nhận transform/rigidbody/animator từ NGO để tránh mỗi máy tự chạy logic riêng.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ClientNetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(ClientNetworkAnimator))]
[RequireComponent(typeof(Animator))]
public class NGOPlayerSync : NetworkBehaviour
{
    [Header("Local Simulation")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlayerStateMachine _stateMachine;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerAnimator _playerAnimator;

    [Header("Netcode Components")]
    [SerializeField] private ClientNetworkTransform _networkTransform;
    [SerializeField] private NetworkRigidbody _networkRigidbody;
    [SerializeField] private ClientNetworkAnimator _networkAnimator;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Optional Owner-Only Behaviours")]
    [SerializeField] private Behaviour[] _ownerOnlyBehaviours;

    private bool _hasAppliedSpawnState;

    private void Awake()
    {
        CacheReferences();
        ApplyNetcodeDefaults();
    }

    private void Reset()
    {
        CacheReferences();
        BuildOwnerOnlyBehaviourList();
        ApplyNetcodeDefaults();
    }

    private void OnValidate()
    {
        CacheReferences();
        ApplyNetcodeDefaults();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ApplyAuthorityState();
        _hasAppliedSpawnState = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _hasAppliedSpawnState = false;
        SetLocalSimulationEnabled(false);
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();

        if (_hasAppliedSpawnState)
        {
            ApplyAuthorityState();
        }
    }

    public override void OnLostOwnership()
    {
        base.OnLostOwnership();

        if (_hasAppliedSpawnState)
        {
            ApplyAuthorityState();
        }
    }

    private void CacheReferences()
    {
        _inputHandler ??= GetComponent<PlayerInputHandler>();
        _stateMachine ??= GetComponent<PlayerStateMachine>();
        _playerController ??= GetComponent<PlayerController>();
        _playerAnimator ??= GetComponent<PlayerAnimator>();

        _networkTransform ??= GetComponent<ClientNetworkTransform>();
        _networkRigidbody ??= GetComponent<NetworkRigidbody>();
        _networkAnimator ??= GetComponent<ClientNetworkAnimator>();
        _rigidbody ??= GetComponent<Rigidbody>();
    }

    private void BuildOwnerOnlyBehaviourList()
    {
        _ownerOnlyBehaviours = new Behaviour[]
        {
            _inputHandler,
            _stateMachine,
            _playerController,
            _playerAnimator
        };
    }

    private void ApplyNetcodeDefaults()
    {
        if (_networkTransform != null)
        {
            _networkTransform.SyncPositionX = true;
            _networkTransform.SyncPositionY = true;
            _networkTransform.SyncPositionZ = true;
            _networkTransform.SyncRotAngleX = false;
            _networkTransform.SyncRotAngleY = true;
            _networkTransform.SyncRotAngleZ = false;
            _networkTransform.UseQuaternionSynchronization = true;
            _networkTransform.UseHalfFloatPrecision = true;
            _networkTransform.UseUnreliableDeltas = true;
            _networkTransform.Interpolate = true;
            _networkTransform.SlerpPosition = false;
            _networkTransform.InLocalSpace = false;
        }

        if (_networkRigidbody != null)
        {
            _networkRigidbody.UseRigidBodyForMotion = true;
            _networkRigidbody.AutoUpdateKinematicState = true;
            _networkRigidbody.AutoSetKinematicOnDespawn = true;
        }
    }

    private void ApplyAuthorityState()
    {
        // Script này dùng ClientNetworkTransform/ClientNetworkAnimator nên authority = owner.
        bool isAuthority = IsOwner;

        // KHÔNG BẬT SIMULATION NẾU ĐANG TRONG LOBBY
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            SetLocalSimulationEnabled(false);
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }
            return;
        }

        SetLocalSimulationEnabled(isAuthority);

        if (_rigidbody != null && _networkRigidbody == null)
        {
            _rigidbody.isKinematic = !isAuthority;
            _rigidbody.linearVelocity = isAuthority ? _rigidbody.linearVelocity : Vector3.zero;
            _rigidbody.angularVelocity = isAuthority ? _rigidbody.angularVelocity : Vector3.zero;
        }
    }

    private void SetLocalSimulationEnabled(bool enabled)
    {
        if (!enabled && _inputHandler != null)
        {
            _inputHandler.LockAllInput();
        }

        if (_ownerOnlyBehaviours == null || _ownerOnlyBehaviours.Length == 0)
        {
            BuildOwnerOnlyBehaviourList();
        }

        foreach (var behaviour in _ownerOnlyBehaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = enabled;
        }
    }

    /// <summary>
    /// Thực hiện dịch chuyển (Teleport) nhân vật một cách an toàn và đồng bộ.
    /// Logic dựa trên TeleportManager của dự án.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (IsServer)
        {
            // Nếu gọi từ Server, gửi lệnh xuống Client sở hữu (Owner)
            TeleportClientRpc(position, rotation);
        }
        else if (IsOwner)
        {
            // Nếu là Owner, thực hiện trực tiếp
            StartCoroutine(PerformTeleportCoroutine(position, rotation));
        }
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position, Quaternion rotation)
    {
        // Chỉ thực hiện trên máy khách sở hữu (Owner) nhân vật này
        if (IsOwner)
        {
            StartCoroutine(PerformTeleportCoroutine(position, rotation));
        }
    }

    private IEnumerator PerformTeleportCoroutine(Vector3 position, Quaternion rotation)
    {
        // 1. Tạm thời tắt Rigidbody Interpolation để tránh rubber banding
        RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;
        bool hasRigidbody = _rigidbody != null;
        
        if (hasRigidbody)
        {
            originalInterpolation = _rigidbody.interpolation;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true; // Tạm khóa vật lý
        }

        // 2. Cập nhật vị trí transform
        transform.position = position;
        transform.rotation = rotation;

        // 3. Thông báo cho NetworkTransform (NGO 1.5+)
        if (_networkTransform != null)
        {
            _networkTransform.Teleport(position, rotation, transform.localScale);
        }

        // 4. Chờ 1 frame vật lý để Engine và NetworkTransform ghi nhận vị trí mới
        yield return new WaitForFixedUpdate();

        // 5. Khôi phục trạng thái Rigidbody
        if (hasRigidbody && _rigidbody != null)
        {
            _rigidbody.isKinematic = !IsOwner; // Trả lại trạng thái theo Authority
            _rigidbody.interpolation = originalInterpolation;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[NGOPlayerSync] Teleported to {position}");
    }
}
