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
    public bool Reverse {
        get { return reverse; }
    }
    int curPatrolPointID = 1;
    Path patrolPath;
    Path reversePath;
    Path curPatrolPath;
    public Path CurrentPath {
        get { return patrolPath; }
    }
    public List<Vector3> pathPoints;
    public List<PatrolManager.PatrolGraphNode> pathPatrolGraphNode;

    public List<PatrolManager.PatrolGraphNode> newBranchGraphNode;

    public bool TBranch = false;

    public Dictionary<Vector3, int> lookAroundPoints = new Dictionary<Vector3, int>();
    public int LookAroundPoints(int id) {
        if (lookAroundPoints.ContainsKey(curPatrolPath.lookPoints[id])) return lookAroundPoints[curPatrolPath.lookPoints[id]];
        else return -1;
    }
    //int[] lookAroundPoints;

    public Vector3 startPos {
        get { return curPatrolPath.lookPoints[0]; }
    }

    Enemy enemy;
    public Enemy patrolEnemy{
        get { return enemy; }
    }

    Vector3 branchEnd;
    public Vector3 BranchEnd {
        get { return branchEnd; }
    }
    public void AddNewBranchEndLook(Vector3 pos) {
        branchEnd = pos;
        if (!lookAroundPoints.ContainsKey(pos))
        {
            lookAroundPoints.Add(pos, 1);
        }
    }
    public void RemoveBranchEndLook()
    {
        if (lookAroundPoints.ContainsKey(branchEnd))
        {
            lookAroundPoints.Remove(branchEnd);
        }
    }

    //固定路線
    public PatrolPath(bool cycle, List<Vector3> patrolPoint, List<PatrolManager.PatrolGraphNode> patrolGraphNode, float turnDst, int[] looks)
    {
        reverse = false;
        curPatrolPointID = 1;
        cycleType = cycle;
        pathPoints = patrolPoint;
        pathPatrolGraphNode = patrolGraphNode;
        bool lastLook = false;
        Vector3[] points = new Vector3[patrolPoint.Count];
        points[0] = patrolPoint[0];
        //points[patrolPoint.Count - 1] = patrolPoint[patrolPoint.Count - 1];
        for (int i = 1; i < patrolPoint.Count; i++)
        {
            float angle = .0f;
            if (i < patrolPoint.Count - 1) angle = Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[i + 1] - patrolPoint[i]));
            else angle = cycle ? Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[1] - patrolPoint[i])) : 0.0f;
            points[i] = patrolPoint[i];
        }
        for (int i = 0; i < looks.Length; i++) {
            lookAroundPoints.Add(patrolPoint[i], looks[i]);
        }

        if (cycle)
        {
            patrolPath = new Path(points, turnDst);
        }
        else
        {
            patrolPath = new Path(points, turnDst);
            System.Array.Reverse(points);
            reversePath = new Path(points, turnDst);
        }
        curPatrolPath = patrolPath;
    }


    //第一次建立路線
    public PatrolPath(bool cycle, List<Vector3> patrolPoint, List<PatrolManager.PatrolGraphNode> patrolGraphNode, float turnDst) {
        reverse = false;
        curPatrolPointID = 1;
        cycleType = cycle;
        pathPoints = patrolPoint;
        pathPatrolGraphNode = patrolGraphNode;
        bool lastLook = false;
        Vector3[] points = new Vector3[patrolPoint.Count];
        points[0] = patrolPoint[0];
        //points[patrolPoint.Count - 1] = patrolPoint[patrolPoint.Count - 1];
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
                    lookAroundPoints.Add(patrolPoint[i], Random.Range(1, 3));
                    lastLook = true;
                }
                else
                {
                    lastLook = false;
                }
            }
            else if (Random.Range(0.0f, 1.0f) < 0.3f)
            {
                float length = (patrolPoint[i] - patrolPoint[i - 1]).sqrMagnitude;
                //如果不是繞圈，跟兩端點太近的節點也不選為旋轉點，或是上個點是旋轉的也是
                if ((lastLook || (!cycle && (i == patrolPoint.Count - 2 || i == 1))) ? (length > 40.0f) : true)
                {
                    lookAroundPoints.Add(patrolPoint[i], Random.Range(1, 3));
                    lastLook = true;
                }
                else {
                    lastLook = false;
                }

            }
            else
            {
                lastLook = false;
            }
            points[i] = patrolPoint[i];
        }

        if (cycle)
        {
            patrolPath = new Path(points, turnDst);
        }
        else {
            if(Random.Range(0.0f, 1.0f) < 0.3f) lookAroundPoints.Add(patrolPoint[0], Random.Range(1, 3));
            //if (!patrolPoint.Contains(patrolPoint[patrolPoint.Count - 1])) lookAroundPoints.Add(patrolPoint[patrolPoint.Count - 1], 1);
            patrolPath = new Path(points, turnDst);
            System.Array.Reverse(points);
            reversePath = new Path(points, turnDst);
        }
        curPatrolPath = patrolPath;
    }

    //動態更新路線
    public PatrolPath(bool cycle, List<Vector3> patrolPoint, List<PatrolManager.PatrolGraphNode> patrolGraphNode,Dictionary<Vector3, int>lookAround,  List<Vector3> branchPos, float turnDst)
    {
        reverse = false;
        curPatrolPointID = 1;
        cycleType = cycle;
        lookAroundPoints = lookAround;
        pathPoints = patrolPoint;
        pathPatrolGraphNode = patrolGraphNode;
        bool lastLook = false;
        Vector3[] points = new Vector3[patrolPoint.Count];
        points[0] = patrolPoint[0];
        points[patrolPoint.Count - 1] = patrolPoint[patrolPoint.Count - 1];
        for (int i = 1; i < patrolPoint.Count; i++)
        {
            points[i] = patrolPoint[i];
            if (branchPos.Contains(patrolPoint[i]) || branchPos.Contains(patrolPoint[i - 1])) continue;  //分支不做旋轉巡視
            float angle = .0f;
            if (i < patrolPoint.Count - 1) angle = Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[i + 1] - patrolPoint[i]));
            else angle = cycle ? Vector3.Angle((patrolPoint[i] - patrolPoint[i - 1]), (patrolPoint[1] - patrolPoint[i])) : 0.0f;
        }

        if (cycle)
        {
            patrolPath = new Path(points, turnDst);
        }
        else
        {
            patrolPath = new Path(points, turnDst);
            System.Array.Reverse(points);
            reversePath = new Path(points, turnDst);
        }
        curPatrolPath = patrolPath;

    }

    //回原本路徑路線
    public PatrolPath(List<Vector3> patrolPoint, List<PatrolManager.PatrolGraphNode> patrolGraphNode, float turnDst)
    {
        reverse = false;
        curPatrolPointID = 1;
        cycleType = false;
        pathPoints = patrolPoint;
        pathPatrolGraphNode = patrolGraphNode;
        bool lastLook = false;
        Vector3[] points = new Vector3[patrolPoint.Count];
        points[0] = patrolPoint[0];
        //points[patrolPoint.Count - 1] = patrolPoint[patrolPoint.Count - 1];
        for (int i = 1; i < patrolPoint.Count; i++)
        {
            points[i] = patrolPoint[i];
        }
        patrolPath = new Path(points, turnDst);
        curPatrolPath = patrolPath;
    }

    public void SetNewBranchNode(List<PatrolManager.PatrolGraphNode> nodes)
    {
        newBranchGraphNode = nodes;
    }
    public void SetEnemy(Enemy _enemy)
    {
        enemy = _enemy;
    }

    public void StartPatrolAtNewBranchEnd() {
        curPatrolPointID = pathPoints.IndexOf(branchEnd);
        if (curPatrolPointID >= pathPoints.Count - 1)
        {
            curPatrolPointID = 1;
            reverse = true;
            curPatrolPath = reversePath;
            reversePath = patrolPath;
            patrolPath = curPatrolPath;
        }
        else if (curPatrolPointID == 0)
        {
            curPatrolPointID = 1;
        }
        else curPatrolPointID++;
        Debug.Log("new start iddddddddd   " + curPatrolPointID);
    }

    public bool MoveInPatrolRoute(Vector3 pos, ref Vector3 nextPos, ref int lookAroundNum)
    {
        Vector2 pos2D = new Vector2(pos.x, pos.z);
        //for (int i = 0; i < curPatrolPath.lookPoints.Length; i++) {
        //    Debug.Log(curPatrolPath.lookPoints[i]);
        //}
        if (curPatrolPath == null || curPatrolPath.turnBoundaries == null || curPatrolPath.turnBoundaries[curPatrolPointID] == null) {
            Debug.Log(patrolEnemy.transform.name + "  is retard ");
            patrolEnemy.ErrorCatch();
            return false;
        } 
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

                    lookAroundNum = (lookAroundPoints.ContainsKey(curPatrolPath.lookPoints[0])) ? lookAroundPoints[curPatrolPath.lookPoints[0]] : 0;
                    //if (!reverse) lookAroundNum = lookAroundPoints[curPatrolPath.lookPoints[0]];
                    //else lookAroundNum = lookAroundPoints[curPatrolPath.lookPoints[0]]; 
                }
                else {
                    lookAroundNum = (lookAroundPoints.ContainsKey(curPatrolPath.lookPoints[curPatrolPath.finishLineIndex])) ? lookAroundPoints[curPatrolPath.lookPoints[curPatrolPath.finishLineIndex]] : 0;
                }
                nextPos = curPatrolPath.lookPoints[curPatrolPointID];

            }
            else
            {
                lookAroundNum = (lookAroundPoints.ContainsKey(curPatrolPath.lookPoints[curPatrolPointID])) ? lookAroundPoints[curPatrolPath.lookPoints[curPatrolPointID]] : 0;
                curPatrolPointID++;
                nextPos = curPatrolPath.lookPoints[curPatrolPointID];


                //if(!reverse)lookAroundNum = lookAroundPoints[curPatrolPointID - 1];
                //else lookAroundNum = lookAroundPoints[lookAroundPoints.Length - curPatrolPointID];
            }
            return true;
        }
        else {
            nextPos = curPatrolPath.lookPoints[curPatrolPointID];
            return false;
        } 
    }

    public bool MoveBackPatrolRoute(Vector3 pos, ref Vector3 nextPos)
    {
        Vector2 pos2D = new Vector2(pos.x, pos.z);

        if (curPatrolPath.lookPoints.Length < 2) {
            Vector3 diff = curPatrolPath.lookPoints[0] - pos;
            nextPos = curPatrolPath.lookPoints[0];
            if (diff.sqrMagnitude < 0.1f) return true;
            else return false;
        }

        if (curPatrolPath.turnBoundaries[curPatrolPointID].HasCrossedLine(pos2D))
        {
            if (curPatrolPointID == curPatrolPath.finishLineIndex)
            {
                nextPos = curPatrolPath.lookPoints[curPatrolPointID];
                return false;
            }
            else
            {
                curPatrolPointID++;
                nextPos = curPatrolPath.lookPoints[curPatrolPointID];
            }
        }
        else
        {
            nextPos = curPatrolPath.lookPoints[curPatrolPointID];
        }
        return true;
    }

    public void SetPathReverse() {
        curPatrolPath = reversePath;
        reversePath = patrolPath;
        patrolPath = curPatrolPath;
        reverse = !reverse;
    }
    public Vector3  GetPathPoint(int id) {
        Debug.Log(id);
        return curPatrolPath.lookPoints[id];
    }
    public void SetPatrolPathID(int id) {
        curPatrolPointID = id;
    }
    public void ResetPath() {
        curPatrolPointID = 1;
        if (reverse) {
            reverse = false;
            curPatrolPath = reversePath;
            reversePath = patrolPath;
            patrolPath = curPatrolPath;
        }
    }
}
