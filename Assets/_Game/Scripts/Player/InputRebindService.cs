using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputRebindService — Cho phép người chơi đổi phím binding.
/// Load binding trước khi PlayerInputHandler khởi tạo (Awake vs Start).
/// Fire EventBus.OnInputBindingChanged sau khi save thành công.
/// SRS §14.6
/// </summary>
public class InputRebindService : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;

    private InputActionRebindingExtensions.RebindingOperation _rebindOp;

    // Actions không được phép rebind
    private static readonly HashSet<string> NON_REBINDABLE = new()
    {
        "SkipCutScene"
    };

    private void Awake()
    {
        LoadBindings(); // Phải chạy trước PlayerInputHandler.Start()
    }

    private void OnDestroy()
    {
        _rebindOp?.Dispose();
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Load binding overrides từ PlayerPrefs và apply vào InputActionAsset.
    /// Gọi trong Awake() — phải chạy trước PlayerInputHandler.
    /// </summary>
    public void LoadBindings()
    {
        var savedJson = PlayerPrefs.GetString(Constants.PlayerPrefsKeys.INPUT_BINDINGS, string.Empty);
        if (!string.IsNullOrEmpty(savedJson))
        {
            _inputActions.LoadBindingOverridesFromJson(savedJson);

#if UNITY_EDITOR || DEBUG_BUILD
            Debug.Log("[InputRebindService] Bindings loaded from PlayerPrefs.");
#endif
        }
    }

    /// <summary>
    /// Bắt đầu quá trình rebind cho 1 action. Async — game vào listen mode.
    /// </summary>
    /// <param name="actionName">Tên action (ví dụ: "Jump")</param>
    /// <param name="bindingIndex">Index của binding trong action (0 = binding chính)</param>
    /// <param name="onComplete">Callback khi rebind xong: (success, newKeyName)</param>
    /// <param name="onConflict">Callback khi conflict: (conflictActionName)</param>
    public void RebindAction(
        string actionName,
        int bindingIndex,
        Action<bool, string> onComplete,
        Action<string> onConflict = null)
    {
        if (NON_REBINDABLE.Contains(actionName))
        {
#if UNITY_EDITOR || DEBUG_BUILD
            Debug.LogWarning($"[InputRebindService] '{actionName}' không thể rebind.");
#endif
            onComplete?.Invoke(false, string.Empty);
            return;
        }

        var action = _inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"[InputRebindService] Action '{actionName}' không tìm thấy.");
            onComplete?.Invoke(false, string.Empty);
            return;
        }

        action.Disable(); // Phải disable trước khi rebind

        _rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                var newKey = op.selectedControl?.displayName ?? string.Empty;

                // Kiểm tra conflict
                var conflict = FindConflict(action, bindingIndex);
                if (conflict != null)
                {
                    // Revert binding này — để caller xử lý Overwrite/Cancel dialog
                    op.Dispose();
                    action.Enable();
                    onConflict?.Invoke(conflict);
                    return;
                }

                op.Dispose();
                action.Enable();
                SaveBindings();
                EventBus.RaiseInputBindingChanged();
                onComplete?.Invoke(true, newKey);

#if UNITY_EDITOR || DEBUG_BUILD
                Debug.Log($"[InputRebindService] '{actionName}' → '{newKey}'");
#endif
            })
            .OnCancel(op =>
            {
                op.Dispose();
                action.Enable();
                onComplete?.Invoke(false, string.Empty);
            })
            .Start();
    }

    /// <summary>Hủy rebind đang chờ input.</summary>
    public void CancelRebind()
    {
        _rebindOp?.Cancel();
    }

    /// <summary>Reset toàn bộ binding về mặc định.</summary>
    public void ResetAllBindings()
    {
        _inputActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(Constants.PlayerPrefsKeys.INPUT_BINDINGS);
        EventBus.RaiseInputBindingChanged();

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log("[InputRebindService] All bindings reset to default.");
#endif
    }

    /// <summary>Lấy display name của binding hiện tại cho action.</summary>
    public string GetBindingDisplayName(string actionName, int bindingIndex = 0)
    {
        var action = _inputActions.FindAction(actionName);
        return action?.GetBindingDisplayString(bindingIndex) ?? string.Empty;
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>Tìm action khác đang dùng cùng binding. Trả null nếu không conflict.</summary>
    private string FindConflict(InputAction rebindingAction, int bindingIndex)
    {
        var newPath = rebindingAction.bindings[bindingIndex].effectivePath;
        foreach (var action in _inputActions)
        {
            if (action == rebindingAction) continue;
            foreach (var binding in action.bindings)
            {
                if (binding.effectivePath == newPath)
                    return action.name;
            }
        }
        return null;
    }

    private void SaveBindings()
    {
        var json = _inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(Constants.PlayerPrefsKeys.INPUT_BINDINGS, json);

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log("[InputRebindService] Bindings saved.");
#endif
    }
}
