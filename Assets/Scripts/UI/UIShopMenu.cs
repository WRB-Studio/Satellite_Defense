using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopMenu : MonoBehaviour
{
    public static UIShopMenu Instance { get; private set; }
    [Header("Colors")]
    public Color selectedTabColor;
    public Color activeEntityColor;
    public Color cantBuyTextColor;
    [Header("Shop")]
    public GameObject shopMenuPanel;
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
    public TextMeshProUGUI txtPurchaseAction;
    [Header("Attributes")]
    public TextMeshProUGUI txtItemLevel;
    public Transform attributeParent;
    public GameObject attributePrefab;
    public ScrollRect attributeScroll;
    public Sprite[] attributeIcons;

    private Color tabColor;
    private Color selectColor;
    private Color buyColor;
    private IngameEntity.eEntityType category = IngameEntity.eEntityType.Planet;
    private GameObject[] catalog;
    private int index;
    private IngameEntity currentItem;
    private readonly List<AttributeRow> attributeRows = new();

    private sealed class AttributeRow
    {
        public GameObject Instance { get; }
        private readonly TextMeshProUGUI value;
        private readonly TextMeshProUGUI title;
        private readonly TextMeshProUGUI description;
        private readonly Image icon;

        public AttributeRow(GameObject instance, Transform layoutRoot)
        {
            Instance = instance;
            var header = instance.transform.GetChild(0);
            value = header.Find("txtTotalValue").GetComponent<TextMeshProUGUI>();
            title = header.Find("txtAttributeName").GetComponent<TextMeshProUGUI>();
            description = instance.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            icon = header.Find("Symbol/imgFrame/imgSymbol").GetComponent<Image>();
            var button = header.Find("Symbol/imgFrame").GetComponent<Button>();
            UIController.Bind(button, () =>
            {
                description.gameObject.SetActive(!description.gameObject.activeSelf);
                Utilities.RefreshLayout(layoutRoot);
            });
        }

        public void Show(EntityAttribute attribute, int level, Sprite sprite)
        {
            Instance.SetActive(true);
            value.text = attribute.GetAttributeEffectString(level);
            title.text = EntityAttribute.GetAttributeName(attribute.attributeType);
            description.text = attribute.GetDescription();
            description.gameObject.SetActive(false);
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite);
        }
    }

    private void Awake() => Instance = this;

    public void Init()
    {
        tabColor = btnTabPlanets.image.color;
        selectColor = btnStateSelect.transform.GetChild(0).GetComponent<Image>().color;
        buyColor = btnBuyUpgrade.image.color;
        if (!txtPurchaseAction)
        {
            var actionLabel = btnBuyUpgrade.transform.Find("txtPurchaseAction");
            if (actionLabel) txtPurchaseAction = actionLabel.GetComponent<TextMeshProUGUI>();
        }
        UIController.Bind(btnTabPlanets, () => SelectCategory(IngameEntity.eEntityType.Planet));
        UIController.Bind(btnTabWeapons, () => SelectCategory(IngameEntity.eEntityType.Weapon));
        UIController.Bind(btnTabEnemyTypes, () => SelectCategory(IngameEntity.eEntityType.Enemy));
        UIController.Bind(btnTabBackgrounds, () => SelectCategory(IngameEntity.eEntityType.Background));
        UIController.Bind(btnClose, () => GameController.Instance.CloseShop());
        UIController.Bind(btnLeft, () => ChangeIndex(-1));
        UIController.Bind(btnRight, () => ChangeIndex(1));
        UIController.Bind(btnStateSelect, SelectItem);
        UIController.Bind(btnBuyUpgrade, PurchaseItem);
    }

    public void Show(bool visible)
    {
        shopMenuPanel.SetActive(visible);
        if (visible) SelectCategory(category);
    }

    private void SelectCategory(IngameEntity.eEntityType value)
    {
        category = value;
        catalog = GameController.Instance.GetCategoryItems(category);
        var active = GameController.Instance.GetActiveItem(category);
        index = Mathf.Max(0, System.Array.IndexOf(catalog, active ? active.gameObject : null));
        btnTabPlanets.image.color = category == IngameEntity.eEntityType.Planet ? selectedTabColor : tabColor;
        btnTabWeapons.image.color = category == IngameEntity.eEntityType.Weapon ? selectedTabColor : tabColor;
        btnTabEnemyTypes.image.color = category == IngameEntity.eEntityType.Enemy ? selectedTabColor : tabColor;
        btnTabBackgrounds.image.color = category == IngameEntity.eEntityType.Background ? selectedTabColor : tabColor;
        Refresh();
    }

    private void ChangeIndex(int direction)
    {
        index = Mathf.Clamp(index + direction, 0, catalog.Length - 1);
        Refresh();
    }

    private void SelectItem()
    {
        if (GameController.Instance.SelectItem(currentItem)) Refresh();
        else UIController.Instance.ShowSaveMessage(SaveGameController.LastError);
    }

    private void PurchaseItem()
    {
        if (!GameController.Instance.PurchaseItem(currentItem))
        {
            UIController.Instance.ShowSaveMessage(SaveGameController.LastError);
            return;
        }
        AudioController.PlaySound(AudioController.Instance.soundBuy);
        Refresh();
    }

