using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIIngameHud : MonoBehaviour
{
    public static UIIngameHud Instance { get; private set; }
    public GameObject ingameHud;
    public TextMeshProUGUI txtScoreIngame;
    public Button btnPause;
    private readonly List<Image> lives = new();

    private void Awake() => Instance = this;

    public void Init()
    {
        UIController.Bind(btnPause, () => GameController.Instance.Pause());
        ScoreController.Instance.Changed -= ShowScore;
        ScoreController.Instance.Changed += ShowScore;
        if (lives.Count == 0)
            foreach (Transform child in GameController.Instance.imgLiveParent)
                if (child.TryGetComponent<Image>(out var image)) lives.Add(image);
        ShowScore(0);
    }

    private void ShowScore(long score) => txtScoreIngame.text = Utilities.NumberToString(score);

    public void SetLives(int count)
    {
        var game = GameController.Instance;
        while (lives.Count < count) lives.Add(Instantiate(game.imgLive, game.imgLiveParent));
        for (int i = 0; i < lives.Count; i++) lives[i].gameObject.SetActive(i < count);
    }

    private void OnDestroy()
    {
        if (ScoreController.Instance) ScoreController.Instance.Changed -= ShowScore;
        if (Instance == this) Instance = null;
    }
}
