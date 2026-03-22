using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerModel — Assign đúng model (Male/Female) và Animator Controller theo role (Host/Client).
/// Host → Male mesh + Host AnimController.
/// Client → Female mesh + Client AnimController.
/// Detect role qua NGO OnNetworkSpawn.
/// SRS §4.1.1
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

    /// <summary>
    /// Gọi khi NetworkObject spawn — thời điểm chắc chắn biết role.
    /// ApplyModel chạy trên tất cả clients → remote player cũng thấy đúng model.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Lấy ID chủ sở hữu của NetworkObject này.
        // NẾU thuộc về ServerClientId (thường là 0) -> là nhân vật của Host. Ngược lại là của Client.
        bool isHostPlayer = OwnerClientId == NetworkManager.ServerClientId;
        ApplyModel(isHostPlayer);
    }

    private void ApplyModel(bool isHost)
    {
        // Bật đúng mesh
        if (_meshMale != null)
            _meshMale.SetActive(isHost);

        if (_meshFemale != null)
            _meshFemale.SetActive(!isHost);

        // Gán đúng Animator Controller
        if (_animator != null)
        {
            _animator.runtimeAnimatorController = isHost ? _hostAnimatorController : _clientAnimatorController;
            _animator.avatar = isHost ? _avatarMaleAnimator : _avatarFemaleAnimator;
        }

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log($"[PlayerModel] Applied {(isHost ? "Male (Host)" : "Female (Client)")} model.");
#endif
    }
}
