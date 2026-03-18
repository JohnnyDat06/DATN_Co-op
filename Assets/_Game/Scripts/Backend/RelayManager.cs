using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// RelayManager — Xử lý toàn bộ luồng tạo phòng (Host) và tham gia phòng (Client) qua Unity Relay.
/// Không Singleton — inject vào class cần thiết.
/// Phụ thuộc: AuthManager phải IsAuthenticated = true trước khi gọi bất kỳ method nào.
/// SRS §6.2 · §5.1
/// </summary>
public class RelayManager : MonoBehaviour
{
    // ─── Events ───────────────────────────────────────────────────────────────

    /// <summary>Được gọi khi Host tạo phòng thành công. string = JoinCode.</summary>
    public event Action<string> OnHostCreated;

    /// <summary>Được gọi khi Client join phòng thành công.</summary>
    public event Action OnClientJoined;

    /// <summary>Được gọi khi có lỗi Relay. string = error message.</summary>
    public event Action<string> OnRelayFailed;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Host tạo Relay allocation. Trả về JoinCode 6 ký tự.
    /// </summary>
    /// <returns>JoinCode 6 ký tự để Client nhập.</returns>
    public async Task<string> CreateRelayAsync()
    {
        try
        {
            // 1. Tạo allocation (maxConnections không tính host)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(
                Constants.Gameplay.MAX_RELAY_PLAYERS - 1
            );

            // 2. Lấy JoinCode
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 3. Cấu hình Unity Transport dùng Relay
            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            // 4. Start Host
            NetworkManager.Singleton.StartHost();

            OnHostCreated?.Invoke(joinCode);
            Debug.Log($"[RelayManager] Host created. JoinCode: {joinCode}");
            return joinCode;
        }
        catch (Exception e)
        {
            OnRelayFailed?.Invoke(e.Message);
            Debug.LogError($"[RelayManager] CreateRelay failed: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Client join bằng JoinCode. Throws nếu code sai hoặc phòng đầy.
    /// </summary>
    /// <param name="joinCode">JoinCode 6 ký tự nhận từ Host.</param>
    public async Task JoinRelayAsync(string joinCode)
    {
        try
        {
            // 1. Join allocation bằng JoinCode
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(
                joinCode.Trim().ToUpper()
            );

            // 2. Cấu hình Unity Transport
            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            // 3. Start Client
            NetworkManager.Singleton.StartClient();

            OnClientJoined?.Invoke();
            Debug.Log($"[RelayManager] Client joined. JoinCode: {joinCode}");
        }
        catch (Exception e)
        {
            OnRelayFailed?.Invoke(e.Message);
            Debug.LogError($"[RelayManager] JoinRelay failed: {e.Message}");
            throw;
        }
    }
}
