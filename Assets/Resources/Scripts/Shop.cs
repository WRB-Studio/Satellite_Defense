using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    [Header("Shop content")]
    public GameObject[] planetPrefabs;
    public GameObject[] weaponPrefabs;
    public GameObject[] backgroundPrefabs;
    public GameObject[] enemyTypePrefabs;

    [Header("Color settings")]
    public Color selectedTabColor;
    private Color originTabColor;
    public Color selectedObjectColor;
    public Color unselectedObjectColor;
    public Color selectedObjectTextColor;
    public Color unSelectedObjectTextColor;
    public Color lockedTextColor;
    public Color unlockedTextColor;
    public Color cantBuyTextColor;

    [Header("Shop main elements")]
    public GameObject shopMainGO;
    public Button btClose;

    [Header("Tabs")]
    public Button btTabPlanets;
    public Button btTabWeapons;
    public Button btTabBackgrounds;
    public Button btTabEnemyTypes;

    [Header("Shop display content")]
    public Text txtObjectName;
    public Image imagePlanetAtmosphere;
    public Image imagePlanetOrWeapon;
    public Image imageBackground;
    public Image imgCostPremiumCoin;
    public Text txtObjectCost;
    public Button btSelectOrBuy;
    public Button btLeft;
    public Button btRight;

    [Header("Sounds")]
    public AudioClip soundBuy;
    public AudioClip soundSelect;

    private GameHandler ghScrp;



    private void Awake()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();

        btClose.onClick.AddListener(() => showHideShop());

        btTabPlanets.onClick.AddListener(() => onTabPressed(0));
        btTabWeapons.onClick.AddListener(() => onTabPressed(1));
        btTabBackgrounds.onClick.AddListener(() => onTabPressed(2));
        btTabEnemyTypes.onClick.AddListener(() => onTabPressed(3));
    }

    private void Start()
    {
        originTabColor = btTabPlanets.GetComponent<Image>().color;

        shopMainGO.SetActive(false);
    }


    public void init()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();

        StopAllCoroutines();

        if(Application.isEditor)
            resetPrefabLists();
    }

    public void resetPrefabLists()
    {
        resetPrefabList(planetPrefabs);
        resetPrefabList(weaponPrefabs);
        resetPrefabList(backgroundPrefabs);
        resetPrefabList(enemyTypePrefabs);
    }

    public void resetPrefabList(GameObject[] prefabList)
    {
        //reset all planets
        for (int index = 0; index < prefabList.Length; index++)
        {
            if (prefabList[index].GetComponent<Planet>() != null)
            {
                prefabList[index].GetComponent<Planet>().unlocked = false;
                prefabList[index].GetComponent<Planet>().active = false;
            }

            if (prefabList[index].GetComponent<Weapon>() != null)
            {
                prefabList[index].GetComponent<Weapon>().unlocked = false;
                prefabList[index].GetComponent<Weapon>().active = false;
            }

            if (prefabList[index].GetComponent<Background>() != null)
            {
                prefabList[index].GetComponent<Background>().unlocked = false;
                prefabList[index].GetComponent<Background>().active = false;
            }

            if (prefabList[index].GetComponent<EnemyType>() != null)
            {
                prefabList[index].GetComponent<EnemyType>().unlocked = false;
                prefabList[index].GetComponent<EnemyType>().active = false;
            }
        }


        if (prefabList[0].GetComponent<Planet>() != null)
        {
            prefabList[0].GetComponent<Planet>().unlocked = true;
            prefabList[0].GetComponent<Planet>().active = true;
        }

        if (prefabList[0].GetComponent<Weapon>() != null)
        {
            prefabList[0].GetComponent<Weapon>().unlocked = true;
            prefabList[0].GetComponent<Weapon>().active = true;
        }

        if (prefabList[0].GetComponent<Background>() != null)
        {
            prefabList[0].GetComponent<Background>().unlocked = true;
            prefabList[0].GetComponent<Background>().active = true;
        }

        if (prefabList[0].GetComponent<EnemyType>() != null)
        {
            prefabList[0].GetComponent<EnemyType>().unlocked = true;
            prefabList[0].GetComponent<EnemyType>().active = true;
        }
    }


    public void showHideShop()
    {
        if (!shopMainGO.activeSelf)
        {
            shopMainGO.SetActive(true);
            showPlanetContent(0);
        }
        else
        {
            if (shopMainGO.activeSelf)
                StaticAudioHandler.playSound(ghScrp.openDisplay).pitch = 0.9f;
            shopMainGO.SetActive(false);
            ghScrp.showHideMainMenue(true);
        }
    }

    public void showHideShop(bool show)
    {
        if (show)
        {
            shopMainGO.SetActive(show);
            showPlanetContent(0);
        }
        else
        {
            if (shopMainGO.activeSelf)
                StaticAudioHandler.playSound(ghScrp.openDisplay).pitch = 0.9f;
            shopMainGO.SetActive(show);
        }
    }


    private void onTabPressed(int tabIndex)
    {
        btTabPlanets.GetComponent<Image>().color = originTabColor;
        btTabWeapons.GetComponent<Image>().color = originTabColor;
        btTabBackgrounds.GetComponent<Image>().color = originTabColor;
        btTabEnemyTypes.GetComponent<Image>().color = originTabColor;

        switch (tabIndex)
        {
            case 0://planet content
                //selectPlanet(getActivePlanetIndex());
                //btTabPlanets.GetComponent<Image>().color = selectedTabColor;
                showPlanetContent(0);
                break;
            case 1://weapon content
                //selectWeapon(getActiveWeaponIndex());
                //btTabWeapons.GetComponent<Image>().color = selectedTabColor;
                showWeaponContent(0);
                break;
            case 2://background content
                //selectBackground(getActiveBackgroundIndex());
                //btTabBackgrounds.GetComponent<Image>().color = selectedTabColor;
                showBackgroundContent(0);
                break;
            case 3://enemyType content
                //selectEnemyType(getActiveEnemyTypeIndex());
                //btTabEnemyTypes.GetComponent<Image>().color = selectedTabColor;
                showEnemyTypeContent(0);
                break;
            default:
                break;
        }
    }


    private void showPlanetContent(int index)
    {
        btTabPlanets.GetComponent<Image>().color = selectedTabColor;
        btTabWeapons.GetComponent<Image>().color = originTabColor;
        btTabBackgrounds.GetComponent<Image>().color = originTabColor;
        btTabEnemyTypes.GetComponent<Image>().color = originTabColor;

        if (index < 0 || index > planetPrefabs.Length - 1)
            return;

        StaticAudioHandler.playSound(ghScrp.openDisplay).pitch = 1.2f;

        Planet curPlanetScrp = planetPrefabs[index].GetComponent<Planet>();

        txtObjectName.text = curPlanetScrp.planetName;

        imagePlanetAtmosphere.transform.parent.gameObject.SetActive(true);
        imagePlanetAtmosphere.gameObject.SetActive(true);
        imagePlanetOrWeapon.gameObject.SetActive(true);
        imageBackground.transform.parent.gameObject.SetActive(false);

        imagePlanetAtmosphere.sprite = curPlanetScrp.transform.Find("Atmosphere").GetComponent<SpriteRenderer>().sprite;
        imagePlanetAtmosphere.rectTransform.localScale = curPlanetScrp.transform.Find("Atmosphere").localScale;
        imagePlanetAtmosphere.GetComponent<Image>().color = curPlanetScrp.transform.Find("Atmosphere").GetComponent<SpriteRenderer>().color;

        imagePlanetOrWeapon.sprite = curPlanetScrp.GetComponent<SpriteRenderer>().sprite;
        imagePlanetOrWeapon.color = curPlanetScrp.GetComponent<SpriteRenderer>().color;
        //imagePlanetOrWeapon.rectTransform.localScale = curPlanetScrp.transform.localScale;

        if (curPlanetScrp.unlocked)
        {
            txtObjectCost.text = "unlocked";
            txtObjectCost.color = unlockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(false);

            if (curPlanetScrp.active)
            {
                changeBuySelectButton(1, true);
            }
            else
            {
                changeBuySelectButton(1, false);
                btSelectOrBuy.onClick.RemoveAllListeners();
                btSelectOrBuy.onClick.AddListener(() => selectPlanet(index));
            }
        }
        else
        {
            txtObjectCost.text = curPlanetScrp.cost.ToString();
            txtObjectCost.color = lockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(true);

            changeBuySelectButton(2, false);
            if (curPlanetScrp.cost > ghScrp.premiumCoinsScrp.getPremiumCoins())
            {
                btSelectOrBuy.interactable = false;
                btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = cantBuyTextColor;
            }

            btSelectOrBuy.onClick.RemoveAllListeners();
            btSelectOrBuy.onClick.AddListener(() => buyPlanet(index));
        }

        btLeft.onClick.RemoveAllListeners();
        btRight.onClick.RemoveAllListeners();
        btLeft.onClick.AddListener(() => showPlanetContent(index - 1));
        btRight.onClick.AddListener(() => showPlanetContent(index + 1));
    }

    private void showWeaponContent(int index)
    {
        btTabPlanets.GetComponent<Image>().color = originTabColor;
        btTabWeapons.GetComponent<Image>().color = selectedTabColor;
        btTabBackgrounds.GetComponent<Image>().color = originTabColor;
        btTabEnemyTypes.GetComponent<Image>().color = originTabColor;

        if (index < 0 || index > weaponPrefabs.Length - 1)
            return;

        StaticAudioHandler.playSound(ghScrp.openDisplay).pitch = 1.2f;

        Weapon curWeaponScrp = weaponPrefabs[index].GetComponent<Weapon>();

        txtObjectName.text = curWeaponScrp.weaponName;

        imagePlanetAtmosphere.transform.parent.gameObject.SetActive(true);
        imagePlanetAtmosphere.gameObject.SetActive(false);
        imagePlanetOrWeapon.gameObject.SetActive(true);
        imageBackground.transform.parent.gameObject.SetActive(false);

        imagePlanetOrWeapon.sprite = curWeaponScrp.transform.Find("SatelliteModel").GetComponent<SpriteRenderer>().sprite;
        imagePlanetOrWeapon.color = curWeaponScrp.transform.Find("SatelliteModel").GetComponent<SpriteRenderer>().color;

        imagePlanetOrWeapon.rectTransform.localScale = Vector2.one;

        if (curWeaponScrp.unlocked)
        {
            txtObjectCost.text = "unlocked";
            txtObjectCost.color = unlockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(false);

            if (curWeaponScrp.active)
            {
                changeBuySelectButton(1, true);
            }
            else
            {
                changeBuySelectButton(1, false);

                btSelectOrBuy.onClick.RemoveAllListeners();
                btSelectOrBuy.onClick.AddListener(() => selectWeapon(index));
            }
        }
        else
        {
            txtObjectCost.text = curWeaponScrp.cost.ToString();
            txtObjectCost.color = lockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(true);

            changeBuySelectButton(2, false);
            if (curWeaponScrp.cost > ghScrp.premiumCoinsScrp.getPremiumCoins())
            {
                btSelectOrBuy.interactable = false;
                btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = cantBuyTextColor;
            }

            btSelectOrBuy.onClick.RemoveAllListeners();
            btSelectOrBuy.onClick.AddListener(() => buyWeapon(index));
        }

        btLeft.onClick.RemoveAllListeners();
        btRight.onClick.RemoveAllListeners();
        btLeft.onClick.AddListener(() => showWeaponContent(index - 1));
        btRight.onClick.AddListener(() => showWeaponContent(index + 1));
    }

    private void showBackgroundContent(int index)
    {
        btTabPlanets.GetComponent<Image>().color = originTabColor;
        btTabWeapons.GetComponent<Image>().color = originTabColor;
        btTabBackgrounds.GetComponent<Image>().color = selectedTabColor;
        btTabEnemyTypes.GetComponent<Image>().color = originTabColor;

        if (index < 0 || index > backgroundPrefabs.Length - 1)
            return;

        StaticAudioHandler.playSound(ghScrp.openDisplay).pitch = 1.2f;

        Background curBackgroundScrp = backgroundPrefabs[index].GetComponent<Background>();

        txtObjectName.text = curBackgroundScrp.backgroundName;

        imagePlanetAtmosphere.transform.parent.gameObject.SetActive(false);
        imagePlanetAtmosphere.gameObject.SetActive(false);
        imagePlanetOrWeapon.gameObject.SetActive(false);
        imageBackground.transform.parent.gameObject.SetActive(true);

        imageBackground.sprite = curBackgroundScrp.GetComponent<SpriteRenderer>().sprite;
        imageBackground.color = curBackgroundScrp.GetComponent<SpriteRenderer>().color;

        if (curBackgroundScrp.unlocked)
        {
            txtObjectCost.text = "unlocked";
            txtObjectCost.color = unlockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(false);

            if (curBackgroundScrp.active)
            {
                changeBuySelectButton(1, true);
            }
            else
            {
                changeBuySelectButton(1, false);

                btSelectOrBuy.onClick.RemoveAllListeners();
                btSelectOrBuy.onClick.AddListener(() => selectBackground(index));
            }
        }
        else
        {
            txtObjectCost.text = curBackgroundScrp.cost.ToString();
            txtObjectCost.color = lockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(true);

            changeBuySelectButton(2, false);
            if (curBackgroundScrp.cost > ghScrp.premiumCoinsScrp.getPremiumCoins())
            {
                btSelectOrBuy.interactable = false;
                btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = cantBuyTextColor;
            }
            btSelectOrBuy.onClick.RemoveAllListeners();
            btSelectOrBuy.onClick.AddListener(() => buyBackground(index));
        }

        btLeft.onClick.RemoveAllListeners();
        btRight.onClick.RemoveAllListeners();
        btLeft.onClick.AddListener(() => showBackgroundContent(index - 1));
        btRight.onClick.AddListener(() => showBackgroundContent(index + 1));
    }

    private void showEnemyTypeContent(int index)
    {
        btTabPlanets.GetComponent<Image>().color = originTabColor;
        btTabWeapons.GetComponent<Image>().color = originTabColor;
        btTabBackgrounds.GetComponent<Image>().color = originTabColor;
        btTabEnemyTypes.GetComponent<Image>().color = selectedTabColor;

        if (index < 0 || index > enemyTypePrefabs.Length - 1)
            return;

        StaticAudioHandler.playSound(ghScrp.openDisplay).pitch = 1.2f;

        EnemyType curEnemyTypeScrp = enemyTypePrefabs[index].GetComponent<EnemyType>();

        txtObjectName.text = curEnemyTypeScrp.enemyTypeName;

        imagePlanetOrWeapon.transform.parent.gameObject.SetActive(true);
        imagePlanetAtmosphere.gameObject.SetActive(false);
        imagePlanetOrWeapon.gameObject.SetActive(true);
        imageBackground.transform.parent.gameObject.SetActive(false);

        imagePlanetOrWeapon.sprite = curEnemyTypeScrp.enemyPrefabs[0].GetComponent<SpriteRenderer>().sprite;
        imagePlanetOrWeapon.color = curEnemyTypeScrp.enemyPrefabs[0].GetComponent<SpriteRenderer>().color;

        imagePlanetOrWeapon.rectTransform.localScale = Vector2.one;

        if (curEnemyTypeScrp.unlocked)
        {
            txtObjectCost.text = "unlocked";
            txtObjectCost.color = unlockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(false);

            if (curEnemyTypeScrp.active)
            {
                changeBuySelectButton(1, true);
            }
            else
            {
                changeBuySelectButton(1, false);

                btSelectOrBuy.onClick.RemoveAllListeners();
                btSelectOrBuy.onClick.AddListener(() => selectEnemyType(index));
            }
        }
        else
        {
            txtObjectCost.text = curEnemyTypeScrp.cost.ToString();
            txtObjectCost.color = lockedTextColor;
            imgCostPremiumCoin.gameObject.SetActive(true);

            changeBuySelectButton(2, false);
            if (curEnemyTypeScrp.cost > ghScrp.premiumCoinsScrp.getPremiumCoins())
            {
                btSelectOrBuy.interactable = false;
                btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = cantBuyTextColor;
            }

            btSelectOrBuy.onClick.RemoveAllListeners();
            btSelectOrBuy.onClick.AddListener(() => buyEnemyType(index));
        }

        btLeft.onClick.RemoveAllListeners();
        btRight.onClick.RemoveAllListeners();
        btLeft.onClick.AddListener(() => showEnemyTypeContent(index - 1));
        btRight.onClick.AddListener(() => showEnemyTypeContent(index + 1));
    }


    private void changeBuySelectButton(int state, bool active)
    {
        btSelectOrBuy.interactable = true;

        switch (state)
        {
            case 1://unlocked
                if (active)
                {
                    btSelectOrBuy.GetComponent<Image>().color = selectedObjectColor;
                    btSelectOrBuy.transform.Find("Text").GetComponent<Text>().text = "Selected";
                    btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = selectedObjectTextColor;
                    btSelectOrBuy.interactable = false;
                }
                else
                {
                    btSelectOrBuy.GetComponent<Image>().color = unselectedObjectColor;
                    btSelectOrBuy.transform.Find("Text").GetComponent<Text>().text = "Select";
                    btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = unlockedTextColor;
                }
                break;
            case 2://locked
                btSelectOrBuy.GetComponent<Image>().color = unselectedObjectColor;
                btSelectOrBuy.transform.Find("Text").GetComponent<Text>().text = "Buy";
                btSelectOrBuy.transform.Find("Text").GetComponent<Text>().color = lockedTextColor;
                break;
            default:
                break;
        }
    }


    private void buyPlanet(int index)
    {
        if (planetPrefabs[index].GetComponent<Planet>().cost <= ghScrp.premiumCoinsScrp.getPremiumCoins())
        {
            ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_buy_something_in_the_shop);

            StaticAudioHandler.playSound(soundBuy);
            ghScrp.premiumCoinsScrp.addPremiumCoins(-planetPrefabs[index].GetComponent<Planet>().cost);
            planetPrefabs[index].GetComponent<Planet>().unlocked = true;

            SaveSystem.saveAll();

            showPlanetContent(index);
        }
    }

    private void buyWeapon(int index)
    {
        if (weaponPrefabs[index].GetComponent<Weapon>().cost <= ghScrp.premiumCoinsScrp.getPremiumCoins())
        {
            ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_buy_something_in_the_shop);

            StaticAudioHandler.playSound(soundBuy);
            ghScrp.premiumCoinsScrp.addPremiumCoins(-weaponPrefabs[index].GetComponent<Weapon>().cost);
            weaponPrefabs[index].GetComponent<Weapon>().unlocked = true;

            SaveSystem.saveAll();

            showWeaponContent(index);
        }
    }

    private void buyBackground(int index)
    {
        if (backgroundPrefabs[index].GetComponent<Background>().cost <= ghScrp.premiumCoinsScrp.getPremiumCoins())
        {
            ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_buy_something_in_the_shop);

            StaticAudioHandler.playSound(soundBuy);
            ghScrp.premiumCoinsScrp.addPremiumCoins(-backgroundPrefabs[index].GetComponent<Background>().cost);
            backgroundPrefabs[index].GetComponent<Background>().unlocked = true;

            SaveSystem.saveAll();

            showBackgroundContent(index);
        }
    }

    private void buyEnemyType(int index)
    {
        if (enemyTypePrefabs[index].GetComponent<EnemyType>().cost <= ghScrp.premiumCoinsScrp.getPremiumCoins())
        {
            ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_buy_something_in_the_shop);

            StaticAudioHandler.playSound(soundBuy);
            ghScrp.premiumCoinsScrp.addPremiumCoins(-enemyTypePrefabs[index].GetComponent<EnemyType>().cost);
            enemyTypePrefabs[index].GetComponent<EnemyType>().unlocked = true;

            SaveSystem.saveAll();

            showEnemyTypeContent(index);
        }
    }

    
    private void selectPlanet(int index)
    {
        for (int i = 0; i < planetPrefabs.Length; i++)
        {
            if (i == index)
            {
                if (!planetPrefabs[i].GetComponent<Planet>().active)
                    StaticAudioHandler.playSound(soundSelect);
                planetPrefabs[i].GetComponent<Planet>().active = true;
            }
            else
            {
                planetPrefabs[i].GetComponent<Planet>().active = false;
            }
        }

        ghScrp.loadActivePlanet();
        checkAllGeometryStyleActiveAchivement();
        SaveSystem.saveAll();

        showPlanetContent(index);
    }

    private void selectWeapon(int index)
    {
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            if (i == index)
            {
                if (!weaponPrefabs[i].GetComponent<Weapon>().active)
                    StaticAudioHandler.playSound(soundSelect);
                weaponPrefabs[i].GetComponent<Weapon>().active = true;
            }
            else
            {
                weaponPrefabs[i].GetComponent<Weapon>().active = false;
            }
        }

        ghScrp.loadActiveWeapon();
        checkAllGeometryStyleActiveAchivement();
        SaveSystem.saveAll();

        showWeaponContent(index);
    }

    private void selectBackground(int index)
    {
        for (int i = 0; i < backgroundPrefabs.Length; i++)
        {
            if (i == index)
            {
                if (!backgroundPrefabs[i].GetComponent<Background>().active)
                    StaticAudioHandler.playSound(soundSelect);
                backgroundPrefabs[i].GetComponent<Background>().active = true;
            }
            else
            {
                backgroundPrefabs[i].GetComponent<Background>().active = false;
            }
        }

        ghScrp.loadActiveBackground();
        checkAllGeometryStyleActiveAchivement();
        SaveSystem.saveAll();

        showBackgroundContent(index);
    }

    private void selectEnemyType(int index)
    {
        for (int i = 0; i < enemyTypePrefabs.Length; i++)
        {
            if (i == index)
            {
                if (!enemyTypePrefabs[i].GetComponent<EnemyType>().active)
                    StaticAudioHandler.playSound(soundSelect);
                enemyTypePrefabs[i].GetComponent<EnemyType>().active = true;
            }
            else
            {
                enemyTypePrefabs[i].GetComponent<EnemyType>().active = false;
            }
        }

        ghScrp.loadActiveEnemyType();
        checkAllGeometryStyleActiveAchivement();
        SaveSystem.saveAll();

        showEnemyTypeContent(index);
    }


    public GameObject getActivePlanet()
    {
        for (int index = 0; index < planetPrefabs.Length; index++)
        {
            if (planetPrefabs[index].GetComponent<Planet>().active == true)
            {
                return planetPrefabs[index];
            }
        }

        return planetPrefabs[0];
    }

    public GameObject getActiveWeapon()
    {
        for (int index = 0; index < weaponPrefabs.Length; index++)
        {
            if (weaponPrefabs[index].GetComponent<Weapon>().active == true)
            {
                return weaponPrefabs[index];
            }
        }

        return weaponPrefabs[0];
    }

    public GameObject getActiveBackground()
    {
        for (int index = 0; index < backgroundPrefabs.Length; index++)
        {
            if (backgroundPrefabs[index].GetComponent<Background>().active == true)
            {
                return backgroundPrefabs[index];
            }
        }

        return backgroundPrefabs[0];
    }

    public GameObject getActiveEnemyType()
    {
        for (int index = 0; index < enemyTypePrefabs.Length; index++)
        {
            if (enemyTypePrefabs[index].GetComponent<EnemyType>().active == true)
            {
                return enemyTypePrefabs[index];
            }
        }

        return enemyTypePrefabs[0];
    }


    public int getActivePlanetIndex()
    {
        for (int index = 0; index < planetPrefabs.Length; index++)
        {
            if (planetPrefabs[index].GetComponent<Planet>().active)
            {
                return index;
            }
        }

        return 0;
    }

    public int getActiveWeaponIndex()
    {
        for (int index = 0; index < weaponPrefabs.Length; index++)
        {
            if (weaponPrefabs[index].GetComponent<Weapon>().active)
            {
                return index;
            }
        }

        return 0;
    }

    public int getActiveBackgroundIndex()
    {
        for (int index = 0; index < backgroundPrefabs.Length; index++)
        {
            if (backgroundPrefabs[index].GetComponent<Background>().active)
            {
                return index;
            }
        }

        return 0;
    }

    public int getActiveEnemyTypeIndex()
    {
        for (int index = 0; index < enemyTypePrefabs.Length; index++)
        {
            if (enemyTypePrefabs[index].GetComponent<EnemyType>().active)
            {
                return index;
            }
        }

        return 0;
    }



    public void playServiceSavegameLoad()
    {
        GameHandler ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();
        Savegame savegame = GameObject.Find("GooglePlayService").GetComponent<PlayCloudDataManager>().CurSavegame;

        //unlocked planets
        foreach (int id in savegame.unlockedPlanetIDs)
        {
            for (int planetIndex = 0; planetIndex < planetPrefabs.Length; planetIndex++)
            {
                if (id == planetPrefabs[planetIndex].GetComponent<Planet>().id)
                    planetPrefabs[planetIndex].GetComponent<Planet>().unlocked = true;
            }
        }
        //active planet
        for (int i = 0; i < planetPrefabs.Length; i++)
        {
            planetPrefabs[i].GetComponent<Planet>().active = false;

            if (i == savegame.activePlanetID)
                planetPrefabs[i].GetComponent<Planet>().active = true;               
        }


        //unlocked weapons
        foreach (int id in savegame.unlockedWeaponIDs)
        {
            for (int weaponIndex = 0; weaponIndex < weaponPrefabs.Length; weaponIndex++)
            {
                if (id == weaponPrefabs[weaponIndex].GetComponent<Weapon>().id)
                    weaponPrefabs[weaponIndex].GetComponent<Weapon>().unlocked = true;
            }
        }
        //active weapon
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            if (i == savegame.activeWeaponID)
                weaponPrefabs[i].GetComponent<Weapon>().active = true;
            else
                weaponPrefabs[i].GetComponent<Weapon>().active = false;
        }


        //unlocked background
        foreach (int id in savegame.unlockedBackgroundIDs)
        {
            for (int backgroundIndex = 0; backgroundIndex < backgroundPrefabs.Length; backgroundIndex++)
            {
                if (id == backgroundPrefabs[backgroundIndex].GetComponent<Background>().id)
                    backgroundPrefabs[backgroundIndex].GetComponent<Background>().unlocked = true;
            }
        }
        //active background
        for (int i = 0; i < backgroundPrefabs.Length; i++)
        {
            if (i == savegame.activeBackgroundID)
                backgroundPrefabs[i].GetComponent<Background>().active = true;
            else
                backgroundPrefabs[i].GetComponent<Background>().active = false;
        }


        //unlocked enemyType
        foreach (int id in savegame.unlockedEnemyTypeIDs)
        {
            for (int enemyTypeIndex = 0; enemyTypeIndex < enemyTypePrefabs.Length; enemyTypeIndex++)
            {
                if (id == enemyTypePrefabs[enemyTypeIndex].GetComponent<EnemyType>().id)
                    enemyTypePrefabs[enemyTypeIndex].GetComponent<EnemyType>().unlocked = true;
            }
        }
        //active enemyType
        for (int i = 0; i < enemyTypePrefabs.Length; i++)
        {
            if (i == savegame.activeEnemyTypeID)
                enemyTypePrefabs[i].GetComponent<EnemyType>().active = true;
            else
                enemyTypePrefabs[i].GetComponent<EnemyType>().active = false;
        }        
    }

    private void checkAllGeometryStyleActiveAchivement()
    {
        if (getActivePlanet().GetComponent<Planet>().id == 9 &&
           getActiveWeapon().GetComponent<Weapon>().id == 4 &&
           getActiveBackground().GetComponent<Background>().id == 7 &&
           getActiveEnemyType().GetComponent<EnemyType>().id == 2)
        {
            ghScrp.playCloudManagerScrp.unlockAchievement(GPGSIds.achievement_geometry_style);
        }
    }
}
