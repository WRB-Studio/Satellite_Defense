using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PremiumCoinController : MonoBehaviour
{
    public static PremiumCoinController Instance { get; private set; }
    public GameObject txtPemiumCoinEffect;
    public long premiumCoinsPerScore = 100000;
    public long Coins => SaveGameController.Data.Coins;
    private readonly List<GameObject> popups = new();

    private void Awake() => Instance = this;

    public void Init()
    {
        SaveGameController.Changed -= RefreshDisplay;
        SaveGameController.Changed += RefreshDisplay;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (UIMainMenu.Instance)
            UIMainMenu.Instance.txtPremiumCoins.text = Utilities.NumberToString(Coins);
    }

    public void CollectCoin(int baseValue, Vector3 position)
    {
        int bonus = GameController.Instance.Stats.BonusCoinValue;
        long credited = SaveGameController.CreditCoins(System.Math.Max(1L, (long)baseValue + bonus));
        if (credited == 0 || !txtPemiumCoinEffect) return;
        var effect = Instantiate(txtPemiumCoinEffect, UIIngameHud.Instance.ingameHud.transform);
        effect.transform.position = GameController.Instance.GameCamera.WorldToScreenPoint(position);
        effect.GetComponentInChildren<TextMeshProUGUI>().text = "+" + Utilities.NumberToString(credited);
        popups.RemoveAll(popup => !popup);
        popups.Add(effect);
        Destroy(effect, 3f);
    }

    public void ClearPopups()
    {
        foreach (var popup in popups)
        {
            if (!popup) continue;
            popup.SetActive(false);
            Destroy(popup);
        }
        popups.Clear();
    }

    private void OnDestroy()
    {
        SaveGameController.Changed -= RefreshDisplay;
        if (Instance == this) Instance = null;
    }
}
