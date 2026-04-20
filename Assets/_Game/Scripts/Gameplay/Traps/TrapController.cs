using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Project.Gameplay.Traps
{
    /// <summary>
    /// Quản lý bẫy gây sát thương trong môi trường (Lưỡi cưa, Cây lao).
    /// Hỗ trợ gây sát thương liên tục hoặc theo đợt.
    /// Logic chỉ chạy trên Server để đảm bảo đồng bộ máu (Multiplayer).
    /// </summary>
    public class TrapController : NetworkBehaviour
    {
        public enum TrapType
        {
            Continuous, // Gây sát thương liên tục (như lưỡi cưa)
            Burst       // Gây sát thương theo đợt (như cây lao đâm lên)
        }

        [Header("Trap Settings")]
        [SerializeField] private TrapType _type = TrapType.Burst;
        [SerializeField] private float _damageAmount = 10f;
        [SerializeField] private float _damageInterval = 0.5f; // Giây giữa các lần gây dame (dùng cho Continuous)
        
        [Header("Collision")]
        [SerializeField] private string _targetTag = "Player"; // Tag của đối tượng nhận dame
        [SerializeField] private LayerMask _targetLayer;

        // Lưu trữ thời gian gây dame tiếp theo cho mỗi đối tượng (dùng cho bẫy liên tục)
        private Dictionary<IDamageable, float> _nextDamageTime = new Dictionary<IDamageable, float>();

        private void OnTriggerEnter(Collider other)
        {
            // Chỉ Server mới xử lý logic gây dame
            if (!IsServer) return;

            // Kiểm tra Tag hoặc Layer (tùy config)
            if (!other.CompareTag(_targetTag)) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
            {
                // Thử tìm ở parent nếu collider nằm ở object con
                damageable = other.GetComponentInParent<IDamageable>();
            }

            if (damageable != null && !damageable.IsDead)
            {
                if (_type == TrapType.Burst)
                {
                    ApplyDamage(damageable);
                }
                else if (_type == TrapType.Continuous)
                {
                    // Đăng ký đối tượng vào danh sách tick sát thương
                    if (!_nextDamageTime.ContainsKey(damageable))
                    {
                        _nextDamageTime[damageable] = Time.time;
                    }
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsServer || _type != TrapType.Continuous) return;

            if (!other.CompareTag(_targetTag)) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = other.GetComponentInParent<IDamageable>();
            }

            if (damageable != null && !damageable.IsDead)
            {
                if (_nextDamageTime.TryGetValue(damageable, out float nextTime))
                {
                    if (Time.time >= nextTime)
                    {
                        ApplyDamage(damageable);
                        _nextDamageTime[damageable] = Time.time + _damageInterval;
                    }
                }
                else
                {
                    // Trường hợp enter nhưng chưa kịp lưu (hiếm)
                    _nextDamageTime[damageable] = Time.time + _damageInterval;
                    ApplyDamage(damageable);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = other.GetComponentInParent<IDamageable>();
            }

            if (damageable != null && _nextDamageTime.ContainsKey(damageable))
            {
                _nextDamageTime.Remove(damageable);
            }
        }

        private void ApplyDamage(IDamageable target)
        {
            if (target == null || target.IsDead) return;

            target.TakeDamage(_damageAmount);
            
#if UNITY_EDITOR || DEBUG_BUILD
            Debug.Log($"[TrapController] Applied {_damageAmount} damage to {((MonoBehaviour)target).name}. Type: {_type}");
#endif
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _nextDamageTime.Clear();
        }
    }
}
