using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceInputController : NetworkBehaviour
{
    private bool _isMuted = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            _isMuted = !_isMuted;
            if (VivoxManager.Instance != null)
            {
                VivoxManager.Instance.SetMicrophoneMute(_isMuted);
            }
            Debug.Log($"[VoiceInputController] Microphone: {(_isMuted ? "OFF" : "ON")}");
        }
    }
}
