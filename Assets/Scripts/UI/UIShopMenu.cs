using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.UI;
using static EntityAttribute;

public class UIShopMenu : MonoBehaviour
{
    public static UIShopMenu Instance;

    private enum eBuySelectButtonState { None, CantBuy, BuyIt, SelectIt, ActiveIt }

    [Header("Shop content")]
    public GameObject[] planetPrefabs;
    public GameObject[] weaponPrefabs;
    public GameObject[] backgroundPrefabs;
    public GameObject[] enemyTypePrefabs;

    [Header("Color settings")]
    public Color selectedTabColor;
    private Color originTabColor;
    public Color activeEntityColor;
    public Color cantBuyTextColor;

    private Color originBtnStateSelectColor;
    private Color originBtnBuyUpgradeColor;

    [Header("Shop main elements")]
    public GameObject shopMenuPanel;

    [Header("Tabs")]
    public Button btnTabPlanets;
    public Button btnTabWeapons;
    public Button btnTabEnemyTypes;
    public Button btnTabBackgrounds;
    public Button btnClose;

    [Header("Content")]
    public TextMeshProUGUI txtContentTitle;
    public Image imgPlanetAtmosphere;
    public Image imgPlanetOrWeapon;
    public Image imgBackground;
    public Button btnLeft;
    public Button btnRight;
    public Button btnStateSelect;
    public Image imgPremiumCoin;
    public Button btnBuyUpgrade;
    public TextMeshProUGUI txtItemCost;

    private Vector2 originTxtItemCostWidthHeight;

    [Header("Attributes")]
    public TextMeshProUGUI txtItemLevel;
    public Transform attributeParent;
    public GameObject attributePrefab;
    public Sprite[] attributeIcons;
    private List<GameObject> attributeItemList = new List<GameObject>();
    private bool showAttributeInfo = false;
    private List<GameObject> attributInfoList = new List<GameObject>();

    private IngameEntity.eEntityType curShopCategory = IngameEntity.eEntityType.None;
    private int curShopItemIndex = 0;
    private GameObject[] curShopItemList;
    [HideInInspector] public IngameEntity curShopItem;

    [HideInInspector] public GameObject activePlanetItem;
    [HideInInspector] public GameObject activeWeaponItem;
    [HideInInspector] public GameObject activeBackgroundItem;
    [HideInInspector] public GameObject activeEnemyTypeItem;



    private void Awake()
    {
        Instance = this;

        originTxtItemCostWidthHeight = txtItemCost.GetComponent<RectTransform>().sizeDelta;
    }

    public void Init()
    {
        btnTabPlanets.onClick.RemoveAllListeners();
        btnTabWeapons.onClick.RemoveAllListeners();
        btnTabBackgrounds.onClick.RemoveAllListeners();
        btnTabEnemyTypes.onClick.RemoveAllListeners();
        btnClose.onClick.RemoveAllListeners();
        btnLeft.onClick.RemoveAllListeners();
        btnRight.onClick.RemoveAllListeners();

        btnTabPlanets.onClick.AddListener(() => SetCurrentShopCategory(IngameEntity.eEntityType.Planet));
        btnTabWeapons.onClick.AddListener(() => SetCurrentShopCategory(IngameEntity.eEntityType.Weapon));
        btnTabBackgrounds.onClick.AddListener(() => SetCurrentShopCategory(IngameEntity.eEntityType.Background));
        btnTabEnemyTypes.onClick.AddListener(() => SetCurrentShopCategory(IngameEntity.eEntityType.Enemy));

        btnClose.onClick.AddListener(() => UIController.Instance.ShowHideMenu(UIController.eMenuType.Shop, false));

        btnLeft.onClick.AddListener(() => ChangeShopIndex(-1));
        btnRight.onClick.AddListener(() => ChangeShopIndex(1));

        btnStateSelect.onClick.AddListener(() => ActivateItem());
        btnBuyUpgrade.onClick.AddListener(() => BuyUpgradeCurShopItem());

        btnClose.interactable = true;
        btnTabPlanets.interactable = true;
        btnTabWeapons.interactable = true;
        btnTabBackgrounds.interactable = true;
        btnTabEnemyTypes.interactable = true;
        btnLeft.interactable = true;
        btnRight.interactable = true;

        originTabColor = btnTabPlanets.GetComponent<Image>().color;
        originBtnStateSelectColor = btnStateSelect.transform.GetChild(0).GetComponent<Image>().color;
        originBtnBuyUpgradeColor = btnBuyUpgrade.GetComponent<Image>().color;

        StopAllCoroutines();

        if (Application.isEditor)
            ResetPrefabLists();

        activePlanetItem = planetPrefabs[0];
        activeWeaponItem = weaponPrefabs[0];
        activeBackgroundItem = backgroundPrefabs[0];
        activeEnemyTypeItem = enemyTypePrefabs[0];

        Load();

        SetCurrentShopCategory(IngameEntity.eEntityType.Planet);

        ShowHideMenu(false);
    }


