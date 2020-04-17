using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    int ID = 33;
    Dictionary<string, int> areaDic = new Dictionary<string, int>();   //碰撞名和地區的字典
    Dictionary<string, PatrolArea> existAreas = new Dictionary<string, PatrolArea>();  //已知地區和編號的字典

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string CheckExistArea(string name)
    {
        if (name.Length <= 0) return name;
        if (!areaDic.ContainsKey(name))
        {
            areaDic.Add(name, ID);
            PatrolArea area = new PatrolArea(name);
            string id = char.ToString(System.Convert.ToChar(ID));
            existAreas.Add(id, area);
            ID++;
            return id;
        }
        else
        {
            return char.ToString(System.Convert.ToChar(areaDic[name]));
        }
    }

    public string AddPatrolArea(string name) {

        if (!areaDic.ContainsKey(name))
        {
            
            areaDic.Add(name, ID);
            PatrolArea area = new PatrolArea(name);
            string id = char.ToString(System.Convert.ToChar(ID));
            existAreas.Add(id, area);
            //Debug.Log("convert  " + name + " to " + id);
            ID++;
            return id;

        }
        else {
            return char.ToString(System.Convert.ToChar(areaDic[name]));
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
