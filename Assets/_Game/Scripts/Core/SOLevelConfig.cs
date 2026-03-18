using UnityEngine;

/// <summary>
/// SOLevelConfig — Config data cho từng Level/màn chơi.
/// SRS §13.3 · §7.2
/// </summary>
[CreateAssetMenu(fileName = "SOLevelConfig", menuName = "CoopGame/Level/LevelConfig")]
public class SOLevelConfig : ScriptableObject
{
    [Header("Scene")]
    [Tooltip("Tên scene trong Build Settings — dùng Constants.Scenes.*")]
    public string SceneName;

    [Tooltip("Index màn (1–4)")]
    public int LevelIndex;

    [Header("Audio")]
    [Tooltip("AudioClip nhạc nền BGM của màn này")]
    public AudioClip BGMTrack;

    [Tooltip("AudioClip ambient loop của màn này")]
    public AudioClip AmbientTrack;

    [Header("Save")]
    [Tooltip("Bật auto-save khi hoàn thành màn")]
    public bool AutoSaveEnabled = true;

    [Header("CutScene")]
    [Tooltip("Timeline hoặc VideoClip cho cảnh intro khi bắt đầu màn")]
    public UnityEngine.Object CutSceneIntroClip;   // TimelineAsset hoặc VideoClip

    [Tooltip("Timeline hoặc VideoClip cho cảnh outro khi hoàn thành màn")]
    public UnityEngine.Object CutSceneOutroClip;
}
