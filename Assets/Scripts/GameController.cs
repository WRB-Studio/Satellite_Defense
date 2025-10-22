using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UIShopMenu;

public class GameController : MonoBehaviour, ISaveable
{
    public static GameController Instance;

    [Header("Live settings")]
    public Image imgLive;
    public int startLifes = 3;
    public int currentLifes = 0;
    public int maxLives = 5;
    public Transform imgLiveParent;
    private List<Image> instantiatedImgLives = new List<Image>();

    [Header("Game stops")]
    private static bool isPause = false;
    private static bool isGameOver = false;
    [HideInInspector] public static bool gameIsInitialized = false;

    [Header("Parents")]
    public Transform planetParent;
    public Transform weaponParent;
    public Transform backgroundParent;
    public Transform enemyParent;

    [Header("Background settings")]
    public GameObject star;

    [Header("Joystick")]
    public bool enableJoystickControll = false;
    public GameObject joystickGO;
    public Joystick joystick;

    [HideInInspector] public GameObject instantiatedPlanet;
    [HideInInspector] public GameObject instantiatedWeapon;
    [HideInInspector] public GameObject instantiatedBackground;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        if (enableJoystickControll)
            joystickGO.SetActive(true);
        else
            joystickGO.SetActive(false);

        Load();

        InitAllScripts();

        UIController.Instance.FadeOutSplashScreen();

