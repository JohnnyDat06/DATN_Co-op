using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Networking.LobbySystem
{
    [DefaultExecutionOrder(1000)]
    public class LobbyPlayerState : NetworkBehaviour
    {
        public NetworkVariable<int> CharacterIndex = new NetworkVariable<int>(0, 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Server sẽ chỉ định Slot đứng (0, 1, 2...) để tránh việc Client tự tính toán sai dẫn đến chụm vào nhau
        public NetworkVariable<int> LobbySlotIndex = new NetworkVariable<int>(-1, 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private static int _localSelectedCharacterIndex = 0;
        private bool _isInLobby = true;

        public override void OnNetworkSpawn()
        {
            _isInLobby = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Lobby");
            
            // Lắng nghe sự kiện chuyển cảnh để cập nhật trạng thái
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadComplete;
            }

            if (_isInLobby) {
                Debug.Log($"[LobbySlot] Player {OwnerClientId} joined lobby. IsServer: {IsServer}");
                DisableMovementPermanently();

                // CHỈ SERVER: Quyết định Slot cho người chơi mới vào
                if (IsServer)
                {
                    AssignSlotServerRpc();
                }
            }
            else {
                Debug.Log($"[LobbySlot] Player {OwnerClientId} spawned in Game. Enabling movement.");
                EnableMovement();
            }

            CharacterIndex.OnValueChanged += (oldVal, newVal) => ApplyVisual(newVal);
            IsReady.OnValueChanged += (oldVal, newVal) => Debug.Log($"Player {OwnerClientId} Ready: {newVal}");
            
            // Lắng nghe thay đổi slot để cập nhật vị trí ngay lập tức
            LobbySlotIndex.OnValueChanged += (oldVal, newVal) => {
                if (newVal != -1) {
                    FixPosition();
                }
            };

            if (IsOwner)
            {
                SetCharacterServerRpc(_localSelectedCharacterIndex);
            }
        }

        [ServerRpc]
        public void ToggleReadyServerRpc()
        {
            IsReady.Value = !IsReady.Value;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleLoadComplete;
            }
        }

        private void HandleLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            _isInLobby = sceneName.Contains("Lobby");
            if (!_isInLobby)
            {
                Debug.Log($"[LobbySlot] Scene loaded: {sceneName}. Enabling movement for Player {OwnerClientId}");
                EnableMovement();
                
                // Ép Camera bám theo lần nữa nếu cần
                if (IsOwner) {
                    var initializer = GetComponent<PlayerCameraInitializer>();
                    if (initializer != null) {
                        initializer.StopAllCoroutines();
                        initializer.StartCoroutine("InitializeCameraRoutine");
                    }
                }
            }
        }

        private void EnableMovement()
        {
            // Bật lại các script đã bị tắt ở Lobby
            if (TryGetComponent<NGOPlayerSync>(out var sync)) sync.enabled = true;
            if (TryGetComponent<ClientPlayerMove>(out var move)) move.enabled = true;
            if (TryGetComponent<UnityEngine.InputSystem.PlayerInput>(out var input)) input.enabled = true;

            // Unlock InputHandler nếu có
            if (TryGetComponent<PlayerInputHandler>(out var inputHandler)) {
                inputHandler.enabled = true;
                inputHandler.UnlockAllInput();
            }

            // Nếu có Animator, đảm bảo nó hoạt động
            if (TryGetComponent<Animator>(out var anim)) anim.enabled = true;

            // Bật lại các StateMachine/Controller/Model
            if (TryGetComponent<PlayerController>(out var pc)) pc.enabled = true;
            if (TryGetComponent<PlayerStateMachine>(out var psm)) psm.enabled = true;
            
            // StarterAssets support
            var taController = GetComponent("ThirdPersonController");
            if (taController != null && taController is Behaviour b1) b1.enabled = true;

            var saInputs = GetComponent("StarterAssetsInputs");
            if (saInputs != null && saInputs is Behaviour b2) b2.enabled = true;

            var playerModel = GetComponent("PlayerModel");
            if (playerModel != null && playerModel is Behaviour b3) b3.enabled = true;

            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            Debug.Log($"[LobbySlot] ALL movement components enabled for Player {OwnerClientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        private void AssignSlotServerRpc()
        {
            // Dùng trực tiếp danh sách IDs để đảm bảo tính duy nhất và thứ tự
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            int slot = clientIds.IndexOf(OwnerClientId);
            
            if (slot != -1) {
                LobbySlotIndex.Value = slot;
                Debug.Log($"[LobbySlot] Assigned Slot {slot} to Player {OwnerClientId}. Total connected: {clientIds.Count}");
            } else {
                Debug.LogWarning($"[LobbySlot] Could not find Player {OwnerClientId} in ConnectedClientsIds!");
            }
        }

        private void Update()
        {
            if (_isInLobby && LobbySlotIndex.Value != -1)
            {
                FixPosition();
                DisableMovementPermanently();
            }
        }

        private void DisableMovementPermanently()
        {
            // Tắt tất cả các script có thể can thiệp vào nhân vật
            if (TryGetComponent<NGOPlayerSync>(out var sync)) sync.enabled = false;
            
            // Tìm và tắt script ClientPlayerMove
            if (TryGetComponent<ClientPlayerMove>(out var move)) move.enabled = false;

            // Tìm và tắt PlayerInput
            if (TryGetComponent<UnityEngine.InputSystem.PlayerInput>(out var input)) input.enabled = false;

            // Lock InputHandler nếu có
            if (TryGetComponent<PlayerInputHandler>(out var inputHandler)) {
                inputHandler.LockAllInput();
                inputHandler.enabled = false;
            }

            // Tắt các script khác
            if (TryGetComponent<PlayerController>(out var pc)) pc.enabled = false;
            if (TryGetComponent<PlayerStateMachine>(out var psm)) psm.enabled = false;

            // StarterAssets support
            var taController = GetComponent("ThirdPersonController");
            if (taController != null && taController is Behaviour b1) b1.enabled = false;

            var saInputs = GetComponent("StarterAssetsInputs");
            if (saInputs != null && saInputs is Behaviour b2) b2.enabled = false;

            var playerModel = GetComponent("PlayerModel");
            if (playerModel != null && playerModel is Behaviour b3) b3.enabled = false;

            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Animator anim = GetComponent<Animator>();
            if (anim != null) {
                anim.enabled = true;  
                anim.speed = 1.0f;    
                anim.SetFloat("Speed", 0f);
                anim.SetBool("IsGrounded", true);
                anim.SetBool("IsMoving", false);
                anim.SetBool("IsCrouching", false);
                anim.SetBool("IsDead", false);
            }
        }

        private void FixPosition()
        {
            // Tìm các bục đứng (Tag LobbyAnchor)
            var anchors = GameObject.FindGameObjectsWithTag("LobbyAnchor").OrderBy(a => a.name).ToList();
            if (anchors.Count == 0) return;

            // Lấy SlotIndex đã đồng bộ từ Server
            int slot = LobbySlotIndex.Value;
            if (slot == -1) return; 

            int anchorIndex = slot % anchors.Count;
            GameObject targetAnchor = anchors[anchorIndex];
            
            float yOffset = 0.5f; 
            if (targetAnchor.TryGetComponent<Renderer>(out var renderer))
            {
                yOffset = renderer.bounds.extents.y;
            }

            // Ép vị trí tuyệt đối vào bục được chỉ định
            if (Vector3.Distance(transform.position, targetAnchor.transform.position) > 0.05f) {
                transform.position = targetAnchor.transform.position + Vector3.up * yOffset;
                transform.rotation = targetAnchor.transform.rotation;
            }
        }

        private void ApplyVisual(int index)
        {
            if (this == null) return;

            Transform male = FindDeepChild(transform, "MeshMale");
            Transform female = FindDeepChild(transform, "MeshFemale");

            if (male != null) male.gameObject.SetActive(index == 0);
            if (female != null) female.gameObject.SetActive(index == 1);

            foreach (var r in GetComponentsInChildren<Renderer>(true)) {
                r.enabled = true;
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent) {
                if (child.name == name) return child;
                Transform result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        [ServerRpc]
        public void ToggleCharacterServerRpc() {
            CharacterIndex.Value = (CharacterIndex.Value + 1) % 2;
            UpdateLocalIndexClientRpc(CharacterIndex.Value);
        }

        [ServerRpc]
        public void SetCharacterServerRpc(int index) {
            CharacterIndex.Value = index;
        }

        [ClientRpc]
        private void UpdateLocalIndexClientRpc(int index) {
            if (IsOwner) _localSelectedCharacterIndex = index;
        }
    }
}