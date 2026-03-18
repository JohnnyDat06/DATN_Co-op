using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkManagerWrapper — Wrapper xử lý callbacks của NGO NetworkManager và bridge vào EventBus.
/// Tách biệt NGO khỏi game logic. Subscribe trong OnEnable, unsubscribe trong OnDisable (tránh memory leak).
/// SRS §5.1
/// </summary>
public class NetworkManagerWrapper : MonoBehaviour
{
    private void OnEnable()
    {
        // Try early subscribe. If NetworkManager not ready, we rely on Start().
        if (NetworkManager.Singleton != null)
        {
            SubscribeToNetworkEvents();
        }
    }

    private void Start()
    {
        // Ensure subscription happens if OnEnable missed it
        if (NetworkManager.Singleton != null && !hasSubscribed)
        {
            SubscribeToNetworkEvents();
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && hasSubscribed)
        {
            UnsubscribeFromNetworkEvents();
        }
    }

    private bool hasSubscribed = false;

    private void SubscribeToNetworkEvents()
    {
        if (hasSubscribed) return;
        
        NetworkManager.Singleton.OnClientConnectedCallback  += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        hasSubscribed = true;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (!hasSubscribed) return;

        NetworkManager.Singleton.OnClientConnectedCallback  -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        hasSubscribed = false;
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkWrapper] Client connected: {clientId}");
        EventBus.RaiseClientConnected(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkWrapper] Client disconnected: {clientId}");
        EventBus.RaiseClientDisconnected(clientId);
    }
}
