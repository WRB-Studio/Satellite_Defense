using System.Collections;
using System.ComponentModel;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public static ScoreController Instance;

    public float multiplier = 1f;
    public float minMultiplier = 0.5f;

    public double Score;
    public long score { get => Utilities.Round(Score); }
    
    [HideInInspector] public long bestScore = 0;

    private Coroutine curScoreCountRoutine = null;



    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        StopAllCoroutines();

        Load();

        Score = 0;
    }

    private void Load()
    {
        bestScore = SaveGameController.savegame.bestScore;
    }


    public void AddScore(double addNewScore)
    {
        if (UIMainMenu.Instance.mainMenuPanel.activeSelf) return;

        long oldScore = score;
        var gc = GameController.Instance;

        float scoreMultiplier = multiplier + gc.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.ScoreMultiplier);
        if (gc.GetCurLives() == 1) scoreMultiplier += gc.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.ScoreBoostOnLowHP);
        Score += addNewScore * scoreMultiplier;

        if (curScoreCountRoutine != null) StopCoroutine(curScoreCountRoutine);
        curScoreCountRoutine = StartCoroutine(AddScoreCountRoutine(oldScore, score));
    }

    public void checkScoreAchivement()
    {
        //TODO:
        //if (score > 15000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_15k);
        //if (score > 50000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_50k);
        //if (score > 100000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_100k);
        //if (score > 250000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_250k);
        //if (score > 500000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_500k);
        //if (score > 750000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_750k);
        //if (score > 1000000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_1000k);
        //if (score > 2000000) ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_first_2000k);
    }

    private IEnumerator AddScoreCountRoutine(long oldScore, long newScore)
    {
        yield return Utilities.CountAnimationRoutine(UIIngameHud.Instance.txtScoreIngame, oldScore, newScore, 0.05f, 0.2f);
    }

}
