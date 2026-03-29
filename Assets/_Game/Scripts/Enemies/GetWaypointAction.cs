using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Get Next Waypoint", story: "Get next waypoint from [Manager] and save to [TargetLocation]", category: "Arathrox/Patrol", id: "GetWaypointAction")]
public partial class GetWaypointAction : Action
{
    [Header("References")]
    [Tooltip("Biến Blackboard để lưu vị trí điểm đến")]
    [SerializeReference] public BlackboardVariable<Vector3> TargetLocation;

    [Tooltip("Nếu để trống, sẽ tự tìm WaypointManager trên GameObject hiện tại")]
    [SerializeReference] public BlackboardVariable<WaypointManager> Manager;

    [Header("Settings")]
    [Tooltip("Bán kính lệch ngẫu nhiên xung quanh Waypoint")]
    [SerializeReference] public BlackboardVariable<float> RandomOffset = new BlackboardVariable<float>(0.5f);

    private WaypointManager _cachedManager;
    private EnemyMovement _movement;

    protected override Status OnStart()
    {
        if (GameObject == null) return Status.Failure;

        // 1. Tìm EnemyMovement để kiểm tra quyền Server
        _movement = GameObject.GetComponent<EnemyMovement>();
        if (_movement != null && _movement.IsSpawned && !_movement.IsServer)
        {
            return Status.Success; // Client không chạy logic AI
        }

        // 2. Tìm WaypointManager
        if (Manager.Value != null)
        {
            _cachedManager = Manager.Value;
        }
        else if (_cachedManager == null)
        {
            _cachedManager = GameObject.GetComponent<WaypointManager>();
        }

        if (_cachedManager == null)
        {
            LogFailure("Không tìm thấy WaypointManager!");
            return Status.Failure;
        }

        // 3. Lấy điểm đến tiếp theo
        Vector3 nextPoint = _cachedManager.GetNextWaypoint();

        // 4. Xử lý độ lệch ngẫu nhiên
        if (RandomOffset.Value > 0)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * RandomOffset.Value;
            nextPoint += new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        TargetLocation.Value = nextPoint;
        return Status.Success;
    }
}
