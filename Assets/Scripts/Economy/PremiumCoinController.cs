using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PremiumCoinController : MonoBehaviour, ISaveable
{
    public static PremiumCoinController Instance;

    public GameObject txtPemiumCoinEffect;
    public long premiumCoinsPerScore = 100000;

    public long premiumCoins = 0;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        StopAllCoroutines();

        Load();
    }

    private void OnValidate()
    {
        if (GameController.gameIsInitialized && premiumCoins > 0)
        {
            UIMainMenu.Instance.txtPremiumCoins.text = Utilities.numberToString(premiumCoins);

            SaveGameController.SavePremiumCoins(premiumCoins);
        }
    }

    public void AddPremiumCoins(long addNewPremiumCoins)
    {
        long oldPremiumCoins = premiumCoins;

        premiumCoins += addNewPremiumCoins;

        if(!UIMainMenu.Instance.mainMenuPanel.activeSelf)
            UIMainMenu.Instance.txtPremiumCoins.text = Utilities.numberToString(premiumCoins);
        else
            StartCoroutine(Utilities.CountAnimationRoutine(UIMainMenu.Instance.txtPremiumCoins, oldPremiumCoins, premiumCoins, 0.05f, 0.5f, AudioController.Instance.soundCoinCount));

        SaveGameController.SavePremiumCoins(premiumCoins);
    }

    public void AddPremiumCoins(long addNewPremiumCoins, Vector3 position)
    {
        GameObject newEffect = Instantiate(txtPemiumCoinEffect, UIIngameHud.Instance.ingameHud.transform);
        newEffect.transform.position = Camera.main.WorldToScreenPoint(position);
        newEffect.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = addNewPremiumCoins.ToString();
        Destroy(newEffect, 3);

        AddPremiumCoins(addNewPremiumCoins + Utilities.Round(GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.BonusCoinValue)));
    }


    /*---------------Save & Load-----------------*/

    public void Load()
    {
        premiumCoins = SaveGameController.savegame.premiumCoins;
        UIMainMenu.Instance.txtPremiumCoins.text = Utilities.numberToString(premiumCoins);
    }

    public void Save()
    {

    }

}
