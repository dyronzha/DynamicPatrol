using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    int ID = 27;
    Dictionary<string, int> areaDic = new Dictionary<string, int>();
    Dictionary<string, PatrolArea> existAreas = new Dictionary<string, PatrolArea>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string AddPatrolArea(string name) {

        if (!areaDic.ContainsKey(name))
        {
            areaDic.Add(name, ID);
            PatrolArea area = new PatrolArea(name);
            string id = System.Convert.ToString(ID);
            existAreas.Add(id, area);
            ID++;
            return id;

        }
        else {
            return System.Convert.ToString(areaDic[name]);
            //if (existAreas.ContainsKey(ID))
            //{
            //    PatrolArea area = new PatrolArea();
            //}
            //else
            //{
            //    //existAreas[id];
            //}
        }


        
    }

}
