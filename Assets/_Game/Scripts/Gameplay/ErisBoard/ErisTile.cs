using UnityEngine;

public class ErisTile : MonoBehaviour
{
    public Vector2Int GridPos;
    [SerializeField] private MeshRenderer _renderer;
    private Outline _outline;
    private bool _isRevealedCorrect = false;

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();
        _outline.enabled = false;
        _outline.OutlineWidth = 5f;
        _outline.OutlineColor = Color.yellow;
    }

    public void Init(Vector2Int pos)
    {
        GridPos = pos;
        ResetTile();
    }

    public void SetOutline(bool active) => _outline.enabled = active;

    public void SetColor(Color color, bool isCorrectStep = false)
    {
        if (_renderer == null) return;
        // Nếu ô này đã từng đi đúng (màu xanh), giữ nguyên màu xanh trừ khi bị Reset hoàn toàn
        if (_isRevealedCorrect && !isCorrectStep && color != Color.white) return; 
        
        _renderer.material.color = color;
        if (isCorrectStep) _isRevealedCorrect = true;
    }

    public void ApplyTemporaryRed()
    {
        // Chỉ đổi màu hiển thị sang đỏ, KHÔNG đổi flag _isRevealedCorrect
        if (_renderer != null) _renderer.material.color = Color.red;
    }

    public void RestoreColor()
    {
        if (_renderer == null) return;
        // Trả lại màu dựa trên việc ô này đã được khám phá đúng hay chưa
        _renderer.material.color = _isRevealedCorrect ? Color.green : Color.white;
    }

    public void ResetTile()
    {
        _isRevealedCorrect = false;
        if (_renderer != null) _renderer.material.color = Color.white;
        SetOutline(false);
    }
}
