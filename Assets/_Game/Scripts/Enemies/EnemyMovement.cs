using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hệ thống di chuyển của Enemy đồng bộ online.
/// Sử dụng NavMesh để dẫn đường và Root Motion để di chuyển thực tế.
/// Animation được kiểm soát bằng tham số "Speed" (0: Idle, 1: Walk, 2: Run).
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class EnemyMovement : NetworkBehaviour
{
    #region Configuration
    [Header("Movement Settings")]
    [Tooltip("Tốc độ xoay của enemy.")]
    [SerializeField] protected float _alignSpeed = 360f;

    [Tooltip("Khoảng cách để dừng lại khi đến đích.")]
    [SerializeField] protected float _stopDistance = 0.1f;

    [Header("Animation Speed Scale")]
    [Tooltip("Tỉ lệ tốc độ animation khi đi bộ (ảnh hưởng trực tiếp đến tốc độ di chuyển Root Motion).")]
    [SerializeField] protected float _walkSpeedScale = 1f;

    [Tooltip("Tỉ lệ tốc độ animation khi chạy (ảnh hưởng trực tiếp đến tốc độ di chuyển Root Motion).")]
    [SerializeField] protected float _runSpeedScale = 1f;

    [Header("State Control")]
    [Tooltip("Cờ xác định enemy đang đi bộ hay chạy.")]
    [SerializeField] protected bool _isRunning = false;
    #endregion

    #region Internal State
    protected NavMeshAgent _agent;
    protected Animator _animator;

    protected bool _hasTarget;

    // Animator Parameter Hashes
    protected readonly int _hashSpeed = Animator.StringToHash("Speed");
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        // Chỉ Server mới xử lý logic di chuyển và NavMesh
        if (!IsServer)
        {
            _agent.enabled = false;
            this.enabled = false;
            return;
        }

        // Vô hiệu hóa cập nhật tự động của Agent để dùng Root Motion
        _agent.updateRotation = false;
        _agent.updatePosition = false;
    }

    protected virtual void Update()
    {
        if (!IsServer) return;

        if (_hasTarget && !_agent.isStopped)
        {
            HandleNormalMovement();
        }
    }

    protected virtual void OnAnimatorMove()
    {
        if (!IsServer || _animator == null) return;

        // Di chuyển bằng Root Motion
        Vector3 deltaPosition = _animator.deltaPosition;
        Vector3 newPos = transform.position + deltaPosition;

        if (_agent != null && _agent.isOnNavMesh)
        {
            // 1. Kiểm tra va chạm tường (Wall Check)
            if (_agent.Raycast(newPos, out NavMeshHit hit))
            {
                newPos.x = hit.position.x;
                newPos.z = hit.position.z;
            }

            // 2. Bám mặt đất (Ground Snap)
            if (NavMesh.SamplePosition(newPos, out NavMeshHit heightHit, 1.0f, NavMesh.AllAreas))
            {
                newPos.y = Mathf.Lerp(transform.position.y, heightHit.position.y, 20f * Time.deltaTime);
            }

            transform.position = newPos;
            
            // Cập nhật vị trí logic cho Agent
            _agent.nextPosition = transform.position;
        }
        else
        {
            transform.position = newPos;
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Ra lệnh cho enemy di chuyển tới vị trí đích.
    /// </summary>
    public virtual void MoveTo(Vector3 position, bool run = false)
    {
        if (!IsServer) return;
        
        _isRunning = run;

        if (_agent.destination != position)
        {
            _agent.SetDestination(position);
        }
        
        _hasTarget = true;
        _agent.isStopped = false;
    }

    /// <summary>
    /// Cập nhật trạng thái chạy/đi bộ.
    /// </summary>
    public virtual void SetRunning(bool run)
    {
        if (!IsServer) return;
        _isRunning = run;
    }

    /// <summary>
    /// Dừng enemy ngay lập tức.
    /// </summary>
    public virtual void Stop()
    {
        if (!IsServer) return;

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
        
        _hasTarget = false;
        _agent.isStopped = true;

        _animator.speed = 1f; // Reset về tốc độ chuẩn khi Idle
        _animator.SetFloat(_hashSpeed, 0, 0.1f, Time.deltaTime);
    }
    #endregion

    #region Helper Logic
    protected virtual void HandleNormalMovement()
    {
        // Kiểm tra xem đã đến đích chưa
        if (!_agent.pathPending && _agent.remainingDistance <= _stopDistance)
        {
            Stop();
            return;
        }

        // Xoay về hướng di chuyển (steeringTarget)
        RotateTowards(_agent.steeringTarget);

        // Áp dụng tỉ lệ tốc độ animation tương ứng
        _animator.speed = _isRunning ? _runSpeedScale : _walkSpeedScale;

        // Tính toán Speed mục tiêu cho Animator: 1 (Walk) hoặc 2 (Run)
        float targetSpeed = _isRunning ? 2f : 1f;
        
        // Cập nhật Animator với damping để chuyển động mượt mà
        _animator.SetFloat(_hashSpeed, targetSpeed, 0.1f, Time.deltaTime);
    }

    protected virtual void RotateTowards(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0;
        
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _alignSpeed * Time.deltaTime);
        }
    }
    #endregion
}
