using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Outline))]
public abstract class InteractableBase : NetworkBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "Interact";
    [SerializeField] private bool canInteract = true;
    [SerializeField] protected bool showOutlineOnHover = true;

    private Outline _outline;

    public virtual string InteractionPrompt => interactionPrompt;
    public virtual bool CanInteract => canInteract;

    public bool ShowOutlineOnHover
    {
        get => showOutlineOnHover;
        set
        {
            showOutlineOnHover = value;
            if (!value && _outline != null)
            {
                _outline.enabled = false;
            }
        }
    }

    protected virtual void Awake()
    {
        _outline = GetComponent<Outline>();
        if (_outline != null)
        {
            _outline.enabled = false;
        }
    }

    public virtual void OnHoverEnter()
    {
        if (showOutlineOnHover && _outline != null)
        {
            _outline.enabled = true;
        }
    }

    public virtual void OnHoverExit()
    {
        if (_outline != null)
        {
            _outline.enabled = false;
        }
    }

    public abstract void Interact();

    public void SetInteractable(bool state)
    {
        canInteract = state;
    }
}
