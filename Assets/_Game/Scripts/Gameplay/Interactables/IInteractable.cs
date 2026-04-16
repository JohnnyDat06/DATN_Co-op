using UnityEngine;

/// <summary>
/// Interface chung cho moi vat the co the tuong tac trong game.
/// </summary>
public interface IInteractable
{
    string InteractionPrompt { get; }
    bool CanInteract { get; }
    bool IsActivated { get; }

    void OnHoverEnter();
    void OnHoverExit();
    void Interact(ulong playerId);

    /// <summary>
    /// Tra ve Transform dung de hien thi Prompt UI.
    /// </summary>
    Transform GetPromptTransform();
}
