using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector2 destination;
    public Transform destinationObject;
    private float moveSpeed = 1;
    private int damage = 1;
    public float lifeTime = 5;

    [Header("Sounds")]
    public AudioClip soundNormalLaser;
    public AudioClip soundJumpLaser;
    public float jumpLaserSoundPitch = 0;

    [Header("jumpLaser settings")]
    public int maxLaserJumpHits = 3;
    public float targetingDistance = 2;
    private bool laserJumperActive = false;



    void Start()
    {
        StartCoroutine(lifeTimeCounter());

        float moveSpeedUpgrade = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.WeaponProjectileSpeed);
        moveSpeed = moveSpeedUpgrade <= 0 ? moveSpeed : moveSpeed > 10 ? 10 : moveSpeed + moveSpeedUpgrade;

        int damageUpgrade = damage + (int)GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.WeaponDamage);
        damage = damageUpgrade <= 0 ? damage : damageUpgrade;
    }


    public void OnFixedUpdate()
    {
        if (laserJumperActive)
            if (destinationObject == null && Utilities.IsInsideViewWithPadding(transform, 1))
                destinationObject = getNearestTarget();

        if (laserJumperActive && destinationObject != null)
            transform.position = Vector2.MoveTowards(transform.position, destinationObject.position, Time.fixedDeltaTime * moveSpeed);
        else
            transform.position = Vector2.MoveTowards(transform.position, destination, Time.fixedDeltaTime * moveSpeed);
    }


    public void initLaser(Vector2 newDestination, bool laserJumper, Color laserColor, bool playAudio, int damageVal, float speedVal)
    {
        if (laserJumper)
        {
            AudioController.PlaySound(soundJumpLaser, true).pitch += jumpLaserSoundPitch;
        }
        else
        {
            if (playAudio) AudioController.PlaySound(soundNormalLaser, true).pitch += 1.5f;
            GetComponent<SpriteRenderer>().color = laserColor;
        }

        laserJumperActive = laserJumper;

        destination = newDestination * 10;

        float rot_z = Mathf.Atan2(destination.y, destination.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);

        damageVal = damage;
        moveSpeed = speedVal;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            collision.transform.GetComponent<Enemy>().hit(damage, Enemy.eHitType.Laser, collision.contacts[0].point);

            if (!laserJumperActive || maxLaserJumpHits == 0)
                Weapon.RemoveBullet(this);
            else
                maxLaserJumpHits--;
        }
    }

    private Transform getNearestTarget()
    {
        GameObject nextTarget = null;
        float nearestTargetDistance = 100;
        foreach (Enemy enemy in EnemyController.Instance.GetInstantiatedEnemys().ToArray())
        {
            if (enemy == null)
                continue;

            float curDistance = Vector2.Distance(transform.position, enemy.transform.position);
            if (Utilities.IsInsideViewWithPadding(enemy.transform, 1) && curDistance <= targetingDistance && curDistance < nearestTargetDistance)
            {
                nearestTargetDistance = curDistance;
                nextTarget = enemy.gameObject;
            }
        }

        if (nextTarget == null)
            return null;

        return nextTarget.transform;
    }

    private IEnumerator lifeTimeCounter()
    {
        while (lifeTime > 0)
        {
            while (GameController.GetIsPause() || GameController.GetIsGameOver())
                yield return null;

            yield return new WaitForSeconds(0.1f);
            lifeTime -= 0.1f;
        }

        Destroy(gameObject);
    }

}
