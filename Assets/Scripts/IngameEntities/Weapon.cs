using System.Collections.Generic;
using UnityEngine;

public class Weapon : IngameEntity
{
    [Header("Prefabs & Projectiles")]
    public GameObject normalLaserPrefab;
    public GameObject jumpLaserPrefab;
    public Color normalLaserColor;
    [Min(0f)] public float jumpLaserDuration = 5f;
    [Min(0.01f)] public float minFireRate = 0.1f;
    public GameObject deathExplosion;

    public int WeaponLevel { get; private set; } = 1;
    public bool CanUpgradeEmitters => WeaponLevel < 3;
    public bool CanUpgradeFireRate => shotInterval > minFireRate;
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
        var game = GameController.Instance;
        emitterGroup = transform.Find("LaserEmitterGrp");
        rotationSpeed = Mathf.Max(0f, game.GetAttribute(EntityAttribute.eAttributeType.WeaponRotationSpeed, 40f));
        shotInterval = Mathf.Max(minFireRate, game.GetAttribute(EntityAttribute.eAttributeType.WeaponFireRate, 1f));
        projectileSpeed = Mathf.Clamp(game.GetAttribute(EntityAttribute.eAttributeType.WeaponProjectileSpeed, 1f), 0.1f, 10f);
        damage = Mathf.Max(1, Utilities.Round(game.GetAttribute(EntityAttribute.eAttributeType.WeaponDamage, 1f)));
        ResetWeaponLevel();
    }

    private void FixedUpdate()
    {
        var game = GameController.Instance;
        if (!game || !game.IsSimulationRunning) return;
        shotCooldown = Mathf.Max(0f, shotCooldown - Time.fixedDeltaTime);
        jumpLaserRemaining = Mathf.Max(0f, jumpLaserRemaining - Time.fixedDeltaTime);

        Vector2 direction;
        if (game.enableJoystickControll)
        {
            if (!game.joystick.IsPressed || game.joystick.Direction.sqrMagnitude < 0.001f) return;
            direction = game.joystick.Direction;
        }
        else
        {
            if (!Utilities.TryGetAimPosition(out Vector2 screenPosition)) return;
            direction = Utilities.ScreenToWorld(screenPosition) - (Vector2)transform.position;
        }
        if (direction.sqrMagnitude < 0.001f) return;
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
        WeaponLevel = Mathf.Min(3, WeaponLevel + 1);
        RefreshEmitters();
    }

    private void RefreshEmitters()
    {
        activeEmitters.Clear();
        foreach (Transform emitter in emitterGroup)
            if (emitter.name.Contains("LVL" + WeaponLevel)) activeEmitters.Add(emitter);
    }

    public void FireRateUpgrade(float amount) => shotInterval = Mathf.Max(minFireRate, shotInterval - Mathf.Max(0f, amount));
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
