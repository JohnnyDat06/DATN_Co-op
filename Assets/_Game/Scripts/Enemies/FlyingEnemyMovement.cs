using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// FlyingEnemyMovement — Xử lý di chuyển bay mượt mà.
/// Giải quyết triệt để lỗi giật bằng cách tách biệt hoàn toàn NavMeshAgent khỏi Transform.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class FlyingEnemyMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 360f;
    [SerializeField] private float _horizontalSmoothTime = 0.15f;
    
    [Header("Height Settings")]
    [SerializeField] private float _minHoverHeight = 2.5f;
    [SerializeField] private float _maxHoverHeight = 4.5f;
    [SerializeField] private float _verticalSmoothTime = 0.5f;
    [SerializeField] private float _heightChangeInterval = 4f;
    [SerializeField] private LayerMask _groundLayer;

    private NavMeshAgent _agent;
    private Vector3 _targetPosition;
    private GameObject _lookTarget; // Đối tượng để xoay mặt về phía đó
    private bool _isMoving;
    
    // Vertical Logic
    private float _targetHoverHeight;
    private float _currentVerticalVelocity;
    private float _heightChangeTimer;

    // Smoothing
    private Vector3 _currentMoveVelocity;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        // QUAN TRỌNG: Tắt quyền tự cập nhật vị trí và xoay của Agent để tránh lỗi giật (Jitter)
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;

        _targetHoverHeight = (_minHoverHeight + _maxHoverHeight) / 2f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            _agent.enabled = false;
            this.enabled = false;
            return;
        }
        _heightChangeTimer = _heightChangeInterval;
    }

    private void Update()
    {
        if (!IsServer) return;

        // 1. Cập nhật độ cao ngẫu nhiên
        HandleRandomHeightLogic();

        // 2. Đồng bộ vị trí Agent với Transform (chỉ trục X, Z) để Agent biết nó đang ở đâu trên NavMesh
        _agent.nextPosition = transform.position;

        // 3. Xử lý di chuyển trục Y
        UpdateVerticalPosition();
        
        // 4. Xử lý di chuyển trục X, Z
        if (_isMoving)
        {
            HandleHorizontalMovement();
        }
    }

    private void HandleRandomHeightLogic()
    {
        // Nếu đang Swoop (độ cao mục tiêu rất thấp), không đổi độ cao ngẫu nhiên
        if (_targetHoverHeight < _minHoverHeight && _targetHoverHeight > 0.1f) return;

        _heightChangeTimer -= Time.deltaTime;
        if (_heightChangeTimer <= 0)
        {
            _targetHoverHeight = Random.Range(_minHoverHeight, _maxHoverHeight);
            _heightChangeTimer = _heightChangeInterval;
        }
    }

    /// <summary>
    /// Ra lệnh di chuyển tới đích và nhìn vào mục tiêu (nếu có).
    /// </summary>
    public void MoveTo(Vector3 position, GameObject lookTarget = null)
    {
        if (!IsServer) return;
        
        _targetPosition = position;
        _lookTarget = lookTarget;
        _agent.SetDestination(position);
        _isMoving = true;
    }

    public void Stop()
    {
        if (!IsServer) return;
        _isMoving = false;
        _lookTarget = null;
        if (_agent.isOnNavMesh) _agent.ResetPath();
    }

    public void SetHoverHeight(float newHeight, float smoothTime = 0.2f)
    {
        _targetHoverHeight = newHeight;
        _verticalSmoothTime = smoothTime;
        _heightChangeTimer = _heightChangeInterval;
    }

    public void ResetHoverHeight()
    {
        _targetHoverHeight = Random.Range(_minHoverHeight, _maxHoverHeight);
        _verticalSmoothTime = 0.5f;
    }

    private void HandleHorizontalMovement()
    {
        // Lấy hướng di chuyển tiếp theo từ NavMesh
        Vector3 nextStep = _agent.steeringTarget;
        Vector3 moveDir = (nextStep - transform.position);
        moveDir.y = 0;

        if (moveDir.magnitude < 0.1f && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _isMoving = false;
            return;
        }

        // --- XOAY MẶT ---
        // Ưu tiên xoay về phía LookTarget (Người chơi), nếu không có thì xoay theo hướng đi
        Vector3 lookDir = moveDir;
        if (_lookTarget != null)
        {
            lookDir = (_lookTarget.transform.position - transform.position);
            lookDir.y = 0;
        }

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        }

        // --- DI CHUYỂN ---
        Vector3 velocity = moveDir.normalized * _moveSpeed;
        Vector3 targetPos = transform.position + velocity * Time.deltaTime;
        
        // Sử dụng SmoothDamp để triệt tiêu mọi rung động nhỏ từ NavMesh steering
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentMoveVelocity, _horizontalSmoothTime);
    }

    private void UpdateVerticalPosition()
    {
        float currentY = transform.position.y;
        float groundY = currentY - _targetHoverHeight;

        // Raycast tìm mặt đất
        Vector3 rayOrigin = transform.position + Vector3.up * 2f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 40f, _groundLayer, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
        }

        float targetY = groundY + _targetHoverHeight;
        float nextY = Mathf.SmoothDamp(currentY, targetY, ref _currentVerticalVelocity, _verticalSmoothTime);
        
        transform.position = new Vector3(transform.position.x, nextY, transform.position.z);
    }
}
