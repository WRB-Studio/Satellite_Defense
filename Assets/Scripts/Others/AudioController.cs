using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }
    [Header("Music")]
    public AudioClip mainMenuMusic;
    public AudioClip ingameMusic;
    [Header("UI")]
    public AudioClip soundClick;
    public AudioClip openDisplay;
    public AudioClip soundAddLive;
    public AudioClip soundCoinCount;
    public AudioClip soundBuy;
    public AudioClip soundSelect;
    public AudioClip soundNewBestScore;
    [Header("Gameplay")]
    public AudioClip soundEnemyHit;
    public AudioClip soundItemDrop;
    public AudioClip soundPlanetHit;
    public AudioClip soundPlanetDeath;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Min(1)] public int maxSoundSources = 32;

    private readonly List<AudioSource> soundSources = new();
    private AudioSource musicSource;
    private int nextSource;

    private void Awake()
    {
        Instance = this;
        musicSource = CreateSource("Music");
        musicSource.loop = true;
    }

    private AudioSource CreateSource(string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    public static void PlaySound(AudioClip clip, bool randomPitch = false, float? pitch = null)
    {
        if (!Instance || !clip) return;
        AudioSource source = null;
        foreach (var candidate in Instance.soundSources)
        {
            if (!candidate.isPlaying)
            {
                source = candidate;
                break;
            }
        }
        if (!source && Instance.soundSources.Count < Mathf.Max(1, Instance.maxSoundSources))
        {
            source = Instance.CreateSource("Sound");
            Instance.soundSources.Add(source);
        }
        if (!source)
        {
            source = Instance.soundSources[Instance.nextSource];
            Instance.nextSource = (Instance.nextSource + 1) % Instance.soundSources.Count;
        }
        source.Stop();
        source.volume = Instance.volume;
        source.pitch = Mathf.Clamp(pitch ?? (randomPitch ? Random.Range(0.8f, 0.9f) : 1f), 0.1f, 3f);
        source.clip = clip;
        source.Play();
    }

    public static void PlayMusic(AudioClip clip)
    {
        if (!Instance || !clip) return;
        var source = Instance.musicSource;
        source.volume = Instance.volume;
        if (source.clip == clip && source.isPlaying) return;
        source.clip = clip;
        source.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
