using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    public Text txtScoreIngame;
    public Text txtScoreGameOver;
    public Text txtBestScoreGameOver;
    public Text txtBestScoreMainMenue;

    public long score = 0;
    private long bestScore = 0;

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

        txtScoreIngame.text = "0";
        txtScoreGameOver.text = "0";
        score = 0;

        txtScoreGameOver.gameObject.SetActive(false);
    }



    public void addScore(long addNewScore)
    {
        if (ghScrp.getInMainMenue())
            return;

        score += addNewScore;

        txtScoreIngame.text = Utilities.numberToString(score);
    }

    public long getScore()
    {
        return score;
    }

    public long getBestScore()
    {
        return bestScore;
    }

    private void checkScoreAchivement()
    {
        if (score > 15000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_15k);
        if (score > 50000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_50k);
        if (score > 100000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_100k);
        if (score > 250000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_250k);
        if (score > 500000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_500k);
        if (score > 750000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_750k);
        if (score > 1000000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_1000k);
        if (score > 2000000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_2000k);
    }


    public void gameOverScoreCount()
    {
        StartCoroutine(scoreCounter());
    }

    private IEnumerator scoreCounter()
    {
        txtScoreGameOver.gameObject.SetActive(true);
        ghScrp.premiumCoinsScrp.txtPremiumCoinsGameOver.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        if (score > 0)
        {
            long tmpScore = score;
            long tmpGameOverScoreCounter = 0;
            long counter = 1;
            while (tmpGameOverScoreCounter < score)
            {
                if (counter < 0)
                    break;

                yield return new WaitForSeconds(ghScrp.scoreCountDelay);

                tmpScore -= counter;
                txtScoreIngame.text = Utilities.numberToString(tmpScore);

                tmpGameOverScoreCounter += counter;
                txtScoreGameOver.text = "Score: " + Utilities.numberToString(tmpGameOverScoreCounter);

                StaticAudioHandler.playSound(soundCoinCount, 1.2f);
                counter *= 2;
            }

            txtScoreIngame.text = Utilities.numberToString(0);
            txtScoreGameOver.text = "Score: " + Utilities.numberToString(score);

            checkScoreAchivement();

            if (score > bestScore)
            {
                yield return new WaitForSeconds(0.2f);
                StaticAudioHandler.playSound(soundCoinCount, 1.2f);
                bestScore = score;
                txtBestScoreGameOver.text = "Best: " + Utilities.numberToString(bestScore);
            }
        }

        ghScrp.premiumCoinsScrp.gameOverPremiumCoinsCount();
    }

    
    public void playServiceSavegameLoad()
    {
        Savegame sg = ghScrp.playCloudManagerScrp.CurSavegame;

        bestScore = sg.bestScore;
        txtBestScoreGameOver.text = "Best: " + Utilities.numberToString(bestScore);
        txtBestScoreMainMenue.text = "Best score: " + Utilities.numberToString(bestScore);
    }
}
