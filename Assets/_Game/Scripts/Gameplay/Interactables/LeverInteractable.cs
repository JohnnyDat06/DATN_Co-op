using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// LeverInteractable — Công tắc toggle: kích hoạt/tắt luân phiên mỗi khi Player tương tác.
/// Ví dụ: Đòn bẩy mở/đóng cửa, công tắc bật/tắt bẫy.
/// SRS §4.2.1
/// </summary>
public class LeverInteractable : InteractableBase
{
    // ─── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Lever Settings")]
    [Tooltip("Sự kiện khi lever bị tắt (từ ON → OFF).")]
    public UnityEvent OnDeactivated;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Lever luôn cho phép reactivation (toggle)
        _allowReactivation = true;
    }

    // ─── IInteractable Override ───────────────────────────────────────────────

    /// <summary>
    /// Đảo trạng thái lever mỗi khi Player tương tác.
    /// </summary>
    public override void Interact(ulong playerId)
    {
        if (!_playersInRange.Contains(playerId)) return;
        ToggleServerRpc(playerId);
    }

    // ─── ServerRpc ────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void ToggleServerRpc(ulong playerId)
    {
        Debug.Log($"[LeverInteractable] <color=yellow>{_interactableId}</color> — Player {playerId} toggle. Trạng thái hiện tại: {IsActivated}");

        if (!IsActivated)
        {
            // OFF → ON
            ServerActivate();
        }
        else
        {
            // ON → OFF
            ServerDeactivate();
        }
    }

    // ─── NetworkVariable Callback ─────────────────────────────────────────────

    protected override void OnActivatedValueChanged(bool previousValue, bool newValue)
    {
        base.OnActivatedValueChanged(previousValue, newValue);

        // OFF -> ON được xử lý bởi base (gọi OnActivated)
        
        // Cụ thể cho Lever: ON -> OFF
        if (!newValue && previousValue)
        {
            OnDeactivated?.Invoke();
            Debug.Log($"[LeverInteractable] {_interactableId} đã TẮT đồng bộ trên Client/Host.");
        }
    }
}
