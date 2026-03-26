using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Game.Network
{
    /// <summary>
    /// PlayerSpawner — Quản lý việc spawn player tại các vị trí chỉ định.
    /// Sử dụng hàm Teleport của Player để đảm bảo đồng bộ hóa chuẩn xác.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        
        // Lưu trữ danh sách clientId đã được xử lý
        private HashSet<ulong> processedClients = new HashSet<ulong>();

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            
            // Xử lý cho Host nếu họ đã spawn
            if (IsHost)
            {
                HandleClientConnected(NetworkManager.ServerClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            if (processedClients.Contains(clientId)) return;

            StartCoroutine(DelayedSpawn(clientId));
        }

        private System.Collections.IEnumerator DelayedSpawn(ulong clientId)
        {
            // Đợi đến khi PlayerObject của client này thực sự được tạo ra trên Server
            float timeout = 3f;
            while (timeout > 0)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
                {
                    // Tìm component NGOPlayerSync trên PlayerObject
                    if (client.PlayerObject.TryGetComponent<NGOPlayerSync>(out var playerSync))
                    {
                        // Xác định vị trí spawn (theo thứ tự clientId hoặc số lượng player)
                        int spawnIndex = processedClients.Count % spawnPoints.Length;
                        processedClients.Add(clientId);

                        if (spawnPoints.Length > spawnIndex)
                        {
                            Transform target = spawnPoints[spawnIndex];
                            
                            // Gọi hàm Teleport của Player - Hàm này sẽ tự động lo việc đồng bộ qua ClientRpc
                            playerSync.Teleport(target.position, target.rotation);
                            
                            Debug.Log($"[PlayerSpawner] Player {clientId} assigned to SpawnPoint {spawnIndex}");
                        }
                        yield break;
                    }
                }
                
                yield return null;
                timeout -= Time.deltaTime;
            }
            
            Debug.LogWarning($"[PlayerSpawner] Timeout waiting for player {clientId} to spawn.");
        }
    }
}
