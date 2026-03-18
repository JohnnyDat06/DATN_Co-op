using System;
using UnityEngine;

/// <summary>
/// EventBus — Static class chứa toàn bộ C# Action của dự án.
/// Là trung tâm giao tiếp giữa mọi module.
/// Không module nào được gọi trực tiếp module khác nếu có thể dùng EventBus.
/// SRS §13.2
/// </summary>
public static class EventBus
{
    // ─── Player ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: PlayerHealth | Subscriber: RespawnManager, HUDController
    /// </summary>
    public static event Action<ulong> OnPlayerDied;

    /// <summary>
    /// Publisher: RespawnManager | Subscriber: PlayerController, HUDController, CameraManager
    /// </summary>
    public static event Action<ulong, Vector3> OnPlayerRespawned;

    // ─── Level ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: LevelGoal | Subscriber: SceneLoader, CloudSaveManager
    /// </summary>
    public static event Action<int> OnLevelCompleted;

    /// <summary>
    /// Publisher: CheckpointTrigger | Subscriber: CheckpointManager, CloudSaveManager
    /// </summary>
    public static event Action<string, Vector3> OnCheckpointReached;

    // ─── Interactable ─────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: InteractableBase | Subscriber: Door, Platform, any receiver
    /// </summary>
    public static event Action<string> OnInteractableActivated;

    /// <summary>
    /// Publisher: CoopInteractable | Subscriber: PromptUIManager
    /// </summary>
    public static event Action<string, ulong> OnCoopInteractablePlayerReady;

    /// <summary>
    /// Publisher: CoopInteractable | Subscriber: PromptUIManager
    /// </summary>
    public static event Action<string> OnCoopInteractableReset;

    // ─── Game State ───────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: PauseManager | Subscriber: UIManager, Network
    /// </summary>
    public static event Action OnGamePaused;

    /// <summary>
    /// Publisher: PauseManager | Subscriber: UIManager, Network
    /// </summary>
    public static event Action OnGameResumed;

    // ─── Settings ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: SettingsManager | Subscriber: AudioMixer, ScreenShakeController
    /// </summary>
    public static event Action OnSettingsChanged;

    /// <summary>
    /// Publisher: AccessibilitySettingsService | Subscriber: ScreenShakeController, PromptUI
    /// </summary>
    public static event Action OnAccessibilityChanged;

    // ─── Network / Lobby ──────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: LobbyManager | Subscriber: SceneLoader
    /// </summary>
    public static event Action OnAllPlayersReady;

    /// <summary>
    /// Publisher: NGO NetworkManager | Subscriber: GameFlowManager
    /// </summary>
    public static event Action<ulong> OnClientConnected;

    /// <summary>
    /// Publisher: NGO NetworkManager | Subscriber: GameFlowManager
    /// </summary>
    public static event Action<ulong> OnClientDisconnected;

    // ─── Camera ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: CameraZoneTrigger, CutSceneManager | Subscriber: CameraManager
    /// TODO: replace int with CameraPreset enum (T2-1)
    /// </summary>
    public static event Action<int> OnCameraPresetChanged;

    /// <summary>
    /// Publisher: CameraSettingsService | Subscriber: VCam
    /// </summary>
    public static event Action OnCameraSettingsChanged;

    // ─── FX ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: PlayerController, Environment | Subscriber: ScreenShakeController
    /// TODO: replace int with ScreenShakeData struct (T5-3)
    /// </summary>
    public static event Action<int> OnScreenShakeRequested;

    // ─── CutScene ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: CutSceneManager | Subscriber: PlayerInputHandler
    /// </summary>
    public static event Action OnCutSceneStarted;

    /// <summary>
    /// Publisher: CutSceneManager | Subscriber: PlayerInputHandler
    /// </summary>
    public static event Action OnCutSceneEnded;

    // ─── Input ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Publisher: InputRebindService | Subscriber: PlayerInputHandler, PromptUIManager
    /// </summary>
    public static event Action OnInputBindingChanged;

    // ─── Raise Methods ────────────────────────────────────────────────────────

