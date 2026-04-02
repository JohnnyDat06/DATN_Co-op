using Unity.Netcode;
using UnityEngine;

namespace Game.Interactions
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private float detectionRadius = 0.5f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private bool useSphereCast = true;

        [Header("UI Reference (Optional)")]
        // You could add a reference to an interaction UI here
        
        public static System.Action<IInteractable> OnInteractableFound;
        public static System.Action OnInteractableLost;
        
        private PlayerInputHandler _inputHandler;
        private Camera _mainCamera;
        private IInteractable _currentInteractable;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _inputHandler = GetComponent<PlayerInputHandler>();
            _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                Debug.LogWarning("[PlayerInteraction] Main Camera not found at spawn. Will try to find in Update.");
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            CheckForInteractable();
            HandleInteractionInput();
        }

        private void CheckForInteractable()
        {
            Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            IInteractable foundInteractable = null;

            if (useSphereCast)
            {
                if (Physics.SphereCast(ray, detectionRadius, out RaycastHit hit, interactionDistance, interactableLayer))
                {
                    foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
                }
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
                {
                    foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
                }
            }

            if (foundInteractable != _currentInteractable)
            {
                if (_currentInteractable != null)
                {
                    _currentInteractable.OnHoverExit();
                    OnInteractableLost?.Invoke();
                }

                _currentInteractable = foundInteractable;

                if (_currentInteractable != null && _currentInteractable.CanInteract)
                {
                    _currentInteractable.OnHoverEnter();
                    OnInteractableFound?.Invoke(_currentInteractable);
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (_inputHandler != null && _inputHandler.InteractPressed)
            {
                if (_currentInteractable != null && _currentInteractable.CanInteract)
                {
                    _currentInteractable.Interact();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_mainCamera == null) return;
            
            Gizmos.color = Color.yellow;
            Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Gizmos.DrawRay(ray.origin, ray.direction * interactionDistance);
            
            if (useSphereCast)
            {
                Gizmos.DrawWireSphere(ray.origin + ray.direction * interactionDistance, detectionRadius);
            }
        }
    }
}
