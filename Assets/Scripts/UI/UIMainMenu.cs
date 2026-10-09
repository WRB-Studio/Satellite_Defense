using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : MonoBehaviour
{
    public const string PrivacyPolicyUrl = "https://wrb-studio.github.io/Satellite_Defense/privacy-policy.html";
    public static UIMainMenu Instance { get; private set; }
    public GameObject mainMenuPanel;
    public TextMeshProUGUI txtPremiumCoins;
    public TextMeshProUGUI txtBestScore;
    public Button btnPlay;
    public Button btnShop;
    public Button btnExit;
    public Button btnPrivacyPolicy;

    private void Awake() => Instance = this;

    public void Init()
    {
        UIController.Bind(btnPlay, () => GameController.Instance.StartNewGame());
        UIController.Bind(btnShop, () => GameController.Instance.OpenShop());
        UIController.Bind(btnExit, ExitGame);
        UIController.Bind(btnPrivacyPolicy, () => Application.OpenURL(PrivacyPolicyUrl));
    }

    public void Show(bool visible)
    {
        mainMenuPanel.SetActive(visible);
        if (!visible) return;
        btnPlay.interactable = SaveGameController.CanSave;
        txtBestScore.transform.parent.gameObject.SetActive(SaveGameController.Data.BestScore > 0);
        txtBestScore.text = Utilities.NumberToString(SaveGameController.Data.BestScore);
        txtPremiumCoins.text = Utilities.NumberToString(SaveGameController.Data.Coins);
    }

    public void ExitGame()
    {
        if (!SaveGameController.Save() && SaveGameController.HasPendingChanges)
        {
            UIController.Instance.ShowSaveMessage("Progress is still waiting to be saved. Check free space and try again.");
            return;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
