using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder;

public class PatrolArea
{
    PatrolManager patrolManager;
    public PatrolArea(string _name, PatrolManager manager, int gridX, int gridY) {
        name = _name;
        patrolManager = manager;
        hasCouculateNode = new bool[gridX, gridY];
        couculateNodeDir = new Vector2Int[gridX, gridY];
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

    public class SpreadGridNode {
        public bool choosen;
        public bool stopSpread;
        public int x;
        public int y;
        public Vector2Int direction;
        public Vector2Int oringin = new Vector2Int(-1, -1);
        public SpreadGridNode(int _x, int _y, int _dir) {
            x = _x;
            y = _y;
            stopSpread = false;
            choosen = false;
            if (_dir == 11) direction = new Vector2Int(0, 1);
            else if (_dir == 12) direction = new Vector2Int(-1, 0);
            else if (_dir == 13) direction = new Vector2Int(1, 0);
            else if (_dir == 14) direction = new Vector2Int(0, -1);
            else if (_dir == 23) direction = new Vector2Int(-1, 1);
            else if (_dir == 24) direction = new Vector2Int(1, 1);
            else if (_dir == 26) direction = new Vector2Int(-1, -1);
            else if (_dir == 27) direction = new Vector2Int(1, -1);
            else {
                direction = Vector2Int.zero;
                stopSpread = true;
            }
            oringin = new Vector2Int(x, y);
        }
        public SpreadGridNode(int _x, int _y, Vector2Int _dir) {
            x = _x;
            y = _y;
            stopSpread = false;
            choosen = false;
            direction = _dir;
            oringin = new Vector2Int(x, y);
        }
    }

    public bool[,] hasCouculateNode;
    public Vector2Int[,] couculateNodeDir;
    public List<string> spreadGridNmae = new List<string>();
    public Dictionary<string, SpreadGridNode>spreadGrids = new Dictionary<string, SpreadGridNode>();



    public void AddSpreadGrid( int _x, int _y, int _dir) {
        string key = _x.ToString() + "," + _y.ToString();
        //spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        if (name.CompareTo("Border") == 0) spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        else spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        spreadGridNmae.Add(key);
        
    }
    public void AddSpreadGrid(int _x, int _y, Vector2Int _dir)
    {
        string key = _x.ToString() + "," + _y.ToString();
        //spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        if (name.CompareTo("Border") == 0) spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        else spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        spreadGridNmae.Add(key);
    }
    public void AddSpreadGridTilt(int _x, int _y, Vector2Int _dir)
    {
        string key = _x.ToString() + "," + _y.ToString() + "," + _dir;
        spreadGrids.Add(key, new SpreadGridNode(_x, _y, _dir));
        spreadGridNmae.Add(key);
    }

    public void UpdateSpreadGrid(int _x, int _y, int _dir) {
        string loc = _x.ToString() + "," + _y.ToString();
        if (spreadGrids.ContainsKey(loc)) {
            if (_dir == 11) spreadGrids[loc].direction = new Vector2Int(0, 1);
            else if (_dir == 12) spreadGrids[loc].direction = new Vector2Int(-1, 0);
            else if (_dir == 13) spreadGrids[loc].direction = new Vector2Int(1, 0);
            else if (_dir == 14) spreadGrids[loc].direction = new Vector2Int(0, -1);
            else if (_dir == 23) spreadGrids[loc].direction = new Vector2Int(-1, 1);
            else if (_dir == 24) spreadGrids[loc].direction = new Vector2Int(1, 1);
            else if (_dir == 26) spreadGrids[loc].direction = new Vector2Int(-1, -1);
            else if (_dir == 27) spreadGrids[loc].direction = new Vector2Int(1, -1);
            else spreadGrids[loc].direction = Vector2Int.zero;
        }
    }
    public void RemoveSpreadGrid(int _x, int _y) {
        string loc = _x.ToString() + "," + _y.ToString();
        if (spreadGrids.ContainsKey(loc))
        {
            spreadGrids.Remove(loc);
            spreadGridNmae.Remove(loc);
        }
    }

    public void Spread() {
        for (int i = spreadGridNmae.Count - 1; i >= 0; i--) {
            if (spreadGrids[spreadGridNmae[i]].stopSpread) {
                spreadGrids.Remove(spreadGridNmae[i]);
                spreadGridNmae.RemoveAt(i);
            }
        }
    }
    public void AddNewPoint() { 
    
    }
}
