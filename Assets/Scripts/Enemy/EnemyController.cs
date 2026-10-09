using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyController : MonoBehaviour
{
    public static EnemyController Instance { get; private set; }
    public Transform spawnParent;
    [FormerlySerializedAs("minSpawnRate")]
    [Min(0.01f)] public float minSpawnInterval = 0.3f;
    public Vector2 minMaxEnemyScale = new(0.2f, 0.5f);
    [Range(0f, 180f)] public float splitSpreadDegrees = 25f;
    [Min(1)] public int weaponEmitterAddByKills = 20;
    public int Kills { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;

    private readonly List<Enemy> enemies = new();
    private GameObject[] enemyPrefabs;
    private float startSpawnInterval;
    private float splitChance;
    private int splitPieces;
    private float spawnCountdown;
    private int spawnCount;
    private int killsSinceUpgrade;

    private void Awake() => Instance = this;

    public void Init(EnemyType type)
    {
        RemoveAllEnemies();
        enemyPrefabs = type.enemyPrefabs;
        Kills = 0;
        ResetWeaponProgress();
        spawnCount = 0;
        spawnCountdown = 0f;
        var stats = GameController.Instance.Stats;
        startSpawnInterval = Mathf.Max(minSpawnInterval, stats.SpawnInterval);
        splitChance = stats.SplitChance;
        splitPieces = stats.MaxSplitPieces;
    }

    private void Update()
    {
        if (!GameController.Instance.IsSimulationRunning || enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        spawnCountdown -= Time.deltaTime;
        if (spawnCountdown > 0f) return;
        float scale = Random.Range(minMaxEnemyScale.x, minMaxEnemyScale.y);
        Spawn(RandomSpawnPosition(), Vector3.one * scale, false, Vector2.zero);
        float progress = Mathf.Clamp01(spawnCount++ / 150f);
        float interval = Mathf.Lerp(startSpawnInterval, minSpawnInterval, progress * progress);
        spawnCountdown = Mathf.Max(minSpawnInterval, Random.Range(interval * 0.85f, interval * 1.15f));
    }

    private Enemy Spawn(Vector2 position, Vector3 scale, bool split, Vector2 target)
    {
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        var enemy = Instantiate(prefab, position, Quaternion.identity, spawnParent).GetComponent<Enemy>();
        enemy.transform.localScale = scale;
        enemy.isSplitPiece = split;
        enemies.Add(enemy);
        enemy.Init(target);
        return enemy;
    }

    public bool TrySplit(Enemy source)
    {
        if (source.isSplitPiece || splitPieces <= 0 || Random.value >= splitChance) return false;
        int count = Random.Range(1, splitPieces + 1);
        for (int i = 0; i < count; i++)
        {
            var offset = Random.insideUnitCircle * Mathf.Max(source.transform.lossyScale.x, source.transform.lossyScale.y) * 0.75f;
            Vector2 position = (Vector2)source.transform.position + offset;
            Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-splitSpreadDegrees, splitSpreadDegrees)) * source.Direction;
            Spawn(position, source.transform.localScale * Random.Range(0.4f, 0.7f), true, position + direction * 30f);
        }
        return true;
    }

    private Vector2 RandomSpawnPosition()
    {
        var camera = GameController.Instance.GameCamera;
        float height = camera.orthographicSize;
        float width = height * camera.aspect;
        return (Vector2)camera.transform.position +
            new Vector2(Random.Range(-width - 1.5f, width + 1.5f), (Random.value < 0.5f ? -1f : 1f) * (height + 0.5f));
    }

    public void RemoveAllEnemies()
    {
        while (enemies.Count > 0)
        {
            var enemy = enemies[enemies.Count - 1];
            enemies.RemoveAt(enemies.Count - 1);
            if (enemy) enemy.Despawn();
        }
    }

    public void Unregister(Enemy enemy) => enemies.Remove(enemy);
    public void ResetWeaponProgress() => killsSinceUpgrade = 0;

    public void AddKill()
    {
        if (!GameController.Instance.IsPlaying) return;
        Kills++;
        if (++killsSinceUpgrade < Mathf.Max(1, weaponEmitterAddByKills)) return;
        killsSinceUpgrade = 0;
        GameController.Instance.ActiveWeapon.UpgradeEmitters();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
