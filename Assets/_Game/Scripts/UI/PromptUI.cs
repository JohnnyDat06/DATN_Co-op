using UnityEngine;

/// <summary>
/// [DEPRECATED] PromptUI — Script này đã được thay thế hoàn toàn bởi InteractPromptHUD.cs.
/// Giữ lại file này để Unity không báo missing script trên các Prefab cũ chưa clean up.
/// Hãy xóa component này khỏi mọi Prefab/GameObject và dùng InteractPromptHUD thay vào.
/// </summary>
[System.Obsolete("Đã bị thay thế bởi InteractPromptHUD. Hãy xóa component này và dùng InteractPromptHUD.")]
public class PromptUI : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning(
            "[PromptUI] Component này đã lỗi thời (DEPRECATED). " +
            "Hãy xóa nó khỏi Prefab và dùng InteractPromptHUD trên Canvas HUD thay thế.",
            this
        );
    }
}
