using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public float rotationSpeed = 30f;
    public bool randomRotation = true;
    public int scoreGain = 1;
    public bool isSplitPiece;
    public GameObject deathExplosion;
    public GameObject bulletExplosion;
    public bool IsAlive => !removed;
    public Vector2 Direction { get; private set; }

    private Rigidbody2D body;
    private int healthPoints;
    private int damage;
    private float moveSpeed;
    private bool removed;
    private Transform trail;

    public void Init(Vector2 target)
    {
        var stats = GameController.Instance.Stats;
        body = GetComponent<Rigidbody2D>();
        if (randomRotation) rotationSpeed = Random.Range(-Mathf.Abs(rotationSpeed), Mathf.Abs(rotationSpeed));
        if (transform.childCount > 0) trail = transform.GetChild(0);
        healthPoints = stats.EnemyHealth;
        damage = stats.EnemyDamage;
        moveSpeed = stats.EnemySpeed;
        moveSpeed *= isSplitPiece ? Random.Range(0.65f, 0.85f) : Random.Range(0.85f, 1.15f);
        Direction = (target - body.position).normalized;
        if (Direction.sqrMagnitude < 0.001f) Direction = Vector2.down;
        body.linearVelocity = Direction * moveSpeed;
        body.angularVelocity = rotationSpeed;
    }

    private void FixedUpdate()
    {
        if (removed || !GameController.Instance.IsSimulationRunning) return;
        body.linearVelocity = Direction * moveSpeed;
        body.angularVelocity = rotationSpeed;
        if (Utilities.IsOutsideViewWithMargin(transform.position, 8f)) Despawn();
    }

    public void Hit(int amount, Vector2 hitPoint)
    {
        if (removed || !GameController.Instance.IsSimulationRunning) return;
        healthPoints -= Mathf.Max(0, amount);
        if (healthPoints > 0)
        {
            AudioController.PlaySound(AudioController.Instance.soundEnemyHit, pitch: 2.5f);
            GameController.Instance.SpawnEffect(bulletExplosion, hitPoint, 2f);
            return;
        }
        // Disable immediately: other contacts in this physics step must not award another kill.
        removed = true;
        AudioController.PlaySound(AudioController.Instance.soundEnemyHit);
        DeathEffect();
        ScoreController.Instance.AddScore(scoreGain * Mathf.Max(1f, moveSpeed * 2f));
        EnemyController.Instance.AddKill();
        if (!EnemyController.Instance.TrySplit(this)) PowerUpController.Instance.SpawnRandomItem(transform.position);
        Remove();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (removed || !GameController.Instance.IsSimulationRunning ||
            !collision.gameObject.TryGetComponent<Planet>(out var planet)) return;
        removed = true;
        DeathEffect();
        AudioController.PlaySound(AudioController.Instance.soundPlanetHit);
        Remove();
        planet.Hit(damage);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (removed || !GameController.Instance.IsSimulationRunning || !collision.CompareTag("ImpulseWave")) return;
        removed = true;
        DeathEffect();
        AudioController.PlaySound(AudioController.Instance.soundPlanetHit, pitch: Random.Range(1.3f, 1.5f));
        Remove();
    }

    private void DeathEffect()
    {
        var game = GameController.Instance;
        game.SpawnEffect(deathExplosion, transform.position, 2f);
        if (!trail) return;
        trail.SetParent(game.EffectsRoot, true);
        if (trail.TryGetComponent<ParticleSystem>(out var particles)) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(trail.gameObject, 3f);
        trail = null;
    }

    public void Despawn()
    {
        removed = true;
        Remove();
    }

    private void Remove()
    {
        if (EnemyController.Instance) EnemyController.Instance.Unregister(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (EnemyController.Instance) EnemyController.Instance.Unregister(this);
    }
}
