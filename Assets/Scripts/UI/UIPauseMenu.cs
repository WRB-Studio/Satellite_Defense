using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIShopMenu;

public class UIPauseMenu : MonoBehaviour
{
    public static UIPauseMenu Instance;

    [Header("UI Pause")]
    public GameObject pauseMenuPanel;
    public Button btnContinue;
    public Button btnReplay;
    public Button btnBackToMainMenu;
    public TextMeshProUGUI txtStopGame;

    [Header("UI Score")]
    public TextMeshProUGUI txtScoreGameOver;
    public TextMeshProUGUI txtPremiumCoinsGameOver;
    public TextMeshProUGUI txtBestScore;
    public Image imgNewBestSymbol;

    private bool inScoreCalculation = false;

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        btnContinue.onClick.RemoveAllListeners();
        btnReplay.onClick.RemoveAllListeners();
        btnBackToMainMenu.onClick.RemoveAllListeners();

        //pause menu buttons
        btnContinue.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            UIController.Instance.ShowHideMenu(UIController.eMenuType.PauseMenu, false);
        });

        btnReplay.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            GameController.Instance.StartNewGame();
        });

        btnBackToMainMenu.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            BackToMainMenu();
        });

        btnContinue.interactable = true;
        btnReplay.interactable = true;
        btnBackToMainMenu.interactable = true;

        var img = btnContinue.transform.GetChild(0).GetComponent<Image>();
        var c = img.color;
        c.a = 1f;
        img.color = c;

        txtScoreGameOver.text = "0";
        txtPremiumCoinsGameOver.text = "0";
        txtBestScore.text = "0";

        imgNewBestSymbol.transform.parent.gameObject.SetActive(false);
        txtScoreGameOver.transform.parent.gameObject.SetActive(false);
        txtPremiumCoinsGameOver.transform.parent.gameObject.SetActive(false);

        ShowHideMenu(false);
    }


    public void ShowHideMenu(bool show)
    {
        pauseMenuPanel.SetActive(show);

        if (show)
        {
            GameController.SetPause(true);

            txtScoreGameOver.transform.parent.gameObject.SetActive(false);
            txtPremiumCoinsGameOver.transform.parent.gameObject.SetActive(false);

            txtBestScore.text = SaveGameController.savegame.bestScore.ToString();
            imgNewBestSymbol.gameObject.SetActive(false);
        }
        else
        {
            GameController.SetPause(false);
        }
    }


    public void BackToMainMenu()
    {
        PowerUpController.Instance.RemoveAllItems();

        EnemyController.Instance.RemoveAllEnemies();

        txtStopGame.text = "Pause";

        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Planet);
        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Weapon);
        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Background);
        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Enemy);

        UIController.Instance.ShowHideMenu(UIController.eMenuType.MainMenu, true);
    }

    public void ShowGameOver()
    {
        StartCoroutine(ShowGameOverScreen());
    }

    private IEnumerator ShowGameOverScreen()
    {
        SetInScoreCalculation(true);

        var img = btnContinue.transform.GetChild(0).GetComponent<Image>();
        var c = img.color;
        c.a = 0.1f;
        img.color = c;

        GameController.SetPauseOnlyValue(true);

        PowerUpController.Instance.RemoveAllItems();
        EnemyController.Instance.RemoveAllEnemies();

        GameController.Instance.instantiatedWeapon.GetComponent<Weapon>().DestroyWeapon();

        yield return new WaitForSeconds(1.25f);

        GameController.SetGameOverOnlyValue(true);
        ShowHideMenu(true);
        txtStopGame.text = "GAME OVER";
        txtScoreGameOver.transform.parent.gameObject.SetActive(true);
        txtPremiumCoinsGameOver.transform.parent.gameObject.SetActive(true);

        StartCoroutine(ScoreCountAnimationRoutine());
    }

    private IEnumerator ScoreCountAnimationRoutine()
    {
        float countDelay = UIController.Instance.animiationCountDelay;

        yield return new WaitForSeconds(0.2f);

        long score = ScoreController.Instance.score;
        long bestScore = ScoreController.Instance.bestScore;

        if (score > 0)
        {
            StartCoroutine(Utilities.CountAnimationRoutine(UIIngameHud.Instance.txtScoreIngame, score, 0, countDelay, 2, null));
            yield return StartCoroutine(Utilities.CountAnimationRoutine(txtScoreGameOver, 0, score, countDelay, 2, AudioController.Instance.soundCoinCount));

            ScoreController.Instance.checkScoreAchivement();

            if (score > bestScore)
            {
                yield return new WaitForSeconds(0.2f);
                AudioController.PlaySound(clip: AudioController.Instance.soundNewBestScore, pitch: 1.2f);
                bestScore = score;
                txtBestScore.text = Utilities.numberToString(bestScore);
                imgNewBestSymbol.gameObject.SetActive(true);

                SaveGameController.SaveBestScore(bestScore);
            }
            else
            {
                txtBestScore.text = Utilities.numberToString(bestScore);
            }
        }

        StartCoroutine(PremiumCoinsCounterRoutine());
    }

    private IEnumerator PremiumCoinsCounterRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        if (ScoreController.Instance.score > 0)
        {
            long scoreToPremiumCoinCalculation = Utilities.Round(ScoreController.Instance.score / PremiumCoinController.Instance.premiumCoinsPerScore);

            if (scoreToPremiumCoinCalculation > 0)
            {
                yield return StartCoroutine(Utilities.CountAnimationRoutine(
                    txtPremiumCoinsGameOver,
                    0,
                    scoreToPremiumCoinCalculation,
                    UIController.Instance.animiationCountDelay, 2, AudioController.Instance.soundCoinCount));

                yield return new WaitForSeconds(0.2f);
                PremiumCoinController.Instance.AddPremiumCoins(scoreToPremiumCoinCalculation);
            }
        }

        SetInScoreCalculation(false);
    }

    public void SetInScoreCalculation(bool newValue)
    {
        inScoreCalculation = newValue;

        if (inScoreCalculation)
        {
            UIIngameHud.Instance.btnPause.interactable = false;
            btnContinue.interactable = false;
            btnReplay.interactable = false;
            btnBackToMainMenu.interactable = false;
        }
        else
        {
            UIIngameHud.Instance.btnPause.interactable = false;
            btnContinue.interactable = false;
            btnReplay.interactable = true;
            btnBackToMainMenu.interactable = true;
        }
    }


}
