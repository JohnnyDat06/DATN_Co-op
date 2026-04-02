using Unity.Netcode;
using UnityEngine;

/// <summary>
/// SoloInteractable — Vật thể chỉ cần 1 Player tương tác để kích hoạt (one-shot).
/// Ví dụ: Nút bấm mở cửa, công tắc đèn đơn giản.
/// Khi Player bấm Interact → ServerRpc → Server set IsActivated = true → OnActivated fire.
/// SRS §4.2.1
/// </summary>
public class SoloInteractable : InteractableBase
{
    // ─── IInteractable Override ───────────────────────────────────────────────

    /// <summary>
    /// Gọi bởi PlayerStateMachine khi Player bấm nút Interact và đang trong range.
    /// Gửi ServerRpc để server xét duyệt và kích hoạt.
    /// </summary>
    public override void Interact(ulong playerId)
    {
        // Chỉ cho Interact khi chưa được kích hoạt (hoặc cho phép reactivation)
        if (IsActivated && !_allowReactivation) return;
        if (!_playersInRange.Contains(playerId)) return;

        ActivateServerRpc(playerId);
    }

    // ─── ServerRpc ────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void ActivateServerRpc(ulong playerId)
    {
        Debug.Log($"[SoloInteractable] <color=cyan>{_interactableId}</color> — Player {playerId} gửi yêu cầu kích hoạt.");

        // Double-check trên Server
        if (IsActivated && !_allowReactivation)
        {
            Debug.Log($"[SoloInteractable] {_interactableId} đã kích hoạt, bỏ qua.");
            return;
        }

        ServerActivate();
    }
}
