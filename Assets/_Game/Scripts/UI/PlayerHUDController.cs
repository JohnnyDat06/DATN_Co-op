using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// PlayerHUDController — Quản lý UI thanh máu cho Host và Client.
/// Đồng bộ trạng thái từ NetworkVariable của PlayerHealth.
/// Sử dụng Lerp để trượt thanh máu mượt mà.
/// SRS §9.3
/// </summary>
public class PlayerHUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _hostSlider;
    [SerializeField] private Slider _clientSlider;
    [SerializeField] private GameObject _hostBarRoot;
    [SerializeField] private GameObject _clientBarRoot;

    [Header("Settings")]
    [SerializeField] private float _smoothSpeed = 5f;
    [SerializeField] private bool _hideIfFull = false;

    private PlayerHealth _hostHealth;
    private PlayerHealth _clientHealth;

    private float _targetHostValue = 1f;
    private float _targetClientValue = 1f;

    private void Start()
    {
        // Khởi tạo ẩn nếu chưa có player
        if (_hostBarRoot != null) _hostBarRoot.SetActive(false);
        if (_clientBarRoot != null) _clientBarRoot.SetActive(false);
    }

    private void Update()
    {
        // Tìm player nếu chưa có
        if (_hostHealth == null || _clientHealth == null)
        {
            FindPlayers();
        }

        // Cập nhật giá trị Slider mượt mà bằng Lerp
        UpdateSliderSmoothly(_hostSlider, _targetHostValue, _hostBarRoot);
        UpdateSliderSmoothly(_clientSlider, _targetClientValue, _clientBarRoot);
    }

    private void FindPlayers()
    {
        var allPlayers = Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (!player.IsSpawned) continue;

            // Host thường có OwnerClientId = 0
            if (player.OwnerClientId == 0)
            {
                if (_hostHealth == null)
                {
                    _hostHealth = player;
                    _hostHealth.OnHealthChanged += HandleHostHealthChanged;
                    if (_hostBarRoot != null) _hostBarRoot.SetActive(true);
                    _targetHostValue = _hostHealth.CurrentHealth / _hostHealth.MaxHealth;
                    _hostSlider.value = _targetHostValue;
                }
            }
            else
            {
                if (_clientHealth == null)
                {
                    _clientHealth = player;
                    _clientHealth.OnHealthChanged += HandleClientHealthChanged;
                    if (_clientBarRoot != null) _clientBarRoot.SetActive(true);
                    _targetClientValue = _clientHealth.CurrentHealth / _clientHealth.MaxHealth;
                    _clientSlider.value = _targetClientValue;
                }
            }
        }
    }

    private void HandleHostHealthChanged(float curr, float max)
    {
        _targetHostValue = Mathf.Clamp01(curr / max);
    }

    private void HandleClientHealthChanged(float curr, float max)
    {
        _targetClientValue = Mathf.Clamp01(curr / max);
    }

    private void UpdateSliderSmoothly(Slider slider, float targetValue, GameObject root)
    {
        if (slider == null) return;

        // Lerp để trượt mượt
        slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * _smoothSpeed);

        // Logic ẩn/hiện nếu cần
        if (_hideIfFull && root != null)
        {
            bool isFull = slider.value >= 0.999f && targetValue >= 1f;
            if (root.activeSelf == isFull) root.SetActive(!isFull);
        }
    }

    private void OnDestroy()
    {
        if (_hostHealth != null) _hostHealth.OnHealthChanged -= HandleHostHealthChanged;
        if (_clientHealth != null) _clientHealth.OnHealthChanged -= HandleClientHealthChanged;
    }
}
