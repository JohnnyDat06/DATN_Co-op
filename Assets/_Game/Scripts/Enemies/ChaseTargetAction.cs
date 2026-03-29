using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
// Updated category to "Enemy AI" to reflect its universal usage across different enemy types
[NodeDescription(name: "Chase Target", story: "Chase [Target]", category: "Enemy AI", id: "ChaseTargetAction")]
public partial class ChaseTargetAction : Action
{
	[Header("Inputs")]
	[Tooltip("The target GameObject to chase (retrieved from the Blackboard).")]
	[SerializeReference] public BlackboardVariable<GameObject> Target;

	[Tooltip("The distance threshold at which the agent stops chasing and reports success.")]
	[SerializeReference] public BlackboardVariable<float> StopDistance = new BlackboardVariable<float>(1.5f);

	[Header("References")]
	[Tooltip("The movement component to execute the chase. Auto-assigned if left empty.")]
	// Changed from ArathroxMovement to the base class EnemyMovement
	[SerializeReference] public BlackboardVariable<EnemyMovement> MovementComponent;

	// Cached reference to the movement component
	private EnemyMovement _movement;

	protected override Status OnStart()
	{
		// 1. Resolve the EnemyMovement component
		// This leverages polymorphism to find ArathroxMovement, CrustaspikanMovement, etc.
		if (MovementComponent != null && MovementComponent.Value != null)
		{
			_movement = MovementComponent.Value;
		}

		if (_movement == null && GameObject != null)
		{
			_movement = GameObject.GetComponent<EnemyMovement>();
		}

		// 2. Validate dependencies
		if (_movement == null)
		{
			LogFailure("No component inheriting from EnemyMovement was found on the GameObject!");
			return Status.Failure;
		}

		if (Target == null || Target.Value == null)
		{
			LogFailure("Chase target is null or unassigned!");
			return Status.Failure;
		}

		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		// Safety check to ensure dependencies remain valid during execution
		if (_movement == null || Target == null || Target.Value == null)
			return Status.Failure;

		// 1. Continuously update the destination
		// Since the target (e.g., the Player) is likely moving, we must update the path every frame.
		_movement.MoveTo(Target.Value.transform.position);

		// 2. Evaluate stopping condition
		// Calculate the distance between the agent and the target.
		float distanceToTarget = Vector3.Distance(GameObject.transform.position, Target.Value.transform.position);

		if (distanceToTarget <= StopDistance.Value)
		{
			// The agent has successfully closed the distance to the target.
			// Returning Success allows the Behavior Graph to transition to the next node (e.g., Attack).
			return Status.Success;
		}

		// The agent is still out of range; maintain the running state.
		return Status.Running;
	}

	protected override void OnEnd()
	{
		// 3. Handle action termination
		// This is crucial for handling aborts (e.g., if a higher-priority branch interrupts the chase).
		// It ensures the agent does not continue sliding or moving after the node ends.
		if (_movement != null)
		{
			_movement.Stop();
		}
	}
}