using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneLoader — Wrapper async scene loading dùng NGO NetworkSceneManager.
/// Đảm bảo cả 2 client cùng load xong mới bắt đầu gameplay.
/// DontDestroyOnLoad — không Singleton, được inject vào class cần thiết.
/// SRS §10.1 · §10.3
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>Progress khi loading scene: 0.0 → 1.0</summary>
    public event Action<float> OnLoadProgress;

    [SerializeField] private GameStateMachine _gameStateMachine;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Chỉ Host được gọi. Load scene theo tên, sync cả 2 client qua NGO NetworkSceneManager.
    /// </summary>
    /// <param name="sceneName">Tên scene cần load (dùng Constants.Scenes.*).</param>
    public void LoadScene(string sceneName)
    {
        if (_gameStateMachine != null)
            _gameStateMachine.TransitionTo(GameState.Loading);

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Shortcut load level theo index (1–4).
    /// </summary>
    /// <param name="levelIndex">Level index từ 1 đến 4.</param>
    public void LoadLevel(int levelIndex)
    {
        var sceneName = levelIndex switch
        {
            1 => Constants.Scenes.LEVEL_01,
            2 => Constants.Scenes.LEVEL_02,
            3 => Constants.Scenes.LEVEL_03,
            4 => Constants.Scenes.LEVEL_04,
            _ => Constants.Scenes.LEVEL_01
        };

        LoadScene(sceneName);
    }

    /// <summary>
    /// Trả về MainMenu, disconnect network trước.
    /// </summary>
    public void LoadMainMenu()
    {
        try
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SceneLoader] NetworkManager shutdown error: {e.Message}");
        }

        SceneManager.LoadScene(Constants.Scenes.MAIN_MENU);
    }

    // ─── Private ──────────────────────────────────────────────────────────────

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // Báo cáo progress bắt đầu
        OnLoadProgress?.Invoke(0f);

        bool loadComplete = false;
        Exception loadError = null;
        AsyncOperation offlineOp = null;
        bool shouldAbort = false;

        try
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                // Host load qua NGO để sync cả 2 client
                var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"[SceneLoader] Failed to start scene load. Status: {status}");
                    shouldAbort = true;
                }
                else
                {
                    // Subscribe sự kiện load complete
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnNetworkLoadComplete;
                }
            }
            else
            {
                // Offline mode: load bình thường
                offlineOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                if (offlineOp == null)
                {
                    loadError = new InvalidOperationException("LoadSceneAsync returned null operation.");
                }
            }
        }
        catch (Exception e)
        {
            loadError = e;
            Debug.LogError($"[SceneLoader] LoadScene error: {e.Message}");
        }

        if (loadError != null || shouldAbort)
            yield break;

        if (offlineOp != null)
        {
            while (!offlineOp.isDone)
            {
                OnLoadProgress?.Invoke(offlineOp.progress);
                yield return null;
            }

            loadComplete = true;
        }

        // Chờ network load hoàn thành (nếu online)
        float timeout = 30f;
        float elapsed = 0f;
        while (!loadComplete && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            OnLoadProgress?.Invoke(Mathf.Clamp01(elapsed / timeout * 0.9f));
            yield return null;
        }

        OnLoadProgress?.Invoke(1f);

        if (_gameStateMachine != null)
            _gameStateMachine.TransitionTo(GameState.Playing);

        // Cleanup
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnNetworkLoadComplete;

        void OnNetworkLoadComplete(string name, LoadSceneMode mode, System.Collections.Generic.List<ulong> clients, System.Collections.Generic.List<ulong> timedOut)
        {
            if (name == sceneName)
                loadComplete = true;
        }
    }
}
