using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Planet : IngameEntity
{
    public static Planet Instance;

    [Header("----------------------------------------")]

    [Header("Movement")]
    public float rotationSpeed;

    [Header("Animation GO's")]
    public GameObject planetExplosion;
    public GameObject animExplosion;
    public GameObject animImpulseWave;
    private GameObject currentImpulseWave;

    [Header("Sounds")]
    public AudioClip soundExplosion;
    public AudioClip soundImpulseWave;

    private bool revived = false;


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        if (Random.value < 0.5f)
            rotationSpeed = -rotationSpeed;
    }

    private void Update()
    {
        transform.Rotate(0, 0, rotationSpeed);
    }


    public void Hit(int damage = 1)
    {
        //endless mode in main menu
        if (UIMainMenu.Instance.mainMenuPanel.activeSelf)
        {
            GameController.Instance.ResetWeaponEmitterKillCounter();
            ImpulesWave();
        }
        else
        {
            GameController.Instance.ResetWeaponEmitterKillCounter();
            Camera.main.transform.GetComponent<Animator>().Play("hitShake");

            GameController.Instance.ChangeLife(-damage);

            if (GameController.Instance.currentLifes <= 0)
            {
                if (GameController.Instance.UpgradeActive(EntityAttribute.eAttributeType.PlanetRevive) && !revived)
                {
                    revived = true;
                    GameController.Instance.ResetLifes();
                    ImpulesWave();
                    EnemyController.Instance.RemoveAllEnemies();
                    return;
                }

                GameObject explosion = Instantiate(animExplosion);
                explosion.transform.localScale *= 2f;
                Destroy(explosion, 20);
                AudioController.PlaySound(soundExplosion);

                GameController.SetGameOver();
                if (planetExplosion != null)
                {
                    GameObject planetExp = Instantiate(planetExplosion, transform.position, Quaternion.identity);
                    planetExp.name = "PlanetExplosion";
                    Destroy(planetExp, 20);
                }

                Destroy(gameObject);
            }
            else
            {
                ImpulesWave();
            }
        }            
    }

    private void ImpulesWave()
    {
        //Explosion on hit when upgrade type eUpgradeType.PlanetExplosionOnHit is active
        if (!GameController.Instance.UpgradeActive(EntityAttribute.eAttributeType.PlanetExplosionOnHit) || currentImpulseWave != null) return;

        currentImpulseWave = Instantiate(animImpulseWave);
        Destroy(currentImpulseWave, 1.4f);
        AudioController.PlaySound(soundImpulseWave, pitch: Random.Range(0.9f, 1.3f));
    }


    public void Save()
    {
    }

    public void Load()
    {
        /*PlanetData data = SaveSystem.loadPlanet(id);
        if (data == null)
        {
            save();
            data = SaveSystem.loadPlanet(id);
        }

        unlocked = data.unlocked;
        active = data.active;*/
    }

}
