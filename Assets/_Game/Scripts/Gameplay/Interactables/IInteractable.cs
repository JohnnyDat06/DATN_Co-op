/// <summary>
/// Interface cơ bản cho mọi vật thể tương tác trong game.
/// Implement bởi InteractableBase và các class con: SoloInteractable, CoopInteractable.
/// SRS §4.2.1 · §4.2.2
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Được gọi khi Player thực hiện tương tác.
    /// </summary>
    /// <param name="playerId">NetworkClientId của Player thực hiện tương tác.</param>
    void Interact(ulong playerId);

    /// <summary>
    /// True nếu vật thể đã được kích hoạt (IsActivated = true).
    /// </summary>
    bool IsActivated { get; }
}
