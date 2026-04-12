using System;
using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wind Attack", story: "Fire wind projectile from [Self] at [Target]", category: "Enemy AI", id: "WindAttackAction")]
public partial class WindAttackAction : Action
{
    [Header("Inputs")]
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeField] private GameObject _windProjectilePrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private float _attackDuration = 1.0f;

    private float _timer;
    private bool _fired;

    protected override Status OnStart()
    {
        if (Self == null || Self.Value == null || Target == null || Target.Value == null)
            return Status.Failure;

        if (_windProjectilePrefab == null) return Status.Failure;

        _timer = 0;
        _fired = false;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _timer += Time.deltaTime;

        // Chỉ Server mới thực hiện Spawn viên đạn
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !_fired)
        {
            _fired = true;
            
            // Xoay về hướng Target
            Vector3 dir = (Target.Value.transform.position - Self.Value.transform.position).normalized;
            dir.y = 0;
            Self.Value.transform.rotation = Quaternion.LookRotation(dir);

            // Spawn Projectile
            Vector3 spawnPos = (_spawnPoint != null) ? _spawnPoint.position : Self.Value.transform.position + Self.Value.transform.forward;
            GameObject projectile = UnityEngine.Object.Instantiate(_windProjectilePrefab, spawnPos, Self.Value.transform.rotation);
            projectile.GetComponent<NetworkObject>().Spawn();
        }

        if (_timer >= _attackDuration)
        {
            return Status.Success;
        }

        return Status.Running;
    }
}
