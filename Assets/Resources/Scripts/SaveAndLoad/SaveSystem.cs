using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

public static class SaveSystem
{   
    public static void saveAll()
    {
        GameHandler ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();
        Score scoreScrp = GameObject.Find("Score").GetComponent<Score>();
        PremiumCoins premiumCoinsScrp = GameObject.Find("PremiumCoins").GetComponent<PremiumCoins>();
        Shop shopScrp = GameObject.Find("Shop").GetComponent<Shop>();
        PlayCloudDataManager playCloudManagerScrp = GameObject.Find("GooglePlayService").GetComponent<PlayCloudDataManager>();

        playCloudManagerScrp.CurSavegame.bestScore = scoreScrp.getBestScore();
        playCloudManagerScrp.CurSavegame.premiumCoins = premiumCoinsScrp.getPremiumCoins();

        List<int> unlockedPlanetIDs = new List<int>();
        foreach (GameObject planet in shopScrp.planetPrefabs)
        {
            Planet curPlanetScrp = planet.GetComponent<Planet>();
            if (curPlanetScrp.unlocked)
                unlockedPlanetIDs.Add(curPlanetScrp.id);
        }
        playCloudManagerScrp.CurSavegame.unlockedPlanetIDs = unlockedPlanetIDs.ToArray();
        playCloudManagerScrp.CurSavegame.activePlanetID = shopScrp.getActivePlanet().GetComponent<Planet>().id;


        List<int> unlockedWeaponIDs = new List<int>();
        foreach (GameObject weapon in shopScrp.weaponPrefabs)
        {
            Weapon curWeaponScrp = weapon.GetComponent<Weapon>();
            if (curWeaponScrp.unlocked)
                unlockedWeaponIDs.Add(curWeaponScrp.id);
        }
        playCloudManagerScrp.CurSavegame.unlockedWeaponIDs = unlockedWeaponIDs.ToArray();
        playCloudManagerScrp.CurSavegame.activeWeaponID = shopScrp.getActiveWeapon().GetComponent<Weapon>().id;


        List<int> unlockedBackgroundIDs = new List<int>();
        foreach (GameObject background in shopScrp.backgroundPrefabs)
        {
            Background curBackgroundScrp = background.GetComponent<Background>();
            if (curBackgroundScrp.unlocked)
                unlockedBackgroundIDs.Add(curBackgroundScrp.id);
        }
        playCloudManagerScrp.CurSavegame.unlockedBackgroundIDs = unlockedBackgroundIDs.ToArray();
        playCloudManagerScrp.CurSavegame.activeBackgroundID = shopScrp.getActiveBackground().GetComponent<Background>().id;


        List<int> unlockedEnemyTypeIDs = new List<int>();
        foreach (GameObject enemyType in shopScrp.enemyTypePrefabs)
        {
            EnemyType curEnemyTypeScrp = enemyType.GetComponent<EnemyType>();
            if (curEnemyTypeScrp.unlocked)
                unlockedEnemyTypeIDs.Add(curEnemyTypeScrp.id);
        }
        playCloudManagerScrp.CurSavegame.unlockedEnemyTypeIDs = unlockedEnemyTypeIDs.ToArray();
        playCloudManagerScrp.CurSavegame.activeEnemyTypeID = shopScrp.getActiveEnemyType().GetComponent<EnemyType>().id;

        /*Savegame newSavegame = new Savegame(bestScore, premiumCoins,
                                         boughtPlanetIDs.ToArray(), activePlanet,
                                         boughtWeaponIDs.ToArray(), activeWeapon,
                                         boughtBackgroundIDs.ToArray(), activeBackground,
                                         boughtEnemyTypeIDs.ToArray(), activeEnemyType);

        ghScrp.playCloudManagerScrp.saveGame(newSavegame);*/

        playCloudManagerScrp.SaveToCloud();
    }
    
    public static void loadAll()
    {
        GameHandler ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();
        Score scoreScrp = GameObject.Find("Score").GetComponent<Score>();

        PremiumCoins premiumCoinsScrp = GameObject.Find("PremiumCoins").GetComponent<PremiumCoins>();
        Shop shopScrp = GameObject.Find("Shop").GetComponent<Shop>();
        PlayCloudDataManager playCloudManagerScrp = GameObject.Find("GooglePlayService").GetComponent<PlayCloudDataManager>();
        
        scoreScrp.playServiceSavegameLoad();
        premiumCoinsScrp.playServiceSavegameLoad();
        shopScrp.playServiceSavegameLoad();
    }

}
