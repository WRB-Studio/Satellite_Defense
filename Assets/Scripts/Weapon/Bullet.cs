using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Min(0.01f)] public float lifeTime = 5f;
    [Header("Sounds")]
    public AudioClip soundNormalLaser;
    public AudioClip soundJumpLaser;
    public float jumpLaserSoundPitch;
    [Header("Jump laser")]
    [Min(0)] public int maxLaserJumpHits = 3;
    [Min(0f)] public float targetingDistance = 2f;

    private Weapon owner;
    private Rigidbody2D body;
    private Collider2D hitbox;
    private Enemy target;
    private Vector2 direction;
    private float moveSpeed;
    private float remainingLife;
    private int damage;
    private int hitsRemaining;
    private bool jumpLaser;
    private bool removed;
    private readonly HashSet<int> hitEnemies = new();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Init(Weapon weapon, Vector2 shotDirection, bool jump, Color color, bool playAudio, int shotDamage, float speed)
    {
        owner = weapon;
        direction = shotDirection.normalized;
        jumpLaser = jump;
        damage = Mathf.Max(1, shotDamage);
        moveSpeed = Mathf.Clamp(speed, 0.1f, 10f);
        remainingLife = Mathf.Max(0.01f, lifeTime);
        hitsRemaining = jump ? Mathf.Max(0, maxLaserJumpHits) + 1 : 1;
        if (!jump && TryGetComponent<SpriteRenderer>(out var sprite)) sprite.color = color;
        if (playAudio)
            AudioController.PlaySound(jump ? soundJumpLaser : soundNormalLaser,
                pitch: Random.Range(0.8f, 0.9f) + (jump ? jumpLaserSoundPitch : 1.5f));
    }

    private void FixedUpdate()
    {
        var game = GameController.Instance;
        if (removed || !game || !game.IsSimulationRunning) return;
        remainingLife -= Time.fixedDeltaTime;
        if (remainingLife <= 0f || Utilities.IsOutsideViewWithMargin(transform.position, 2f))
        {
            Despawn();
            return;
        }

        if (jumpLaser)
        {
            if (!target || !target.IsAlive) target = FindTarget();
            if (target) direction = ((Vector2)target.transform.position - body.position).normalized;
        }
        body.linearVelocity = direction * moveSpeed;
        body.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
    }

    private Enemy FindTarget()
    {
        Enemy nearest = null;
        float distanceSquared = targetingDistance * targetingDistance;
        foreach (var enemy in EnemyController.Instance.Enemies)
        {
            if (!enemy || !enemy.IsAlive || hitEnemies.Contains(enemy.GetInstanceID()) ||
                !Utilities.IsInsideViewWithPadding(enemy.transform.position, 0f)) continue;
            float distance = ((Vector2)enemy.transform.position - body.position).sqrMagnitude;
            if (distance > distanceSquared) continue;
            distanceSquared = distance;
            nearest = enemy;
        }
        return nearest;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (removed || !GameController.Instance.IsSimulationRunning ||
            !collision.gameObject.TryGetComponent<Enemy>(out var enemy) || !enemy.IsAlive ||
            !hitEnemies.Add(enemy.GetInstanceID())) return;

        Physics2D.IgnoreCollision(hitbox, collision.collider);
        target = null;
        enemy.Hit(damage, collision.GetContact(0).point);
        if (--hitsRemaining <= 0) Despawn();
    }

    public void Despawn()
    {
        if (removed) return;
        removed = true;
        if (owner) owner.RemoveBullet(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (owner) owner.RemoveBullet(this);
    }
}
