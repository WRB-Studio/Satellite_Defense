using UnityEngine;

public static class GameSettings
{
    public static bool MusicEnabled { get { Load(); return music; } }
    public static bool SoundsEnabled { get { Load(); return sounds; } }
    public static bool JoystickEnabled { get { Load(); return joystick; } }
    private static bool loaded, music, sounds, joystick, transient;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { loaded = false; transient = false; }

    private static void Load()
    {
        if (loaded) return;
        music = PlayerPrefs.GetInt("settings.music", 1) != 0;
        sounds = PlayerPrefs.GetInt("settings.sounds", 1) != 0;
        joystick = PlayerPrefs.GetInt("settings.joystick", 1) != 0;
        loaded = true;
    }

    public static void SetMusic(bool enabled) { Load(); music = enabled; Save(); }
    public static void SetSounds(bool enabled) { Load(); sounds = enabled; Save(); }
    public static void SetJoystick(bool enabled) { Load(); joystick = enabled; Save(); }

    private static void Save()
    {
        if (!transient)
        {
            PlayerPrefs.SetInt("settings.music", music ? 1 : 0);
            PlayerPrefs.SetInt("settings.sounds", sounds ? 1 : 0);
            PlayerPrefs.SetInt("settings.joystick", joystick ? 1 : 0);
            PlayerPrefs.Save();
        }
        if (AudioController.Instance) AudioController.Instance.ApplySettings();
        if (GameController.Instance) GameController.Instance.ApplyControlSettings();
    }

#if UNITY_EDITOR
    public static void UseDefaultsForValidation()
    {
        loaded = transient = music = sounds = joystick = true;
    }
#endif
}
