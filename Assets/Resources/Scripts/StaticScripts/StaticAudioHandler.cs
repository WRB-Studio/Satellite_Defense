using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticAudioHandler : MonoBehaviour
{

    public static float stdVolume = 0.6f;
    private static List<AudioSource> audioSrcSoundList = new List<AudioSource>();
    private static AudioSource audioSrcMusic;

    public static AudioSource playSound(AudioClip sound, bool randomPitch = false)
    {
        GameObject goAudioSrc = new GameObject("tmpAudioSrc");
        AudioSource audioSrc = goAudioSrc.AddComponent<AudioSource>();
        audioSrcSoundList.Add(audioSrc);
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        audioSrc.volume = stdVolume;
        audioSrc.clip = sound;
        if (randomPitch)
            audioSrc.pitch = Random.Range(0.8f, 0.9f);
        audioSrc.Play();
        DontDestroyOnLoad(goAudioSrc);
        Destroy(goAudioSrc, audioSrc.clip.length);
        return audioSrc;

    }

    public static AudioSource playSound(AudioClip sound)
    {
        GameObject goAudioSrc = new GameObject("tmpAudioSrc");
        AudioSource audioSrc = goAudioSrc.AddComponent<AudioSource>();
        audioSrcSoundList.Add(audioSrc);
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        audioSrc.clip = sound;
        audioSrc.Play();
        DontDestroyOnLoad(goAudioSrc);
        Destroy(goAudioSrc, audioSrc.clip.length);
        return audioSrc;

    }

    public static AudioSource playSound(AudioClip sound, float newPitch = 1)
    {
        GameObject goAudioSrc = new GameObject("tmpAudioSrc");
        AudioSource audioSrc = goAudioSrc.AddComponent<AudioSource>();
        audioSrcSoundList.Add(audioSrc);
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
        audioSrc.volume = stdVolume;
        audioSrc.clip = sound;
        audioSrc.pitch = newPitch;
        audioSrc.Play();
        DontDestroyOnLoad(goAudioSrc);
        Destroy(goAudioSrc, audioSrc.clip.length);
        return audioSrc;

    }


    public static AudioSource playMusic(AudioClip musicClip)
    {
        if (audioSrcMusic != null)
            Destroy(audioSrcMusic);
        GameObject goAudioSrc = new GameObject("tmpAudioSrcMusic");
        AudioSource audioSrc = goAudioSrc.AddComponent<AudioSource>();
        audioSrcMusic = audioSrc;
        audioSrc.volume = stdVolume;
        audioSrc.clip = musicClip;
        audioSrc.loop = true;
        audioSrc.Play();
        DontDestroyOnLoad(goAudioSrc);
        return audioSrc;
    }

    public static AudioSource getAudioSrcMusic()
    {
        return audioSrcMusic;
    }
}
