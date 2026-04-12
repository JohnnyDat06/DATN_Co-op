using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Retreat Action", story: "Retreat from [Target]", category: "Enemy AI", id: "RetreatAction")]
public partial class RetreatAction : Action
{
    [Header("Inputs")]
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<FlyingEnemyMovement> Movement;

    [Header("Settings")]
    [SerializeField] private float _retreatDistance = 10f;
    [SerializeField] private float _stopDistance = 1.0f;

    private Vector3 _retreatPoint;

    protected override Status OnStart()
    {
        if (Movement == null || Movement.Value == null || Target == null || Target.Value == null)
            return Status.Failure;

        if (Movement.Value.IsServer)
        {
            // Cất cánh
            Movement.Value.ResetHoverHeight();

            // Tính toán điểm rút lui
            Vector3 dir = (GameObject.transform.position - Target.Value.transform.position).normalized;
            dir.y = 0;
            _retreatPoint = GameObject.transform.position + dir * _retreatDistance;

            // Đảm bảo điểm nằm trên NavMesh
            if (NavMesh.SamplePosition(_retreatPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _retreatPoint = hit.position;
            }

            Movement.Value.MoveTo(_retreatPoint);
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Movement == null || Movement.Value == null)
            return Status.Failure;

        if (!Movement.Value.IsServer) return Status.Running;

        float distance = Vector3.Distance(GameObject.transform.position, _retreatPoint);
        if (distance <= _stopDistance)
        {
            Movement.Value.Stop();
            return Status.Success;
        }

        return Status.Running;
    }
}
