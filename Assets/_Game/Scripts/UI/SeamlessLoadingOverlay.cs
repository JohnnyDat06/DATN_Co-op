using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SeamlessLoadingOverlay — Quản lý màn hình đen và thanh Progress Bar.
/// </summary>
public class SeamlessLoadingOverlay : MonoBehaviour
{
    public static SeamlessLoadingOverlay Instance { get; private set; }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _toBeContinuedText;
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
        if (_toBeContinuedText != null) _toBeContinuedText.gameObject.SetActive(false);

        // Đăng ký sự kiện khi nạp scene mới để tự động dọn dẹp
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Nếu quay về Lobby, tự động ẩn màn hình đen và các chữ "To Be Continued"
        if (scene.name.Contains("Lobby"))
        {
            Debug.Log("[SeamlessLoadingOverlay] Lobby detected. Auto-cleaning overlay.");
            ShowToBeContinued(false);
            ShowProgressBar(true);
            FadeOut();
        }
    }

    private void Update()
    {
        if (_progressSlider != null && _canvasGroup.alpha > 0.01f)
        {
            // Bò dần về phía đích
            _progressSlider.value = Mathf.MoveTowards(_progressSlider.value, _targetProgress, Time.unscaledDeltaTime * 1.2f);
        }
    }

    public void ShowToBeContinued(bool show, string text = "To Be Continued!")
    {
        if (_toBeContinuedText != null)
        {
            _toBeContinuedText.text = text;
            _toBeContinuedText.gameObject.SetActive(show);
        }
    }

    public void ShowProgressBar(bool show)
    {
        if (_progressSlider != null)
        {
            _progressSlider.gameObject.SetActive(show);
        }
    }

    public void FadeIn(System.Action onComplete = null)
    {
        Debug.Log("[SeamlessLoadingOverlay] FadeIn called");
        StopAllCoroutines();
        _targetProgress = 0f;
        if (_progressSlider != null) _progressSlider.value = 0;
        
        // Mặc định hiện slider, trừ khi được bảo ẩn đi trước đó
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
