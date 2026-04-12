using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set Combat Target", story: "Set [Target] to Combat Component", category: "Enemy AI", id: "SetCombatTargetAction")]
public partial class SetCombatTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool> IsDetected;

    private EnemyCombat _combat;

    protected override Status OnStart()
    {
        if (_combat == null)
        {
            _combat = GameObject.GetComponent<EnemyCombat>();
        }

        if (_combat != null && Target != null)
        {
            _combat.SetCombatTarget(Target.Value, IsDetected != null ? IsDetected.Value : true);
            return Status.Success;
        }

        return Status.Failure;
    }
}
