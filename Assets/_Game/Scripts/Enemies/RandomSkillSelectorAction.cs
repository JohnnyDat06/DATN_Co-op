using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Random Skill Selector", story: "Pick random skill to [SkillID]", category: "Enemy AI", id: "RandomSkillSelectorAction")]
public partial class RandomSkillSelectorAction : Action
{
    [Header("Outputs")]
    [Tooltip("ID kỹ năng được chọn (1: Ranged, 2: Melee).")]
    [SerializeReference] public BlackboardVariable<int> SkillID;

    [Header("Settings")]
    [Tooltip("Tỉ lệ ra đòn cận chiến (0.0 - 1.0). Còn lại là tầm xa.")]
    [SerializeField] private float _meleeProbability = 0.4f;

    protected override Status OnStart()
    {
        if (SkillID == null) return Status.Failure;

        float roll = UnityEngine.Random.value;
        if (roll <= _meleeProbability)
        {
            SkillID.Value = 2; // Melee
        }
        else
        {
            SkillID.Value = 1; // Ranged
        }

        return Status.Success;
    }
}
