using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

namespace Game.Interactions
{
    public class InteractableObject : InteractableBase
    {
        [Header("Events")]
        [SerializeField] private UnityEvent onInteracted;

        public override void Interact()
        {
            if (!CanInteract) return;

            Debug.Log($"[InteractableObject] Interacted with {gameObject.name}");
            
            // Nếu là Server/Host, thực hiện logic ngay
            if (IsServer)
            {
                ExecuteInteraction();
            }
            else // Nếu là Client, gửi yêu cầu lên Server
            {
                InteractServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void InteractServerRpc(ServerRpcParams rpcParams = default)
        {
            // Logic xử lý trên Server
            ExecuteInteraction();
        }

        private void ExecuteInteraction()
        {
            // Thực hiện sự kiện UnityEvent trên Server (nếu cần đồng bộ trạng thái qua mạng)
            // Hoặc gửi ClientRpc để tất cả các máy cùng chạy hiệu ứng/âm thanh
            onInteracted?.Invoke();
            InteractClientRpc();
        }

        [ClientRpc]
        private void InteractClientRpc()
        {
            // Tránh chạy lại trên máy chủ nếu nó đã chạy trong ExecuteInteraction
            if (IsServer) return;
            
            onInteracted?.Invoke();
        }
    }
}
