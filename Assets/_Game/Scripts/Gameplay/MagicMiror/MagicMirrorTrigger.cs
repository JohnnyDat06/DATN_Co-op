using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay
{
    public class MagicMirrorTrigger : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private MagicMirrorController mirrorController;
        [SerializeField] private GameObject[] additionalObjectsToActivate;

        [Header("Detection Settings")]
        [SerializeField] private Vector3 boxSize = new Vector3(2f, 2f, 2f);
        [SerializeField] private Vector3 boxOffset = Vector3.zero;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float detectionInterval = 0.2f;

        private bool isPlayerNearby;
        private float nextCheckTime;

        private void Update()
        {
            // Only the server should process the trigger logic
            if (!IsServer) return;

            if (Time.time >= nextCheckTime)
            {
                CheckForPlayer();
                nextCheckTime = Time.time + detectionInterval;
            }
        }

        private void CheckForPlayer()
        {
            // Use BoxCast or OverlapBox to detect player
            bool detected = Physics.CheckBox(transform.position + transform.TransformDirection(boxOffset), boxSize / 2, transform.rotation, playerLayer);

            if (detected && !isPlayerNearby)
            {
                OnPlayerEnter();
            }
            else if (!detected && isPlayerNearby)
            {
                OnPlayerExit();
            }

            isPlayerNearby = detected;
        }

        private void OnPlayerEnter()
        {
            if (mirrorController != null)
            {
                mirrorController.ToggleMirror(true);
            }

            ToggleAdditionalObjects(true);
        }

        private void OnPlayerExit()
        {
            if (mirrorController != null)
            {
                mirrorController.ToggleMirror(false);
            }

            ToggleAdditionalObjects(false);
        }

        private void ToggleAdditionalObjects(bool active)
        {
            foreach (var obj in additionalObjectsToActivate)
            {
                if (obj != null)
                {
                    // If the additional object has a NetworkObject, we should consider how to sync it.
                    // For simple visual objects, server-side activation will sync if they are spawned.
                    // If they are just local gameobjects in the scene, we might need a ClientRpc.
                    
                    obj.SetActive(active);
                    
                    // Sync active state if it's a NetworkObject
                    if (obj.TryGetComponent<NetworkObject>(out var netObj))
                    {
                        // Note: GameObject.SetActive is NOT automatically synced by NGO.
                        // For a real production game, these objects should probably have their own 
                        // NetworkBehaviour to sync state, or we use a ClientRpc here.
                    }
                }
            }
            
            // Sync the activation of additional objects to all clients
            ToggleAdditionalObjectsClientRpc(active);
        }

        [Rpc(SendTo.NotServer)]
        private void ToggleAdditionalObjectsClientRpc(bool active)
        {
            if (IsServer) return; // Already done on server

            foreach (var obj in additionalObjectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(active);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxOffset, boxSize);
        }
    }
}
