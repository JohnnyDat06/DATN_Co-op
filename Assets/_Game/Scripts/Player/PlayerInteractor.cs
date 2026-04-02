using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerInteractor — Component gắn vào Player để phát hiện và gọi Interactable gần nhất.
/// Tách biệt hoàn toàn khỏi PlayerStateMachine (Decoupled).
/// Chỉ active trên IsOwner — không ảnh hưởng proxy.
/// SRS §4.2.1
/// </summary>
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteractor : NetworkBehaviour
{
    // ─── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Detection")]
    [Tooltip("Bán kính phát hiện Interactable xung quanh Player (mét).")]
    [SerializeField] private float _interactRadius = 2f;

    [Tooltip("Layer mask của các vật thể Interactable.")]
    [SerializeField] private LayerMask _interactableLayerMask;

    // ─── Runtime ─────────────────────────────────────────────────────────────

    private PlayerInputHandler _input;
    private IInteractable      _currentTarget;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        // Tìm Interactable gần nhất liên tục mỗi frame
        DetectNearestInteractable();

        // Khi Player bấm Interact và đã có target trong range
        if (_input.InteractPressed && _currentTarget != null)
        {
            _currentTarget.Interact(OwnerClientId);
        }
    }

    // ─── Detection ────────────────────────────────────────────────────────────

    private void DetectNearestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _interactRadius, _interactableLayerMask);

        IInteractable nearest      = null;
        float         nearestDist  = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;
            if (interactable.IsActivated) continue; // Bỏ qua đã kích hoạt rồi (one-shot)

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest     = interactable;
            }
        }

        _currentTarget = nearest;
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}
