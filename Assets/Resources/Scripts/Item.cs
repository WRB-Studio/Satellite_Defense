using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
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

    private GameHandler ghScript;

    public enum enumItemType
    {
        hearth, fireRate, shootUpgrade, coin, jumpLaser
    }



    private void Start()
    {
        ghScript = GameObject.Find("GameHandler").GetComponent<GameHandler>();
    }

    public void onTouch()
    {
        switch (itemType)
        {
            case enumItemType.hearth:
                ghScript.changeLife(true);
                break;
            case enumItemType.fireRate:
                StaticAudioHandler.playSound(soundFireRate);
                ghScript.activWeapon.GetComponent<Weapon>().fireRateUpgrade(fireRateUpgradeHeight);
                break;
            case enumItemType.shootUpgrade:
                StaticAudioHandler.playSound(soundShootUpgrade);
                ghScript.activWeapon.GetComponent<Weapon>().shootUpgrade();
                break;
            case enumItemType.coin:
                StaticAudioHandler.playSound(soundCoin);
                long addScore = scorePerCoin;
                ghScript.scoreScrp.addScore(addScore);
                ghScript.premiumCoinsScrp.addPremiumCoins(addPremiumCoins, transform.position);
                break;
            case enumItemType.jumpLaser:
                StaticAudioHandler.playSound(soundJumpLaser);
                ghScript.activWeapon.GetComponent<Weapon>().setJumpLaser();
                break;
            default:
                break;
        }

        Destroy(gameObject);
    }

    private void OnMouseDown()
    {
        if (!ghScript.getIsPause() && !ghScript.getIsGameOver())
            onTouch();
    }

}
