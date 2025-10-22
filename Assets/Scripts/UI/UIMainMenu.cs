using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : MonoBehaviour
{
    public static UIMainMenu Instance;

    [Header("UI Main menu")]
    public GameObject mainMenuPanel;
    public TextMeshProUGUI txtPremiumCoins;
    public TextMeshProUGUI txtBestScore;
    public Button btnPlay;
    public Button btnShop;
    public Button btnExit;
    public Button btnAchivement;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        btnPlay.onClick.RemoveAllListeners();
        btnShop.onClick.RemoveAllListeners();
        btnExit.onClick.RemoveAllListeners();
        btnAchivement.onClick.RemoveAllListeners();

        //main menu buttons
        btnPlay.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            GameController.Instance.StartNewGame();
        });

        btnShop.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            UIController.Instance.ShowHideMenu(UIController.eMenuType.Shop, true);
        });

        btnExit.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            ExitGame();
        });

        btnAchivement.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            UIToastMessage.Instance.ShowToast("Coming soon...", 3f); //TODO: implement achivement system
        });

        btnPlay.interactable = true;
        btnShop.interactable = true;
        btnExit.interactable = true;
        btnAchivement.interactable = true;

        ShowHideMenu(false);
    }


    public void ShowHideMenu(bool show)
    {
        if (show)
        {
            AudioController.PlayMusic(AudioController.Instance.mainMenuMusic);

            mainMenuPanel.SetActive(true);

            if (SaveGameController.savegame.bestScore <= 0)
                txtBestScore.transform.parent.gameObject.SetActive(false);
            else
                txtBestScore.transform.parent.gameObject.SetActive(true);

            txtPremiumCoins.text = Utilities.numberToString(SaveGameController.savegame.premiumCoins);
            txtBestScore.text = Utilities.numberToString(SaveGameController.savegame.bestScore);

            GameController.SetPauseOnlyValue(false);
            GameController.SetGameOverOnlyValue(false);
        }
        else
        {
            mainMenuPanel.SetActive(false);
        }
    }

    public void ShowHideMenu()
    {
        UIController.Instance.ShowHideMenu(UIController.eMenuType.MainMenu, !mainMenuPanel.activeSelf);
    }

    public void ExitGame()
    {
        //scoreScrp.save();
        //premiumCoinsScrp.save();

        //TODO: playCloudManagerScrp.SaveToCloud();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
    }

}
