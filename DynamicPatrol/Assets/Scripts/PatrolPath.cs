using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder;

public class PatrolPath
{
    bool cycleType;
    Path path;
    Path reversePath;
    Path curPath;
    public Path CurrentPath {
        get { return path; }
    }

    int[] lookAroundPoints;

    public PatrolPath(bool cycle, List<Vector3> patrolPoint, float turnDst) {
        cycleType = cycle;
        lookAroundPoints = new int[patrolPoint.Count];

        Vector3[] points = new Vector3[patrolPoint.Count];
        points[0] = patrolPoint[0];
        for (int i = 1; i < patrolPoint.Count-1; i++) {
            float angle = Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[i + 1] - patrolPoint[i]));
            if (angle >= 60.0f) {
                lookAroundPoints[i] = Random.Range(1, 3);
            }
            points[i] = patrolPoint[i];
        }
        if (cycle)
        {
            path = new Path(points, turnDst);
        }
        else {
            path = new Path(points, turnDst);
            System.Array.Reverse(points);
            reversePath = new Path(points, turnDst);
        }
        curPath = path;
    }


    // Start is called before the first frame update
    void Start()
    {
        List<Vector3> hh = new List<Vector3>();
        path = new Path(hh, 2.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
