using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PremiumCoins : MonoBehaviour
{
    public Text txtPremiumCoins;
    public Text txtPremiumCoinsGameOver;
    public GameObject txtPemiumCoinEffect;
    public Button btAdDoublePremCoins;
    public long premiumCoinsPerScore = 100000;

    public long premiumCoins = 0;
    private long curPremiumCoins = 0;
    public bool premiumCoinDoubleEarned = false;

    [Header("Sound")]
    public AudioClip soundCoinCount;

    private GameHandler ghScrp;


    private void Awake()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();
    }

    public void init()
    {
        StopAllCoroutines();

        txtPremiumCoinsGameOver.text = "0";

        txtPremiumCoinsGameOver.gameObject.SetActive(false);

        btAdDoublePremCoins.onClick.RemoveAllListeners();
        btAdDoublePremCoins.onClick.AddListener(() => showPremiumCoinDoubleAd());

        btAdDoublePremCoins.gameObject.SetActive(false);
    }



    public void addPremiumCoins(long addNewPremiumCoins)
    {
        premiumCoins += addNewPremiumCoins;
        txtPremiumCoins.text = Utilities.numberToString(premiumCoins);
    }

    public void addPremiumCoins(long addNewPremiumCoins, Vector3 position)
    {
        GameObject newEffect = Instantiate(txtPemiumCoinEffect, GameObject.Find("IngameMenue").transform);
        newEffect.transform.position = Camera.main.WorldToScreenPoint(position);
        newEffect.transform.GetChild(0).GetComponent<Text>().text = addNewPremiumCoins.ToString();
        Destroy(newEffect, 3);

        addPremiumCoins(addNewPremiumCoins);
    }

    public long getPremiumCoins()
    {
        return premiumCoins;
    }


    public void gameOverPremiumCoinsCount()
    {
        StartCoroutine(premiumCoinsCounter());
    }

    private IEnumerator premiumCoinsCounter()
    {
        yield return new WaitForSeconds(0.2f);

        if (ghScrp.scoreScrp.getScore() > 0)
        {
            long scoreToPremiumCoinCalculation = ghScrp.scoreScrp.getScore() / premiumCoinsPerScore;
            if (scoreToPremiumCoinCalculation > 0)
            {
                long counter = 1;                
                long tmpPremiumCoins = 0;
                while (scoreToPremiumCoinCalculation > 0)
                {
                    yield return new WaitForSeconds(ghScrp.scoreCountDelay);
                    tmpPremiumCoins += counter;
                    scoreToPremiumCoinCalculation -= counter;
                    txtPremiumCoinsGameOver.text = Utilities.numberToString(tmpPremiumCoins);
                    StaticAudioHandler.playSound(soundCoinCount, 1.2f);
                    counter *= 2;
                }

                curPremiumCoins = tmpPremiumCoins;

                yield return new WaitForSeconds(0.2f);
                addPremiumCoins(tmpPremiumCoins);
                
                showHideBtPremiumCoinDouble();
            }
        }

        SaveSystem.saveAll();

        ghScrp.setInScoreCalculation(false);
    }



    public void showHideBtPremiumCoinDouble()
    {
        if (!premiumCoinDoubleEarned && (ghScrp.admobScrp.getRewardAdIsLoaded() || Application.isEditor))
        {
            btAdDoublePremCoins.gameObject.SetActive(true);
        }
        else
        {
            btAdDoublePremCoins.gameObject.SetActive(false);
        }
    }

    private void showPremiumCoinDoubleAd()
    {
        if (!Application.isEditor)
            GameObject.Find("AdMob").GetComponent<AdMob>().watchDoublePremiumCoinAd();
        else
            afterAd(true);
    }

    public void afterAd(bool rewardEarned)
    {
        if (rewardEarned)
        {
            if (premiumCoinDoubleEarned)
                return;

            StartCoroutine(addDoubleAdPremiumCoins());

            premiumCoinDoubleEarned = true;
            btAdDoublePremCoins.gameObject.SetActive(false);
        }
        else
        {
            showHideBtPremiumCoinDouble();
        }
    }

    private IEnumerator addDoubleAdPremiumCoins()
    {
        ghScrp.setInScoreCalculation(true);

        yield return new WaitForSeconds(0.2f);
        StaticAudioHandler.playSound(soundCoinCount, 1.5f);
        txtPremiumCoinsGameOver.text = Utilities.numberToString(curPremiumCoins + curPremiumCoins);
        yield return new WaitForSeconds(0.2f);

        long counter = 1;        
        long tmpPremiumCoins = premiumCoins;
        long tmpNewPremCoins = curPremiumCoins;
        while (tmpNewPremCoins > 0)
        {
            if (counter < 0)
                break;

            yield return new WaitForSeconds(ghScrp.scoreCountDelay);
            tmpPremiumCoins += counter;
            tmpNewPremCoins -= counter;
            txtPremiumCoins.text = Utilities.numberToString(tmpPremiumCoins);
            StaticAudioHandler.playSound(soundCoinCount, 1.2f);
            counter *= 2;
        }

        addPremiumCoins(curPremiumCoins);

        SaveSystem.saveAll();

        ghScrp.setInScoreCalculation(false);
    }
    

    public void playServiceSavegameLoad()
    {
        Savegame sg = ghScrp.playCloudManagerScrp.CurSavegame;

        premiumCoins = sg.premiumCoins;
        txtPremiumCoins.text = Utilities.numberToString(premiumCoins);
    }
}
