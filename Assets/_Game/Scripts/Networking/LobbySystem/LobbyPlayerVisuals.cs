using UnityEngine;
using Unity.Services.Lobbies.Models;

namespace Networking.LobbySystem
{
    public class LobbyPlayerVisuals : MonoBehaviour
    {
        [SerializeField] private GameObject player1Visual;
        [SerializeField] private GameObject player2Visual;

        private void Start()
        {
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.OnLobbyJoined += UpdateVisuals;
                LobbyManager.Instance.OnLobbyLeft += ClearVisuals;
            }
            
            ClearVisuals();
        }

        private void UpdateVisuals(Lobby lobby)
        {
            try {
                int playerCount = lobby.Players.Count;
                
                // Tránh lỗi UnassignedReferenceException làm treo UI
                if (player1Visual != null) player1Visual.SetActive(playerCount >= 1);
                if (player2Visual != null) player2Visual.SetActive(playerCount >= 2);
            } catch (System.Exception e) {
                Debug.LogWarning("LobbyPlayerVisuals Error: " + e.Message);
            }
        }

        private void ClearVisuals()
        {
            if (player1Visual != null) player1Visual.SetActive(false);
            if (player2Visual != null) player2Visual.SetActive(false);
        }
    }
}
