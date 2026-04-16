using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UIButtonJuice — Phiên bản "Sức Sống": Thêm hiệu ứng Rung (Wiggle) và Nảy (Punch).
/// </summary>
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover - Wiggle & Scale")]
    public float hoverScale = 1.15f;
    public float wiggleIntensity = 3f; // Độ nghiêng khi rung (độ)
    public float wiggleSpeed = 20f;    // Tốc độ rung

    [Header("Click - Squash")]
    public float clickScale = 0.85f;
    
    [Header("Settings")]
    public float animationSpeed = 12f;

    [Header("Audio")]
    public SOAudioClip hoverSFX;
    public SOAudioClip clickSFX;

    private Vector3 _originalScale;
    private Quaternion _originalRotation;
    private Vector3 _targetScale;
    private bool _isHovering = false;
    private Coroutine _juiceCoroutine;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _originalRotation = transform.localRotation;
        _targetScale = _originalScale;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        transform.localScale = _originalScale;
        transform.localRotation = _originalRotation;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        _targetScale = _originalScale * hoverScale;
        
        // Hiệu ứng "Punch" nảy lên một cái khi vừa chạm vào
        transform.localScale = _originalScale * (hoverScale + 0.1f); 
        
        StartJuice();

        if (hoverSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hoverSFX);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        _targetScale = _originalScale;
        StartJuice();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = _originalScale * clickScale;
        StartJuice();

        if (clickSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSFX);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _targetScale = _isHovering ? _originalScale * hoverScale : _originalScale;
        StartJuice();
    }

    private void StartJuice()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_juiceCoroutine != null) StopCoroutine(_juiceCoroutine);
        _juiceCoroutine = StartCoroutine(JuiceRoutine());
    }

    private IEnumerator JuiceRoutine()
    {
        while (true)
        {
            // 1. Xử lý Scale (Phóng to/Thu nhỏ/Nảy)
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * animationSpeed);

            // 2. Xử lý Rung (Wiggle) khi đang hover
            if (_isHovering)
            {
                float angle = Mathf.Sin(Time.unscaledTime * wiggleSpeed) * wiggleIntensity;
                transform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // Trở về góc xoay ban đầu
                transform.localRotation = Quaternion.Lerp(transform.localRotation, _originalRotation, Time.unscaledDeltaTime * animationSpeed);
            }

            // Dừng coroutine nếu mọi thứ đã về vị trí cũ và không còn hover
            if (!_isHovering && 
                Vector3.Distance(transform.localScale, _originalScale) < 0.001f && 
                Quaternion.Angle(transform.localRotation, _originalRotation) < 0.1f)
            {
                transform.localScale = _originalScale;
                transform.localRotation = _originalRotation;
                yield break;
            }

            yield return null;
        }
    }
}
