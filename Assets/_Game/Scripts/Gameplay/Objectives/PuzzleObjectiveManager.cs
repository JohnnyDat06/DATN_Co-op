using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Objectives
{
    /// <summary>
    /// Quản lý mục tiêu giải đố: yêu cầu người chơi tương tác với tất cả các vật phẩm chỉ định.
    /// </summary>
    public class PuzzleObjectiveManager : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<string> _requiredInteractableIds = new List<string>();
        [SerializeField] private bool _autoCompleteLevel = false;
        [SerializeField] private int _levelToComplete = 0;
        [SerializeField] private bool _resetOnDeath = false;

        [Header("Events")]
        public UnityEvent OnObjectiveCompleted;
        public UnityEvent<int, int> OnProgressChanged; // Current, Total

        private NetworkList<FixedString32Bytes> _activatedIds;
        private List<InteractableBase> _cachedInteractables = new List<InteractableBase>();

        private void Awake()
        {
            _activatedIds = new NetworkList<FixedString32Bytes>(
                readPerm: NetworkVariableReadPermission.Everyone,
                writePerm: NetworkVariableWritePermission.Server);
            
            CacheRequiredInteractables();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EventBus.OnInteractableActivated += HandleInteractableActivated;
                EventBus.OnPlayerDied += HandlePlayerDied;
            }

            _activatedIds.OnListChanged += HandleListChanged;
            
            // Cập nhật tiến độ ban đầu (cho client join muộn)
            NotifyProgress();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EventBus.OnInteractableActivated -= HandleInteractableActivated;
                EventBus.OnPlayerDied -= HandlePlayerDied;
            }

            _activatedIds.OnListChanged -= HandleListChanged;
        }

        private void HandlePlayerDied(ulong clientId)
        {
            if (_resetOnDeath)
            {
                ResetPuzzle();
            }
        }

        [ContextMenu("Reset Puzzle")]
        public void ResetPuzzle()
        {
            if (!IsServer) return;

            _activatedIds.Clear();
            
            foreach (var interactable in _cachedInteractables)
            {
                interactable.ResetInteractable();
            }

            Debug.Log("[PuzzleObjectiveManager] Puzzle reset.");
        }

        private void CacheRequiredInteractables()
        {
            var allInteractables = GetComponentsInChildren<InteractableBase>(true);
            foreach (var interactable in allInteractables)
            {
                if (_requiredInteractableIds.Contains(interactable.InteractableId))
                {
                    _cachedInteractables.Add(interactable);
                }
            }
        }

        private void HandleInteractableActivated(string interactableId)
        {
            if (!IsServer) return;

            // Kiểm tra xem ID này có nằm trong danh sách yêu cầu không
            if (_requiredInteractableIds.Contains(interactableId))
            {
                // Kiểm tra xem đã được thêm vào danh sách kích hoạt chưa
                bool alreadyAdded = false;
                foreach (var id in _activatedIds)
                {
                    if (id.ToString() == interactableId)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    _activatedIds.Add(interactableId);
                    Debug.Log($"[PuzzleObjectiveManager] Da kich hoat: {interactableId}. Tien do: {_activatedIds.Count}/{_requiredInteractableIds.Count}");
                }
            }
        }

        private void HandleListChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
        {
            NotifyProgress();

            if (_activatedIds.Count >= _requiredInteractableIds.Count && _requiredInteractableIds.Count > 0)
            {
                CheckCompletion();
            }
        }

        private void NotifyProgress()
        {
            OnProgressChanged?.Invoke(_activatedIds.Count, _requiredInteractableIds.Count);
        }

        private void CheckCompletion()
        {
            // Kiểm tra xem tất cả các ID yêu cầu đã có trong danh sách activated chưa
            // (Đề phòng trường hợp danh sách yêu cầu có trùng ID hoặc các ID không mong muốn)
            int matchCount = 0;
            foreach (var reqId in _requiredInteractableIds)
            {
                foreach (var actId in _activatedIds)
                {
                    if (actId.ToString() == reqId)
                    {
                        matchCount++;
                        break;
                    }
                }
            }

            if (matchCount >= _requiredInteractableIds.Count)
            {
                Debug.Log("[PuzzleObjectiveManager] TAT CA VAT PHAM DA DUOC TUONG TAC! Muc tieu hoan thanh.");
                OnObjectiveCompleted?.Invoke();

                if (IsServer && _autoCompleteLevel)
                {
                    EventBus.RaiseLevelCompleted(_levelToComplete);
                }
            }
        }
        
        [ContextMenu("Fetch Interactables in Children")]
        private void FetchInteractablesInChildren()
        {
            var interactables = GetComponentsInChildren<InteractableBase>(true);
            foreach (var interactable in interactables)
            {
                if (!_requiredInteractableIds.Contains(interactable.InteractableId))
                {
                    _requiredInteractableIds.Add(interactable.InteractableId);
                }
            }
        }
    }
}
