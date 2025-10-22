using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum eHitType { None, Planet, Laser }

    private int healthPoints;
    private int damage = 1;
    private float moveSpeed = 1;

    [Header("")]
    public float rotationSpeed = 1;
    public bool randomRotation = true;
    public int scoreGain = 1;
    public bool isSplitPiece = false;

    private Vector2 target = Vector2.zero;

    [Header("Animations")]
    public GameObject deathExplosion;
    public GameObject bulletExplosion;

    private Transform trail;

    private bool inSplitProgress = false;


    public void Init(Vector2 newTarget = new Vector2())
    {
        if (randomRotation) rotationSpeed = Random.Range(-rotationSpeed, rotationSpeed);

        trail = transform.GetChild(0);

        healthPoints = Utilities.Round(GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.EnemyHP));
        damage = Utilities.Round(GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.EnemyDamage));
        moveSpeed = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.EnemySpeed);
        
        if (isSplitPiece) 
            moveSpeed *= Random.Range(0.65f, 0.85f);
        else 
            moveSpeed *= Random.Range(0.85f, 1.15f);

        target = newTarget;        
    }

    public void OnUpdate()
    {
        transform.position = Vector2.MoveTowards(transform.position, target, Time.deltaTime * moveSpeed);
        transform.Rotate(0, 0, rotationSpeed);

        if(Utilities.IsOutsideViewWithMargin(transform, 8f)) EnemyController.RemoveEnemy(this);
    }

    public void hit(int damage, eHitType currentHitType, Vector2 hitPoint)
    {
        healthPoints -= damage;

        switch (currentHitType)
        {
            case eHitType.None:
                break;

            case eHitType.Planet:
                AudioController.PlaySound(AudioController.Instance.soundPlanetHit);
                DetachTrail();
                Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
                EnemyController.RemoveEnemy(this);

                return;

            case eHitType.Laser:
                if (healthPoints <= 0)
                {
                    AudioController.PlaySound(AudioController.Instance.soundEnemyHit);
                    DetachTrail();
                    Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);

                    //multiplie score gain with current move speed (faster meteors, more points)
                    float tmpScoreGainMulti = moveSpeed * 2;
                    if (tmpScoreGainMulti < 1) tmpScoreGainMulti = 1;
                    int newScore = Utilities.Round(scoreGain * tmpScoreGainMulti);
                    ScoreController.Instance.AddScore(newScore);

                    if (currentHitType == eHitType.Laser) EnemyController.Instance.AddKill();

                    if (!isSplitPiece && !inSplitProgress)
                    {
                        inSplitProgress = true;
                        EnemyController.Instance.TryToSplit(transform);
                    }
                    else
                    {
                        PowerUpController.Instance.SpawnRandomItem(transform.position);
                    }

                    EnemyController.RemoveEnemy(this);
                }
                else
                {
                    AudioController.PlaySound(clip: AudioController.Instance.soundEnemyHit, pitch: 2.5f);
                    if (bulletExplosion != null)
                        Destroy(Instantiate(bulletExplosion, hitPoint, bulletExplosion.transform.rotation, transform), 2);
                }
                break;
        }
    }

    private void DetachTrail()
    {
        if (!trail) return;

        trail.SetParent(null, worldPositionStays: true);

        var ps = trail.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.loop = false;
            ps.Stop();
        }

        Destroy(trail.gameObject, 3f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.tag == "Planet")
        {
            collision.transform.GetComponent<Planet>().Hit(damage);

            AudioController.PlaySound(AudioController.Instance.soundPlanetHit);
            Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
            EnemyController.RemoveEnemy(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == "ImpulseWave")
        {
            AudioController.PlaySound(AudioController.Instance.soundPlanetHit, pitch: Random.Range(1.3f, 1.5f));
            Destroy(Instantiate(deathExplosion, transform.position, deathExplosion.transform.rotation), 2);
            EnemyController.RemoveEnemy(this);
        }
    }

}