        gameIsInitialized = true;
    }

    public void InitAllScripts()
    {
        UIController.Instance.Init();

        ScoreController.Instance.Init();
        PremiumCoinController.Instance.Init();
        EnemyController.Instance.Init();

        InstantiateActiveItem(IngameEntity.eEntityType.Planet);
        InstantiateActiveItem(IngameEntity.eEntityType.Weapon);
        InstantiateActiveItem(IngameEntity.eEntityType.Background);
        InstantiateActiveItem(IngameEntity.eEntityType.Enemy);
    }

    public static void SetPause(bool pause)
    {
        isPause = pause;

        Utilities.SetAllParticlesPaused(isPause, new List<GameObject>() { GameObject.Find("PlanetExplosion") });
    }

    public static void SetPauseOnlyValue(bool pause)
    {
        isPause = pause;
    }

    public static bool GetIsPause()
    {
        return isPause;
    }


    public void StartNewGame()
    {
        AudioController.PlayMusic(AudioController.Instance.ingameMusic);

        UIController.Instance.Init();
        ScoreController.Instance.Init();
        PremiumCoinController.Instance.Init();
        EnemyController.Instance.Init();

        InstantiateActiveItem(IngameEntity.eEntityType.Planet);
        InstantiateActiveItem(IngameEntity.eEntityType.Weapon);
        InstantiateActiveItem(IngameEntity.eEntityType.Background);
        InstantiateActiveItem(IngameEntity.eEntityType.Enemy);

        ResetLifes();

        EnemyController.Instance.RemoveAllEnemies();
        PowerUpController.Instance.RemoveAllItems();

        isGameOver = false;
        isPause = false;

        StopAllCoroutines();
        StartCoroutine(RandomStarBlink());

        UIController.Instance.ShowHideMenu(UIController.eMenuType.IngameMenu, true);
    }

    public void ResetLifes()
    {
        //remove all lifes
        for (int i = 0; i < imgLiveParent.childCount; i++)
            Destroy(imgLiveParent.GetChild(i).gameObject);
        instantiatedImgLives = new List<Image>();
        currentLifes = 0;

        //add start lifes
        int startUpgradeLives = Utilities.Round(GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.PlanetStartHP));
        ChangeLife(startUpgradeLives, false);
    }


    public static void SetGameOver()
    {
        UIController.Instance.ShowHideMenu(UIController.eMenuType.GameOverMenu, true);
    }

    public static void SetGameOverOnlyValue(bool gameOver)
    {
        isGameOver = gameOver;
    }

    public static bool GetIsGameOver()
    {
        return isGameOver;
    }


    /*---------------Upgrade Getter methods-----------------*/
    public float GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType upgradeType)
    {
        float SafeGetEffect(GameObject obj)
        {
            if (obj == null) return 0f;

            var entity = obj.GetComponent<IngameEntity>();
            if (entity == null) return 0f;

            var upgrade = entity.GetAttributeByType(upgradeType);
            if (upgrade == null) return 0f;

            return upgrade.GetAttributeEffect(entity.entityLevel);
        }

        float result = 0;
        result += SafeGetEffect(instantiatedPlanet);
        result += SafeGetEffect(instantiatedWeapon);
        result += SafeGetEffect(instantiatedBackground);
        result += SafeGetEffect(UIShopMenu.Instance.activeEnemyTypeItem);

        return result;
    }

    public bool UpgradeActive(EntityAttribute.eAttributeType upgradeType)
    {
        bool SafeIsUpgraded(GameObject obj)
        {
            if (obj == null) return false;

            var entity = obj.GetComponent<IngameEntity>();
            if (entity == null) return false;

            var upgrade = entity.GetAttributeByType(upgradeType);
            if (upgrade == null) return false;

            return entity.entityLevel > 1;
        }

        return SafeIsUpgraded(instantiatedPlanet) 
            || SafeIsUpgraded(instantiatedWeapon) 
            || SafeIsUpgraded(instantiatedBackground) 
            || SafeIsUpgraded(UIShopMenu.Instance.activeEnemyTypeItem);
    }



    /*---------------Life methods-----------------*/
    //public void ChangeLife(bool add, bool PlaySound = true)
    //{
    //    if (add && (instantiatedImgLives.Count < maxLives))
    //    {
    //        if (PlaySound)
    //            AudioController.PlaySound(AudioController.Instance.soundAddLive);

    //        instantiatedImgLives.Add(Instantiate(imgLive,
    //            new Vector2(imgLive.transform.position.x + (imgLive.GetComponent<RectTransform>().rect.width * instantiatedImgLives.Count), imgLive.transform.position.y),
    //            imgLive.transform.rotation, imgLiveParent));

    //        currentLifes++;
    //    }
    //    else if (!add)
    //    {
    //        Image lastLife = instantiatedImgLives[instantiatedImgLives.Count - 1];
    //        instantiatedImgLives.Remove(lastLife);
    //        Destroy(lastLife.gameObject);

    //        currentLifes--;
    //    }
    //}

    public void ChangeLife(int amount, bool playSound = true)
    {
        if (amount > 0)
        {
            for (int i = 0; i < amount; i++)
            {
                if (instantiatedImgLives.Count >= maxLives)
                    break;

                if (playSound)
                    AudioController.PlaySound(AudioController.Instance.soundAddLive);

                instantiatedImgLives.Add(Instantiate(
                    imgLive,
                    new Vector2(
                        imgLive.transform.position.x + (imgLive.GetComponent<RectTransform>().rect.width * instantiatedImgLives.Count),
                        imgLive.transform.position.y),
                    imgLive.transform.rotation,
                    imgLiveParent));

                currentLifes++;
            }
        }
        else if (amount < 0)
        {
            for (int i = 0; i < -amount; i++)
            {
                if (instantiatedImgLives.Count <= 0)
                    break;

                Image lastLife = instantiatedImgLives[instantiatedImgLives.Count - 1];
                instantiatedImgLives.Remove(lastLife);
                Destroy(lastLife.gameObject);

                currentLifes--;
            }
        }
    }


    public int GetMaxLives()
    {
        int maxUpgradeLives = (int)GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.PlanetMaxHP);
        return maxUpgradeLives > 0 ? maxLives : maxUpgradeLives;
    }

    public int GetCurLives()
    {
        return currentLifes;
    }

    public void ResetWeaponEmitterKillCounter()
    {
        EnemyController.Instance.killCounterForWeaponEmitterActivation = 0;
        instantiatedWeapon.GetComponent<Weapon>().ChangeWeaponLevel(0);
    }


    /*---------------Load active items (planet, weapon, background, enemy)-----------------*/

    public void InstantiateActiveItem(IngameEntity.eEntityType category)
    {
        switch (category)
        {
            case IngameEntity.eEntityType.Planet:
                if (instantiatedPlanet != null) Destroy(instantiatedPlanet);
                instantiatedPlanet = Instantiate(UIShopMenu.Instance.activePlanetItem, planetParent);
                instantiatedPlanet.GetComponent<Planet>().Init();
                break;

            case IngameEntity.eEntityType.Weapon:
                if (instantiatedWeapon != null) Destroy(instantiatedWeapon);
                instantiatedWeapon = Instantiate(UIShopMenu.Instance.activeWeaponItem, weaponParent);
                instantiatedWeapon.GetComponent<Weapon>().Init();
                break;

            case IngameEntity.eEntityType.Background:
                if (instantiatedBackground != null) Destroy(instantiatedBackground);
                instantiatedBackground = Instantiate(UIShopMenu.Instance.activeBackgroundItem, backgroundParent);
                break;

            case IngameEntity.eEntityType.Enemy:
                EnemyController.Instance.LoadActiveEnemyType();
                EnemyController.Instance.Init();
                break;

            default:
                break;
        }
    }


    /*---------------Starblink animation-----------------*/
    private IEnumerator RandomStarBlink()
    {
        while (true)
        {
            for (int starSpawnCount = 0; starSpawnCount < Random.Range(1, 20); starSpawnCount++)
            {
                GameObject starInstanz = Instantiate(star, new Vector2(Random.Range(-8, 8), Random.Range(-8, 8)), star.transform.rotation);
                starInstanz.transform.parent = instantiatedBackground.transform;
                Destroy(starInstanz, 1);

                yield return new WaitForSeconds(Random.Range(0.01f, 0.06f));
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }


    /*---------------Save & Load-----------------*/

    public void Load()
    {
        SaveGameController.Load();
    }

    public void Save()
    {

    }

}