    public void ResetPrefabLists()
    {
        ResetPrefabList(planetPrefabs);
        ResetPrefabList(weaponPrefabs);
        ResetPrefabList(backgroundPrefabs);
        ResetPrefabList(enemyTypePrefabs);
    }

    public void ResetPrefabList(GameObject[] prefabs)
    {
        foreach (var prefab in prefabs)
        {
            var item = prefab.GetComponent<IngameEntity>();
            item.active = false;
            item.unlocked = false;
        }

        var firstPrefab = prefabs[0].GetComponent<IngameEntity>();
        firstPrefab.active = true;
        firstPrefab.unlocked = true;
    }


    public void ShowHideMenu()
    {
        ShowHideMenu(!shopMenuPanel.activeSelf);
    }

    public void ShowHideMenu(bool show)
    {
        if (show)
        {
            SetCurrentShopCategory(IngameEntity.eEntityType.Planet);
            UIController.Instance.modalPanelShop.SetActive(true);
        }
        else
        {
            if (shopMenuPanel.activeSelf)
                AudioController.PlaySound(clip: AudioController.Instance.openDisplay, pitch: 0.9f);
        }

        shopMenuPanel.SetActive(show);

    }


    /*---------------Shop Content-----------------*/

    private void SetCurrentShopCategory(IngameEntity.eEntityType shopCategory)
    {
        curShopCategory = shopCategory;
        SetTabs(shopCategory);

        curShopItemList = GetCategoryItems(shopCategory);
        if (curShopItemList == null || curShopItemList.Length == 0)
            return;

        GameObject active = shopCategory switch
        {
            IngameEntity.eEntityType.Planet => activePlanetItem,
            IngameEntity.eEntityType.Weapon => activeWeaponItem,
            IngameEntity.eEntityType.Background => activeBackgroundItem,
            IngameEntity.eEntityType.Enemy => activeEnemyTypeItem,
            _ => null
        };

        curShopItemIndex = Mathf.Max(0, System.Array.IndexOf(curShopItemList, active));
        curShopItem = curShopItemList[curShopItemIndex].GetComponent<IngameEntity>();
        UpdateCurShopItem();
    }


