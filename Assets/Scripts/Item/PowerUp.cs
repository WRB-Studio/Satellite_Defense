using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum enumItemType { hearth, fireRate, shootUpgrade, coin, jumpLaser }

    public enumItemType itemType;

    [Header("Firerate settings")]
    public float fireRateUpgradeHeight = 0.1f;

    [Header("Coin settings")]
    public long scorePerCoin = 2500;
    public int addPremiumCoins = 1;

    [Header("Audio")]
    public AudioClip soundFireRate;
    public AudioClip soundShootUpgrade;
    public AudioClip soundCoin;
    public AudioClip soundJumpLaser;


    public void onTouch()
    {
        switch (itemType)
        {
            case enumItemType.hearth:
                GameController.Instance.ChangeLife(1);
                break;
            case enumItemType.fireRate:
                AudioController.PlaySound(soundFireRate);
                GameController.Instance.instantiatedWeapon.GetComponent<Weapon>().FireRateUpgrade(fireRateUpgradeHeight);
                break;
            case enumItemType.shootUpgrade:
                AudioController.PlaySound(soundShootUpgrade);
                GameController.Instance.instantiatedWeapon.GetComponent<Weapon>().shootUpgrade();
                break;
            case enumItemType.coin:
                AudioController.PlaySound(soundCoin);
                long addScore = scorePerCoin;
                ScoreController.Instance.AddScore(addScore);

                int addPremiumCoinUpgrade = addPremiumCoins + (int)GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.BonusCoinValue);
                addPremiumCoins = addPremiumCoinUpgrade <= 0 ? 1 : addPremiumCoinUpgrade;
                PremiumCoinController.Instance.AddPremiumCoins(addPremiumCoins, transform.position);
                break;
            case enumItemType.jumpLaser:
                AudioController.PlaySound(soundJumpLaser);
                GameController.Instance.instantiatedWeapon.GetComponent<Weapon>().setJumpLaser();
                break;
            default:
                break;
        }

        Destroy(gameObject);
    }

    private void OnMouseDown()
    {
        if (!GameController.GetIsPause() && !GameController.GetIsGameOver())
            onTouch();
    }

    private void OnDestroy()
    {
        PowerUpController.Instance.RemoveItem(this);
    }

}
