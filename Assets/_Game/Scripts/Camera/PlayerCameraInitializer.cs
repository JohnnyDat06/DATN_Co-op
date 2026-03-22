using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gắn trên Player prefab. Thiết lập camera cho local player khi spawn.
/// Đảm bảo mỗi máy khách chỉ điều khiển camera bám theo player của chính nó.
/// </summary>
public class PlayerCameraInitializer : NetworkBehaviour
{
    [Header("Look At Target")]
    [SerializeField] private Transform _cameraLookTarget;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Chỉ xử lý nếu đây là local player (máy khách sở hữu player này)
        if (!IsOwner)
        {
            return;
        }

        InitializeCamera();
    }

    private void InitializeCamera()
    {
        if (CameraManager.Instance == null)
        {
            Debug.LogError("[PlayerCameraInitializer] CameraManager.Instance không tìm thấy trong scene!");
            return;
        }

        // Tự động tìm LookTarget nếu chưa được gán
        if (_cameraLookTarget == null)
        {
            _cameraLookTarget = FindLookTargetRecursive(transform);
        }

        // Gán target cho CameraManager (Singleton cục bộ trên máy này)
        CameraManager.Instance.SetPlayerTarget(transform, _cameraLookTarget);

        // Setup cursor mặc định
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

#if UNITY_EDITOR || DEBUG_BUILD
        Debug.Log($"[PlayerCameraInitializer] Camera targets initialized for {(IsHost ? "Host" : "Client")} player.");
#endif
    }

    private Transform FindLookTargetRecursive(Transform parent)
    {
        if (parent.name == "CameraLookTarget") return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindLookTargetRecursive(parent.GetChild(i));
            if (result != null) return result;
        }

        return parent; // Trả về chính nó nếu không tìm thấy con nào tên CameraLookTarget
    }
}
