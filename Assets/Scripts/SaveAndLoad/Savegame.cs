
using System.Collections.Generic;

[System.Serializable]
public class Savegame
{
    public long bestScore;

    public long premiumCoins;

    public List<int> unlockedPlanetIDs;
    public int activePlanetID;

    public List<int> unlockedWeaponIDs;
    public int activeWeaponID;

    public List<int> unlockedBackgroundIDs;
    public int activeBackgroundID;

    public List<int> unlockedEnemyTypeIDs;
    public int activeEnemyTypeID;

    public List<EntityLevelEntry> planetLevels = new List<EntityLevelEntry>();
    public List<EntityLevelEntry> weaponLevels = new List<EntityLevelEntry>();
    public List<EntityLevelEntry> backgroundLevels = new List<EntityLevelEntry>();
    public List<EntityLevelEntry> enemyTypeLevels = new List<EntityLevelEntry>();

    public Savegame()
    {
        bestScore = 0;
        premiumCoins = 0;

        unlockedPlanetIDs = new List<int> { 1 };
        activePlanetID = 1;

        unlockedWeaponIDs = new List<int> { 1 };
        activeWeaponID = 1;

        unlockedBackgroundIDs = new List<int> { 1 };
        activeBackgroundID = 1;

        unlockedEnemyTypeIDs = new List<int> { 1 };
        activeEnemyTypeID = 1;

        planetLevels.Add(new EntityLevelEntry(1, 1));
        weaponLevels.Add(new EntityLevelEntry(1, 1));
        backgroundLevels.Add(new EntityLevelEntry(1, 1));
        enemyTypeLevels.Add(new EntityLevelEntry(1, 1));
    }

}


[System.Serializable]
public class EntityLevelEntry
{
    public int id;
    public int level = 1;
    public EntityLevelEntry(int id, int level) { this.id = id; this.level = level; }
}
