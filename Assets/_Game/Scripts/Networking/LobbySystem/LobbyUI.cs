using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Networking.LobbySystem
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button quickJoinButton;

        [Header("Join Room UI")]
        [SerializeField] private GameObject joinRoomPanel;
        [SerializeField] private TMP_InputField roomCodeInputField;
        [SerializeField] private Button confirmJoinButton;
        [SerializeField] private Button backFromJoinButton;

        [Header("Lobby/Room UI")]
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;

        private async void Start()
        {
            // Initial state
            ShowMainMenu();

            // Authentication
            await LobbyManager.Instance.Authenticate("Player_" + Random.Range(10, 99));

            // Button Listeners
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            quickJoinButton.onClick.AddListener(OnQuickJoinClicked);

            confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
            backFromJoinButton.onClick.AddListener(OnBackFromJoinClicked);

            readyButton.onClick.AddListener(OnReadyClicked);
            startButton.onClick.AddListener(OnStartClicked);
            leaveButton.onClick.AddListener(OnLeaveClicked);

            // Lobby Manager Events
            LobbyManager.Instance.OnLobbyJoined += UpdateRoomUI;
            LobbyManager.Instance.OnLobbyLeft += ShowMainMenu;
        }

        private void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            joinRoomPanel.SetActive(false);
            roomPanel.SetActive(false);
        }

        private async void OnCreateRoomClicked()
        {
            await LobbyManager.Instance.CreateLobby("MyRoom", 2, false);
        }

        private void OnJoinRoomClicked()
        {
            mainMenuPanel.SetActive(false);
            joinRoomPanel.SetActive(true);
        }

        private async void OnQuickJoinClicked()
        {
            await LobbyManager.Instance.QuickJoinLobby();
        }

        private async void OnConfirmJoinClicked()
        {
            string code = roomCodeInputField.text;
            if (!string.IsNullOrEmpty(code))
            {
                await LobbyManager.Instance.JoinLobbyByCode(code);
            }
        }

        private void OnBackFromJoinClicked()
        {
            ShowMainMenu();
        }

        private void UpdateRoomUI(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            mainMenuPanel.SetActive(false);
            joinRoomPanel.SetActive(false);
            roomPanel.SetActive(true);

            if (lobby.Data.ContainsKey("RoomCode"))
            {
                roomCodeText.text = "Room Code: " + lobby.Data["RoomCode"].Value;
            }

            // Only host can start
            startButton.gameObject.SetActive(lobby.HostId == LobbyManager.Instance.GetPlayerId());
            
            // Character visualization would go here
            Debug.Log($"Players in lobby: {lobby.Players.Count}");
        }

        private void Update()
        {
            // Cập nhật chữ trên nút Ready dựa trên trạng thái thực tế
            UpdateReadyButtonVisual();
        }

        private void UpdateReadyButtonVisual()
        {
            if (readyButton == null) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<LobbyPlayerState>();
                if (localPlayer != null)
                {
                    var btnText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = localPlayer.IsReady.Value ? "UNREADY" : "READY";
                    }
                }
            }
        }

        private void OnReadyClicked()
        {
            Debug.Log("[LobbyUI] Ready Button Clicked!");
            
            if (NetworkManager.Singleton == null) { Debug.LogError("NetworkManager is null!"); return; }
            if (NetworkManager.Singleton.LocalClient == null) { Debug.LogError("LocalClient is null!"); return; }
            
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj == null)
            {
                Debug.LogError("Local PlayerObject is null! The Player Prefab might not be spawned yet. Check NetworkManager settings.");
                return;
            }

            var localPlayer = playerObj.GetComponent<LobbyPlayerState>();
            if (localPlayer != null)
            {
                Debug.Log($"[LobbyUI] Sending ToggleReadyServerRpc. Current State: {localPlayer.IsReady.Value}");
                localPlayer.ToggleReadyServerRpc();
            }
            else
            {
                Debug.LogError("LobbyPlayerState component NOT FOUND on PlayerObject!");
            }
        }

        private void OnStartClicked()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var players = GameObject.FindObjectsByType<LobbyPlayerState>(FindObjectsSortMode.None);
                int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;

                Debug.Log($"[LobbyUI] Start attempt: Found {players.Length} player states, Netcode says {connectedCount} clients connected.");
                
                if (players.Length < connectedCount)
                {
                    Debug.LogWarning($"[LobbyUI] Wait! We have {connectedCount} clients but only {players.Length} LobbyPlayerState objects found in scene.");
                    return;
                }

                bool allReady = true;
                foreach (var p in players)
                {
                    Debug.Log($"[LobbyUI] Checking Player {p.OwnerClientId}: IsReady = {p.IsReady.Value}");
                    if (!p.IsReady.Value) allReady = false;
                }

                if (allReady && players.Length > 0)
                {
                    Debug.Log("[LobbyUI] SUCCESS: All players ready. Loading scene LV1...");
                    LobbyManager.Instance.StartGame("LV1");
                }
                else
                {
                    Debug.LogWarning($"[LobbyUI] CANNOT START: AllReady={allReady}, TotalPlayers={players.Length}");
                }
            }
        }

        private async void OnLeaveClicked()
        {
            await LobbyManager.Instance.LeaveLobby();
        }
    }
}

