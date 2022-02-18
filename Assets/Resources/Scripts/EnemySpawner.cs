using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private GameObject[] enemyPrefabs;
    private List<GameObject> instantiatedEnemys = new List<GameObject>();

    public float minSpawnRate = 0.3f;
    private int waveCounter = 0;

    private GameHandler ghScrp;




    private void Start()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();
    }

    public void init()
    {
        ghScrp = GameObject.Find("GameHandler").GetComponent<GameHandler>();

        enemyPrefabs = ghScrp.activeEnemyType.GetComponent<EnemyType>().enemyPrefabs;

        StopAllCoroutines();
        removeAllEnemys();

        StartCoroutine(spawnEnemys());
    }


    void Update()
    {
        //if game is paused
        while (ghScrp.getIsPause())
            return;

        //enemy moving
        foreach (GameObject enemy in instantiatedEnemys.ToArray())
        {
            if (enemy == null)
            {
                instantiatedEnemys.Remove(enemy);
                continue;
            }
            enemy.GetComponent<Enemy>().onUpdate();
        }
    }


    private GameObject instantiateRandomEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length - 1)], randomSpawnPosition(), Quaternion.identity);
        float randScale = Random.Range(transform.localScale.x - 0.2f, transform.localScale.x + 0.2f);
        enemy.transform.localScale = new Vector2(randScale, randScale);
        enemy.transform.parent = transform;
        instantiatedEnemys.Add(enemy);
        return enemy;
    }

    private Vector2 randomSpawnPosition()
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

    private IEnumerator spawnEnemys()
    {
        GameObject curEnemyInstanz = null;

        waveCounter = 0;

        while (true)
        {
            waveCounter++;
            if (ghScrp.getInMainMenue())
                waveCounter = 3;

            int enemiesPerWave = waveCounter;

            //Spawn Wave
            for (int spawnCounter = 0; spawnCounter < enemiesPerWave; spawnCounter++)
            {
                while (ghScrp.getIsPause() || ghScrp.getIsGameOver())
                    yield return null;

                curEnemyInstanz = instantiateRandomEnemy();

                //Random enemy speed
                Enemy enemyScript = curEnemyInstanz.GetComponent<Enemy>();

                float randSpeed = Random.Range(enemyScript.moveSpeed + 0.04f * waveCounter, enemyScript.moveSpeed + 0.07f * waveCounter);
                enemyScript.moveSpeed = randSpeed;

                float maxSpawnRate = 1.8f - (0.1f * waveCounter);
                if (maxSpawnRate < minSpawnRate)
                    maxSpawnRate = minSpawnRate;
                yield return new WaitForSeconds(Random.Range(minSpawnRate, maxSpawnRate));
            }

            while (instantiatedEnemys.Count > 1)
                yield return null;
        }
    }

    public List<GameObject> getInstantiatedEnemys()
    {
        return instantiatedEnemys;
    }

    public void removeAllEnemys()
    {
        foreach (GameObject enemy in instantiatedEnemys.ToArray())
        {
            instantiatedEnemys.Remove(enemy);
            Destroy(enemy);
        }
    }
}
