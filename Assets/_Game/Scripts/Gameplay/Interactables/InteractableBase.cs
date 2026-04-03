using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : NetworkBehaviour, IInteractable
{
    [Header("Interactable Settings")]
    [SerializeField] private string _interactionPrompt = "Interact";
    [SerializeField] protected string _interactableId = "interactable_001";
    [SerializeField] private bool _canInteract = true;
    [SerializeField] protected bool _allowReactivation = false;
    [SerializeField] private bool _showOutlineOnHover = true;
    [SerializeField] protected float _maxInteractDistance = 3f;
    public UnityEvent OnActivated;

    [Header("Prompt UI")]
    [SerializeField] private GameObject _promptUIPrefab;
    [SerializeField] private Vector3 _promptUIOffset = new Vector3(0f, 1.5f, 0f);

    protected readonly NetworkVariable<bool> _isActivated = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private GameObject _promptUIInstance;
    private Outline _outline;
    private Collider _cachedCollider;

    public string InteractionPrompt => _interactionPrompt;
    public bool CanInteract => _canInteract && (_allowReactivation || !_isActivated.Value);
    public bool IsActivated => _isActivated.Value;

    protected virtual void Awake()
    {
        _outline = GetComponent<Outline>();
        _cachedCollider = GetComponent<Collider>();

        if (_outline != null)
        {
            _outline.enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isActivated.OnValueChanged += OnActivatedValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isActivated.OnValueChanged -= OnActivatedValueChanged;
        OnHoverExit();
        HidePromptUI();
    }

    public virtual void OnHoverEnter()
    {
        if (_showOutlineOnHover && _outline != null)
        {
            _outline.enabled = true;
        }

        if (CanInteract)
        {
            ShowPromptUI();
        }
    }

    public virtual void OnHoverExit()
    {
        if (_outline != null)
        {
            _outline.enabled = false;
        }

        HidePromptUI();
    }

    public abstract void Interact(ulong playerId);

    protected bool CanPlayerInteract(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client)) return false;
        if (client.PlayerObject == null) return false;

        Vector3 playerPosition = client.PlayerObject.transform.position;
        Vector3 targetPosition = GetInteractionPoint(playerPosition);
        return Vector3.Distance(playerPosition, targetPosition) <= _maxInteractDistance;
    }

    protected bool TryGetPlayerObject(ulong clientId, out NetworkObject playerObject)
    {
        playerObject = null;

        if (NetworkManager.Singleton == null) return false;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client)) return false;
        if (client.PlayerObject == null) return false;

        playerObject = client.PlayerObject;
        return true;
    }

    protected virtual Vector3 GetInteractionPoint(Vector3 playerPosition)
    {
        if (_cachedCollider == null)
        {
            _cachedCollider = GetComponent<Collider>();
        }

        if (_cachedCollider == null)
        {
            return transform.position;
        }

        return _cachedCollider.ClosestPoint(playerPosition);
    }

    protected void ServerActivate()
    {
        if (!IsServer) return;
        if (_isActivated.Value && !_allowReactivation) return;

        _isActivated.Value = true;
        Debug.Log($"[InteractableBase] <color=cyan>{_interactableId}</color> kich hoat tren Server.");
    }

    protected void ServerDeactivate()
    {
        if (!IsServer) return;
        _isActivated.Value = false;
    }

    protected virtual void OnActivatedValueChanged(bool previousValue, bool newValue)
    {
        if (newValue && !previousValue)
        {
            if (!_allowReactivation)
            {
                HidePromptUI();
            }

            OnActivated?.Invoke();
            EventBus.RaiseInteractableActivated(_interactableId);
        }
    }

    protected void ShowPromptUI()
    {
        if (_promptUIInstance != null)
        {
            _promptUIInstance.SetActive(true);
            return;
        }

        if (_promptUIPrefab == null) return;

        _promptUIInstance = Instantiate(
            _promptUIPrefab,
            transform.position + _promptUIOffset,
            Quaternion.identity,
            transform);
        _promptUIInstance.transform.localPosition = _promptUIOffset;
    }

    protected void HidePromptUI()
    {
        if (_promptUIInstance != null)
        {
            _promptUIInstance.SetActive(false);
        }
    }

    public void SetInteractable(bool state)
    {
        _canInteract = state;
        if (!state)
        {
            OnHoverExit();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _maxInteractDistance);
    }
}
