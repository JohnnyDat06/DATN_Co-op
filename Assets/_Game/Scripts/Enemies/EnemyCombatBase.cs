using UnityEngine;
using Unity.Netcode;
using Unity.Behavior;

/// <summary>
/// EnemyCombatBase — Lớp cơ sở trừu tượng cho hệ thống chiến đấu Enemy.
/// Quản lý target tracking, đồng bộ trạng thái phát hiện, và xoay mặt về phía mục tiêu.
/// Mỗi loại quái (Armadil, Owl, Boss...) kế thừa class này và tự triển khai logic tấn công riêng.
/// </summary>
public abstract class EnemyCombatBase : NetworkBehaviour
{
    #region Target Settings
    [Header("Target Settings")]
    [Tooltip("Mục tiêu hiện tại (Có thể gán thủ công hoặc qua AI Action)")]
    public GameObject Target;
    
    [Tooltip("Trạng thái phát hiện đồng bộ mạng")]
    public NetworkVariable<bool> IsDetected = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    #endregion

    #region Blackboard References
    [Header("Blackboard References (Optional)")]
    [Tooltip("Dùng để debug hoặc tự động lấy nếu gán trong Inspector")]
    [SerializeReference] public BlackboardVariable<GameObject> BlackboardTarget;
    [SerializeReference] public BlackboardVariable<bool> BlackboardIsDetected;

    [Header("Behavior Graph Settings (Auto-Link)")]
    [SerializeField] private string _playerVariableName = "Player";
    [SerializeField] private string _detectedVariableName = "IsDetected";
    #endregion

    #region Combat Settings
    [Header("Combat Settings")]
    [SerializeField] protected float _rotationSpeed = 360f;
    #endregion

    #region Internal State
    private BehaviorGraphAgent _agent;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        _agent = GetComponent<BehaviorGraphAgent>();
    }

    protected virtual void Update()
    {
        if (!IsServer) return;

        // Ưu tiên lấy từ Blackboard nếu Target đang null
        UpdateTargetFromBlackboard();

        if (IsDetected.Value && Target != null)
        {
            RotateTowardsTarget(Target);
        }

        // Hook cho subclass xử lý logic chiến đấu mỗi frame
        OnCombatUpdate();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Gán mục tiêu chiến đấu trực tiếp (Dùng bởi AI Action).
    /// </summary>
    public void SetCombatTarget(GameObject target, bool detected)
    {
        Target = target;
        IsDetected.Value = detected;
    }
    #endregion

    #region Protected Hooks
    /// <summary>
    /// Hook cho subclass xử lý logic combat mỗi frame (Server-only).
    /// Ví dụ: MeleeEnemyCombat dùng để check OverlapSphere liên tục trong thời gian active.
    /// </summary>
    protected virtual void OnCombatUpdate() { }
    #endregion

    #region Blackboard Bridge
    /// <summary>
    /// Cập nhật Target và IsDetected từ Behavior Graph Blackboard.
    /// </summary>
    protected void UpdateTargetFromBlackboard()
    {
        // 1. Cập nhật Target (Mục tiêu)
        if (BlackboardTarget != null && BlackboardTarget.Value != null)
        {
            Target = BlackboardTarget.Value;
        }
        else if (_agent != null)
        {
            // Fallback: Tìm theo tên trong Blackboard sử dụng API GetVariable
            if (_agent.GetVariable(_playerVariableName, out BlackboardVariable<GameObject> playerVar))
            {
                Target = playerVar != null ? playerVar.Value : null;
            }
        }
        
        // 2. Cập nhật Trạng thái phát hiện
        bool currentDetection = false;
        bool hasDetectionSource = false;

        if (BlackboardIsDetected != null)
        {
            currentDetection = BlackboardIsDetected.Value;
            hasDetectionSource = true;
        }
        else if (_agent != null)
        {
            // Fallback: Tìm theo tên trong Blackboard sử dụng API GetVariable
            if (_agent.GetVariable(_detectedVariableName, out BlackboardVariable<bool> detectedVar))
            {
                currentDetection = detectedVar != null ? detectedVar.Value : false;
                hasDetectionSource = true;
            }
        }

        if (hasDetectionSource && IsDetected.Value != currentDetection)
        {
            IsDetected.Value = currentDetection;
        }
    }
    #endregion

    #region Rotation
    /// <summary>
    /// Xoay enemy về phía mục tiêu trên mặt phẳng XZ.
    /// </summary>
    protected void RotateTowardsTarget(GameObject target)
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }
    #endregion
}
