using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder;

public class PatrolPath
{
    bool cycleType;
    public bool CycleRoute {
        get { return cycleType; }
    }
    bool reverse = false;
    int curPatrolPointID = 1;
    Path patrolPath;
    Path reversePath;
    Path curPatrolPath;
    public Path CurrentPath {
        get { return patrolPath; }
    }
    public List<Vector3> pathPoints;
    public List<PatrolManager.PatrolGraphNode> pathPatrolGraphNode;

    int[] lookAroundPoints;
    public int LookAroundPoints(int id) {
        return ((!reverse) ? (lookAroundPoints[id]) : (lookAroundPoints[lookAroundPoints.Length - 1 - id]));
    }

    public Vector3 startPos {
        get { return curPatrolPath.lookPoints[0]; }
    }

    Enemy enemy;
    public Enemy patrolEnemy{
        get { return enemy; }
    }

    public PatrolPath(bool cycle, List<Vector3> patrolPoint, List<PatrolManager.PatrolGraphNode> patrolGraphNode, float turnDst) {
        reverse = false;
        curPatrolPointID = 1;
        cycleType = cycle;
        lookAroundPoints = new int[patrolPoint.Count];
        pathPoints = patrolPoint;
        pathPatrolGraphNode = patrolGraphNode;
        bool lastLook = false;
        Vector3[] points = new Vector3[patrolPoint.Count];
        points[0] = patrolPoint[0];
        points[patrolPoint.Count - 1] = patrolPoint[patrolPoint.Count - 1];
        for (int i = 1; i < patrolPoint.Count; i++) {
            float angle = .0f;
            if (i < patrolPoint.Count-1) angle = Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[i + 1] - patrolPoint[i]));
            else angle = cycle ? Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[1] - patrolPoint[i])) : 0.0f;

            if (angle >= 60.0f)
            {
                float length = (patrolPoint[i] - patrolPoint[i - 1]).sqrMagnitude;
                Debug.Log(i+ "   " + patrolPoint[i] + "    " + patrolPoint[i-1] + "leeeeength  " + length);
                //如果不是繞圈，跟兩端點太近的節點也不選為旋轉點，或是上個點是旋轉的也是
                if ((lastLook || (!cycle && (i == patrolPoint.Count - 2 || i == 1))) ? (length > 40.0f) : true)
                {
                    lookAroundPoints[i] = Random.Range(1, 3);
                    lastLook = true;
                }
                else
                {
                    lookAroundPoints[i] = 0;
                    lastLook = false;
                }
            }
            else if (Random.Range(0.0f, 1.0f) < 0.3f)
            {
                float length = (patrolPoint[i] - patrolPoint[i - 1]).sqrMagnitude;
                //如果不是繞圈，跟兩端點太近的節點也不選為旋轉點，或是上個點是旋轉的也是
                if ((lastLook || (!cycle && (i == patrolPoint.Count - 2 || i == 1))) ? (length > 40.0f) : true)
                {
                    lookAroundPoints[i] = Random.Range(1, 3);
                    lastLook = true;
                }
                else {
                    lookAroundPoints[i] = 0;
                    lastLook = false;
                }

            }
            else
            {
                lookAroundPoints[i] = 0;
                lastLook = false;
            }
            points[i] = patrolPoint[i];
        }

        if (cycle)
        {
            patrolPath = new Path(points, turnDst);
        }
        else {
            patrolPath = new Path(points, turnDst);
            System.Array.Reverse(points);
            reversePath = new Path(points, turnDst);
        }
        curPatrolPath = patrolPath;
    }

    public void SetEnemy(Enemy _enemy) {
        enemy = _enemy;
    }

    public bool MoveInPatrolRoute(Vector3 pos, ref Vector3 nextPos, ref int lookAroundNum)
    {
        Vector2 pos2D = new Vector2(pos.x, pos.z);
        if (curPatrolPath.turnBoundaries[curPatrolPointID].HasCrossedLine(pos2D))
        {
            if (curPatrolPointID == curPatrolPath.finishLineIndex)
            {
                curPatrolPointID = 1;
                if (!cycleType)
                {
                    curPatrolPath = reversePath;
                    reversePath = patrolPath;
                    patrolPath = curPatrolPath;
                    reverse = !reverse;

                    if (!reverse) lookAroundNum = lookAroundPoints[curPatrolPath.finishLineIndex];
                    else lookAroundNum = lookAroundPoints[0]; 
                }
                else {
                    lookAroundNum = lookAroundPoints[curPatrolPath.finishLineIndex];
                }
                nextPos = curPatrolPath.lookPoints[curPatrolPointID];

            }
            else
            {
                curPatrolPointID++;
                nextPos = curPatrolPath.lookPoints[curPatrolPointID];
                if(!reverse)lookAroundNum = lookAroundPoints[curPatrolPointID - 1];
                else lookAroundNum = lookAroundPoints[lookAroundPoints.Length - curPatrolPointID];
            }
            return true;
        }
        else {
            nextPos = curPatrolPath.lookPoints[curPatrolPointID];
            return false;
        } 
    }
}