    private void UpdateCurShopItem()
    {
        AudioController.PlaySound(clip: AudioController.Instance.openDisplay, pitch: 1.2f);

        txtContentTitle.text = curShopItem.itemName;
        txtItemLevel.text = "LvL: " + curShopItem.entityLevel + " / " + curShopItem.maxEntityLevel;

        switch (curShopCategory)
        {
            case IngameEntity.eEntityType.Planet:
                {
                    SetVisibility(atmoContainer: true, atmo: true, main: true, bgContainer: false);

                    var atmoTr = curShopItem.transform.Find("Atmosphere");
                    var atmoSR = atmoTr.GetComponent<SpriteRenderer>();
                    imgPlanetAtmosphere.sprite = atmoSR.sprite;
                    imgPlanetAtmosphere.rectTransform.localScale = atmoTr.localScale;
                    imgPlanetAtmosphere.color = atmoSR.color;

                    var sr = curShopItem.GetComponent<SpriteRenderer>();
                    imgPlanetOrWeapon.sprite = sr.sprite;
                    imgPlanetOrWeapon.color = sr.color;
                    break;
                }

            case IngameEntity.eEntityType.Weapon:
                {
                    SetVisibility(atmoContainer: true, atmo: false, main: true, bgContainer: false);

                    var satSR = curShopItem.transform.Find("SatelliteModel").GetComponent<SpriteRenderer>();
                    imgPlanetOrWeapon.sprite = satSR.sprite;
                    imgPlanetOrWeapon.color = satSR.color;
                    imgPlanetOrWeapon.rectTransform.localScale = Vector2.one;
                    break;
                }

            case IngameEntity.eEntityType.Background:
                {
                    SetVisibility(atmoContainer: false, atmo: false, main: false, bgContainer: true);

                    var sr = curShopItem.GetComponent<SpriteRenderer>();
                    imgBackground.sprite = sr.sprite;
                    imgBackground.color = sr.color;
                    break;
                }

            case IngameEntity.eEntityType.Enemy:
                {
                    SetVisibility(atmoContainer: true, atmo: false, main: true, bgContainer: false);

                    var enemyType = curShopItem.GetComponent<EnemyType>();
                    var sr = enemyType.enemyPrefabs[0].GetComponent<SpriteRenderer>();
                    imgPlanetOrWeapon.sprite = sr.sprite;
                    imgPlanetOrWeapon.color = sr.color;
                    imgPlanetOrWeapon.rectTransform.localScale = Vector2.one;
                    break;
                }
        }

        ConfigureBtnStateSelect();
        ConfigureBtnBuyUpgrade();

        Utilities.UIRefreshDeep(shopMenuPanel.GetComponent<RectTransform>());
    }

    public void ChangeShopIndex(int direction)
    {
        int newIndex = Mathf.Clamp(curShopItemIndex + direction, 0, curShopItemList.Length - 1);

        if (newIndex == curShopItemIndex) return;

        curShopItemIndex = newIndex;

        curShopItem = curShopItemList[curShopItemIndex].GetComponent<IngameEntity>();

        UpdateCurShopItem();
    }


    private GameObject[] GetCategoryItems(IngameEntity.eEntityType category)
    {
        switch (category)
        {
            case IngameEntity.eEntityType.Planet:
                return planetPrefabs;

            case IngameEntity.eEntityType.Weapon:
                return weaponPrefabs;

            case IngameEntity.eEntityType.Background:
                return backgroundPrefabs;

            case IngameEntity.eEntityType.Enemy:
                return enemyTypePrefabs;

            default:
                return null;
        }
    }

    private void SetTabs(IngameEntity.eEntityType cat)
    {
        btnTabPlanets.GetComponent<Image>().color = originTabColor;
        btnTabWeapons.GetComponent<Image>().color = originTabColor;
        btnTabBackgrounds.GetComponent<Image>().color = originTabColor;
        btnTabEnemyTypes.GetComponent<Image>().color = originTabColor;

        switch (cat)
        {
            case IngameEntity.eEntityType.None:
                break;

            case IngameEntity.eEntityType.Planet:
                btnTabPlanets.GetComponent<Image>().color = selectedTabColor;
                SetVisibility(atmoContainer: true, atmo: true, main: true, bgContainer: false);
                break;

            case IngameEntity.eEntityType.Weapon:
                btnTabWeapons.GetComponent<Image>().color = selectedTabColor;
                SetVisibility(atmoContainer: true, atmo: false, main: true, bgContainer: false);
                break;

            case IngameEntity.eEntityType.Background:
                btnTabBackgrounds.GetComponent<Image>().color = selectedTabColor;
                SetVisibility(atmoContainer: false, atmo: false, main: false, bgContainer: true);
                break;

            case IngameEntity.eEntityType.Enemy:
                btnTabEnemyTypes.GetComponent<Image>().color = selectedTabColor;
                SetVisibility(atmoContainer: true, atmo: false, main: true, bgContainer: false);
                break;

            default:
                break;
        }
    }

