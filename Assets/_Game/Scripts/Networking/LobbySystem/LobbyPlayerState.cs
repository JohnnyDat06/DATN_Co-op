using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;

namespace Networking.LobbySystem
{
    [DefaultExecutionOrder(1000)]
    public class LobbyPlayerState : NetworkBehaviour
    {
        public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>("", 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> CharacterIndex = new NetworkVariable<int>(0, 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<int> LobbySlotIndex = new NetworkVariable<int>(-1, 
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private static int _localSelectedCharacterIndex = 0;
        private bool _isInLobby = true;

        public override void OnNetworkSpawn()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            _isInLobby = sceneName.Contains("Lobby");
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadComplete;
            }

            if (_isInLobby) {
                DisableMovementPermanently();
                if (IsServer) AssignSlotServerRpc();
            }
            else {
                // Đang vào Game trực tiếp (không qua Lobby) hoặc Spawn lại
                EnableMovement();
                if (IsOwner) StartCoroutine(InitialSpawnCoroutine(0.1f));
            }

            CharacterIndex.OnValueChanged += (oldVal, newVal) => ApplyVisual(newVal);

            if (IsOwner)
            {
                SetCharacterServerRpc(_localSelectedCharacterIndex);
                string nameFromLobby = LobbyManager.Instance.GetPlayerName();
                if (!string.IsNullOrEmpty(nameFromLobby)) SetPlayerNameServerRpc(nameFromLobby);
            }
        }

        private void HandleLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            _isInLobby = sceneName.Contains("Lobby");
            
            // KHÓA VẬT LÝ NGAY LẬP TỨC ĐỂ CHỜ TELEPORT
            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (!_isInLobby)
            {
                Debug.Log($"[LobbySlot] Scene LOADED: {sceneName}. Locking physics until Teleport completes.");
                if (IsOwner) {
                    StartCoroutine(InitialSpawnCoroutine(0.1f));
                    StartCoroutine(FinalEnforcementCoroutine());
                }
            }
        }

        private IEnumerator InitialSpawnCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            TeleportToSpawn();
            EnableMovement(); 
        }

        private IEnumerator FinalEnforcementCoroutine()
        {
            yield return new WaitForSeconds(2.1f);
            Debug.Log("[LobbySlot] FINAL ENFORCEMENT: Re-teleporting to ensure Netcode stability.");
            TeleportToSpawn();
            EnableMovement();
        }

        private void TeleportToSpawn()
        {
            Vector3 spawnPos = transform.position;
            string spawnTagName = $"SpawnPoint_{LobbySlotIndex.Value}";
            GameObject spawnPoint = GameObject.Find(spawnTagName);
            
            if (spawnPoint == null) spawnPoint = GameObject.FindGameObjectWithTag("Respawn");

            if (spawnPoint != null) {
                spawnPos = spawnPoint.transform.position + Vector3.up * 0.3f;
                if (TryGetComponent<NGOPlayerSync>(out var sync)) {
                    sync.Teleport(spawnPos, spawnPoint.transform.rotation);
                } else {
                    transform.position = spawnPos;
                }
            }

            var initializer = GetComponent<PlayerCameraInitializer>();
            if (initializer != null) {
                initializer.StopAllCoroutines();
                initializer.StartCoroutine("InitializeCameraRoutine");
            }
        }

        private void EnableMovement()
        {
            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if (TryGetComponent<NGOPlayerSync>(out var sync)) sync.enabled = true;
            if (TryGetComponent<ClientPlayerMove>(out var move)) move.enabled = true;
            if (TryGetComponent<PlayerInputHandler>(out var inputHandler)) {
                inputHandler.enabled = true;
                inputHandler.UnlockAllInput();
            }

            if (TryGetComponent<PlayerController>(out var pc)) pc.enabled = true;
            if (TryGetComponent<PlayerStateMachine>(out var psm)) psm.enabled = true;
            if (TryGetComponent<CapsuleCollider>(out var col)) col.enabled = true;
        }

        private void DisableMovementPermanently()
        {
            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            if (TryGetComponent<NGOPlayerSync>(out var sync)) sync.enabled = false;
            if (TryGetComponent<PlayerInputHandler>(out var inputHandler)) inputHandler.LockAllInput();
            if (TryGetComponent<PlayerController>(out var pc)) pc.enabled = false;
            if (TryGetComponent<PlayerStateMachine>(out var psm)) psm.enabled = false;
        }

        [ServerRpc(RequireOwnership = false)]
        private void AssignSlotServerRpc()
        {
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            int slot = clientIds.IndexOf(OwnerClientId);
            if (slot != -1) LobbySlotIndex.Value = slot;
        }

        private void Update()
        {
            if (!_isInLobby) return;
            if (LobbySlotIndex.Value != -1) FixPosition();
        }

        private void FixPosition()
        {
            var anchors = GameObject.FindGameObjectsWithTag("LobbyAnchor").OrderBy(a => a.name).ToList();
            if (anchors.Count == 0) return;
            int anchorIndex = LobbySlotIndex.Value % anchors.Count;
            GameObject targetAnchor = anchors[anchorIndex];
            transform.position = targetAnchor.transform.position + Vector3.up * 0.5f;
            transform.rotation = targetAnchor.transform.rotation;
        }

        private void ApplyVisual(int index)
        {
            Transform male = FindDeepChild(transform, "MeshMale");
            Transform female = FindDeepChild(transform, "MeshFemale");
            if (male != null) male.gameObject.SetActive(index == 0);
            if (female != null) female.gameObject.SetActive(index == 1);
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

        [ServerRpc] public void ToggleReadyServerRpc() => IsReady.Value = !IsReady.Value;
        [ServerRpc] public void SetPlayerNameServerRpc(string name) => PlayerName.Value = name;
        [ServerRpc] public void SetCharacterServerRpc(int index) => CharacterIndex.Value = index;
    }
}
