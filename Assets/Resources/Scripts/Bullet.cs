using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector2 destination;
    public Transform destinationObject;
    public float moveSpeed = 1;
    public float lifeTime = 5;
    public int damage = 1;

    [Header("Sounds")]
    public AudioClip soundNormalLaser;
    public AudioClip soundJumpLaser;
    public float jumpLaserSoundPitch = 0;

    [Header("jumpLaser settings")]
    public int maxLaserJumpHits = 3;
    public float targetingDistance = 2;
    private bool laserJumperActive = false;

    private GameHandler ghScrp;




    void Start()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();
        StartCoroutine(lifeTimeCounter());
    }


    public void onUpdate()
    {
        if (laserJumperActive)
            if (destinationObject == null && checkIsInCameraView(transform))
                destinationObject = getNearestTarget();

        if (laserJumperActive && destinationObject != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, destinationObject.position, Time.fixedDeltaTime * moveSpeed);
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, destination, Time.fixedDeltaTime * moveSpeed);
        }
    }



    public void initLaser(Vector2 newDestination, bool laserJumper, Color laserColor, bool playAudio)
    {
        if (laserJumper)
        {
            StaticAudioHandler.playSound(soundJumpLaser, true).pitch += jumpLaserSoundPitch;
        }
        else
        {
            if (playAudio)
                StaticAudioHandler.playSound(soundNormalLaser, true).pitch += 1.5f;
            GetComponent<SpriteRenderer>().color = laserColor;
        }

        laserJumperActive = laserJumper;

        destination = newDestination * 10;

        float rot_z = Mathf.Atan2(destination.y, destination.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            collision.transform.GetComponent<Enemy>().hit(damage, Enemy.HitType.Laser);

            if (!laserJumperActive || maxLaserJumpHits == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                maxLaserJumpHits--;
            }
        }
    }

    private Transform getNearestTarget()
    {
        GameObject nextTarget = null;
        float nearestTargetDistance = 100;
        foreach (GameObject enemy in GameObject.Find("EnemySpawner").GetComponent<EnemySpawner>().getInstantiatedEnemys().ToArray())
        {
            if (enemy == null)
                continue;

            float curDistance = Vector2.Distance(transform.position, enemy.transform.position);
            if (checkIsInCameraView(enemy.transform) && curDistance <= targetingDistance && curDistance < nearestTargetDistance)
            {
                nearestTargetDistance = curDistance;
                nextTarget = enemy;
            }
        }

        if (nextTarget == null)
            return null;

        return nextTarget.transform;
    }

    private bool checkIsInCameraView(Transform target)
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(target.position);
        if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
            return true;
        else return false;
    }


    private IEnumerator lifeTimeCounter()
    {
        while (lifeTime > 0)
        {
            while (ghScrp.getIsPause() || ghScrp.getIsGameOver())
                yield return null;

            yield return new WaitForSeconds(0.1f);
            lifeTime -= 0.1f;
        }

        Destroy(gameObject);
    }

}
