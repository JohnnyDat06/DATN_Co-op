using UnityEngine;
using TMPro;

/// <summary>
/// PromptUI — Billboard UI hiển thị gợi ý tương tác phía trên vật thể.
/// Tự xoay mặt về phía Camera. Lắng nghe OnInputBindingChanged để cập nhật icon phím.
/// Lắng nghe OnAccessibilityChanged để thay đổi cỡ chữ.
/// SRS §9.2
/// </summary>
public class PromptUI : MonoBehaviour
{
    // ─── Inspector Fields ─────────────────────────────────────────────────────

    [Header("Solo Prompt")]
    [Tooltip("Nhãn hiển thị tên phím bấm (ví dụ: [E]).")]
    [SerializeField] private TextMeshProUGUI _keyLabel;

    [Tooltip("Nhãn hiển thị tên hành động (ví dụ: Tương tác).")]
    [SerializeField] private TextMeshProUGUI _actionLabel;

    [Header("Font Sizes")]
    [SerializeField] private float _fontSizeSmall  = 14f;
    [SerializeField] private float _fontSizeNormal = 18f;
    [SerializeField] private float _fontSizeLarge  = 24f;

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private Camera _mainCamera;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        EventBus.OnInputBindingChanged  += RefreshKeyLabel;
        EventBus.OnAccessibilityChanged += RefreshFontSize;
        RefreshKeyLabel();
        RefreshFontSize();
    }

    private void OnDisable()
    {
        EventBus.OnInputBindingChanged  -= RefreshKeyLabel;
        EventBus.OnAccessibilityChanged -= RefreshFontSize;
    }

    private void LateUpdate()
    {
        // Billboard: luôn xoay mặt về phía Camera
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            return;
        }
        transform.forward = _mainCamera.transform.forward;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Đặt text hành động hiển thị trên Prompt (ví dụ: "Mở cửa", "Kéo hộp").
    /// </summary>
    public void SetActionText(string actionText)
    {
        if (_actionLabel != null)
            _actionLabel.text = actionText;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật nhãn phím bấm từ InputSystem binding hiện tại.
    /// Gọi khi enable và khi OnInputBindingChanged.
    /// </summary>
    private void RefreshKeyLabel()
    {
        // TODO: Khi InputRebindService (T1-6) xây xong, truy vấn binding thực tế.
        // Hiện tại hiển thị mặc định.
        if (_keyLabel != null)
            _keyLabel.text = "[E]";
    }

    /// <summary>
    /// Cập nhật cỡ chữ theo Accessibility setting.
    /// </summary>
    private void RefreshFontSize()
    {
        int sizeKey = PlayerPrefs.GetInt(Constants.PlayerPrefsKeys.ACCESSIBILITY_PROMPT_SIZE, 1);
        float targetSize = sizeKey switch
        {
            0 => _fontSizeSmall,
            2 => _fontSizeLarge,
            _ => _fontSizeNormal
        };

        if (_keyLabel    != null) _keyLabel.fontSize    = targetSize;
        if (_actionLabel != null) _actionLabel.fontSize = targetSize;
    }
}