    private void SetVisibility(bool atmoContainer, bool atmo, bool main, bool bgContainer)
    {
        var atmoParent = imgPlanetAtmosphere.transform.parent.gameObject;
        var bgParent = imgBackground.transform.parent.gameObject;

        atmoParent.SetActive(atmoContainer);
        imgPlanetAtmosphere.gameObject.SetActive(atmo);
        imgPlanetOrWeapon.gameObject.SetActive(main);
        bgParent.SetActive(bgContainer);
    }

    private void ConfigureBtnStateSelect()
    {
        var label = btnStateSelect.GetComponentInChildren<TextMeshProUGUI>();

        if (!curShopItem.unlocked)
        {
            btnStateSelect.interactable = false;
            label.text = "Locked";
            btnStateSelect.transform.GetChild(0).GetComponent<Image>().color = cantBuyTextColor;
        }
        else if (curShopItem.active)
        {
            btnStateSelect.interactable = false;
            label.text = "Active";
            btnStateSelect.transform.GetChild(0).GetComponent<Image>().color = activeEntityColor;
        }
        else
        {
            btnStateSelect.interactable = true;
            label.text = "Activate";
            btnStateSelect.transform.GetChild(0).GetComponent<Image>().color = originBtnStateSelectColor;
        }
    }

    private void ConfigureBtnBuyUpgrade()
    {
        long coins = PremiumCoinController.Instance ? PremiumCoinController.Instance.premiumCoins : 0L;
        var info = curShopItem;

        bool hasUpgrades = info.attribute != null && info.attribute.Count > 0;

        txtItemLevel.gameObject.SetActive(hasUpgrades);
        txtItemCost.transform.parent.Find("imgPremiumCoin").gameObject.SetActive(true);
        txtItemCost.GetComponent<RectTransform>().sizeDelta = originTxtItemCostWidthHeight;

        UpdateAttributeInfos();

        if (!info.unlocked) // --- Locked ---
        {
            btnBuyUpgrade.interactable = info.cost <= coins;
            txtItemCost.text = Utilities.numberToString(info.cost);
        }
        else if (!hasUpgrades) // --- Unlocked, no upgrades configured (Fallback) ---
        {
            btnBuyUpgrade.interactable = false;
            txtItemCost.text = "Purchased";
            txtItemCost.transform.parent.Find("imgPremiumCoin").gameObject.SetActive(false);
            txtItemCost.GetComponent<RectTransform>().sizeDelta = new Vector2(originTxtItemCostWidthHeight.x * 1.5f, originTxtItemCostWidthHeight.y * 1.5f);
        }
        else if (info.entityLevel >= info.maxEntityLevel) // --- Unlocked +  max upgrade level reached ---
        {
            btnBuyUpgrade.interactable = false;
            txtItemCost.text = "Max Level";
            txtItemCost.transform.parent.Find("imgPremiumCoin").gameObject.SetActive(false);
            txtItemCost.GetComponent<RectTransform>().sizeDelta = new Vector2(originTxtItemCostWidthHeight.x * 1.5f, originTxtItemCostWidthHeight.y * 1.5f);
        }
        else
        {
            int cost = info.GetAttributeCostByLevel(info.entityLevel);
            btnBuyUpgrade.interactable = cost <= coins;
            txtItemCost.text = Utilities.numberToString(cost);
        }

        btnBuyUpgrade.GetComponent<Image>().color = btnBuyUpgrade.interactable ? originBtnBuyUpgradeColor : cantBuyTextColor;
    }


    private void UpdateAttributeInfos()
    {
        foreach (var info in attributeItemList) Destroy(info);
        attributeItemList.Clear();
        attributInfoList.Clear();

        AddAttributeInfo(eAttributeType.PlanetStartHP, 0, true);
        AddAttributeInfo(eAttributeType.PlanetMaxHP, 0, true);
        AddAttributeInfo(eAttributeType.PlanetRevive, 0, true);
        AddAttributeInfo(eAttributeType.PlanetExplosionOnHit, 0, true);

        AddAttributeInfo(eAttributeType.WeaponRotationSpeed, 0, true);
        AddAttributeInfo(eAttributeType.WeaponFireRate, 0, true);
        AddAttributeInfo(eAttributeType.WeaponProjectileSpeed, 0, true);
        AddAttributeInfo(eAttributeType.WeaponDamage, 0, true);

        AddAttributeInfo(eAttributeType.EnemyHP, 0, true);
        AddAttributeInfo(eAttributeType.EnemySpeed, 0, true);
        AddAttributeInfo(eAttributeType.EnemyDamage, 0, true);
        AddAttributeInfo(eAttributeType.EnemySplitCount, 0, true);
        AddAttributeInfo(eAttributeType.EnemySplitChance, 0, true);
        AddAttributeInfo(eAttributeType.EnemySpawnRate, 0, true);

        AddAttributeInfo(eAttributeType.CoinChance, 0, true);
        AddAttributeInfo(eAttributeType.ScoreMultiplier, 0, true);
        AddAttributeInfo(eAttributeType.BonusCoinValue, 0, true);
        AddAttributeInfo(eAttributeType.ScoreBoostOnLowHP, 0, true);
    }

