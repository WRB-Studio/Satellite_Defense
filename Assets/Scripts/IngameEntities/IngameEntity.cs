using System;
using System.Collections.Generic;
using UnityEngine;

public class IngameEntity : MonoBehaviour
{
    public enum eEntityType { None, Planet, Weapon, Enemy, Background }

    public eEntityType entityType;
    [Header("Shop")]
    public int id;
    public string itemName = "unnamed";
    public long cost;
    [Header("Upgrades")]
    [Min(1)] public int maxEntityLevel = 3;
    [Min(0)] public int upgradeBaseCost = 100;
    [Min(0.01f)] public float upgradeCostMultiplier = 1f;
    public List<EntityAttribute> attribute = new List<EntityAttribute>();

    public int Level => SaveGameController.Data.GetLevel(entityType, id, maxEntityLevel);
    public bool IsUnlocked => SaveGameController.Data.IsUnlocked(entityType, id);
    public bool IsActive => SaveGameController.Data.GetActiveId(entityType) == id;

    public int GetAttributeCostByLevel(int level)
    {
        double price = Math.Max(0, upgradeBaseCost) * Math.Pow(Math.Max(0.01, upgradeCostMultiplier), Math.Max(0, level - 1));
        return (int)Math.Min(int.MaxValue, Math.Round(price, MidpointRounding.AwayFromZero));
    }

    public EntityAttribute GetAttributeByType(EntityAttribute.eAttributeType type) =>
        attribute.Find(value => value != null && value.attributeType == type);
}
