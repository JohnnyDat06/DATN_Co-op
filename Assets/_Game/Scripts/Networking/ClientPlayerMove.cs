using System;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private StarterAssetsInputs _starterAssetsInputs;
    [SerializeField] private ThirdPersonController _thirdPersonController;
    //[SerializeField] private Transform _rootCamera;

    private bool _lastJumpState;

    private void Awake()
    {
        _playerInput.enabled = false;
        _starterAssetsInputs.enabled = false;
        _thirdPersonController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _lastJumpState = false;
        
        if (IsOwner)
        {
            _playerInput.enabled = true;
            _starterAssetsInputs.enabled = true;
            _thirdPersonController.enabled = true;
            //var _cinemachine = FindObjectOfType<Unity.Cinemachine.CinemachineCamera>();
            //_cinemachine.Follow = transform;
        }

        // if (IsServer)
        // {
        // }
    }

    // [Rpc(SendTo.Server)]
    // private void UpdateInputServerRpc(Vector2 move, Vector2 look, bool jump, bool sprint)
    // {
    //     // Replicate jump as a one-shot press to avoid repeated jumps while key is held/latches.
    //     bool jumpPressed = jump && !_lastJumpState;
    //     _lastJumpState = jump;
    //
    //     _starterAssetsInputs.MoveInput(move);   
    //     _starterAssetsInputs.LookInput(look);
    //     _starterAssetsInputs.JumpInput(jumpPressed);
    //     _starterAssetsInputs.SprintInput(sprint);
    // }
    //
    // private void LateUpdate()
    // {
    //     if (!IsOwner) return;
    //     
    //     UpdateInputServerRpc(_starterAssetsInputs.move, _starterAssetsInputs.look, _starterAssetsInputs.jump, _starterAssetsInputs.sprint);
    // }
}
