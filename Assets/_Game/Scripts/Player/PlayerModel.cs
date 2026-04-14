using Unity.Netcode;
using UnityEngine;
using System.Linq;

/// <summary>
/// PlayerModel — VIP Update: Tự động kích hoạt bản thân và xếp chỗ đứng.
/// </summary>
public class PlayerModel : NetworkBehaviour
{
    [Header("Mesh")]
    [SerializeField] private GameObject _meshMale;
    [SerializeField] private GameObject _meshFemale;

    [Header("Animator Controllers")]
    [SerializeField] private RuntimeAnimatorController _hostAnimatorController;
    [SerializeField] private RuntimeAnimatorController _clientAnimatorController;
    
    [Header("Avatar Animator")]
    [SerializeField] private Avatar _avatarMaleAnimator;
    [SerializeField] private Avatar _avatarFemaleAnimator;
    
    [SerializeField] private Animator _animator;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1. CƯỠNG ÉP BẬT ACTIVE CỦA CHÍNH NÓ (Fix lỗi tàng hình do Prefab bị tắt)
        gameObject.SetActive(true);

        // 2. Xếp bục đứng
        AutoPosition();

        // 3. Hiển thị mô hình
        bool isHostPlayer = OwnerClientId == NetworkManager.ServerClientId;
        ApplyModel(isHostPlayer);
    }

    private void AutoPosition()
    {
        if (NetworkManager.Singleton == null) return;

        var anchors = GameObject.FindGameObjectsWithTag("LobbyAnchor")
                                .OrderBy(a => a.name)
                                .ToList();

        if (anchors.Count == 0) return;

        // Dùng IndexOf để xác định slot thực tế thay vì ID
        var clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        int slot = clientIds.IndexOf(OwnerClientId);
        
        if (slot == -1) slot = 0; // Fallback cho Host/Server
        int index = slot % anchors.Count;

        transform.position = anchors[index].transform.position;
        transform.rotation = anchors[index].transform.rotation;
        
        Debug.Log($"[PlayerModel] AutoPosition assigned Player {OwnerClientId} to Anchor {index} (Slot {slot})");
    }

    private void ApplyModel(bool isHost)
    {
        // Bật mesh tương ứng
        if (_meshMale != null) _meshMale.SetActive(isHost);
        if (_meshFemale != null) _meshFemale.SetActive(!isHost);

        if (_animator != null)
        {
            _animator.runtimeAnimatorController = isHost ? _hostAnimatorController : _clientAnimatorController;
            _animator.avatar = isHost ? _avatarMaleAnimator : _avatarFemaleAnimator;
        }

        // 4. ÉP HIỆN HÌNH TẤT CẢ CON (Nếu mesh bị tắt sẵn)
        foreach (var r in GetComponentsInChildren<Renderer>(true)) {
            r.enabled = true;
        }

        Debug.Log($"[PlayerModel] Forced Active and Applied {(isHost ? "Male" : "Female")} for Player {OwnerClientId}");
    }
}
