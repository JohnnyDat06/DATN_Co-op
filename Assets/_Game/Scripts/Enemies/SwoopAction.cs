using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Swoop Action", story: "Swoop to [Target]", category: "Enemy AI", id: "SwoopAction")]
public partial class SwoopAction : Action
{
    [Header("Inputs")]
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<FlyingEnemyMovement> Movement;

    [Header("Settings")]
    [SerializeField] private float _swoopSpeed = 20f;
    [SerializeField] private float _stopDistance = 1.0f;

    protected override Status OnStart()
    {
        if (Movement == null || Movement.Value == null || Target == null || Target.Value == null)
            return Status.Failure;

        if (Movement.Value.IsServer)
        {
            // Hạ độ cao bay xuống 0 (chạm đất)
            Movement.Value.SetHoverHeight(0f, 8f);
            Movement.Value.MoveTo(Target.Value.transform.position);
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Movement == null || Movement.Value == null || Target == null || Target.Value == null)
            return Status.Failure;

        if (!Movement.Value.IsServer) return Status.Running;

        // Cập nhật vị trí đích liên tục trong khi lao
        Movement.Value.MoveTo(Target.Value.transform.position);

        float distance = Vector3.Distance(GameObject.transform.position, Target.Value.transform.position);
        if (distance <= _stopDistance)
        {
            Movement.Value.Stop();
            return Status.Success;
        }

        return Status.Running;
    }
}
