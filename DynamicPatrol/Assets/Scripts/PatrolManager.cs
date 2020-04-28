using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    int ID = 0;
    List<string> areaNames = new List<string>();
    Dictionary<string, PatrolArea> areaDic = new Dictionary<string, PatrolArea>();   //碰撞名和地區的字典
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CheckExistArea(string name)
    {
        if (name.Length > 0 && !areaDic.ContainsKey(name))
        {
            areaNames.Add(name);
            PatrolArea area = new PatrolArea(name);
            areaDic.Add(name, area);
        }

    }
    public PatrolArea FindAreaInDic(string name) {
        if (areaDic.ContainsKey(name)) {
            return areaDic[name];
        }
        return null;
    }

}
