using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneLoader — Quản lý nạp cảnh đồng bộ. Host điều khiển, Client lắng nghe.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    public event Action<float> OnLoadProgress;
    [SerializeField] private GameStateMachine _gameStateMachine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Chỉ Host gọi.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Client gọi cái này khi nhận lệnh từ LoadingSyncManager.
    /// </summary>
    public void StartClientLoadingSimulation()
    {
        if (NetworkManager.Singleton.IsHost) return;
        StartCoroutine(ClientProgressRoutine());
    }

    private IEnumerator ClientProgressRoutine()
    {
        float progress = 0;
        // Bò dần lên 90% trong khi đợi Server báo xong
        while (progress < 0.9f)
        {
            progress = Mathf.MoveTowards(progress, 0.9f, Time.deltaTime * 0.5f);
            if (SeamlessLoadingOverlay.Instance != null) SeamlessLoadingOverlay.Instance.SetProgress(progress);
            yield return null;
        }
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        Debug.Log($"<color=yellow>[HOST] Loading scene: {sceneName}</color>");
        if (SeamlessLoadingOverlay.Instance != null) SeamlessLoadingOverlay.Instance.SetProgress(0f);

        bool allClientsReady = false;

        if (NetworkManager.Singleton.IsServer)
        {
            var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started)
            {
                // Đợi cho đến khi TẤT CẢ mọi người (bao gồm Client) load xong
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += (name, mode, clients, timedOut) => {
                    if (name == sceneName) allClientsReady = true;
                };
            }
        }

        float fakeProgress = 0f;
        while (!allClientsReady)
        {
            fakeProgress = Mathf.MoveTowards(fakeProgress, 0.9f, Time.deltaTime * 0.4f);
            if (SeamlessLoadingOverlay.Instance != null) SeamlessLoadingOverlay.Instance.SetProgress(fakeProgress);
            yield return null;
        }

        // Đã xong phần Load Scene! 
        // LƯU Ý: Chúng ta KHÔNG gọi LoadingSyncManager.Instance.EndLoadingFadeClientRpc() ở đây nữa.
        // Quyền này sẽ được nhường cho PlayerSpawner sau khi nó đã Teleport toàn bộ người chơi xong.
        Debug.Log("<color=green>[HOST] Scene loaded. Waiting for PlayerSpawner to position everyone...</color>");

        if (_gameStateMachine != null) _gameStateMachine.TransitionTo(GameState.Playing);
    }

    public void LoadMainMenu()
    {
        try { if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown(); } catch { }
        SceneManager.LoadScene(Constants.Scenes.MAIN_MENU);
    }
}
