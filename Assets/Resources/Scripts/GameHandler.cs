using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHandler : MonoBehaviour
{
    [Header("Debug settings")]
    public bool enableDebugPanel = false;
    public Text txtDebug;
    public Button btCloseDebugPanel;

    [Header("SaveAndLoad settings")]
    public bool removeSavegame = false;

    [Header("Other classes")]
    public Score scoreScrp;
    public PremiumCoins premiumCoinsScrp;
    public Shop shopScrp;
    public QuickInfo quickInfoScrp;
    public EnemySpawner enemySpawnerScrp;
    public AdMob admobScrp;
    public PlayCloudDataManager playCloudManagerScrp;

    [Header("Live settings")]
    public Image imgLive;
    public int startLifes = 3;
    public int currentLifes = 0;
    public int maxLives = 5;
    public Transform imgLiveParent;
    public AudioClip soundAddLive;
    private List<Image> instantiatedImgLives = new List<Image>();

    [Header("Game stops")]
    private bool isPaused = false;
    private bool isGameOver = false;
    private bool inMainMenue = false;

    [Header("UI Main menue")]
    public GameObject mainMenue;
    public Button btPlay;
    public Button btShop;
    public Button btExit;
    public Button btAchivement;

    [Header("UI Pause")]
    public GameObject pauseMenue;
    public Button btContinue;
    public Button btReplay;
    public Button btBackToMenue;
    public Text txtStopGame;
    private bool inScoreCalculation = false;
    public float scoreCountDelay = 0.1f;

    [Header("UI IngameMenue")]
    public GameObject ingameMenue;
    public Button btPause;

    [Header("UI Premium coins positions")]
    public Transform premiumcoinsGroup;
    public Transform premCoinsPosMainMenue;
    public Transform premCoinsPosShop;
    public Transform premCoinsPosPauseMenue;

    [Header("Audio")]
    public AudioClip mainMenueMusic;
    public AudioClip ingameMusic;
    public AudioClip soundClick;
    public AudioClip openDisplay;
    private AudioSource tmpMainMenueMusic;
    private AudioSource tmpIngameMusic;

    [Header("Planet settings")]
    public GameObject planetParent;
    public GameObject activPlanet;

    [Header("Weapon settings")]
    public GameObject weaponParent;
    public GameObject activWeapon;

    [Header("Background settings")]
    public GameObject backgroundParent;
    public GameObject activeBackground;
    public GameObject star;

    [Header("Enemy settings")]
    public GameObject activeEnemyType;
    public int kills = 0;
    public int weaponEmitterAddByKills = 20;
    private int killCounterForWeaponEmitterActivation = 0;

    [Header("Joystick")]
    public bool enableJoystickControll = false;
    public GameObject joystickGO;
    public Joystick joystick;

    private EventSystem myEventSystem;


    private void Start()
    {
        if (enableDebugPanel)
            txtDebug.transform.parent.gameObject.SetActive(true);
        else
            txtDebug.transform.parent.gameObject.SetActive(false);

        btCloseDebugPanel.onClick.AddListener(delegate
        {
            btCloseDebugPanel.transform.parent.gameObject.SetActive(false);
        });
    }

    public void init()
    {
        myEventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        if (enableJoystickControll)
            joystickGO.SetActive(true);
        else
            joystickGO.SetActive(false);

        initButtons();

        initAllScripts();

        loadActivePlanet();
        loadActiveWeapon();
        loadActiveBackground();
        loadActiveEnemyType();

        showHideMainMenue(true);

        StartCoroutine(introCoroutine());
    }

    private void initButtons()
    {
        //main menue buttons
        btPlay.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            startNewGame();
        });
        btShop.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            showHideShop();
        });
        btExit.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            exitGame();
        });

        btAchivement.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            playCloudManagerScrp.showAchivementsUI();
        });
        if (!playCloudManagerScrp.isAuthenticated)
            btAchivement.gameObject.SetActive(false);

        //ingame menue buttons
        btPause.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            showHidePauseMenue();
        });


        //pause menue buttons
        btContinue.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            showHidePauseMenue();
        });
        btReplay.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            replayGame();
        });
        btBackToMenue.onClick.AddListener(delegate
        {
            StaticAudioHandler.playSound(soundClick);
            backToMenue();
        });
    }

    private void initAllScripts()
    {
        shopScrp.init();

        loadActivePlanet();
        loadActiveWeapon();
        loadActiveBackground();
        loadActiveEnemyType();

        scoreScrp.init();
        premiumCoinsScrp.init();
        enemySpawnerScrp.init();
    }

    private IEnumerator introCoroutine()
    {
        if (GameObject.Find("IntroFadePanel") != null)
        {
            Transform introFadePanel = GameObject.Find("IntroFadePanel").transform;
            Transform mainMenueTitle = introFadePanel.transform.GetChild(0);

            while (introFadePanel.GetComponent<Image>().color.a > 0)
            {
                yield return new WaitForSeconds(0.02f);

                Color tmpColor = introFadePanel.GetComponent<Image>().color;
                tmpColor.a -= 0.025f;
                introFadePanel.GetComponent<Image>().color = tmpColor;
                mainMenueTitle.GetComponent<Image>().color = tmpColor;
            }

            introFadePanel.gameObject.SetActive(false);
        }
    }

    /*private void Update()
    {
        if (enableJoystickControll)
        {
            if (getIsPause() || getIsGameOver() || shopScrp.shopMainGO.activeSelf)
                joystickGO.SetActive(false);
            else
                joystickGO.SetActive(true);
        }

        myEventSystem.SetSelectedGameObject(null);//deselect ui-elements after clicking on it.
    }*/



    public bool getIsPause()
    {
        return isPaused;
    }

    public bool getIsGameOver()
    {
        return isGameOver;
    }

    public void setGameOver()
    {
        StartCoroutine(delayForGameOverScreen());
    }

    public bool getInMainMenue()
    {
        return inMainMenue;
    }

    private void changePremiumCoinsGroupParent(int menueType)
    {
        switch (menueType)
        {
            case 0://main menue
                premiumcoinsGroup.SetParent(premCoinsPosMainMenue, false);
                break;
            case 1://shop menue
                premiumcoinsGroup.SetParent(premCoinsPosShop, false);
                break;
            case 2://pause menue
                premiumcoinsGroup.SetParent(premCoinsPosPauseMenue, false);
                break;
            default:
                break;
        }
    }


    /*---------------Pause menue-----------------*/
    private IEnumerator delayForGameOverScreen()
    {
        setInScoreCalculation(true);

        isPaused = true;

        if (GameObject.FindGameObjectsWithTag("Item") != null)
            foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item"))
                Destroy(item.gameObject);

        activWeapon.GetComponent<Weapon>().destroy();

        yield return new WaitForSeconds(0.5f);

        foreach (GameObject enemy in enemySpawnerScrp.getInstantiatedEnemys().ToArray())
        {
            yield return null;
            if (enemy != null)
                enemy.GetComponent<Enemy>().hit(0, Enemy.HitType.none);
        }

        yield return new WaitForSeconds(1.5f);

        isGameOver = true;
        showHidePauseMenue();
        txtStopGame.text = "GAME OVER";

        scoreScrp.gameOverScoreCount();
    }

    public void setInScoreCalculation(bool newValue)
    {
        inScoreCalculation = newValue;

        if (inScoreCalculation)
        {
            btPause.interactable = false;
            btContinue.interactable = false;
            btReplay.interactable = false;
            btBackToMenue.interactable = false;
        }
        else
        {
            btPause.interactable = false;
            btContinue.interactable = false;
            btReplay.interactable = true;
            btBackToMenue.interactable = true;
        }
    }

    public void backToMenue()
    {
        SaveSystem.saveAll();

        if (GameObject.FindGameObjectsWithTag("Item") != null)
        {
            foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item"))
                Destroy(item.gameObject);
        }

        foreach (GameObject enemy in enemySpawnerScrp.getInstantiatedEnemys().ToArray())
        {
            if (enemy != null)
                enemy.GetComponent<Enemy>().hit(0, Enemy.HitType.none);
        }

        txtStopGame.text = "Pause";

        loadActivePlanet();
        loadActiveWeapon();
        loadActiveBackground();
        loadActiveEnemyType();

        if (Application.platform == RuntimePlatform.Android)
            admobScrp.showInterstitialAd(AdMob.AfterAdType.toMainMenue);
        else
            showHideMainMenue(true);

        admobScrp.init();
    }

    public void replayGame()
    {
        if (Application.platform == RuntimePlatform.Android)
            admobScrp.showInterstitialAd(AdMob.AfterAdType.replay);
        else
            startNewGame();
    }

    public void showHidePauseMenue()
    {
        if (pauseMenue.activeSelf)
        {
            pauseMenue.SetActive(false);
            isPaused = false;
            admobScrp.showHideBanner(false);
        }
        else
        {
            pauseMenue.SetActive(true);
            isPaused = true;
            changePremiumCoinsGroupParent(2);
            admobScrp.showHideBanner(true);
        }
    }



    /*---------------Main menue-----------------*/
    public void startNewGame()
    {
        premiumCoinsScrp.premiumCoinDoubleEarned = false;

        showHideMainMenue(false);

        admobScrp.showHideBanner(false);

        initAllScripts();

        if (tmpMainMenueMusic != null)
            Destroy(tmpMainMenueMusic.gameObject);
        tmpIngameMusic = StaticAudioHandler.playMusic(ingameMusic);

        //default interactibilitie
        btPause.interactable = true;
        btContinue.interactable = true;
        btReplay.interactable = true;
        btExit.interactable = true;
        btShop.interactable = true;

        //remove all lifes
        for (int i = 0; i < imgLiveParent.childCount; i++)
        {
            Destroy(imgLiveParent.GetChild(i).gameObject);
        }
        instantiatedImgLives = new List<Image>();
        currentLifes = 0;

        //add start lifes
        for (int i = 0; i < startLifes; i++)
        {
            changeLife(true, false);
        }

        //remove all enemys
        enemySpawnerScrp.removeAllEnemys();

        //remove all items
        if (GameObject.FindGameObjectsWithTag("Item") != null)
            foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item"))
                Destroy(item.gameObject);

        isGameOver = false;
        isPaused = false;

        pauseMenue.SetActive(false);
        shopScrp.showHideShop(false);
        ingameMenue.SetActive(true);

        loadActivePlanet();
        loadActiveWeapon();
        loadActiveBackground();
        loadActiveEnemyType();

        StopAllCoroutines();
        StartCoroutine(randomStarBlink());
    }

    public void showHideMainMenue(bool show)
    {
        if (show)
        {
            if (tmpIngameMusic != null)
                Destroy(tmpIngameMusic.gameObject);
            tmpMainMenueMusic = StaticAudioHandler.playMusic(mainMenueMusic);

            admobScrp.showHideBanner(true);

            ingameMenue.SetActive(false);
            pauseMenue.SetActive(false);
            mainMenue.SetActive(true);

            isPaused = false;
            isGameOver = false;
            inMainMenue = true;

            changePremiumCoinsGroupParent(0);
        }
        else
        {
            mainMenue.SetActive(false);
            inMainMenue = false;
        }
    }

    public void showHideShop()
    {
        shopScrp.showHideShop();
        if (shopScrp.shopMainGO.activeSelf)
            changePremiumCoinsGroupParent(1);
        else
            changePremiumCoinsGroupParent(0);
    }

    public void exitGame()
    {
        //scoreScrp.save();
        //premiumCoinsScrp.save();

        playCloudManagerScrp.SaveToCloud();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
    }



    /*---------------Life methods-----------------*/
    public void changeLife(bool add, bool playSound = true)
    {
        if (add && (instantiatedImgLives.Count < maxLives))
        {
            if (playSound)
                StaticAudioHandler.playSound(soundAddLive);

            instantiatedImgLives.Add(Instantiate(imgLive,
                new Vector2(imgLive.transform.position.x + (imgLive.GetComponent<RectTransform>().rect.width * instantiatedImgLives.Count), imgLive.transform.position.y),
                imgLive.transform.rotation, imgLiveParent));
            currentLifes++;
        }
        else if (!add)
        {
            Image lastLife = instantiatedImgLives[instantiatedImgLives.Count - 1];
            instantiatedImgLives.Remove(lastLife);
            Destroy(lastLife.gameObject);
            currentLifes--;
        }
    }

    public int getCurLives()
    {
        return currentLifes;
    }

    public void addKill()
    {
        kills++;
        killCounterForWeaponEmitterActivation++;
        if (killCounterForWeaponEmitterActivation >= weaponEmitterAddByKills)
        {
            killCounterForWeaponEmitterActivation = 0;
            if (activWeapon.GetComponent<Weapon>().activeEmitters < activWeapon.GetComponent<Weapon>().laserEmitter.Length)
                activWeapon.GetComponent<Weapon>().activeEmitters++;
        }
    }

    public void resetWeaponEmitterKillCounter()
    {
        killCounterForWeaponEmitterActivation = 0;
        if (activWeapon.GetComponent<Weapon>().activeEmitters > 1)
            activWeapon.GetComponent<Weapon>().activeEmitters--;
    }


    /*---------------planet, weapon, background and enemyType setter-----------------*/
    public void loadActivePlanet()
    {
        if (activPlanet != null)
            Destroy(activPlanet);
        activPlanet = Instantiate(shopScrp.getActivePlanet(), planetParent.transform);
    }

    public void loadActiveWeapon()
    {
        if (activWeapon != null)
            Destroy(activWeapon);
        activWeapon = Instantiate(shopScrp.getActiveWeapon(), weaponParent.transform);
    }

    public void loadActiveBackground()
    {
        if (activeBackground != null)
            Destroy(activeBackground);
        activeBackground = Instantiate(shopScrp.getActiveBackground(), backgroundParent.transform);

        Utilities.recalculateBackgroundScale(activeBackground);
    }

    public void loadActiveEnemyType()
    {
        activeEnemyType = shopScrp.getActiveEnemyType();
        enemySpawnerScrp.init();
    }



    /*---------------Starblink animation-----------------*/
    private IEnumerator randomStarBlink()
    {
        while (true)
        {
            for (int starSpawnCount = 0; starSpawnCount < Random.Range(1, 20); starSpawnCount++)
            {
                GameObject starInstanz = Instantiate(star, new Vector2(Random.Range(-8, 8), Random.Range(-8, 8)), star.transform.rotation);
                starInstanz.transform.parent = activeBackground.transform;
                Destroy(starInstanz, 1);

                yield return new WaitForSeconds(Random.Range(0.01f, 0.06f));
            }
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }


    private void OnApplicationQuit()
    {
        if (Application.isEditor)
            shopScrp.resetPrefabLists();
        //SaveSystem.saveAll();
    }



    /*---------------Debugging-----------------*/
    public void addDebugInfo(string debugInfo, bool clearAll = false)
    {
        if (enableDebugPanel)
        {
            if (clearAll)
                txtDebug.text = "";
            txtDebug.text += debugInfo + "; \n";
        }
    }

}
