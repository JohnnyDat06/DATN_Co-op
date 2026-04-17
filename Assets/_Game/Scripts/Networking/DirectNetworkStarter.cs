using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Script hỗ trợ khởi động nhanh Network mà không cần qua Lobby.
    /// Dùng để test các Map trực tiếp trong Unity Editor.
    /// </summary>
    public class DirectNetworkStarter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string defaultIp = "127.0.0.1";
        [SerializeField] private ushort defaultPort = 7777;
        [SerializeField] private bool showGui = true;

        private void Awake()
        {
            // Nếu không có NetworkManager trong scene thì cảnh báo
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[DirectNetworkStarter] NetworkManager missing from scene!");
            }
        }

        private void OnGUI()
        {
            if (!showGui) return;
            
            // Chỉ hiện bảng điều khiển nếu Network chưa chạy
            if (NetworkManager.Singleton != null && 
                !NetworkManager.Singleton.IsClient && 
                !NetworkManager.Singleton.IsServer)
            {
                GUILayout.BeginArea(new Rect(20, 20, 250, 150), "Direct Network Boot", "Window");
                
                if (GUILayout.Button("Host (Server + Player)"))
                {
                    StartAsHost();
                }

                if (GUILayout.Button("Server Only"))
                {
                    NetworkManager.Singleton.StartServer();
                }

                GUILayout.Space(10);
                defaultIp = GUILayout.TextField(defaultIp);
                
                if (GUILayout.Button("Join as Client"))
                {
                    StartAsClient();
                }

                GUILayout.EndArea();
            }
        }

        public void StartAsHost()
        {
            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetConnectionData(defaultIp, defaultPort);
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[DirectNetworkStarter] Started Host on {defaultIp}:{defaultPort}");
        }

        public void StartAsClient()
        {
            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetConnectionData(defaultIp, defaultPort);
            NetworkManager.Singleton.StartClient();
            Debug.Log($"[DirectNetworkStarter] Started Client joining {defaultIp}:{defaultPort}");
        }
    }
}
