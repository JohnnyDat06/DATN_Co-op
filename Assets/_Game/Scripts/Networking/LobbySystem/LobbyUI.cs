using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

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

        private void OnReadyClicked()
        {
            // In a real co-op game, we'd sync this state. 
            // For now, it's a placeholder button as requested.
            Debug.Log("Player Ready!");
        }

        private async void OnStartClicked()
        {
            await LobbyManager.Instance.StartGame();
        }

        private async void OnLeaveClicked()
        {
            await LobbyManager.Instance.LeaveLobby();
        }
    }
}
