using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TempHealthDisplay — HUD tạm để test HP trong P1.
/// Sẽ bị thay bởi HUDController ở T6-2.
/// SRS §9.3
/// </summary>
public class TempHealthDisplay : MonoBehaviour
{
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private GameObject _hpBarRoot; // ẩn/hiện

    private void OnEnable()  => _health.OnHealthChanged += UpdateDisplay;
    private void OnDisable() => _health.OnHealthChanged -= UpdateDisplay;

    private void Start()
    {
        // Ẩn khi đầy máu (SRS §4.1.3)
        if (_hpBarRoot != null)
            _hpBarRoot.SetActive(false);
    }

    private void UpdateDisplay(float current, float max)
    {
        if (_hpSlider != null)
            _hpSlider.value = current / max;

        // Hiện khi HP < 100%, ẩn khi đầy (SRS §9.3)
        if (_hpBarRoot != null)
            _hpBarRoot.SetActive(current < max);
    }
}
