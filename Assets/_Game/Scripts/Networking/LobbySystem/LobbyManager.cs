using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Networking.LobbySystem
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        private Lobby _currentLobby;
        private string _playerId;
        private string _playerName; // Lưu tên để dùng khi join/create
        private float _pollTimer;

        private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
        private const string KEY_ROOM_CODE = "RoomCode";

        public event Action<Lobby> OnLobbyJoined;
        public event Action OnLobbyLeft;

        private void Awake() { Instance = this; DontDestroyOnLoad(gameObject); }

        private void Update() 
        { 
            HandleLobbyPollForUpdates(); 
        }

        private async void HandleLobbyPollForUpdates() 
        {
            if (_currentLobby != null) 
            {
                _pollTimer -= Time.deltaTime;
                if (_pollTimer <= 0f) 
                {
                    _pollTimer = 1.5f;
                    try {
                        _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
                        OnLobbyJoined?.Invoke(_currentLobby);

                        if (_currentLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE) && _currentLobby.HostId != _playerId) 
                        {
                            string code = _currentLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                            if (!string.IsNullOrEmpty(code) && !NetworkManager.Singleton.IsConnectedClient) JoinRelay(code);
                        }
                    } catch { }
                }
            }
        }

        public async Task Authenticate(string playerName) {
            _playerName = playerName; // Lưu lại tên

            if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized) 
                await Unity.Services.Core.UnityServices.InitializeAsync();
            
            if (!AuthenticationService.Instance.IsSignedIn) 
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[LobbyManager] Authenticated (ID: {_playerId}). Name set to: {_playerName}");
        }

        private Player GetPlayerData() {
            return new Player {
                Data = new Dictionary<string, PlayerDataObject> {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) }
                }
            };
        }

        public async Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate) {
            try {
                string roomCode = UnityEngine.Random.Range(1000, 9999).ToString();
                var options = new CreateLobbyOptions { 
                    Data = new Dictionary<string, DataObject> { { KEY_ROOM_CODE, new DataObject(DataObject.VisibilityOptions.Public, roomCode) } },
                    Player = GetPlayerData() // Gửi tên ngay khi tạo
                };
                _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                OnLobbyJoined?.Invoke(_currentLobby);
                
                string relayCode = await CreateRelay();
                await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, new UpdateLobbyOptions { 
                    Data = new Dictionary<string, DataObject> { { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) } } 
                });
            } catch (Exception e) { Debug.LogError(e.Message); }
        }

        public void StartGame(string sceneName) {
            if (NetworkManager.Singleton.IsServer) {
                Debug.Log($"[LobbyManager] StartGame requested for scene: {sceneName}");
                StartCoroutine(StartGameWithFade(sceneName));
            }
        }

        private IEnumerator StartGameWithFade(string sceneName) {
            Debug.Log("<color=cyan><size=20><b>[LOBBY MANAGER] >>> STARTING SEAMLESS SEQUENCE <<<</b></size></color>");
            
            if (LoadingSyncManager.Instance != null) {
                Debug.Log("<color=white><size=15><b>[LOBBY MANAGER] STEP 1: TRIGGERING FADE IN...</b></size></color>");
                LoadingSyncManager.Instance.StartLoadingFadeClientRpc();
            } else {
                Debug.LogError("<color=red><size=20><b>[ERROR] LOADING SYNC MANAGER IS MISSING!</b></size></color>");
            }
            
            // DÙNG REALTIME ĐỂ TRÁNH BỊ KẸT NẾU TIME.TIMESCALE = 0
            yield return new WaitForSecondsRealtime(0.8f);

            if (SceneLoader.Instance != null) {
                Debug.Log("<color=yellow><size=18><b>[LOBBY MANAGER] STEP 2: CALLING SCENE LOADER...</b></size></color>");
                SceneLoader.Instance.LoadScene(sceneName);
            } else {
                Debug.LogError("<color=red><size=20><b>[ERROR] SCENE LOADER INSTANCE NOT FOUND!</b></size></color>");
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        public async Task QuickJoinLobby() {
            try {
                var options = new QuickJoinLobbyOptions {
                    Player = GetPlayerData() // Gửi tên ngay khi join nhanh
                };
                _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
                OnLobbyJoined?.Invoke(_currentLobby);
            } catch { }
        }

        public async Task JoinLobbyByCode(string roomCode) {
            try {
                var query = await LobbyService.Instance.QueryLobbiesAsync();
                foreach (var l in query.Results) {
                    if (l.Data != null && l.Data.ContainsKey(KEY_ROOM_CODE) && l.Data[KEY_ROOM_CODE].Value == roomCode) {
                        var options = new JoinLobbyByIdOptions {
                            Player = GetPlayerData() // Gửi tên ngay khi join bằng code
                        };
                        _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(l.Id, options);
                        OnLobbyJoined?.Invoke(_currentLobby);
                        return;
                    }
                }
            } catch { }
        }

        public async Task LeaveLobby() {
            try {
                if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
                if (_currentLobby != null) await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, _playerId);
                _currentLobby = null; OnLobbyLeft?.Invoke();
            } catch { }
        }

        private async Task<string> CreateRelay() {
            var allocation = await RelayService.Instance.CreateAllocationAsync(2);
            var code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            NetworkManager.Singleton.StartHost();
            return code;
        }

        private async void JoinRelay(string code) {
            try {
                var joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, "dtls"));
                NetworkManager.Singleton.StartClient();
            } catch { }
        }

        public string GetPlayerId() => _playerId;
        public string GetPlayerName() => _playerName;
    }
}
