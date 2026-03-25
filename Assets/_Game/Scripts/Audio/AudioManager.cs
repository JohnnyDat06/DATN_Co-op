using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// <summary>
/// AudioManager — Singleton quản lý việc phát âm thanh toàn cục sử dụng SOAudioClip.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Mixer Settings")]
    public AudioMixer MainMixer;

    private List<AudioSource> _sfxSources = new List<AudioSource>();
    private const int INITIAL_POOL_SIZE = 10;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            CreateNewSFXSource();
        }
    }

    private AudioSource CreateNewSFXSource()
    {
        GameObject go = new GameObject("SFXSource_" + _sfxSources.Count);
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        
        if (MainMixer != null)
        {
            AudioMixerGroup[] groups = MainMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
        }

        _sfxSources.Add(source);
        return source;
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var source in _sfxSources)
        {
            if (!source.isPlaying) return source;
        }
        return CreateNewSFXSource();
    }

    public AudioSource PlaySFXLoop(SOAudioClip config)
    {
        if (config == null || config.Clip == null) return null;

        AudioSource source = GetAvailableSource();
        source.spatialBlend = 0f; // Luôn là 2D cho âm thanh lặp
        source.clip = config.Clip;
        source.volume = config.Volume;
        source.pitch = Random.Range(config.PitchMin, config.PitchMax);
        source.loop = true;
        source.Play();
        return source;
    }

    public void StopSFX(AudioSource source)
    {
        if (source != null)
        {
            source.Stop();
            source.clip = null; // Giải phóng clip
            source.loop = false;
        }
    }

    public void PlaySFX(SOAudioClip config, Vector3? position = null)
    {
        if (config == null || config.Clip == null) return;

        AudioSource source = GetAvailableSource();
        
        // Reset spatial blend dựa trên việc có position hay không
        source.spatialBlend = position.HasValue ? 1f : 0f;
        if (position.HasValue)
        {
            source.transform.position = position.Value;
        }

        source.clip = config.Clip;
        source.volume = config.Volume;
        source.pitch = Random.Range(config.PitchMin, config.PitchMax);
        source.loop = false; 
        source.Play();
    }
}
