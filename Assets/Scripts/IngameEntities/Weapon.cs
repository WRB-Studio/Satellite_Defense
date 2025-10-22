using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Weapon : IngameEntity
{
    [Header("----------------------------------------")]

    private float rotationSpeed = 1f;
    public bool rotateSmooth = false;

    [Header("Prefabs & Projectiles")]
    public GameObject normalLaserPrefab;
    public GameObject jumpLaserPrefab;

    [Header("Projectile Settings")]
    public Color normalLaserColor;
    public float jumpLaserDuration = 5f;
    public bool jumpLaserActive = false;
    private float projectileSpeed = 1;
    private int damage = 1;

    [Header("Firing Attributes")]
    [HideInInspector] public float fireRate = 0.1f;
    public float minFireRate = 0.1f;
    public float fireRatePowerUpped = 0.1f;
    private float fireRateCountDown = 0f;

    [Header("Weapon Progression")]
    private int level = 1;
    public int maxShootUpgrades = 6;
    public int curShootUpgrade = 1;

    [Header("Visuals")]
    public GameObject deathExplosion;

    private Transform[] allLaserEmitter;
    private List<Transform> activeEmitters = new List<Transform>();

    private static List<Bullet> instantiatedBullets = new List<Bullet>();
    private Coroutine jumpLaserRoutine = null;


    private void OnValidate()
    {
        /*
         SpriteRenderer sr = transform.Find("SatelliteModel").GetComponent<SpriteRenderer>();
        if(sr.sprite != weaponSprite)
            sr.sprite = weaponSprite;

        sr.color = Color.white;

        sr.transform.localScale = new Vector3 (weaponScale, weaponScale, weaponScale);

        sr.transform.localPosition = new Vector3(0, weaponPosY, 0);
         */
    }

    public void Init()
    {
        allLaserEmitter = transform.Find("LaserEmitterGrp").Cast<Transform>().ToArray();

        ChangeWeaponLevel(0);

        rotationSpeed = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.WeaponRotationSpeed);

        fireRate = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.WeaponFireRate);

        projectileSpeed = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.WeaponProjectileSpeed);

        damage = (int)GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.WeaponDamage);
    }

    private void FixedUpdate()
    {
        if (GameController.GetIsPause())
            return;

        if (GameController.Instance.enableJoystickControll && GameController.Instance.joystickGO.activeSelf)
        {
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                LookAtJoystickDirection();
                shoot();
            }
        }
        else
        {
            if ((Input.GetMouseButton(0) || Input.touchCount > 0) &&
                ((!UIMainMenu.Instance.mainMenuPanel.activeSelf && !Utilities.isPointerOverUIElement()) ||
                (UIMainMenu.Instance.mainMenuPanel.activeSelf)))
            {
                if (!GameController.GetIsGameOver() && !UIShopMenu.Instance.shopMenuPanel.activeSelf)
                {
                    LookAtClickOrTouch();
                    shoot();
                }
            }
        }

        //bullet moving
        foreach (Bullet bullet in instantiatedBullets.ToArray())
        {
            if (bullet == null)
            {
                instantiatedBullets.Remove(bullet);
                continue;
            }
            bullet.OnFixedUpdate();
        }
    }

    private void OnDestroy()
    {
        foreach (Bullet bullet in instantiatedBullets.ToArray())
            if (bullet != null) RemoveBullet(bullet);
    }


    public void ChangeWeaponLevel(int value)
    {
        level = Mathf.Clamp(level + value, 1, 3);

        activeEmitters.Clear();
        foreach (var item in allLaserEmitter)
        {
            if (item.name.Contains(level.ToString()))
                activeEmitters.Add(item);
        }
    }



    private void LookAtJoystickDirection()
    {
        float heading = Mathf.Atan2(-GameController.Instance.joystick.Horizontal * 50f, GameController.Instance.joystick.Vertical * 50f);
        Quaternion newRotation = Quaternion.Euler(0f, 0f, heading * Mathf.Rad2Deg);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.fixedDeltaTime * rotationSpeed);
    }

    private void LookAtClickOrTouch()
    {
        Vector3 inputPos = (Application.platform == RuntimePlatform.Android && Input.touchCount > 0)
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        inputPos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
        Quaternion targetRot = Quaternion.Euler(0f, 0f, Mathf.Atan2(worldPos.y, worldPos.x) * Mathf.Rad2Deg - 90f);

        if (rotateSmooth)
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        else
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime * 50f);
    }



    private void shoot()
    {
        if (Time.time >= fireRateCountDown)
        {
            fireRateCountDown = Time.time + fireRate;

            InstantiateBullet();
        }
    }

    public void DestroyWeapon()
    {
        Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
        Destroy(gameObject, 0.25f);
    }


    public void FireRateUpgrade(float upgrade)
    {
        fireRate = Mathf.Max(fireRatePowerUpped - upgrade, minFireRate);
    }


    public void shootUpgrade()
    {
        if (curShootUpgrade < maxShootUpgrades)
            curShootUpgrade++;
    }

    public void setJumpLaser()
    {
        if (jumpLaserRoutine != null) StopCoroutine(jumpLaserRoutine);
        jumpLaserRoutine = StartCoroutine(activateJumpLaser());
    }

    private IEnumerator activateJumpLaser()
    {
        jumpLaserActive = true;
        yield return new WaitForSeconds(jumpLaserDuration);
        jumpLaserActive = false;
        jumpLaserRoutine = null;
    }


    private void InstantiateBullet()
    {
        GameObject chooseLaser = jumpLaserActive ? jumpLaserPrefab : normalLaserPrefab;

        bool PlaySound = true;

        foreach (Transform activeEmitter in activeEmitters)
        {
            GameObject bullet = Instantiate(chooseLaser, activeEmitter.position, activeEmitter.transform.rotation);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            Vector2 shootDirection = activeEmitter.up;

            bulletScript.initLaser(shootDirection, jumpLaserActive, normalLaserColor, PlaySound, damage, projectileSpeed);
            instantiatedBullets.Add(bulletScript);

            if (jumpLaserActive)
                break;

            PlaySound = false;
        }
    }

    public static void RemoveBullet(Bullet bullet)
    {
        instantiatedBullets.Remove(bullet);
        Destroy(bullet.gameObject);
    }

}

