
[System.Serializable]
public class Savegame
{
    public long bestScore;

    public long premiumCoins;

    public int[] unlockedPlanetIDs;
    public int activePlanetID;

    public int[] unlockedWeaponIDs;
    public int activeWeaponID;

    public int[] unlockedBackgroundIDs;
    public int activeBackgroundID;

    public int[] unlockedEnemyTypeIDs;
    public int activeEnemyTypeID;

    public Savegame()
    {
        bestScore = 0;
        premiumCoins = 0;

        unlockedPlanetIDs = new int[] { 0 };
        activePlanetID = 0;

        unlockedWeaponIDs = new int[] { 0 };
        activeWeaponID = 0;

        unlockedBackgroundIDs = new int[] { 0 };
        activeBackgroundID = 0;

        unlockedEnemyTypeIDs = new int[] { 0 };
        activeEnemyTypeID = 0;
    }

}
