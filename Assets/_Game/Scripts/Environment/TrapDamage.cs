using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Game.Environment
{
    public enum TrapType { Continuous, Instant }

    /// <summary>
    /// Quản lý gây sát thương cho các bẫy (Lưỡi cưa, Chông, v.v.)
    /// Đồng bộ qua Netcode, chỉ Server thực thi trừ máu.
    /// </summary>
    public class TrapDamage : MonoBehaviour
    {
        [Header("Trap Settings")]
        public TrapType type = TrapType.Continuous;
        
        [Tooltip("Sát thương gây ra (75 là 3/4 của 100 HP)")]
        public float damageAmount = 75f;
        
        [Tooltip("Tốc độ gây sát thương liên tục (giây/lần)")]
        public float damageInterval = 0.5f;

        private Dictionary<ulong, float> _lastDamageTime = new Dictionary<ulong, float>();

        private void OnTriggerEnter(Collider other)
        {
            // Chỉ Server mới xử lý gây sát thương
            if (!NetworkManager.Singleton.IsServer) return;

            if (type == TrapType.Instant && other.CompareTag("Player"))
            {
                ApplyDamage(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (type == TrapType.Continuous && other.CompareTag("Player"))
            {
                if (other.TryGetComponent<NetworkObject>(out var netObj))
                {
                    ulong clientId = netObj.OwnerClientId;
                    if (!_lastDamageTime.ContainsKey(clientId) || Time.time - _lastDamageTime[clientId] >= damageInterval)
                    {
                        ApplyDamage(other);
                        _lastDamageTime[clientId] = Time.time;
                    }
                }
            }
        }

        private void ApplyDamage(Collider playerCollider)
        {
            if (playerCollider.TryGetComponent<IDamageable>(out var damageable))
            {
                // Gọi phương thức TakeDamage của PlayerHealth
                damageable.TakeDamage(damageAmount);
                Debug.Log($"[TrapDamage] {gameObject.name} dealt {damageAmount} damage to {playerCollider.name}");
            }
        }
    }
}
