using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;

    [Header("Music")]
    public AudioClip mainMenuMusic;
    public AudioClip ingameMusic;
    private static AudioSource musicSource;

    [Header("UI sound")]
    public AudioClip soundClick;
    public AudioClip openDisplay;
    public AudioClip soundAddLive;
    public AudioClip soundCoinCount;
    public AudioClip soundBuy;
    public AudioClip soundSelect;
    public AudioClip soundNewBestScore;

    [Header("Enemy sounds")]
    public AudioClip soundEnemyHit;

    [Header("Item sounds")]
    public AudioClip soundItemDrop;

    [Header("Planet")]
    public AudioClip soundPlanetHit;
    public AudioClip soundPlanetDeath;

    public static float stdVolume = 0.6f;
    private static List<AudioSource> audioSrcSoundList = new List<AudioSource>();



    private void Awake()
    {
        Instance = this;
    }

    private AudioSource CreateAudioSource(string name = "tmpAudioSrc")
    {
        var go = new GameObject(name);
        var src = go.AddComponent<AudioSource>();
        audioSrcSoundList.Add(src);

        src.playOnAwake = false;
        src.loop = false;
        src.volume = stdVolume;
        src.spatialBlend = 0f; // 2D

        return src;
    }

    public static AudioSource PlaySound(AudioClip clip, bool randomPitch = false, float? pitch = null)
    {
        var src = Instance.CreateAudioSource();

        src.clip = clip;
        if (randomPitch) src.pitch = Random.Range(0.8f, 0.9f);
        else if (pitch.HasValue) src.pitch = pitch.Value;
        else src.pitch = 1f;

        src.Play();

        DontDestroyOnLoad(src.gameObject);
        // Laufzeit an Pitch anpassen (bei höherer Pitch kürzer)
        var lifeTime = clip.length / Mathf.Max(Mathf.Abs(src.pitch), 0.01f);
        Destroy(src.gameObject, lifeTime);

        return src;
    }

    public static void PlayMusic(AudioClip musicClip)
    {
        if (musicSource == null)
        {
            GameObject goAudioSrc = new GameObject("tmpAudioSrcMusic");
            AudioSource audioSrc = goAudioSrc.AddComponent<AudioSource>();
            musicSource = audioSrc;
        }
        musicSource.volume = stdVolume;
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.Play();
        DontDestroyOnLoad(musicSource.gameObject);
    }

    public static AudioSource GetAudioSrcMusic()
    {
        return musicSource;
    }
}
