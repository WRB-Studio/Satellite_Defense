using UnityEngine;
using UnityEngine.UI;

public class UISettingsMenu : MonoBehaviour
{
    public Button btnClose;
    public Toggle musicToggle, soundsToggle, joystickToggle;
    public Image musicIcon, soundsIcon, controlIcon;
    public Sprite musicOn, musicOff, soundsOn, soundsOff, joystickSymbol, directAimSymbol;

    public void Init()
    {
        UIController.Bind(btnClose, () => GameController.Instance.CloseSettings());
        musicToggle.onValueChanged.RemoveAllListeners();
        soundsToggle.onValueChanged.RemoveAllListeners();
        joystickToggle.onValueChanged.RemoveAllListeners();
        musicToggle.onValueChanged.AddListener(value => { GameSettings.SetMusic(value); Refresh(); Click(); });
        soundsToggle.onValueChanged.AddListener(value => { GameSettings.SetSounds(value); Refresh(); Click(); });
        joystickToggle.onValueChanged.AddListener(value => { GameSettings.SetJoystick(value); Refresh(); Click(); });
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible) Refresh();
    }

    private void Refresh()
    {
        musicToggle.SetIsOnWithoutNotify(GameSettings.MusicEnabled);
        soundsToggle.SetIsOnWithoutNotify(GameSettings.SoundsEnabled);
        joystickToggle.SetIsOnWithoutNotify(GameSettings.JoystickEnabled);
        musicIcon.sprite = GameSettings.MusicEnabled ? musicOn : musicOff;
        soundsIcon.sprite = GameSettings.SoundsEnabled ? soundsOn : soundsOff;
        controlIcon.sprite = GameSettings.JoystickEnabled ? joystickSymbol : directAimSymbol;
    }

    private static void Click() => AudioController.PlaySound(AudioController.Instance.soundClick);
}
