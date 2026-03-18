using UnityEngine;

/// <summary>
/// SOCoopInteractableConfig — Config data cho CoopInteractable (cần 2 player cùng kích hoạt).
/// SRS §13.3 · §5.2
/// </summary>
[CreateAssetMenu(fileName = "SOCoopInteractableConfig", menuName = "CoopGame/Gameplay/CoopInteractableConfig")]
public class SOCoopInteractableConfig : ScriptableObject
{
    [Tooltip("Bán kính trigger mỗi player (m)")]
    public float TriggerRadius = 1.5f;

    [Tooltip("Thời gian timeout — nếu 1 player rời trigger sau khi ready (giây); 0 = không timeout, reset ngay khi rời")]
    public float TimeoutDuration = 0f;

    [Tooltip("Sprite icon khi player đã sẵn sàng (sáng)")]
    public Sprite PromptIconReady;

    [Tooltip("Sprite icon khi đang chờ player (xám)")]
    public Sprite PromptIconWaiting;
}
