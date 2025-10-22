using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIIngameHud : MonoBehaviour
{
    public static UIIngameHud Instance;

    [Header("UI IngameMenu")]
    public GameObject ingameHud;
    public TextMeshProUGUI txtScoreIngame;
    public Button btnPause;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        btnPause.onClick.RemoveAllListeners();

        btnPause.onClick.AddListener(delegate
        {
            AudioController.PlaySound(AudioController.Instance.soundClick);
            UIPauseMenu.Instance.ShowHideMenu(!GameController.GetIsPause());
        });

        btnPause.interactable = true;

        txtScoreIngame.text = "0";

        ShowHideMenu(false);
    }

    public void ShowHideMenu(bool show)
    {
        ingameHud.SetActive(show);
    }

}
