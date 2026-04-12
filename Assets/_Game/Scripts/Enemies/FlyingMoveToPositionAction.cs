using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Flying Move To Position", story: "Flying move to [Position]", category: "Enemy AI", id: "FlyingMoveToPositionAction")]
public partial class FlyingMoveToPositionAction : Action
{
    [Header("Inputs")]
    [SerializeReference] public BlackboardVariable<Vector3> Position;
    [SerializeReference] public BlackboardVariable<FlyingEnemyMovement> Movement;

    [Header("Settings")]
    [SerializeField] private float _stopDistance = 0.5f;

    protected override Status OnStart()
    {
        if (Movement == null || Movement.Value == null) return Status.Failure;

        if (Movement.Value.IsServer)
        {
            Movement.Value.MoveTo(Position.Value);
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Movement == null || Movement.Value == null) return Status.Failure;
        if (!Movement.Value.IsServer) return Status.Running;

        float distance = Vector3.Distance(new Vector3(GameObject.transform.position.x, 0, GameObject.transform.position.z), 
                                         new Vector3(Position.Value.x, 0, Position.Value.z));

        if (distance <= _stopDistance)
        {
            Movement.Value.Stop();
            return Status.Success;
        }

        return Status.Running;
    }
}
