using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static EnemyController Instance;

    [Header("References")]
    public Transform spawnParent;
    private GameObject[] enemyPrefabs;
    private static List<Enemy> instantiatedEnemys = new List<Enemy>();

    [Header("Spawn Settings")]
    private float startSpawnRate = 2f;
    public float minSpawnRate = 0.3f;
    private float ticksToMinSpawnRate = 150f;
    public Vector2 minMaxEnemyScale = new Vector2(0.2f, 0.5f);
    private float splitChance = 0.1f;
    private int splitPieces = 2;
    public float splitSpreadDegrees = 25f;


    [Header("Enemy Settings")]
    public int kills = 0;
    public int weaponEmitterAddByKills = 20;
    [HideInInspector] public int killCounterForWeaponEmitterActivation = 0;



    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        kills = 0;

        startSpawnRate = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.EnemySpawnRate);

        splitChance = GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.EnemySplitChance);

        splitPieces = Utilities.Round(GameController.Instance.GetAllUpgradeEffectValuesOfType(EntityAttribute.eAttributeType.EnemySplitCount));

        LoadActiveEnemyType();

        StopAllCoroutines();
        RemoveAllEnemies();

        StartCoroutine(EnemySpawnRoutine());
    }


    void Update()
    {
        //if game is paused
        if (GameController.GetIsPause()) return;

        //enemy moving
        foreach (Enemy enemy in instantiatedEnemys.ToArray())
        {
            if (enemy == null)
            {
                instantiatedEnemys.Remove(enemy);
                continue;
            }
            enemy.OnUpdate();
        }
    }


    private IEnumerator EnemySpawnRoutine()
    {
        int spawnCount = 0;

        while (true)
        {
            yield return new WaitWhile(() => GameController.GetIsPause() || GameController.GetIsGameOver());

            SpawnRandomEnemy().GetComponent<Enemy>().Init();

            // Progress ratio: 0 = start speed, 1 = maximum intensity (minimum delay)
            float progress = Mathf.Clamp01((float)spawnCount / ticksToMinSpawnRate);
            float currentSpawnDelay = Mathf.Lerp(startSpawnRate, minSpawnRate, progress * progress);

            float randomizedDelay = Mathf.Max(minSpawnRate, Random.Range(currentSpawnDelay * 0.85f, currentSpawnDelay * 1.15f));
            yield return new WaitForSeconds(randomizedDelay);

            spawnCount++;
        }
    }

    private void SpawnSplittedEnemy(Transform dyingEnemy)
    {
        GameObject go = SpawnRandomEnemy();
        Enemy enemy = go.GetComponent<Enemy>();
        enemy.isSplitPiece = true;

        // leicht versetzt spawnen
        float size = Mathf.Max(dyingEnemy.lossyScale.x, dyingEnemy.lossyScale.y) * 0.5f;
        Vector2 offset = Random.insideUnitCircle * size * 1.5f;
        Vector2 startPos = (Vector2)dyingEnemy.position + offset;
        go.transform.position = startPos;

        // kleiner skalieren
        float scaleFactor = Random.Range(0.4f, 0.7f);
        go.transform.localScale = dyingEnemy.localScale * scaleFactor;

        // Zielpunkt berechnen und an Init() übergeben
        Vector2 target = ComputeSplitTarget(dyingEnemy);
        enemy.Init(target);
    }

    private Vector2 ComputeSplitTarget(Transform dyingEnemy)
    {
        // Basisrichtung: Rigidbody2D-velocity oder Fallback
        Vector2 baseDir = Vector2.zero;
        if (dyingEnemy.TryGetComponent<Rigidbody2D>(out var rb))
            baseDir = rb.linearVelocity;

        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Random.insideUnitCircle.normalized;
        else
            baseDir.Normalize();

        // Streuung
        float angle = Random.Range(-splitSpreadDegrees, splitSpreadDegrees);
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        Vector2 dir = ((Vector2)(rot * baseDir)).normalized;

        // Ziel = aktuelle Position + Richtung * Distanz
        return (Vector2)dyingEnemy.position + dir * 30f;
    }


    public bool TryToSplit(Transform dyingEnemy)
    {
        if (Random.value < splitChance)
        {
            for (int i = 0; i < Random.Range(1, splitPieces + 1); i++)
                SpawnSplittedEnemy(dyingEnemy);

            return true;
        }

        return false;
    }


    private GameObject SpawnRandomEnemy()
    {
        GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject newEnemy = Instantiate(randomEnemy, RandomSpawnPosition(), Quaternion.identity, spawnParent);
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();
        enemyScript.isSplitPiece = false;

        float randScale = Random.Range(minMaxEnemyScale.x, minMaxEnemyScale.y);
        newEnemy.transform.localScale = Vector3.one * randScale;

        instantiatedEnemys.Add(enemyScript);

        return newEnemy;
    }

    private Vector2 RandomSpawnPosition()
    {
        float height = Camera.main.orthographicSize;
        float width = Camera.main.orthographicSize * Camera.main.aspect;

        float randTopOrButtom;
        if (Random.value < 0.5f)
            randTopOrButtom = -height - 0.5f;
        else
            randTopOrButtom = height + 0.5f;

        return new Vector2(Random.Range(-width - 1.5f, width + 1.5f), randTopOrButtom);
    }

    public List<Enemy> GetInstantiatedEnemys()
    {
        return instantiatedEnemys;
    }

    public void RemoveAllEnemies()
    {
        foreach (Enemy enemy in instantiatedEnemys.ToArray())
            RemoveEnemy(enemy);
    }

    public static void RemoveEnemy(Enemy enemy)
    {
        instantiatedEnemys.Remove(enemy);
        Destroy(enemy.gameObject);
    }

    public void LoadActiveEnemyType()
    {
        enemyPrefabs = UIShopMenu.Instance.activeEnemyTypeItem.GetComponent<EnemyType>().enemyPrefabs;
    }

    public void AddKill()
    {
        kills++;
        killCounterForWeaponEmitterActivation++;
        if (killCounterForWeaponEmitterActivation >= weaponEmitterAddByKills)
        {
            killCounterForWeaponEmitterActivation = 0;
            GameController.Instance.instantiatedWeapon.GetComponent<Weapon>().ChangeWeaponLevel(1);
        }
    }

}