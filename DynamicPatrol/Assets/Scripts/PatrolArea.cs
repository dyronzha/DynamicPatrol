using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder;

public class PatrolArea
{
    public PatrolArea(string _name) {
        name = _name;
    }

    string name;
    public string Name {
        get { return name; }
        set { name = value; }
    }

    bool[] hasChoosen = new bool[4];
    public bool GetHasChoosen(int id) {
        return hasChoosen[id];
    }
    public void SetHasChoosen(int id, bool value)
    {
        hasChoosen[id] = value;
    }

    public class SpreadGrid {
        bool choosen;
        bool stopSpread;
        public int x;
        public int y;
        public int direction;
        public SpreadGrid(int _x, int _y, int _dir) {
            x = _x;
            y = _y;
            direction = _dir;
            stopSpread = false;
            stopSpread = false;
        }
        
    }

    Dictionary<string, SpreadGrid>spreadGrids = new Dictionary<string, SpreadGrid>();
    public void AddSpreadGrid( int _x, int _y, int _dir) {
        string key = _x.ToString() + "," + _y.ToString();
        spreadGrids.Add(key ,new SpreadGrid(_x, _y, _dir));
    }
    public void UpdateSpreadGrid(int _x, int _y, int _dir) {
        string loc = _x.ToString() + "," + _y.ToString();
        if (spreadGrids.ContainsKey(loc)) {
            spreadGrids[loc].direction = _dir;
        }
    }
    public void RemoveSpreadGrid(int _x, int _y) {
        string loc = _x.ToString() + "," + _y.ToString();
        if (spreadGrids.ContainsKey(loc))
        {
            spreadGrids.Remove(loc);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddNewPoint() { 
    
    }
}
