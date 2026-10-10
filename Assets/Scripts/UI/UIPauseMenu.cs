using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPauseMenu : MonoBehaviour
{
    public static UIPauseMenu Instance { get; private set; }
    public GameObject pauseMenuPanel;
    public Button btnContinue;
    public Button btnReplay;
    public Button btnBackToMainMenu;
    public TextMeshProUGUI txtStopGame;
    public TextMeshProUGUI txtScoreGameOver;
    public TextMeshProUGUI txtPremiumCoinsGameOver;
    public TextMeshProUGUI txtAvailableCoins;
    public TextMeshProUGUI txtBestScore;
    public Image imgNewBestSymbol;

    private Coroutine scoreAnimation;

    private void Awake() => Instance = this;

    public void Init()
    {
        if (!txtAvailableCoins)
        {
            var coinDisplay = pauseMenuPanel.transform.Find("PremiumCoinsPosPauseMenu");
            if (coinDisplay) txtAvailableCoins = coinDisplay.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        UIController.Bind(btnContinue, () => GameController.Instance.Resume());
        UIController.Bind(btnReplay, () => GameController.Instance.StartNewGame());
        UIController.Bind(btnBackToMainMenu, () => GameController.Instance.ReturnToMainMenu());
    }

    public void Hide()
    {
        if (scoreAnimation != null) StopCoroutine(scoreAnimation);
        scoreAnimation = null;
        pauseMenuPanel.SetActive(false);
    }

    private void Prepare(bool gameOver)
    {
        pauseMenuPanel.SetActive(true);
        txtStopGame.text = gameOver ? "GAME OVER" : "Pause";
        btnContinue.interactable = !gameOver;
        btnReplay.interactable = true;
        btnBackToMainMenu.interactable = true;
        var image = btnContinue.transform.GetChild(0).GetComponent<Image>();
        var color = image.color;
        color.a = gameOver ? 0.1f : 1f;
        image.color = color;
        txtScoreGameOver.transform.parent.gameObject.SetActive(gameOver);
        txtPremiumCoinsGameOver.transform.parent.gameObject.SetActive(gameOver);
        txtScoreGameOver.text = "0";
        txtPremiumCoinsGameOver.text = "0";
        txtBestScore.text = Utilities.NumberToString(SaveGameController.Data.BestScore);
        imgNewBestSymbol.transform.parent.gameObject.SetActive(gameOver && GameController.Instance.LastRoundWasBest);
        imgNewBestSymbol.gameObject.SetActive(gameOver && GameController.Instance.LastRoundWasBest);
    }

    public void ShowPause() => Prepare(false);

    public void ShowGameOver()
    {
        Prepare(true);
        scoreAnimation = StartCoroutine(AnimateScore());
    }

    private IEnumerator AnimateScore()
    {
        var game = GameController.Instance;
        yield return new WaitForSecondsRealtime(0.2f);
        if (game.LastRoundWasBest) AudioController.PlaySound(AudioController.Instance.soundNewBestScore, pitch: 1.2f);
        yield return Utilities.CountAnimationRoutine(txtScoreGameOver, 0, game.LastRoundScore,
            UIController.Instance.animiationCountDelay, 2f, AudioController.Instance.soundCoinCount);
        yield return Utilities.CountAnimationRoutine(txtPremiumCoinsGameOver, 0, game.LastRoundCoins,
            UIController.Instance.animiationCountDelay, 2f, AudioController.Instance.soundCoinCount);
        scoreAnimation = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
