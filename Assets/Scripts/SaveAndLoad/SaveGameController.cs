using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveGameController : MonoBehaviour
{
    private static string key = "sattelite_defense_savegame";

    public static Savegame savegame;


    public static void SaveBestScore(long score)
    {
        savegame.bestScore = score;
        Save();
    }

    public static void SavePremiumCoins(long coins)
    {
        savegame.premiumCoins = coins;
        Save();
    }

    public static void SaveActiveItem(IngameEntity.eEntityType category, int itemID)
    {
        switch (category)
        {
            case IngameEntity.eEntityType.Planet:
                savegame.activePlanetID = itemID;
                break;
            case IngameEntity.eEntityType.Weapon:
                savegame.activeWeaponID = itemID;
                break;
            case IngameEntity.eEntityType.Background:
                savegame.activeBackgroundID = itemID;
                break;
            case IngameEntity.eEntityType.Enemy:
                savegame.activeEnemyTypeID = itemID;
                break;
        }

        Save();
    }

    public static void SaveUnlockedItem(IngameEntity.eEntityType category, int itemID, int level)
    {
        List<EntityLevelEntry> list = null;

        switch (category)
        {
            case IngameEntity.eEntityType.Planet:
                savegame.unlockedPlanetIDs.Add(itemID);
                list = savegame.planetLevels;
                break;
            case IngameEntity.eEntityType.Weapon:
                savegame.unlockedWeaponIDs.Add(itemID);
                list = savegame.weaponLevels;
                break;
            case IngameEntity.eEntityType.Background:
                savegame.unlockedBackgroundIDs.Add(itemID);
                list = savegame.backgroundLevels;
                break;
            case IngameEntity.eEntityType.Enemy:
                savegame.unlockedEnemyTypeIDs.Add(itemID);
                list = savegame.enemyTypeLevels;
                break;
        }

        if (list != null)
        {
            var entry = list.Find(e => e.id == itemID);
            if (entry == null)
                list.Add(new EntityLevelEntry(itemID, level));
            else
                entry.level = level;
        }

        Save();
    }



    public static void Save()
    {
        string json = JsonUtility.ToJson(savegame);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        if (!PlayerPrefs.HasKey(key))
        {
            savegame = new Savegame();
            Save();
            return;
        }

        string json = PlayerPrefs.GetString(key);
        savegame = JsonUtility.FromJson<Savegame>(json);

        if (savegame.planetLevels == null) savegame.planetLevels = new();
        if (savegame.weaponLevels == null) savegame.weaponLevels = new();
        if (savegame.backgroundLevels == null) savegame.backgroundLevels = new();
        if (savegame.enemyTypeLevels == null) savegame.enemyTypeLevels = new();

        void EnsureEntry(List<EntityLevelEntry> l, int id, int lvl)
        {
            if (l.Find(e => e.id == id) == null) l.Add(new EntityLevelEntry(id, lvl));
        }
        EnsureEntry(savegame.planetLevels, 1, 1);
        EnsureEntry(savegame.weaponLevels, 1, 1);
        EnsureEntry(savegame.backgroundLevels, 1, 1);
        EnsureEntry(savegame.enemyTypeLevels, 1, 1);
    }


    public static void RemoveSaveGame()
    {
        PlayerPrefs.DeleteAll();
    }




    private static List<EntityLevelEntry> GetLevelList(IngameEntity.eEntityType category)
    {
        switch (category)
        {
            case IngameEntity.eEntityType.Planet: return savegame.planetLevels;
            case IngameEntity.eEntityType.Weapon: return savegame.weaponLevels;
            case IngameEntity.eEntityType.Background: return savegame.backgroundLevels;
            case IngameEntity.eEntityType.Enemy: return savegame.enemyTypeLevels;
            default: return savegame.weaponLevels; // Fallback
        }
    }

    public static int GetEntityLevel(IngameEntity.eEntityType category, int itemID)
    {
        var list = GetLevelList(category);
        var entry = list.FirstOrDefault(e => e.id == itemID);
        return entry != null ? Mathf.Max(1, entry.level) : 1;
    }

    public static void SaveEntityLevel(IngameEntity.eEntityType category, int itemID, int level)
    {
        var list = GetLevelList(category);
        var entry = list.FirstOrDefault(e => e.id == itemID);
        if (entry == null)
            list.Add(new EntityLevelEntry(itemID, Mathf.Max(0, level)));
        else
            entry.level = Mathf.Max(0, level);

        Save();
    }

}
