using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [Header("Attributes")]
    public int hitPoints = 5;
    private int curHitpoints;
    public float moveSpeed = 3;
    public float rotationSpeed = 1;
    public bool randomRotation = true;
    public int scoreGain = 1;

    [Header("Item drops")]
    public float dropChance = 0.2f;
    public float itemLifeTime = 5;
    public GameObject itemHeart;
    public GameObject itemCoin;
    public GameObject itemFireRate;
    public GameObject itemShootUpgrade;
    public GameObject itemJumpLaser;

    [Header("Animations")]
    public GameObject deathExplosion;
    public GameObject bulletExplosion;

    [Header("Sounds")]
    public AudioClip soundHit;
    public AudioClip soundDeath;
    public AudioClip soundItemDrop;
    public AudioClip soundPlanetHit;


    private GameHandler ghScript;

    public enum HitType
    {
        none, Planet, Laser
    }


    void Start()
    {
        if (randomRotation)
        {
            rotationSpeed = Random.Range(rotationSpeed - 2, rotationSpeed + 3);
            if (Random.value > 0.5f)
                rotationSpeed = -rotationSpeed;
        }

        ghScript = GameObject.Find("GameHandler").GetComponent<GameHandler>();
    }

    public void onUpdate()
    {
        transform.position = Vector2.MoveTowards(transform.position, Vector2.zero, Time.fixedDeltaTime * moveSpeed);
        if (transform.position.x <= -10)
            Destroy(gameObject);

        transform.Rotate(0, 0, rotationSpeed);
    }

    public void hit(int damage, HitType currentHitType)
    {
        if (damage > 0)
        {
            curHitpoints -= damage;
        }
        else
        {
            StaticAudioHandler.playSound(soundDeath);
            Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
            if (transform.Find("Trail") != null)
            {
                Destroy(transform.Find("Trail").gameObject, 3);
                transform.Find("Trail").parent = null;
            }
            Destroy(gameObject);
            return;
        }

        if (curHitpoints <= 0)
        {
            StaticAudioHandler.playSound(soundDeath);

            //multiplie score gain with current move speed (faster meteors, more points)
            long tmpScoreGainMulti = (long)(moveSpeed * (moveSpeed * 3));
            if (tmpScoreGainMulti < 1)
                tmpScoreGainMulti = 1;
            long newScore = scoreGain * tmpScoreGainMulti;
            ghScript.scoreScrp.addScore(newScore);

            Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);

            spawnRandomItem();

            if (transform.Find("Trail") != null)
            {
                Destroy(transform.Find("Trail").gameObject, 3);
                transform.Find("Trail").parent = null;
            }

            if (currentHitType == HitType.Laser)
                ghScript.addKill();

            Destroy(gameObject);
        }
        else
        {
            StaticAudioHandler.playSound(soundHit, 2.5f);
            if (bulletExplosion != null)
                Destroy(Instantiate(bulletExplosion, transform.position, deathExplosion.transform.rotation, transform), 2);
        }
    }

    private void spawnRandomItem()
    {
        if (ghScript.getInMainMenue() || !checkIsInCameraView(transform, 0.5f))
            return;

        float randVal = Random.value;
        if (randVal <= dropChance) //% drop chance by enemy death 
        {
            if (ghScript.activWeapon == null)
                return;
            Weapon weaponScrp = ghScript.activWeapon.GetComponent<Weapon>();

            randVal = Random.value;
            if ((randVal < 0.40f) &&
                (ghScript.currentLifes < ghScript.maxLives) &&
                (!checkItemExist(Item.enumItemType.hearth)))
            {
                StaticAudioHandler.playSound(soundItemDrop);
                Destroy(Instantiate(itemHeart, transform.position, itemHeart.transform.rotation), itemLifeTime);
            }
            else if ((randVal >= 0.40f && randVal <= 0.75f) &&
                     (weaponScrp.changeFireRate > weaponScrp.minFireRate) &&
                     (!checkItemExist(Item.enumItemType.fireRate)))
            {
                StaticAudioHandler.playSound(soundItemDrop);
                Destroy(Instantiate(itemFireRate, transform.position, itemFireRate.transform.rotation), itemLifeTime);
            }
            else if ((randVal > 0.75f && randVal <= 0.90f) &&
                     (!checkItemExist(Item.enumItemType.coin)))
            {
                StaticAudioHandler.playSound(soundItemDrop);
                Destroy(Instantiate(itemCoin, transform.position, itemCoin.transform.rotation), itemLifeTime);
            }
            else if ((randVal > 0.90f) &&
                     (!weaponScrp.jumpLaserActive) &&
                     (!checkItemExist(Item.enumItemType.jumpLaser)))
            {
                StaticAudioHandler.playSound(soundItemDrop);
                Destroy(Instantiate(itemJumpLaser, transform.position, itemCoin.transform.rotation), itemLifeTime);
            }
        }
    }

    private bool checkIsInCameraView(Transform target, float padding)
    {
        float height = Camera.main.orthographicSize;
        float width = Camera.main.orthographicSize * Camera.main.aspect;

        if (width - padding > target.position.x && -width + padding < target.position.x &&
            height - padding > target.position.y && -height + padding < target.position.y)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool checkItemExist(Item.enumItemType itemType)
    {
        foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item"))
        {
            if (item.GetComponent<Item>().itemType.Equals(itemType))
                return true;
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.tag == "Planet")
        {
            if (!ghScript.getInMainMenue())
                collision.transform.GetComponent<Planet>().hit();

            StaticAudioHandler.playSound(soundPlanetHit);
            Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
            Destroy(gameObject);
        }
    }

}
