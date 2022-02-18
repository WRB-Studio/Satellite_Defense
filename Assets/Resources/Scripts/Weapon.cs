using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Shop infos")]
    public int id = 0;
    public string weaponName = "unnamed";
    public long cost = 0;
    public bool unlocked = false;
    public bool active = false;

    [Header("Laser types")]
    public GameObject normalLaserPrefab;
    public GameObject jumpLaserPrefab;
    public Color normalLaserColor;

    [Header("Attributes")]
    public Transform[] laserEmitter;
    public int activeEmitters = 1;
    public float rotationSpeed = 1f;

    [Header("Weapon attributes")]
    public float startFireRate = 0.1f;
    public float minFireRate = 0.02f;
    public float changeFireRate = 0.1f;
    private float fireRateCountDown = 0;
    public int maxShootUpgrades = 6;
    public int curShootUpgrade = 1;
    public float jumpLaserDuration = 5;
    public bool jumpLaserActive = false;

    [Header("Animations")]
    public GameObject laserLoading;
    public GameObject deathExplosion;

    private List<GameObject> instantiatedBullets = new List<GameObject>();

    private GameHandler ghScript;



    void Start()
    {
        ghScript = GameObject.Find("GameHandler").GetComponent<GameHandler>();
        changeFireRate = startFireRate;
    }

    private void FixedUpdate()
    {
        if (ghScript.getIsPause())
            return;

        if (ghScript.enableJoystickControll && ghScript.joystickGO.activeSelf)
        {
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                lookAtJoystickDirection();
                shoot();
            }
        }
        else
        {
            if ((Input.GetMouseButton(0) || Input.touchCount > 0) && ((!ghScript.getInMainMenue() && !Utilities.isPointerOverUIElement()) || (ghScript.getInMainMenue())))
            {
                if (!ghScript.getIsGameOver() && !ghScript.shopScrp.shopMainGO.activeSelf)
                {
                    lookAtClickOrTouch();
                    shoot();
                }
            }
        }

        //bullet moving
        foreach (GameObject bullet in instantiatedBullets.ToArray())
        {
            if (bullet == null)
            {
                instantiatedBullets.Remove(bullet);
                continue;
            }
            bullet.GetComponent<Bullet>().onUpdate();
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject bullet in instantiatedBullets.ToArray())
        {
            if (bullet != null)
                Destroy(bullet);
        }
    }



    private void lookAtJoystickDirection()
    {
        float heading = Mathf.Atan2(-ghScript.joystick.Horizontal * 50f, ghScript.joystick.Vertical * 50f);
        Quaternion newRotation = Quaternion.Euler(0f, 0f, heading * Mathf.Rad2Deg);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.fixedDeltaTime * rotationSpeed);
    }

    private void lookAtClickOrTouch()
    {
        Vector3 position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
        if (Application.platform == RuntimePlatform.Android)
        {
            position = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 10);
        }
        position = Camera.main.ScreenToWorldPoint(position);
        float rotationZ = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, rotationZ - 90), Time.fixedDeltaTime * rotationSpeed);
    }

    private void shoot()
    {
        if (Time.time >= fireRateCountDown)
        {
            fireRateCountDown = Time.time + changeFireRate;

            GameObject laserLoadAnim = Instantiate(laserLoading, transform.Find("LaserLoadAnim"));
            Destroy(laserLoadAnim, 0.5f);
            Color tmpColor = normalLaserColor;            

            GameObject chooseLaser = normalLaserPrefab;
            if (jumpLaserActive)
            {
                chooseLaser = jumpLaserPrefab;
                tmpColor = jumpLaserPrefab.GetComponent<SpriteRenderer>().color;
            }
            
            tmpColor.a = 0.5f;
            laserLoadAnim.GetComponent<SpriteRenderer>().color = tmpColor;

            bool playSound = true;
            for (int emitterIndex = 0; emitterIndex < activeEmitters; emitterIndex++)
            {
                GameObject bullet = Instantiate(chooseLaser, laserEmitter[emitterIndex].position, chooseLaser.transform.rotation);
                Vector2 shootDirection = laserEmitter[emitterIndex].position - Vector3.zero;

                bullet.GetComponent<Bullet>().initLaser(shootDirection, jumpLaserActive, normalLaserColor, playSound);
                instantiatedBullets.Add(bullet);

                if (jumpLaserActive)
                    break;

                playSound = false;
            }            
        }
    }

    public void destroy()
    {
        Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
        Destroy(gameObject, 0.5f);
    }

    public void fireRateUpgrade(float upgrade)
    {
        if (changeFireRate > minFireRate)
            changeFireRate -= upgrade;


        if (changeFireRate < minFireRate)
            changeFireRate = minFireRate;
    }

    public void shootUpgrade()
    {
        if (curShootUpgrade < maxShootUpgrades)
            curShootUpgrade++;
    }

    public void setJumpLaser()
    {
        StopCoroutine(activateJumpLaser());
        StartCoroutine(activateJumpLaser());
    }

    private IEnumerator activateJumpLaser()
    {
        jumpLaserActive = true;
        yield return new WaitForSeconds(jumpLaserDuration);
        jumpLaserActive = false;
    }



    public void save()
    {
        //SaveSystem.saveWeapon(this, id);
        SaveSystem.saveAll();
    }

    public void load()
    {
        /*WeaponData data = SaveSystem.loadWeapon(id);
        if (data == null)
        {
            save();
            data = SaveSystem.loadWeapon(id);
        }

        unlocked = data.unlocked;
        active = data.active;*/
    }

}
