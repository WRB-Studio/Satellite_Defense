using UnityEngine;

public class Planet : IngameEntity
{
    [Header("Movement")]
    public float rotationSpeed;
    [Header("Effects")]
    public GameObject planetExplosion;
    public GameObject animExplosion;
    public GameObject animImpulseWave;
    public AudioClip soundExplosion;
    public AudioClip soundImpulseWave;
    [Min(0f)] public float impulseWaveCooldown = 4f;

    private GameObject currentImpulseWave;
    private bool revived;
    private bool dead;
    private float impulseCooldownRemaining;

    public void Init()
    {
        if (Random.value < 0.5f) rotationSpeed = -rotationSpeed;
    }

    private void Update()
    {
        if (GameController.Instance.IsSimulationRunning)
        {
            impulseCooldownRemaining = Mathf.Max(0f, impulseCooldownRemaining - Time.deltaTime);
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }

    public void Hit(int damage = 1)
    {
        var game = GameController.Instance;
        if (dead || damage <= 0 || !game.IsSimulationRunning) return;
        if (!game.IsPlaying)
        {
            ImpulseWave();
            return;
        }
        game.ResetWeaponProgress();
        if (game.GameCamera.TryGetComponent<Animator>(out var animator)) animator.Play("hitShake");
        game.ChangeLife(-damage);
        if (game.CurrentLives > 0)
        {
            ImpulseWave();
            return;
        }
        if (!revived && game.Stats.CanRevive)
        {
            revived = true;
            game.ResetLives();
            ImpulseWave();
            EnemyController.Instance.RemoveAllEnemies();
            return;
        }
        dead = true;
        var explosion = game.SpawnEffect(animExplosion, transform.position, 20f);
        if (explosion) explosion.transform.localScale *= 2f;
        game.SpawnEffect(planetExplosion, transform.position, 20f);
        AudioController.PlaySound(soundExplosion);
        game.EndRound();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void ImpulseWave()
    {
        var game = GameController.Instance;
        if (!game.Stats.HasImpulseWave || currentImpulseWave || impulseCooldownRemaining > 0f) return;
        impulseCooldownRemaining = impulseWaveCooldown;
        currentImpulseWave = game.SpawnEffect(animImpulseWave, transform.position, 1.4f);
        AudioController.PlaySound(soundImpulseWave, pitch: Random.Range(0.9f, 1.3f));
    }
}
