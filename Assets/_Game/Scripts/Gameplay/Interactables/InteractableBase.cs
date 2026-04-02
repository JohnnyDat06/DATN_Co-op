using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// InteractableBase — Abstract NetworkBehaviour là nền tảng cho mọi vật thể tương tác.
/// Phát hiện Player trong vùng trigger, hiển thị PromptUI, quản lý trạng thái IsActivated
/// đồng bộ qua mạng (Server-authoritative).
/// Các class con (SoloInteractable, CoopInteractable) override hàm Interact().
/// SRS §4.2.1
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : NetworkBehaviour, IInteractable
{
    // ─── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Interactable Settings")]
    [Tooltip("ID định danh duy nhất cho vật thể này (dùng với EventBus và CloudSave).")]
    [SerializeField] protected string _interactableId = "interactable_001";

    [Tooltip("Cho phép tương tác nhiều lần (toggle) hay chỉ một lần (one-shot).")]
    [SerializeField] protected bool _allowReactivation = false;

    [Tooltip("Sự kiện Unity gọi khi vật thể được kích hoạt. Kết nối Door, Platform... qua Inspector.")]
    public UnityEvent OnActivated;

    [Header("Prompt UI")]
    [Tooltip("Prefab PromptUI hiển thị gợi ý tương tác. Có thể để trống nếu dùng PromptUI Manager.")]
    [SerializeField] private GameObject _promptUIPrefab;

    [Tooltip("Offset vị trí PromptUI so với tâm vật thể.")]
    [SerializeField] private Vector3 _promptUIOffset = new Vector3(0f, 1.5f, 0f);

    // ─── Network State ────────────────────────────────────────────────────────

    /// <summary>
    /// Trạng thái kích hoạt đồng bộ qua mạng. Server là source of truth.
    /// </summary>
    protected NetworkVariable<bool> _isActivated = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private GameObject _promptUIInstance;

    /// <summary>
    /// Player hiện đang trong vùng trigger và có thể tương tác. Key = clientId.
    /// </summary>
    protected System.Collections.Generic.HashSet<ulong> _playersInRange
        = new System.Collections.Generic.HashSet<ulong>();

    // ─── IInteractable Implementation ─────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsActivated => _isActivated.Value;

    /// <inheritdoc/>
    public abstract void Interact(ulong playerId);

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isActivated.OnValueChanged += OnActivatedValueChanged;

        // Đảm bảo collider là Trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isActivated.OnValueChanged -= OnActivatedValueChanged;
        HidePromptUI();
    }

    // ─── Trigger Detection ────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Constants.Tags.PLAYER)) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        ulong clientId = netObj.OwnerClientId;
        _playersInRange.Add(clientId);

        // Chỉ hiện PromptUI trên máy của chính Player đó (IsOwner)
        if (netObj.IsOwner && !_isActivated.Value)
        {
            ShowPromptUI();
        }

        OnPlayerEntered(clientId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(Constants.Tags.PLAYER)) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        ulong clientId = netObj.OwnerClientId;
        _playersInRange.Remove(clientId);

        // Ẩn PromptUI khi Player rời đi
        if (netObj.IsOwner)
        {
            HidePromptUI();
        }

        OnPlayerExited(clientId);
    }

    // ─── Virtual Hooks for Subclasses ─────────────────────────────────────────

    /// <summary>Được gọi khi 1 Player vào Trigger. Override để xử thêm logic (CoopInteractable).</summary>
    protected virtual void OnPlayerEntered(ulong clientId) { }

    /// <summary>Được gọi khi 1 Player rời Trigger. Override để xử thêm logic (CoopInteractable).</summary>
    protected virtual void OnPlayerExited(ulong clientId) { }

    // ─── Activation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ Server khi điều kiện kích hoạt đã thoả mãn.
    /// Set IsActivated = true, invoke OnActivated, fire EventBus.
    /// </summary>
    protected void ServerActivate()
    {
        if (!IsServer) return;
        if (_isActivated.Value && !_allowReactivation) return;

        _isActivated.Value = true;
        Debug.Log($"[InteractableBase] <color=cyan>{_interactableId}</color> kích hoạt trên Server.");
    }

    /// <summary>
    /// Reset trạng thái về false. Dùng cho LeverInteractable (toggle) hoặc CoopInteractable reset.
    /// </summary>
    protected void ServerDeactivate()
    {
        if (!IsServer) return;
        _isActivated.Value = false;
    }

    // ─── NetworkVariable Callback ─────────────────────────────────────────────

    protected virtual void OnActivatedValueChanged(bool previousValue, bool newValue)
    {
        if (newValue && !previousValue)
        {
            // OFF -> ON
            if (!_allowReactivation)
                HidePromptUI();
                
            OnActivated?.Invoke();
            EventBus.RaiseInteractableActivated(_interactableId);
        }
    }

    // ─── PromptUI ─────────────────────────────────────────────────────────────

    protected void ShowPromptUI()
    {
        if (_promptUIInstance != null)
        {
            _promptUIInstance.SetActive(true);
            return;
        }

        if (_promptUIPrefab != null)
        {
            _promptUIInstance = Instantiate(
                _promptUIPrefab,
                transform.position + _promptUIOffset,
                Quaternion.identity,
                transform   // Gắn làm con để tự di chuyển theo vật thể
            );
            _promptUIInstance.transform.localPosition = _promptUIOffset;
        }
    }

    protected void HidePromptUI()
    {
        if (_promptUIInstance != null)
            _promptUIInstance.SetActive(false);
    }

    // ─── Gizmos (Editor Only) ─────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var col = GetComponent<Collider>();
        if (col is SphereCollider sphere)
            Gizmos.DrawWireSphere(transform.position, sphere.radius);
        else if (col is BoxCollider box)
            Gizmos.DrawWireCube(transform.position + box.center, box.size);
    }
}
