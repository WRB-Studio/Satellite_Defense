using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    [Header("Shop infos")]
    public int id = 0;
    public string backgroundName = "unnamed";
    public long cost = 0;
    public bool unlocked = false;
    public bool active = false;





    public void save()
    {
        //SaveSystem.saveBackground(this, id);
        SaveSystem.saveAll();
    }

    public void load()
    {
        /*BackgroundData data = SaveSystem.loadBackground(id);
        if (data == null)
        {
            save();
            data = SaveSystem.loadBackground(id);
        }

        unlocked = data.unlocked;
        active = data.active;*/
    }

}
