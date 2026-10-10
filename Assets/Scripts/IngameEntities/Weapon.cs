using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Weapon : IngameEntity
{
    [Header("Prefabs & Projectiles")]
    public GameObject normalLaserPrefab;
    public GameObject jumpLaserPrefab;
    public Color normalLaserColor;
    [Min(0f)] public float jumpLaserDuration = 6f;
    [FormerlySerializedAs("minFireRate")]
    [Min(0.01f)] public float minShotInterval = 0.5f;
    [Min(0.01f)] public float secondEmitterShotInterval = 0.75f;
    [Min(0.01f)] public float thirdEmitterShotInterval = 0.55f;
    public GameObject deathExplosion;

    public int WeaponLevel { get; private set; } = 1;
    public bool CanUpgradeEmitters => WeaponLevel < 3 &&
        shotInterval <= (WeaponLevel == 1 ? secondEmitterShotInterval : thirdEmitterShotInterval);
    public bool CanReduceShotInterval => shotInterval > minShotInterval;
    public bool IsJumpLaserActive => jumpLaserRemaining > 0f;

    private float rotationSpeed;
    private float projectileSpeed;
    private int damage;
    private float shotInterval;
    private float shotCooldown;
    private float jumpLaserRemaining;
    private Transform emitterGroup;
    private readonly List<Transform> activeEmitters = new();
    private readonly List<Bullet> bullets = new();

    public void Init()
    {
        var stats = GameController.Instance.Stats;
        emitterGroup = transform.Find("LaserEmitterGrp");
        rotationSpeed = stats.WeaponRotationSpeed;
        shotInterval = Mathf.Max(minShotInterval, stats.ShotInterval);
        projectileSpeed = stats.ProjectileSpeed;
        damage = stats.WeaponDamage;
        ResetWeaponLevel();
    }

    private void FixedUpdate()
    {
        var game = GameController.Instance;
        if (!game || !game.IsSimulationRunning) return;
        shotCooldown = Mathf.Max(0f, shotCooldown - Time.fixedDeltaTime);
        jumpLaserRemaining = Mathf.Max(0f, jumpLaserRemaining - Time.fixedDeltaTime);

        if (!game.TryGetAimDirection(out Vector2 direction)) return;
        var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, rotationSpeed * Time.fixedDeltaTime);
        if (shotCooldown > 0f) return;
        shotCooldown = shotInterval;
        Fire();
    }

    public void ResetWeaponLevel()
    {
        WeaponLevel = 1;
        RefreshEmitters();
    }

    public void UpgradeEmitters()
    {
        if (!CanUpgradeEmitters) return;
        WeaponLevel++;
        RefreshEmitters();
        EnemyController.Instance.ResetWeaponProgress();
    }

    private void RefreshEmitters()
    {
        activeEmitters.Clear();
        foreach (Transform emitter in emitterGroup)
            if (emitter.name.Contains("LVL" + WeaponLevel)) activeEmitters.Add(emitter);
    }

    public void ReduceShotInterval(float amount) => shotInterval = Mathf.Max(minShotInterval, shotInterval - Mathf.Max(0f, amount));
    public void ActivateJumpLaser() => jumpLaserRemaining = Mathf.Max(0f, jumpLaserDuration);

    private void Fire()
    {
        bool jump = IsJumpLaserActive;
        var prefab = jump ? jumpLaserPrefab : normalLaserPrefab;
        for (int i = 0; i < activeEmitters.Count; i++)
        {
            var emitter = activeEmitters[i];
            var bullet = Instantiate(prefab, emitter.position, emitter.rotation, GameController.Instance.EffectsRoot).GetComponent<Bullet>();
            bullets.Add(bullet);
            bullet.Init(this, emitter.up, jump, normalLaserColor, i == 0, damage, projectileSpeed);
            if (jump) break;
        }
    }

    public void RemoveBullet(Bullet bullet) => bullets.Remove(bullet);

    public void ClearBullets()
    {
        while (bullets.Count > 0)
        {
            var bullet = bullets[bullets.Count - 1];
            bullets.RemoveAt(bullets.Count - 1);
            if (bullet) bullet.Despawn();
        }
    }

    public void DestroyWeapon()
    {
        ClearBullets();
        GameController.Instance.SpawnEffect(deathExplosion, transform.position, 2f);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy() => ClearBullets();
}
