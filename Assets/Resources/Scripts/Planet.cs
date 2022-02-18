using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Planet : MonoBehaviour
{
    [Header("Shop infos")]
    public int id = 0;
    public string planetName = "unnamed";
    public long cost = 0;
    public bool unlocked = false;
    public bool active = false;

    [Header("Attributes")]
    public float rotationSpeed;

    [Header("Animation GO's")]
    public GameObject planetExplosion;
    public GameObject animExplosion;

    [Header("Sounds")]
    public AudioClip soundExplosion;    

    private GameHandler ghScript;




    private void Start()
    {
        ghScript = GameObject.Find("GameHandler").GetComponent<GameHandler>();

        if (Random.value < 0.5f)
            rotationSpeed = -rotationSpeed;
    }

    private void Update()
    {
        transform.Rotate(0,0,rotationSpeed);
    }



    public void hit()
    {
        ghScript.resetWeaponEmitterKillCounter();
        Camera.main.transform.GetComponent<Animator>().Play("hitShake");
        ghScript.changeLife(false);
        if (ghScript.currentLifes <= 0)
        {
            GameObject explosion = Instantiate(animExplosion);
            Destroy(explosion, 20);
            explosion.transform.localScale *= 2f;
            StaticAudioHandler.playSound(soundExplosion);
            GameObject.Find("GameHandler").GetComponent<GameHandler>().setGameOver();
            if (planetExplosion != null)
                Destroy(Instantiate(planetExplosion), 20);
            Destroy(gameObject);
        }
    }



    public void save()
    {
    }

    public void load()
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
