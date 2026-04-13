using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyHealthBar — Quản lý thanh máu UI phía trên đầu quái.
/// Tự động ẩn/hiện dựa trên trạng thái phát hiện của EnemyCombat.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth _enemyHealth;
    [SerializeField] private EnemyCombat _enemyCombat;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private GameObject _uiContainer; // Nhóm chứa toàn bộ UI để ẩn/hiện

    [Header("Settings")]
    [SerializeField] private bool _faceCamera = true;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;

        if (_enemyHealth != null)
        {
            // Khởi tạo giá trị ban đầu
            UpdateSlider(_enemyHealth.CurrentHealth.Value, _enemyHealth.CurrentHealth.Value);
            
            // Đăng ký sự kiện thay đổi máu
            _enemyHealth.OnHealthChanged += UpdateSlider;
        }

        // Mặc định ẩn thanh máu
        if (_uiContainer != null) _uiContainer.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnHealthChanged -= UpdateSlider;
        }
    }

    private void Update()
    {
        // 1. Đồng bộ ẩn/hiện với trạng thái IsDetected từ EnemyCombat
        HandleVisibility();

        // 2. Luôn xoay mặt về phía Camera (Billboarding)
        if (_faceCamera && _mainCamera != null && _uiContainer.activeSelf)
        {
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void HandleVisibility()
    {
        if (_enemyCombat == null || _uiContainer == null) return;

        bool shouldShow = _enemyCombat.IsDetected.Value;
        
        // Chỉ SetActive khi trạng thái thay đổi để tối ưu
        if (_uiContainer.activeSelf != shouldShow)
        {
            _uiContainer.SetActive(shouldShow);
        }
    }

    private void UpdateSlider(int oldVal, int newVal)
    {
        if (_healthSlider != null && _enemyHealth != null)
        {
            _healthSlider.maxValue = _enemyHealth.MaxHealth;
            _healthSlider.value = newVal;
        }
    }
}
