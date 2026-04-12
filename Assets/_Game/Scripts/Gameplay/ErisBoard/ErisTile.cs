using UnityEngine;
using MoreMountains.Feedbacks;

public class ErisTile : MonoBehaviour
{
    public Vector2Int GridPos;
    [SerializeField] private MeshRenderer _renderer;
    private Outline _outline;
    private bool _isRevealedCorrect = false;

    [Header("Feel Feedbacks")]
    [SerializeField] private MMF_Player _spawnFeedback;
    [SerializeField] private MMF_Player _despawnFeedback;
    [SerializeField] private MMF_Player _correctFeedback;
    [SerializeField] private MMF_Player _wrongFeedback;
    [SerializeField] private MMF_Player _highlightFeedback;
    [SerializeField] private MMF_Player _idleBounceFeedback; // Feedbacks cho việc nhúng nhảy chờ đợi

    private Vector3 _originalLocalScale;
    private Vector3 _originalLocalPosition;
    private bool _isInitialized = false;

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();
        _outline.enabled = false;
        _outline.OutlineWidth = 4f; 
        _outline.OutlineColor = Color.cyan; 

        _originalLocalScale = transform.localScale;
        _originalLocalPosition = transform.localPosition;
        _isInitialized = true;
    }

    public void Init(Vector2Int pos)
    {
        GridPos = pos;
        ResetTile();
        PlaySpawnFeedback();
    }

    private void ResetTransformImmediate()
    {
        if (!_isInitialized) return;
        
        try {
            if (_spawnFeedback != null && _spawnFeedback.IsPlaying) _spawnFeedback.StopFeedbacks();
            if (_correctFeedback != null && _correctFeedback.IsPlaying) _correctFeedback.StopFeedbacks();
            if (_wrongFeedback != null && _wrongFeedback.IsPlaying) _wrongFeedback.StopFeedbacks();
            if (_highlightFeedback != null && _highlightFeedback.IsPlaying) _highlightFeedback.StopFeedbacks();
            if (_idleBounceFeedback != null && _idleBounceFeedback.IsPlaying) _idleBounceFeedback.StopFeedbacks();
        } catch { }

        transform.localScale = _originalLocalScale;
        transform.localPosition = _originalLocalPosition;
    }

    public void PlayIdleBounce()
    {
        // KHÔNG nhúng nhảy nếu đã đi qua (màu xanh) hoặc đang biến mất
        if (_isRevealedCorrect) return;
        
        if (_idleBounceFeedback != null && !_idleBounceFeedback.IsPlaying)
        {
            _idleBounceFeedback.PlayFeedbacks();
        }
        else if (_idleBounceFeedback == null && _correctFeedback != null && !_correctFeedback.IsPlaying)
        {
            // Fallback dùng tạm Correct feedback nếu bạn chưa gán Idle slot
            _correctFeedback.PlayFeedbacks();
        }
    }

    public void SetHighlight(bool active) 
    {
        if (active)
        {
            if (_highlightFeedback != null && !_highlightFeedback.IsPlaying) 
            {
                ResetTransformImmediate();
                try { _highlightFeedback.PlayFeedbacks(); } catch {}
            }
            _outline.enabled = true;
        }
        else
        {
            try { if (_highlightFeedback != null) _highlightFeedback.StopFeedbacks(); } catch {}
            _outline.enabled = false;
        }
    }

    public void SetColor(Color color, bool isCorrectStep = false)
    {
        if (_renderer == null) return;
        if (_isRevealedCorrect && color == Color.white) return; 
        
        _renderer.material.color = color;
        
        if (isCorrectStep) 
        {
            _isRevealedCorrect = true;
            if (_correctFeedback != null) 
            {
                ResetTransformImmediate();
                try { _correctFeedback.PlayFeedbacks(); } catch {}
            }
        }
    }

    public void ApplyTemporaryRed()
    {
        if (_renderer != null) _renderer.material.color = Color.red;
        if (_wrongFeedback != null) 
        {
            ResetTransformImmediate();
            try { _wrongFeedback.PlayFeedbacks(); } catch {}
        }
        SetHighlight(false);
    }

    public void RestoreColor()
    {
        if (_renderer == null) return;
        _renderer.material.color = _isRevealedCorrect ? Color.green : Color.white;
    }

    public void ResetTile()
    {
        _isRevealedCorrect = false;
        if (_renderer != null) _renderer.material.color = Color.white;
        SetHighlight(false);
        ResetTransformImmediate();
    }

    public void PlaySpawnFeedback()
    {
        if (_spawnFeedback != null) 
        {
            ResetTransformImmediate();
            try { _spawnFeedback.PlayFeedbacks(); } catch {}
        }
    }

    public void PlayDespawnEffect()
    {
        ResetTransformImmediate();
        if (_despawnFeedback != null) 
        {
            try { _despawnFeedback.PlayFeedbacks(); } catch {}
        }
        else
        {
            Destroy(gameObject, 0.2f);
        }
    }
}
