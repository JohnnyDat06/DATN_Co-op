using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteractor : NetworkBehaviour
{
    [Header("Detection")]
    [Tooltip("Khoang cach raycast toi da tu tam camera toi vat the tuong tac.")]
    [SerializeField] private float _interactRadius = 2f;

    [Tooltip("Layer mask cua cac vat the Interactable.")]
    [SerializeField] private LayerMask _interactableLayerMask;

    public static Action<IInteractable> OnInteractableFound;
    public static Action OnInteractableLost;

    private PlayerInputHandler _input;
    private Camera _mainCamera;
    private IInteractable _currentTarget;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        _mainCamera = Camera.main;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        SetCurrentTarget(null);
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        DetectLookTarget();

        if (_input.InteractPressed && _currentTarget != null && _currentTarget.CanInteract)
        {
            _currentTarget.Interact(OwnerClientId);
        }
    }

    private void DetectLookTarget()
    {
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        IInteractable nextTarget = null;
        
        float finaledDistanceInteract = 5 + _interactRadius;
        
        if (Physics.Raycast(ray, out RaycastHit hit, finaledDistanceInteract, _interactableLayerMask, QueryTriggerInteraction.Collide))
        {
            nextTarget = hit.collider.GetComponentInParent<IInteractable>();
            if (nextTarget != null && !nextTarget.CanInteract)
            {
                nextTarget = null;
            }
        }

        SetCurrentTarget(nextTarget);
    }

    private void SetCurrentTarget(IInteractable nextTarget)
    {
        if (ReferenceEquals(_currentTarget, nextTarget)) return;

        if (_currentTarget != null)
        {
            _currentTarget.OnHoverExit();
            OnInteractableLost?.Invoke();
        }

        _currentTarget = nextTarget;

        if (_currentTarget != null)
        {
            _currentTarget.OnHoverEnter();
            OnInteractableFound?.Invoke(_currentTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Camera gizmoCamera = _mainCamera != null ? _mainCamera : Camera.main;
        Ray ray;

        if (gizmoCamera != null)
        {
            ray = gizmoCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            Vector3 origin = transform.position + Vector3.up * 1.6f;
            ray = new Ray(origin, transform.forward);
        }
        float finaledDistanceInteract = 5 + _interactRadius;

        Gizmos.DrawRay(ray.origin, ray.direction * finaledDistanceInteract);
        Gizmos.DrawWireSphere(ray.origin + ray.direction * finaledDistanceInteract, 0.08f);
    }
}
