using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase Target", story: "Chase [Target]", category: "Enemy AI", id: "ChaseTargetAction")]
public partial class ChaseTargetAction : Action
{
    [Header("Inputs")]
    [Tooltip("Mục tiêu để truy đuổi (lấy từ Blackboard).")]
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    [Tooltip("Khoảng cách dừng lại để chuyển sang tấn công.")]
    [SerializeReference] public BlackboardVariable<float> StopDistance = new BlackboardVariable<float>(1.5f);

    [Header("References")]
    [Tooltip("Component di chuyển.")]
    [SerializeReference] public BlackboardVariable<EnemyMovement> MovementComponent;

    private EnemyMovement _movement;

    protected override Status OnStart()
    {
        // 1. Resolve component
        if (MovementComponent != null && MovementComponent.Value != null)
        {
            _movement = MovementComponent.Value;
        }

        if (_movement == null && GameObject != null)
        {
            _movement = GameObject.GetComponent<EnemyMovement>();
        }

        // 2. Validate
        if (_movement == null)
        {
            LogFailure("Không tìm thấy EnemyMovement!");
            return Status.Failure;
        }

        if (Target == null || Target.Value == null)
        {
            return Status.Failure;
        }

        // 3. Bắt đầu chạy (Chỉ thực hiện trên Server bên trong MoveTo, nhưng ta gọi SetRunning để đồng bộ Anim)
        if (_movement.IsServer)
        {
            _movement.SetRunning(true);
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_movement == null || Target == null || Target.Value == null)
            return Status.Failure;

        if (!_movement.IsServer) return Status.Running;

        // 1. Cập nhật vị trí đuổi theo
        _movement.MoveTo(Target.Value.transform.position, true);

        // 2. Kiểm tra khoảng cách
        float distanceToTarget = Vector3.Distance(GameObject.transform.position, Target.Value.transform.position);

        if (distanceToTarget <= StopDistance.Value)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_movement != null && _movement.IsServer)
        {
            _movement.SetRunning(false);
            _movement.Stop();
        }
    }
}
