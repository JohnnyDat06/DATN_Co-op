using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// SimpleEnemyMovement — Di chuyển Enemy dùng NavMeshAgent thuần (không Root Motion).
/// Hỗ trợ thêm các phương thức tấn công tầm xa cho quái vật đơn giản.
/// </summary>
public class SimpleEnemyMovement : EnemyMovement
{
    [Header("Simple Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            _agent.enabled = false;
            this.enabled = false;
            return;
        }

        _agent.updateRotation = true;
        _agent.updatePosition = true;
    }

    protected override void Update()
    {
        if (!IsServer) return;

        if (_hasTarget && !_agent.isStopped)
        {
            _agent.speed = _isRunning ? _runSpeed : _walkSpeed;
            _agent.angularSpeed = _alignSpeed;

            HandleSimpleAnimation();
            
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                Stop();
            }
        }
    }

    protected override void OnAnimatorMove() { }

    private void HandleSimpleAnimation()
    {
        float currentVelocity = _agent.velocity.magnitude;
        float targetAnimSpeed = 0f;

        if (currentVelocity > 0.1f)
        {
            targetAnimSpeed = _isRunning ? 2f : 1f;
        }

        _animator.SetFloat(_hashSpeed, targetAnimSpeed, 0.1f, Time.deltaTime);
    }
}
