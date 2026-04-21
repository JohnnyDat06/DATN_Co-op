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
        [SerializeField] private TMP_InputField playerNameInputField;
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

        [Header("Custom Visuals")]
        [SerializeField] private Sprite readySprite;
        [SerializeField] private Sprite unreadySprite;

        [Header("Audio Configs")]
        [SerializeField] private SOAudioClip hoverSFX;
        [SerializeField] private SOAudioClip clickSFX;

        private void Start()
        {
            // Tự động gắn Juice cho tất cả các nút để đỡ phải kéo tay
            AddJuiceToAllButtons();

            // Initial state
            ShowMainMenu();

            // Button Listeners
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            quickJoinButton.onClick.AddListener(OnQuickJoinClicked);

            confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
            backFromJoinButton.onClick.AddListener(OnBackFromJoinClicked);

            readyButton.onClick.AddListener(OnReadyClicked);
            startButton.onClick.AddListener(OnStartClicked);
            leaveButton.onClick.AddListener(OnLeaveClicked);

            // Input field listeners
            playerNameInputField.onValueChanged.AddListener(OnPlayerNameChanged);
            UpdateMainMenuButtonsState();

            // Lobby Manager Events
            LobbyManager.Instance.OnLobbyJoined += UpdateRoomUI;
            LobbyManager.Instance.OnLobbyLeft += ShowMainMenu;
        }

        private void OnDestroy()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyJoined -= UpdateRoomUI;
                LobbyManager.Instance.OnLobbyLeft -= ShowMainMenu;
            }
        }

        private void OnPlayerNameChanged(string newName)
        {
            UpdateMainMenuButtonsState();
        }

        private void UpdateMainMenuButtonsState()
        {
            if (createRoomButton == null || playerNameInputField == null) return;
            
            bool hasName = !string.IsNullOrEmpty(playerNameInputField.text);
            createRoomButton.interactable = hasName;
            if (joinRoomButton != null) joinRoomButton.interactable = hasName;
            if (quickJoinButton != null) quickJoinButton.interactable = hasName;
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
            if (roomPanel != null) roomPanel.SetActive(false);
        }

        private async Task<bool> EnsureAuthenticated()
        {
            if (string.IsNullOrEmpty(playerNameInputField.text)) return false;
            
            // Show loading or something if needed
            await LobbyManager.Instance.Authenticate(playerNameInputField.text);
            return true;
        }

        private async void OnCreateRoomClicked()
        {
            if (await EnsureAuthenticated())
            {
                await LobbyManager.Instance.CreateLobby("MyRoom", 2, false);
            }
        }

        private void OnJoinRoomClicked()
        {
            mainMenuPanel.SetActive(false);
            joinRoomPanel.SetActive(true);
        }

        private async void OnQuickJoinClicked()
        {
            if (await EnsureAuthenticated())
            {
                await LobbyManager.Instance.QuickJoinLobby();
            }
        }

        private async void OnConfirmJoinClicked()
        {
            string code = roomCodeInputField.text;
            if (!string.IsNullOrEmpty(code) && await EnsureAuthenticated())
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
                    bool isReady = localPlayer.IsReady.Value;
                    
                    // Update Text
                    var btnText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = isReady ? "UNREADY" : "READY";
                    }

                    // Update Image/Sprite
                    var btnImage = readyButton.GetComponent<Image>();
                    if (btnImage != null)
                    {
                        // Nếu đang Ready thì hiện ảnh Unready (để người dùng bấm vào để Unready)
                        // Hoặc ngược lại tùy theo thiết kế của bạn. 
                        // Thông thường ảnh trên nút là "Hành động sẽ thực hiện" hoặc "Trạng thái hiện tại".
                        // Theo yêu cầu của bạn: "bấm redy thì sẽ đổi thành ảnh khác là unready"
                        btnImage.sprite = isReady ? unreadySprite : readySprite;
                    }
                }
            }
        }

        private void OnReadyClicked()
        {
            Debug.Log("[LobbyUI] Ready Button Clicked!");
            
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;
          
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj == null) return;

            if (playerObj.TryGetComponent<LobbyPlayerState>(out var localPlayer))
            {
                Debug.Log($"[LobbyUI] Toggling Ready. Current: {localPlayer.IsReady.Value}");
                localPlayer.ToggleReadyServerRpc();
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
                    Debug.Log("[LobbyUI] SUCCESS: All players ready. Loading scene Map1_Main...");
                    LobbyManager.Instance.StartGame("Map1_Main");
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

        private void AddJuiceToAllButtons()
        {
            // Sử dụng danh sách các nút đã kéo vào Inspector
            Button[] buttons = { 
                createRoomButton, joinRoomButton, quickJoinButton, 
                confirmJoinButton, backFromJoinButton, 
                readyButton, startButton, leaveButton 
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    UIButtonJuice juice = btn.GetComponent<UIButtonJuice>();
                    if (juice == null) juice = btn.gameObject.AddComponent<UIButtonJuice>();
                    
                    // Gán âm thanh từ config của LobbyUI
                    juice.hoverSFX = hoverSFX;
                    juice.clickSFX = clickSFX;
                    
                    Debug.Log($"[LobbyUI] Added Juice and SFX to button: {btn.name}");
                }
            }
        }
    }
}