    private void ShowHideAttributeInfo()
    {
        showAttributeInfo = !showAttributeInfo;
        foreach (var info in attributInfoList) info.SetActive(!showAttributeInfo);
    }

    private void AddAttributeInfo(eAttributeType type, float baseVal, bool showWhenExist = false)
    {
        string name = GetAttributeName(type);
        if (attributeItemList.Any(x => x && x.name == name)) return;

        EntityAttribute upgrade = curShopItem.GetAttributeByType(type);
        float upgradeVal = upgrade != null ? upgrade.GetAttributeEffect(curShopItem.entityLevel) : 0f;
        if (showWhenExist && upgradeVal == 0) return;


        var newAttributeItem = Instantiate(attributePrefab, attributeParent);
        newAttributeItem.name = name;
        attributeItemList.Add(newAttributeItem);

        var img = newAttributeItem.transform.GetChild(0).Find("Symbol").Find("imgFrame").Find("imgSymbol").GetComponent<Image>();
        var txtTotal = newAttributeItem.transform.GetChild(0).Find("txtTotalValue").GetComponent<TextMeshProUGUI>();
        var txtInfo = newAttributeItem.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        var btnInfo = newAttributeItem.transform.GetChild(0).Find("Symbol").Find("imgFrame").GetComponent<Button>();
        
        attributInfoList.Add(txtInfo.gameObject);
        btnInfo.onClick.AddListener(() => ShowHideAttributeInfo());

        img.sprite = GetIconFor(type);
        img.gameObject.SetActive(img.sprite != null);
        txtInfo.text = name;

        string val = (baseVal + upgradeVal).ToString("0.##");
        string pre = "", suf = "";

        switch (type)
        {
            // --- Planet ---
            case eAttributeType.PlanetStartHP:
            case eAttributeType.PlanetMaxHP:
            case eAttributeType.EnemyHP:
                suf = " HP"; break;

            case eAttributeType.PlanetRevive:
            case eAttributeType.PlanetExplosionOnHit:
                val = "Active"; break;

            // --- Weapon ---
            case eAttributeType.WeaponRotationSpeed:
                suf = "°/s"; break;

            case eAttributeType.WeaponFireRate:
                suf = " sh/s"; break; // shots/s → kurz

            case eAttributeType.WeaponProjectileSpeed:
                suf = " u/s"; break;

            case eAttributeType.WeaponDamage:
                suf = " DMG"; break;

            // --- Enemy ---
            case eAttributeType.EnemySpeed:
                suf = " u/s"; break;

            case eAttributeType.EnemyDamage:
                suf = " DMG"; break;

            case eAttributeType.EnemySplitCount:
                suf = "x"; break;

            case eAttributeType.EnemySplitChance:
                suf = " %"; break;

            // --- Score / Economy ---
            case eAttributeType.CoinChance:
                suf = " %"; break;

            case eAttributeType.BonusCoinValue:
                suf = " c"; break; // coins → kurz

            case eAttributeType.ScoreMultiplier:
                pre = "x"; break;

            case eAttributeType.ScoreBoostOnLowHP:
                suf = " %"; break;

            case eAttributeType.EnemySpawnRate:
                suf = " sp/s"; break; // spawns/s → kurz
        }

        txtTotal.text = $"{pre}{val}{suf}".Trim();
    }

