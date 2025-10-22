using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UIShopMenu;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    public enum eMenuType { None, MainMenu, Shop, PauseMenu, IngameMenu, GameOverMenu };

    [Header("Splash Screen")]
    public GameObject splashScreen;
    public float splashScreenShowDuration = 2f;
    public float splashScreenFadeDuration = 2f;

    [Header("Modal Panels")]
    public GameObject modalPanelShop;

    [Header("UI Premium coins positions")]
    public Transform premiumcoinsGroup;
    public Transform premCoinsPosMainMenu;
    public Transform premCoinsPosShop;
    public Transform premCoinsPosPauseMenu;

    [Header("UI settings")]
    public float animiationCountDelay = 0.1f;



    private void Awake()
    {
        Instance = this;

        splashScreen.SetActive(true);
    }

    public void Init()
    {
        UIMainMenu.Instance.Init();
        UIShopMenu.Instance.Init();
        UIPauseMenu.Instance.Init();
        UIIngameHud.Instance.Init();

        UIToastMessage.Instance.Init();

        ShowHideMenu(eMenuType.MainMenu, true);
    }

    public void ShowHideMenu(eMenuType menuType, bool show)
    {
        UIMainMenu mainMenu = UIMainMenu.Instance;
        UIShopMenu shopMenu = UIShopMenu.Instance;
        UIPauseMenu pauseMenu = UIPauseMenu.Instance;
        UIIngameHud ingameMenu = UIIngameHud.Instance;

        mainMenu.ShowHideMenu(false);
        shopMenu.ShowHideMenu(false);
        pauseMenu.ShowHideMenu(false);
        ingameMenu.ShowHideMenu(false);
        modalPanelShop.SetActive(false);

        switch (menuType)
        {
            case eMenuType.None:
                break;

            case eMenuType.MainMenu:
                mainMenu.ShowHideMenu(show);
                break;

            case eMenuType.Shop:
                shopMenu.ShowHideMenu(show);
                mainMenu.ShowHideMenu(true);

                break;

            case eMenuType.PauseMenu:
                pauseMenu.ShowHideMenu(show);
                ingameMenu.ShowHideMenu(true);
                break;

            case eMenuType.IngameMenu:
                ingameMenu.ShowHideMenu(show);
                break;

            case eMenuType.GameOverMenu:
                if (show)
                {
                    pauseMenu.ShowGameOver();
                    ingameMenu.ShowHideMenu(true);
                }
                break;

            default:
                break;
        }

        ChangePremiumCoinsGroupParent(menuType);
    }

    private void ChangePremiumCoinsGroupParent(eMenuType menuType)
    {
        switch (menuType)
        {
            case eMenuType.MainMenu:
                premiumcoinsGroup.SetParent(premCoinsPosMainMenu, false);
                break;

            case eMenuType.Shop:
                premiumcoinsGroup.SetParent(premCoinsPosShop, false);
                break;

            case eMenuType.PauseMenu:
                premiumcoinsGroup.SetParent(premCoinsPosPauseMenu, false);
                break;

            default:
                break;
        }
    }


    /*---------------Splash Screen-----------------*/

    public void FadeOutSplashScreen()
    {
        StartCoroutine(SplashScreenRoutine());
    }

    private IEnumerator SplashScreenRoutine()
    {
        Image backgroundImage = splashScreen.GetComponent<Image>();
        Image titleImage = splashScreen.transform.GetChild(0).GetComponent<Image>();

        float t = 0f;

        // Ausgangsfarben sichern
        Color startPanelColor = backgroundImage.color;
        Color startTextColor = titleImage.color;

        yield return new WaitForSeconds(splashScreenShowDuration);

        while (t < splashScreenFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / splashScreenFadeDuration);

            backgroundImage.color = new Color(startPanelColor.r, startPanelColor.g, startPanelColor.b, alpha);
            titleImage.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, alpha);

            yield return null;
        }

        splashScreen.gameObject.SetActive(false);
    }

}
