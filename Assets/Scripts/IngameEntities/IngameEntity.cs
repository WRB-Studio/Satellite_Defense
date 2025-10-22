using System.Collections.Generic;
using UnityEngine;

public class IngameEntity : MonoBehaviour
{
    public enum eEntityType { None, Planet, Weapon, Enemy, Background }

    public eEntityType entityType = eEntityType.None;

    [Header("----------------------------------------")]
    [Header("Shop infos")]
    public int id = 0;
    public string itemName = "unnamed";
    public long cost = 0;
    public bool unlocked = false;
    public bool active = false;
    [Header("----------------------------------------")]

    [Header("Attribute infos")]
    public int entityLevel = 1;
    public int maxEntityLevel = 3;
    public int upgradeBaseCost = 100;
    public float upgradeCostMultiplier = 0;

    public List<EntityAttribute> attribute = new List<EntityAttribute>();


    private void Awake()
    {
        entityLevel = 1;
    }

    public int GetAttributeCostByLevel(int level)
    {
        return Utilities.Round(upgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, level - 1));
    }

    public EntityAttribute GetAttributeByType(EntityAttribute.eAttributeType type)
    {        
        foreach (EntityAttribute upgrade in attribute)
        {
            if (upgrade.attributeType == type)
                return upgrade;
        }

        return null;
    }

}
