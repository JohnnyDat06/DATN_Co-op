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
    private bool _isTeleporting; // Flag để tránh ApplyAuthorityState can thiệp khi đang dịch chuyển

    public bool IsTeleporting => _isTeleporting;

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
        // Lắng nghe sự kiện chuyển cảnh
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleSceneLoaded;
        }

        // Cập nhật lại trạng thái cảnh trước khi áp dụng logic
        ApplyAuthorityState();
        _hasAppliedSpawnState = true;
        Debug.Log($"[NGOPlayerSync] Spawned in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}. IsOwner: {IsOwner}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleSceneLoaded;
        }
        _hasAppliedSpawnState = false;
        SetLocalSimulationEnabled(false);
    }

    private void HandleSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        // CHỈ XỬ LÝ NẾU LÀ CHÍNH MÁY NÀY LOAD XONG
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        Debug.Log($"[NGOPlayerSync] Local client {clientId} loaded scene: {sceneName}. Applying authority state.");
        
        // Trì hoãn một chút sau khi load scene để Netcode ổn định vị trí ban đầu
        StartCoroutine(DelayedAuthorityStateAfterLoad());
    }

    private IEnumerator DelayedAuthorityStateAfterLoad()
    {
        // Đợi 2 frame để Unity nạp xong physics scene
        yield return null;
        yield return new WaitForFixedUpdate();
        
        if (!_isTeleporting)
        {
            ApplyAuthorityState();
        }
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
        // Nếu đang trong quá trình Teleport, để coroutine tự quản lý vật lý
        if (_isTeleporting) return;

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
            // Nếu là Owner (và không phải Server), thực hiện trực tiếp
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
        if (_isTeleporting) yield break;
        _isTeleporting = true;
        
        Debug.Log($"[NGOPlayerSync] Starting Teleport to {position}");

        // 1. Tắt tạm thời các thành phần gây xung đột vị trí/vật lý
        RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;
        bool hasRigidbody = _rigidbody != null;
        Collider col = GetComponent<Collider>();
        bool originalColState = col != null && col.enabled;
        
        if (hasRigidbody)
        {
            // Tạm thời vô hiệu hóa NetworkRigidbody để nó không tự động set lại isKinematic = false
            if (_networkRigidbody != null) _networkRigidbody.enabled = false;

            originalInterpolation = _rigidbody.interpolation;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true; 
        }

        if (col != null) col.enabled = false; 

        // 2. Cập nhật vị trí transform
        transform.position = position;
        transform.rotation = rotation;

        // 3. Thông báo cho NetworkTransform để đồng bộ ngay lập tức
        if (_networkTransform != null)
        {
            _networkTransform.Teleport(position, rotation, transform.localScale);
        }

        // 4. ĐÓNG BĂNG TRONG 1 GIÂY THỰC (Mô phỏng hành động Pause game của bạn)
        // Điều này giúp Netcode ổn định vị trí và các Client khác nhận được dữ liệu chuẩn
        float freezeTimer = 1.0f;
        while (freezeTimer > 0)
        {
            transform.position = position;
            transform.rotation = rotation;
            if (hasRigidbody)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
            freezeTimer -= Time.deltaTime;
            yield return null;
        }

        // 5. Khôi phục trạng thái
        if (col != null) col.enabled = originalColState;
        
        if (hasRigidbody && _rigidbody != null)
        {
            _rigidbody.interpolation = originalInterpolation;
            _rigidbody.linearVelocity = Vector3.zero;
            
            // Kích hoạt lại NetworkRigidbody
            if (_networkRigidbody != null) _networkRigidbody.enabled = true;

            // Thiết lập lại trạng thái kinematic dựa trên authority
            _rigidbody.isKinematic = !IsOwner; 
        }

        _isTeleporting = false;
        
        // Cập nhật lại state chuẩn
        ApplyAuthorityState();

        Debug.Log($"[NGOPlayerSync] Safety Teleport completed to {position}. Physics restored.");
    }
}
