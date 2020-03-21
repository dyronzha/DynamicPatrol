using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder;

public class PatrolArea : MonoBehaviour
{
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

    List<Node> upPoints = new List<Node>();
    List<Node> downPoints = new List<Node>();
    List<Node> leftPoints = new List<Node>();
    List<Node> rightPoints = new List<Node>();

    int num = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddNewPoint() { 
    
    }
}
