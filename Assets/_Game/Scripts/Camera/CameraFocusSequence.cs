using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// CameraFocusSequence - Thực hiện việc tập trung camera vào một điểm chỉ định trong một khoảng thời gian.
/// Thường dùng khi hoàn thành nhiệm vụ hoặc mở cửa.
/// </summary>
public class CameraFocusSequence : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Camera sẽ được tập trung vào.")]
    [SerializeField] private CinemachineCamera _focusCamera;

    [Header("Settings")]
    [Tooltip("Thời gian camera giữ ở điểm tập trung (giây).")]
    [SerializeField] private float _focusDuration = 3f;
    
    [Tooltip("Thời gian chờ trước khi bắt đầu chuyển camera (giây).")]
    [SerializeField] private float _startDelay = 0.5f;

    [Tooltip("Ưu tiên của camera khi được kích hoạt (nên cao hơn các camera khác).")]
    [SerializeField] private int _activePriority = 100;

    private int _originalPriority;

    private void Awake()
    {
        if (_focusCamera == null)
            _focusCamera = GetComponent<CinemachineCamera>();
            
        if (_focusCamera != null)
            _originalPriority = _focusCamera.Priority.Value;
    }

    /// <summary>
    /// Bắt đầu chuỗi tập trung camera. Có thể gọi từ UnityEvent.
    /// </summary>
    public void StartFocusSequence()
    {
        if (_focusCamera == null)
        {
            Debug.LogWarning($"[CameraFocusSequence] {gameObject.name} không có Focus Camera!");
            return;
        }

        StartCoroutine(FocusRoutine());
    }

    private IEnumerator FocusRoutine()
    {
        yield return new WaitForSeconds(_startDelay);

        // 1. Thông báo bắt đầu Cutscene để khóa điều khiển người chơi
        EventBus.RaiseCutSceneStarted();

        // 2. Tăng ưu tiên để Cinemachine blend sang camera này
        _focusCamera.Priority.Value = _activePriority;

        // 3. Chờ thời gian quan sát
        yield return new WaitForSeconds(_focusDuration);

        // 4. Trả lại ưu tiên ban đầu để quay về camera người chơi
        _focusCamera.Priority.Value = _originalPriority;

        // 5. Thông báo kết thúc Cutscene để trả lại quyền điều khiển
        EventBus.RaiseCutSceneEnded();
    }
}
