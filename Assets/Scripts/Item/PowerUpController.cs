using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    public static PowerUpController Instance;

    public Transform spawnParent;

    [Header("Item drops")]
    public float dropChance = 0.2f;
    public float itemLifeTime = 5;
    public GameObject itemHeart;
    public GameObject itemCoin;
    public GameObject itemFireRate;
    public GameObject itemShootUpgrade;
    public GameObject itemJumpLaser;

    private List<PowerUp> instantiatedItems = new List<PowerUp>();


    private void Awake()
    {
        Instance = this;
    }

    public void SpawnRandomItem(Vector2 spawnPosition)
    {
        if (UIMainMenu.Instance.mainMenuPanel.activeSelf) return;
        if (!Utilities.IsInsideViewWithPadding(spawnPosition, 0.5f)) return;
        if (Random.value > dropChance) return;

        var weapon = GameController.Instance.instantiatedWeapon.GetComponent<Weapon>();

        // --- Gewichte (Basis) ---
        float wHeart = 0.40f;
        float wFire = 0.35f;
        float wCoin = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.CoinChance);
        float wJump = 0.10f;

        // --- CoinChance-Upgrade einarbeiten ---
        float coinChanceUpgrade = GameController.Instance
            .GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.CoinChance); // z.B. +0.10
        wCoin = Mathf.Max(0f, wCoin + coinChanceUpgrade); // negativ zulassen, aber nicht < 0

        // Verfügbarkeit/Constraints: wenn nicht sinnvoll -> Gewicht = 0
        if (!(GameController.Instance.currentLifes < GameController.Instance.GetMaxLives() &&
              !checkItemExist(PowerUp.enumItemType.hearth)))
            wHeart = 0f;

        if (!(weapon.fireRate > weapon.minFireRate &&
              !checkItemExist(PowerUp.enumItemType.fireRate)))
            wFire = 0f;

        if (checkItemExist(PowerUp.enumItemType.coin))
            wCoin = 0f;

        if (!(!weapon.jumpLaserActive && !checkItemExist(PowerUp.enumItemType.jumpLaser)))
            wJump = 0f;

        // Nichts zu droppen?
        float wSum = wHeart + wFire + wCoin + wJump;
        if (wSum <= 0f) return;

        // Weighted Pick
        float r = Random.value * wSum;
        GameObject prefab;
        if ((r -= wHeart) <= 0f) prefab = itemHeart;
        else if ((r -= wFire) <= 0f) prefab = itemFireRate;
        else if ((r -= wCoin) <= 0f) prefab = itemCoin;
        else prefab = itemJumpLaser;

        // Drop + SFX
        var sfx = prefab == itemJumpLaser ? AudioController.Instance.soundPlanetDeath
                                          : AudioController.Instance.soundItemDrop;
        AudioController.PlaySound(sfx);

        var newItem = Instantiate(prefab, spawnPosition, prefab.transform.rotation, spawnParent);
        instantiatedItems.Add(newItem.GetComponent<PowerUp>());
        Destroy(newItem, itemLifeTime);
    }


    //public void SpawnRandomItem(Vector2 spawnPosition)
    //{
    //    if (UIMainMenu.Instance.mainMenuPanel.activeSelf) return;
    //    if (!Utilities.CheckIsInCameraView(spawnPosition, 0.5f)) return;
    //    if (Random.value > dropChance) return;

    //    var weapon = GameController.Instance.instantiatedWeapon.GetComponent<Weapon>();
    //    GameObject prefab = null;
    //    float r = Random.value;

    //    float coinChanceUpgrade = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityUpgrade.eUpgradeType.CoinChance);

    //    if (r < 0.40f &&
    //        GameController.Instance.currentLifes < GameController.Instance.GetMaxLives() &&
    //        !checkItemExist(PowerUp.enumItemType.hearth))
    //    {
    //        prefab = itemHeart;
    //    }
    //    else if (r < 0.75f &&
    //             weapon.changeFireRate > weapon.minFireRate &&
    //             !checkItemExist(PowerUp.enumItemType.fireRate))
    //    {
    //        prefab = itemFireRate;
    //    }
    //    else if (r < 0.90f && !checkItemExist(PowerUp.enumItemType.coin))
    //    {
    //        prefab = itemCoin;
    //    }
    //    else if (!weapon.jumpLaserActive &&
    //             !checkItemExist(PowerUp.enumItemType.jumpLaser))
    //    {
    //        prefab = itemJumpLaser;
    //    }

    //    if (prefab != null)
    //    {
    //        var sfx = prefab == itemJumpLaser
    //            ? AudioController.Instance.soundPlanetDeath
    //            : AudioController.Instance.soundItemDrop;
    //        AudioController.PlaySound(sfx);

    //        var newItem = Instantiate(prefab, spawnPosition, prefab.transform.rotation, spawnParent);
    //        instantiatedItems.Add(newItem.GetComponent<PowerUp>());
    //        Destroy(newItem, itemLifeTime);
    //    }
    //}

    private bool checkItemExist(PowerUp.enumItemType itemType)
    {
        foreach (PowerUp item in instantiatedItems)
        {
            if (item.itemType.Equals(itemType))
                return true;
        }
        return false;
    }

    public void RemoveAllItems()
    {
        foreach (PowerUp item in instantiatedItems.ToArray())
            RemoveItem(item);
    }

    public void RemoveItem(PowerUp item)
    {
        instantiatedItems.Remove(item);
        Destroy(item.gameObject);
    }

}