#if UNITY_EDITOR
    public void RefreshForEditor()
    {
        if (currentItem && shopMenuPanel.activeInHierarchy) Refresh();
    }
#endif

    private void Refresh()
    {
        currentItem = catalog[index].GetComponent<IngameEntity>();
        btnLeft.interactable = index > 0;
        btnRight.interactable = index < catalog.Length - 1;
        txtContentTitle.text = currentItem.itemName;
        txtItemLevel.text = $"Level {currentItem.Level} / {currentItem.maxEntityLevel}";
        UpdatePreview();

        bool owned = currentItem.IsUnlocked;
        bool active = currentItem.IsActive;
        btnStateSelect.interactable = SaveGameController.CanSave && owned && !active;
        btnStateSelect.GetComponentInChildren<TextMeshProUGUI>().text = !owned ? "Locked" : active ? "Active" : "Activate";
        btnStateSelect.transform.GetChild(0).GetComponent<Image>().color = !owned ? cantBuyTextColor : active ? activeEntityColor : selectColor;

        bool hasUpgrades = currentItem.HasUpgrades;
        bool complete = owned && !currentItem.CanUpgrade(currentItem.Level);
        long price = currentItem.GetPurchaseCost(SaveGameController.Data);
        btnBuyUpgrade.interactable = SaveGameController.CanSave && currentItem.CanPurchase(SaveGameController.Data);
        btnBuyUpgrade.image.color = btnBuyUpgrade.interactable ? buyColor : cantBuyTextColor;
        imgPremiumCoin.gameObject.SetActive(!complete);
        txtItemLevel.gameObject.SetActive(hasUpgrades);
        string action;
        if (complete) action = hasUpgrades ? "MAX LEVEL" : "PURCHASED";
        else action = owned ? $"UPGRADE TO LVL {currentItem.Level + 1}" : "BUY";
        if (txtPurchaseAction) txtPurchaseAction.text = action;
        txtItemCost.text = complete ? (txtPurchaseAction ? string.Empty : action) : Utilities.NumberToString(price);
        UpdateAttributes();
        attributeScroll.StopMovement();
        attributeScroll.verticalNormalizedPosition = 1f;
    }

    private void UpdatePreview()
    {
        bool background = category == IngameEntity.eEntityType.Background;
        bool planet = category == IngameEntity.eEntityType.Planet;
        imgPlanetAtmosphere.transform.parent.gameObject.SetActive(!background);
        imgPlanetAtmosphere.gameObject.SetActive(planet);
        imgPlanetOrWeapon.gameObject.SetActive(!background);
        imgBackground.transform.parent.gameObject.SetActive(background);

        SpriteRenderer sprite;
        if (planet)
        {
            sprite = currentItem.GetComponent<SpriteRenderer>();
            var atmosphere = currentItem.transform.Find("Atmosphere");
            var renderer = atmosphere.GetComponent<SpriteRenderer>();
            imgPlanetAtmosphere.sprite = renderer.sprite;
            imgPlanetAtmosphere.color = renderer.color;
        }
        else if (category == IngameEntity.eEntityType.Weapon)
            sprite = currentItem.transform.Find("SatelliteModel").GetComponent<SpriteRenderer>();
        else if (category == IngameEntity.eEntityType.Enemy)
            sprite = ((EnemyType)currentItem).enemyPrefabs[0].GetComponent<SpriteRenderer>();
        else sprite = currentItem.GetComponent<SpriteRenderer>();

        var image = background ? imgBackground : imgPlanetOrWeapon;
        image.sprite = sprite.sprite;
        image.color = sprite.color;
    }

    private void UpdateAttributes()
    {
        int rowIndex = 0;
        int level = currentItem.Level;
        if (currentItem.attribute != null)
        {
            foreach (var attribute in currentItem.attribute)
            {
                if (attribute == null || attribute.attributeType == EntityAttribute.eAttributeType.None) continue;
                if (rowIndex >= attributeRows.Count)
                {
                    var instance = Instantiate(attributePrefab, attributeParent);
                    attributeRows.Add(new AttributeRow(instance, attributeParent));
                }
                attributeRows[rowIndex++].Show(attribute, level, FindIcon(attribute.attributeType));
            }
        }
        for (int i = rowIndex; i < attributeRows.Count; i++) attributeRows[i].Instance.SetActive(false);
        Utilities.RefreshLayout(attributeParent);
    }

    private Sprite FindIcon(EntityAttribute.eAttributeType type)
    {
        foreach (var sprite in attributeIcons)
            if (sprite && sprite.name == type.ToString()) return sprite;
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
