using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SeamlessLoadingOverlay — Quản lý màn hình đen và thanh Progress Bar.
/// </summary>
public class SeamlessLoadingOverlay : MonoBehaviour
{
    public static SeamlessLoadingOverlay Instance { get; private set; }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private float _fadeDuration = 0.5f;

    private float _targetProgress = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        
        // Khởi đầu ẩn màn hình load
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
        if (_progressSlider != null) _progressSlider.value = 0;
    }

    private void Update()
    {
        if (_progressSlider != null && _canvasGroup.alpha > 0.01f)
        {
            // Bò dần về phía đích
            _progressSlider.value = Mathf.MoveTowards(_progressSlider.value, _targetProgress, Time.unscaledDeltaTime * 1.2f);
        }
    }

    public void FadeIn(System.Action onComplete = null)
    {
        Debug.Log("[SeamlessLoadingOverlay] FadeIn called");
        StopAllCoroutines();
        _targetProgress = 0f;
        if (_progressSlider != null) _progressSlider.value = 0;
        StartCoroutine(FadeRoutine(1, onComplete));
    }

    public void FadeOut(System.Action onComplete = null)
    {
        Debug.Log("[SeamlessLoadingOverlay] FadeOut called");
        StopAllCoroutines();
        _targetProgress = 1f;
        StartCoroutine(FadeRoutine(0, onComplete));
    }

    public void SetProgress(float value)
    {
        // Debug.Log($"[SeamlessLoadingOverlay] Progress updated: {value}");
        _targetProgress = Mathf.Max(_targetProgress, value);
    }

    private IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete)
    {
        _canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
