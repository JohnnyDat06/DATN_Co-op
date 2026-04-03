using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class SoloInteractable : InteractableBase
{
    [Header("Events")]
    [SerializeField] private UnityEvent onInteracted;

    public override void Interact(ulong playerId)
    {
        if (!CanInteract) return;

        ActivateServerRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateServerRpc(ulong playerId)
    {
        Debug.Log($"[SoloInteractable] <color=cyan>{_interactableId}</color> - Player {playerId} gui yeu cau kich hoat.");

        if (!CanInteract) return;
        if (!CanPlayerInteract(playerId)) return;

        ServerActivate();
    }

    protected override void OnActivatedValueChanged(bool previousValue, bool newValue)
    {
        base.OnActivatedValueChanged(previousValue, newValue);

        if (newValue && !previousValue)
        {
            onInteracted?.Invoke();
        }
    }
}