    private Sprite GetIconFor(eAttributeType type)
    {
        foreach (var icon in attributeIcons)
            if (icon.name == type.ToString())
                return icon;
        return null;
    }

    private void ActivateItem()
    {
        foreach (var shopItem in curShopItemList)
        {
            var info = shopItem.GetComponent<IngameEntity>();

            if (shopItem == curShopItem.gameObject)
            {
                if (!info.active)
                    AudioController.PlaySound(clip: AudioController.Instance.soundSelect);

                info.active = true;

                switch (curShopCategory)
                {
                    case IngameEntity.eEntityType.Planet:
                        activePlanetItem = shopItem;
                        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Planet);
                        break;

                    case IngameEntity.eEntityType.Weapon:
                        activeWeaponItem = shopItem;
                        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Weapon);
                        break;

                    case IngameEntity.eEntityType.Background:
                        activeBackgroundItem = shopItem;
                        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Background);
                        break;

                    case IngameEntity.eEntityType.Enemy:
                        activeEnemyTypeItem = shopItem;
                        GameController.Instance.InstantiateActiveItem(IngameEntity.eEntityType.Enemy);
                        break;

                    default:
                        break;
                }
            }
            else
            {
                info.active = false;
            }
        }

        SaveGameController.SaveActiveItem(curShopCategory, curShopItem.id);

        UpdateCurShopItem();
    }

    private void BuyUpgradeCurShopItem()
    {
        if (!curShopItem.unlocked && (curShopItem.cost <= PremiumCoinController.Instance.premiumCoins))
        {
            AudioController.PlaySound(clip: AudioController.Instance.soundBuy);
            PremiumCoinController.Instance.AddPremiumCoins(-curShopItem.cost);

            curShopItem.unlocked = true;

            SaveGameController.SaveUnlockedItem(curShopCategory, curShopItem.id, curShopItem.entityLevel);

            UpdateCurShopItem();
        }
        else if (curShopItem.unlocked && (curShopItem.GetAttributeCostByLevel(curShopItem.entityLevel) <= PremiumCoinController.Instance.premiumCoins))
        {
            AudioController.PlaySound(clip: AudioController.Instance.soundBuy);
            PremiumCoinController.Instance.AddPremiumCoins(-curShopItem.GetAttributeCostByLevel(curShopItem.entityLevel));

            curShopItem.entityLevel++;

            SaveGameController.SaveUnlockedItem(curShopCategory, curShopItem.id, curShopItem.entityLevel);

            UpdateCurShopItem();
        }
    }


    /*---------------Other Functions-----------------*/

    public void PlayServiceSavegameLoad()
    {
        Savegame savegame = null; //TODO: playCloudManagerScrp.CurSavegame;

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


    /*---------------Save & Load-----------------*/

    public void Load()
    {
        var savegame = SaveGameController.savegame;

        GameObject LoadShopItems(GameObject[] prefabs, int activeID, List<int> unlockedIDs)
        {
            GameObject activeObject = null;
            var unlocked = new HashSet<int>(unlockedIDs);
            foreach (var go in prefabs)
            {
                var info = go.GetComponent<IngameEntity>();

                if (info.id == activeID)
                    activeObject = go;
                info.active = info.id == activeID;
                info.unlocked = unlocked.Contains(info.id);

                int savedLevel = SaveGameController.GetEntityLevel(info.entityType, info.id);
                info.entityLevel = Mathf.Clamp(savedLevel, 0, info.maxEntityLevel);
            }

            return activeObject;
        }

        activePlanetItem = LoadShopItems(planetPrefabs, savegame.activePlanetID, savegame.unlockedPlanetIDs);
        activeWeaponItem = LoadShopItems(weaponPrefabs, savegame.activeWeaponID, savegame.unlockedWeaponIDs);
        activeBackgroundItem = LoadShopItems(backgroundPrefabs, savegame.activeBackgroundID, savegame.unlockedBackgroundIDs);
        activeEnemyTypeItem = LoadShopItems(enemyTypePrefabs, savegame.activeEnemyTypeID, savegame.unlockedEnemyTypeIDs);
    }

    public void Save()
    {

    }

}