    /// <summary>PlayerHealth raises this khi player chết.</summary>
    public static void RaisePlayerDied(ulong clientId)
        => OnPlayerDied?.Invoke(clientId);

    /// <summary>RespawnManager raises this khi player hồi sinh.</summary>
    public static void RaisePlayerRespawned(ulong clientId, Vector3 spawnPosition)
        => OnPlayerRespawned?.Invoke(clientId, spawnPosition);

    /// <summary>LevelGoal raises this khi hoàn thành màn.</summary>
    public static void RaiseLevelCompleted(int levelIndex)
        => OnLevelCompleted?.Invoke(levelIndex);

    /// <summary>CheckpointTrigger raises this khi chạm checkpoint.</summary>
    public static void RaiseCheckpointReached(string checkpointId, Vector3 spawnPos)
        => OnCheckpointReached?.Invoke(checkpointId, spawnPos);

    /// <summary>InteractableBase raises this khi được kích hoạt.</summary>
    public static void RaiseInteractableActivated(string interactableId)
        => OnInteractableActivated?.Invoke(interactableId);

    /// <summary>CoopInteractable raises this khi một player đã sẵn sàng.</summary>
    public static void RaiseCoopInteractablePlayerReady(string interactableId, ulong clientId)
        => OnCoopInteractablePlayerReady?.Invoke(interactableId, clientId);

    /// <summary>CoopInteractable raises this khi reset trạng thái.</summary>
    public static void RaiseCoopInteractableReset(string interactableId)
        => OnCoopInteractableReset?.Invoke(interactableId);

    /// <summary>PauseManager raises this khi game bị pause.</summary>
    public static void RaiseGamePaused()
        => OnGamePaused?.Invoke();

    /// <summary>PauseManager raises this khi game được resume.</summary>
    public static void RaiseGameResumed()
        => OnGameResumed?.Invoke();

    /// <summary>SettingsManager raises this khi settings thay đổi.</summary>
    public static void RaiseSettingsChanged()
        => OnSettingsChanged?.Invoke();

    /// <summary>AccessibilitySettingsService raises this khi accessibility thay đổi.</summary>
    public static void RaiseAccessibilityChanged()
        => OnAccessibilityChanged?.Invoke();

    /// <summary>LobbyManager raises this khi cả 2 player đã sẵn sàng.</summary>
    public static void RaiseAllPlayersReady()
        => OnAllPlayersReady?.Invoke();

    /// <summary>NetworkManagerWrapper raises this khi client kết nối.</summary>
    public static void RaiseClientConnected(ulong clientId)
        => OnClientConnected?.Invoke(clientId);

    /// <summary>NetworkManagerWrapper raises this khi client ngắt kết nối.</summary>
    public static void RaiseClientDisconnected(ulong clientId)
        => OnClientDisconnected?.Invoke(clientId);

    /// <summary>CameraZoneTrigger/CutSceneManager raises này để đổi camera preset.</summary>
    public static void RaiseCameraPresetChanged(int presetId)
        => OnCameraPresetChanged?.Invoke(presetId);

    /// <summary>CameraSettingsService raises this khi camera settings thay đổi.</summary>
    public static void RaiseCameraSettingsChanged()
        => OnCameraSettingsChanged?.Invoke();

    /// <summary>PlayerController/Environment raises this để yêu cầu screen shake.</summary>
    public static void RaiseScreenShakeRequested(int shakeDataId)
        => OnScreenShakeRequested?.Invoke(shakeDataId);

    /// <summary>CutSceneManager raises this khi cutscene bắt đầu.</summary>
    public static void RaiseCutSceneStarted()
        => OnCutSceneStarted?.Invoke();

    /// <summary>CutSceneManager raises this khi cutscene kết thúc.</summary>
    public static void RaiseCutSceneEnded()
        => OnCutSceneEnded?.Invoke();

    /// <summary>InputRebindService raises này khi input binding thay đổi.</summary>
    public static void RaiseInputBindingChanged()
        => OnInputBindingChanged?.Invoke();
}
