using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    int ID = 0;
    Dictionary<string, int> areaDic = new Dictionary<string, int>();
    Dictionary<int, PatrolArea> existAreas = new Dictionary<int, PatrolArea>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int AddPatrolArea(string name) {

        if (!areaDic.ContainsKey(name))
        {
            areaDic.Add(name, ID);
            PatrolArea area = new PatrolArea();
            existAreas.Add(ID, area);
            ID++;
            return ID-1;

        }
        else {
            return -1;
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
