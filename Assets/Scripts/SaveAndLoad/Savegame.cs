using System;
using System.Collections.Generic;
using UnityEngine;

public interface IReadOnlySavegame
{
    long BestScore { get; }
    long Coins { get; }
    bool IsUnlocked(IngameEntity.eEntityType category, int id);
    int GetLevel(IngameEntity.eEntityType category, int id, int maxLevel);
    int GetActiveId(IngameEntity.eEntityType category);
}

[Serializable]
public class Savegame : IReadOnlySavegame
{
    public const int CurrentVersion = 1;
    public int version = CurrentVersion;
    public long bestScore;
    public long premiumCoins;
    public long BestScore => bestScore;
    public long Coins => premiumCoins;
    public List<int> unlockedPlanetIDs = new List<int> { 1 };
    public int activePlanetID = 1;
    public List<int> unlockedWeaponIDs = new List<int> { 1 };
    public int activeWeaponID = 1;
    public List<int> unlockedBackgroundIDs = new List<int> { 1 };
    public int activeBackgroundID = 1;
    public List<int> unlockedEnemyTypeIDs = new List<int> { 1 };
    public int activeEnemyTypeID = 1;
    public List<EntityLevelEntry> planetLevels = new List<EntityLevelEntry>();
    public List<EntityLevelEntry> weaponLevels = new List<EntityLevelEntry>();
    public List<EntityLevelEntry> backgroundLevels = new List<EntityLevelEntry>();
    public List<EntityLevelEntry> enemyTypeLevels = new List<EntityLevelEntry>();

    public Savegame Clone() => JsonUtility.FromJson<Savegame>(JsonUtility.ToJson(this));

    public bool IsValid() => version == CurrentVersion && bestScore >= 0 && premiumCoins >= 0 &&
        IsCategoryValid(unlockedPlanetIDs, planetLevels, activePlanetID) &&
        IsCategoryValid(unlockedWeaponIDs, weaponLevels, activeWeaponID) &&
        IsCategoryValid(unlockedBackgroundIDs, backgroundLevels, activeBackgroundID) &&
        IsCategoryValid(unlockedEnemyTypeIDs, enemyTypeLevels, activeEnemyTypeID);

    private static bool IsCategoryValid(List<int> unlocked, List<EntityLevelEntry> levels, int active)
    {
        if (unlocked == null || levels == null || !unlocked.Contains(1) || !unlocked.Contains(active)) return false;
        var ids = new HashSet<int>();
        foreach (int id in unlocked)
            if (id <= 0 || !ids.Add(id)) return false;
        ids.Clear();
        foreach (var entry in levels)
            if (entry == null || entry.id <= 0 || entry.level < 1 || !ids.Add(entry.id)) return false;
        return true;
    }

    public List<int> GetUnlockedIds(IngameEntity.eEntityType category) => category switch
    {
        IngameEntity.eEntityType.Planet => unlockedPlanetIDs,
        IngameEntity.eEntityType.Weapon => unlockedWeaponIDs,
        IngameEntity.eEntityType.Background => unlockedBackgroundIDs,
        IngameEntity.eEntityType.Enemy => unlockedEnemyTypeIDs,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public List<EntityLevelEntry> GetLevels(IngameEntity.eEntityType category) => category switch
    {
        IngameEntity.eEntityType.Planet => planetLevels,
        IngameEntity.eEntityType.Weapon => weaponLevels,
        IngameEntity.eEntityType.Background => backgroundLevels,
        IngameEntity.eEntityType.Enemy => enemyTypeLevels,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public int GetActiveId(IngameEntity.eEntityType category) => category switch
    {
        IngameEntity.eEntityType.Planet => activePlanetID,
        IngameEntity.eEntityType.Weapon => activeWeaponID,
        IngameEntity.eEntityType.Background => activeBackgroundID,
        IngameEntity.eEntityType.Enemy => activeEnemyTypeID,
        _ => 0
    };

    public bool IsUnlocked(IngameEntity.eEntityType category, int id) =>
        category != IngameEntity.eEntityType.None && GetUnlockedIds(category).Contains(id);

    public int GetLevel(IngameEntity.eEntityType category, int id, int maxLevel)
    {
        if (category == IngameEntity.eEntityType.None) return 1;
        var entry = GetLevels(category).Find(value => value.id == id);
        return Mathf.Clamp(entry?.level ?? 1, 1, Math.Max(1, maxLevel));
    }

    public bool Select(IngameEntity.eEntityType category, int id)
    {
        if (!IsUnlocked(category, id)) return false;
        switch (category)
        {
            case IngameEntity.eEntityType.Planet: activePlanetID = id; break;
            case IngameEntity.eEntityType.Weapon: activeWeaponID = id; break;
            case IngameEntity.eEntityType.Background: activeBackgroundID = id; break;
            case IngameEntity.eEntityType.Enemy: activeEnemyTypeID = id; break;
            default: return false;
        }
        return true;
    }

    public bool TryPurchase(IngameEntity item)
    {
        if (item == null || !item.CanPurchase(this)) return false;
        bool owned = IsUnlocked(item.entityType, item.id);
        int level = GetLevel(item.entityType, item.id, item.maxEntityLevel);
        long price = item.GetPurchaseCost(this);

        premiumCoins -= price;
        if (!owned) GetUnlockedIds(item.entityType).Add(item.id);
        var levels = GetLevels(item.entityType);
        var entry = levels.Find(value => value.id == item.id);
        int purchasedLevel = owned ? level + 1 : 1;
        if (entry == null) levels.Add(new EntityLevelEntry(item.id, purchasedLevel));
        else entry.level = purchasedLevel;
        return true;
    }

    public long CreditCoins(long amount)
    {
        long credited = Math.Min(Math.Max(0, amount), long.MaxValue - premiumCoins);
        premiumCoins += credited;
        return credited;
    }
}

[Serializable]
public class EntityLevelEntry
{
    public int id;
    public int level = 1;

    public EntityLevelEntry(int id, int level)
    {
        this.id = id;
        this.level = level;
    }
}
