using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public enum eMenuType { None, MainMenu, Shop, PauseMenu, IngameMenu, GameOverMenu }
    [Header("Splash screen")]
    public GameObject splashScreen;
    public float splashScreenShowDuration = 2f;
    public float splashScreenFadeDuration = 2f;
    public GameObject modalPanelShop;
    [Header("Coin display")]
    public Transform premiumcoinsGroup;
    public Transform premCoinsPosMainMenu;
    public Transform premCoinsPosShop;
    public Transform premCoinsPosPauseMenu;
    public float animiationCountDelay = 0.1f;

    private bool initialized;

    private void Awake()
    {
        Instance = this;
        splashScreen.SetActive(true);
    }

    public void Init()
    {
        if (initialized) return;
        initialized = true;
        UIMainMenu.Instance.Init();
        UIShopMenu.Instance.Init();
        UIPauseMenu.Instance.Init();
        UIIngameHud.Instance.Init();
        UIToastMessage.Instance.Init();
        SaveGameController.SaveFailed += ShowSaveMessage;
    }

    public void ShowSaveMessage(string message)
    {
        if (!this || string.IsNullOrEmpty(message) || splashScreen.activeSelf) return;
        UIToastMessage.Instance.ShowToast(message, 8f);
    }

    public static void Bind(Button button, UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            action();
        });
    }

    public void ShowMenu(eMenuType menu)
    {
        UIMainMenu.Instance.Show(menu == eMenuType.MainMenu || menu == eMenuType.Shop);
        UIShopMenu.Instance.Show(menu == eMenuType.Shop);
        UIPauseMenu.Instance.Hide();
        UIIngameHud.Instance.ingameHud.SetActive(menu == eMenuType.IngameMenu || menu == eMenuType.PauseMenu || menu == eMenuType.GameOverMenu);
        UIIngameHud.Instance.btnPause.interactable = menu == eMenuType.IngameMenu;
        modalPanelShop.SetActive(menu == eMenuType.Shop);
        if (menu == eMenuType.PauseMenu) UIPauseMenu.Instance.ShowPause();
        if (menu == eMenuType.GameOverMenu) UIPauseMenu.Instance.ShowGameOver();

        Transform parent = menu switch
        {
            eMenuType.Shop => premCoinsPosShop,
            eMenuType.PauseMenu or eMenuType.GameOverMenu => premCoinsPosPauseMenu,
            _ => premCoinsPosMainMenu
        };
        premiumcoinsGroup.SetParent(parent, false);
    }

    public void FadeOutSplashScreen() => StartCoroutine(FadeSplash());

    private IEnumerator FadeSplash()
    {
        var images = splashScreen.GetComponentsInChildren<Image>();
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, splashScreenShowDuration));
        float elapsed = 0f;
        while (elapsed < splashScreenFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / splashScreenFadeDuration);
            foreach (var image in images)
            {
                var color = image.color;
                color.a = alpha;
                image.color = color;
            }
            yield return null;
        }
        splashScreen.SetActive(false);
        ShowSaveMessage(SaveGameController.LastError ?? SaveGameController.LoadMessage);
    }

    private void OnDestroy()
    {
        SaveGameController.SaveFailed -= ShowSaveMessage;
        if (Instance == this) Instance = null;
    }
}
