using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract { get; }
    
    void OnHoverEnter();
    void OnHoverExit();
    void Interact();
}
