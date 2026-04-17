using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.Netcode;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move To Position", story: "Move Agent to [TargetPosition]", category: "Enemy AI", id: "MoveToPositionAction")]
public partial class MoveToPositionAction : Action
{
	[Header("Inputs")]
	[Tooltip("Vị trí cần đến (Lấy từ Blackboard)")]
	[SerializeReference] public BlackboardVariable<Vector3> TargetPosition;

	[Tooltip("Khoảng cách chấp nhận đã đến đích (Nên khớp hoặc lớn hơn EnemyMovement settings một chút)")]
	[SerializeReference] public BlackboardVariable<float> StoppingDistance = new BlackboardVariable<float>(0.6f);

	[Header("References")]
	[Tooltip("Component di chuyển (Tự động tìm nếu để trống)")]
	[SerializeReference] public BlackboardVariable<EnemyMovement> MovementComponent;

	private EnemyMovement _movement;
	private NavMeshAgent _agent;

	protected override Status OnStart()
	{
		// 1. Tìm Component EnemyMovement
		if (MovementComponent != null && MovementComponent.Value != null)
		{
			_movement = MovementComponent.Value;
		}

		if (_movement == null && GameObject != null)
		{
			_movement = GameObject.GetComponent<EnemyMovement>();
		}

		if (_movement == null)
		{
			LogFailure("Không tìm thấy component kế thừa EnemyMovement trên GameObject!");
			return Status.Failure;
		}

		// Lấy NavMeshAgent từ GameObject
		if (_agent == null && GameObject != null)
		{
			_agent = GameObject.GetComponent<NavMeshAgent>();
		}

		// 2. Ra lệnh di chuyển (CHỈ TRÊN SERVER)
		// Chỉ thực hiện di chuyển nếu NetworkObject đã Spawn và là Server
		if (_movement.IsSpawned && _movement.IsServer)
		{
			_movement.MoveTo(TargetPosition.Value);
		}

		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		// 1. Kiểm tra sự tồn tại của các thành phần
		if (_movement == null || _agent == null) return Status.Failure;

		// 2. CHỈ CHẠY LOGIC TRÊN SERVER. Trên Client, quái được đồng bộ qua NetworkTransform/Animator.
		// Dùng IsSpawned để đảm bảo NetworkObject đã sẵn sàng.
		if (!_movement.IsSpawned || !_movement.IsServer)
		{
			return Status.Running;
		}

		// 3. KIỂM TRA AN TOÀN CHO NAVMESHAGENT (Tránh lỗi GetRemainingDistance khi đổi Scene)
		// Agent phải đang Active và đang nằm trên một vùng NavMesh hợp lệ.
		if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
		{
			return Status.Running;
		}

		// 4. Kiểm tra xem đã đến nơi chưa
		if (!_agent.pathPending)
		{
			// Safe check: Nếu Agent không có đường đi hợp lệ, ta coi như chưa đến (hoặc đã lỗi)
			if (!_agent.hasPath && _agent.pathStatus != NavMeshPathStatus.PathComplete)
			{
				return Status.Running;
			}

			if (_agent.remainingDistance <= StoppingDistance.Value)
			{
				return Status.Success;
			}
		}

		return Status.Running;
	}

	protected override void OnEnd()
	{
		if (_movement != null && _movement.IsSpawned && _movement.IsServer)
		{
			_movement.Stop();
		}
	}
}
