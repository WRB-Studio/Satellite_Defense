using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyType : MonoBehaviour
{
    [Header("Shop infos")]
    public int id = 0;
    public string enemyTypeName = "unnamed";
    public long cost = 0;
    public bool unlocked = false;
    public bool active = false;

    public GameObject[] enemyPrefabs;


    public void save()
    {
        //SaveSystem.saveEnemyType(this, id);
        SaveSystem.saveAll();
    }

    public void load()
    {
        /*EnemyTypeData data = SaveSystem.loadEnemyType(id);
        if (data == null)
        {
            save();
            data = SaveSystem.loadEnemyType(id);
        }

        unlocked = data.unlocked;
        active = data.active;*/
    }
}
