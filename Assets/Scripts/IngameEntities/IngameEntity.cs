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
    public bool HasUpgrades => attribute != null && attribute.Count > 0;

    public bool CanUpgrade(int level) => HasUpgrades && level < maxEntityLevel;

    public long GetPurchaseCost(IReadOnlySavegame progress) => progress.IsUnlocked(entityType, id)
        ? GetUpgradeCost(progress.GetLevel(entityType, id, maxEntityLevel))
        : cost;

    public bool CanPurchase(IReadOnlySavegame progress)
    {
        if (id <= 0 || entityType == eEntityType.None) return false;
        if (progress.IsUnlocked(entityType, id) && !CanUpgrade(progress.GetLevel(entityType, id, maxEntityLevel))) return false;
        long price = GetPurchaseCost(progress);
        return price >= 0 && progress.Coins >= price;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        double price = Math.Max(0, upgradeBaseCost) * Math.Pow(Math.Max(0.01, upgradeCostMultiplier), Math.Max(0, currentLevel - 1));
        return (int)Math.Min(int.MaxValue, Math.Round(price, MidpointRounding.AwayFromZero));
    }

    // Retained for existing editor tools and validation callers.
    public int GetAttributeCostByLevel(int level) => GetUpgradeCost(level);

    public EntityAttribute GetAttributeByType(EntityAttribute.eAttributeType type) =>
        attribute?.Find(value => value != null && value.attributeType == type);
}
