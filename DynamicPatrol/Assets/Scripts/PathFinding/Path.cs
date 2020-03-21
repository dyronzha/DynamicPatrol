using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinder {
    public class Path
    {
        public int curPointID = 1;
        public readonly Vector3[] lookPoints;
        public readonly Line[] turnBoundaries;
        public readonly int finishLineIndex;
        //public readonly bool[] lookArounds;
        //public readonly int[] lookNums;
        //public readonly Vector3[] lookForwards;
        //public bool CurLookAround
        //{
        //    get { return lookArounds[curPointID]; }
        //}
        //public int CurLookNum
        //{
        //    get { return lookNums[curPointID]; }
        //}
        //public Vector3 CurLookFWD
        //{
        //    get { return lookForwards[curPointID]; }
        //}



        public Path(Vector3[] waypoints, Vector3 startPos, float turnDst) //float stoppingDst
        {
            lookPoints = new Vector3[waypoints.Length];
            turnBoundaries = new Line[lookPoints.Length];
            finishLineIndex = turnBoundaries.Length - 1;

            Vector2 previousPoint = V3ToV2(startPos);
            for (int i = 0; i < waypoints.Length; i++)
            {
                lookPoints[i] = new Vector3(waypoints[i].x, 0.0f, waypoints[i].z);
                Vector2 currentPoint = V3ToV2(lookPoints[i]);
                Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
                Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
                turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
                previousPoint = turnBoundaryPoint;
            }

            //float dstFromEndPoint = 0;
            //for (int i = lookPoints.Length - 1; i > 0; i--)
            //{
            //    dstFromEndPoint += Vector3.Distance(lookPoints[i], lookPoints[i - 1]);
            //    if (dstFromEndPoint > stoppingDst)
            //    {
            //        canAttckIndex = i;
            //        break;
            //    }
            //}
        }

        public Path(Vector3[] waypoints, float turnDst) //float stoppingDst
        {
            lookPoints = new Vector3[waypoints.Length];
            turnBoundaries = new Line[lookPoints.Length];
            finishLineIndex = turnBoundaries.Length - 1;

            Vector2 previousPoint = V3ToV2(waypoints[0]);
            for (int i = 1; i < waypoints.Length; i++)
            {
                lookPoints[i] = new Vector3(waypoints[i].x, 0.0f, waypoints[i].z);
                Vector2 currentPoint = V3ToV2(lookPoints[i]);
                Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
                Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
                turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
                previousPoint = turnBoundaryPoint;
            }
        }

        //public Path(BloodBond.PatrolPoint[] waypoints, float turnDst) //float stoppingDst
        //{
        //    lookPoints = new Vector3[waypoints.Length];
        //    turnBoundaries = new Line[lookPoints.Length];
        //    finishLineIndex = turnBoundaries.Length - 1;
        //    lookArounds = new bool[lookPoints.Length];
        //    lookNums = new int[lookPoints.Length];
        //    lookForwards = new Vector3[lookPoints.Length];
        //    Vector2 previousPoint = V3ToV2(waypoints[0].transform.position);

        //    if (waypoints[0].lookAround) {
        //        lookArounds[0] = true;
        //        lookNums[0] = waypoints[0].lookNum;
        //        lookForwards[0] = waypoints[0].transform.forward;
        //        lookPoints[0] = waypoints[0].transform.position;
        //    }
            

        //    for (int i = 1; i < waypoints.Length; i++)
        //    {
        //        lookPoints[i] = new Vector3(waypoints[i].transform.position.x, 0.0f, waypoints[i].transform.position.z);
        //        Vector2 currentPoint = V3ToV2(lookPoints[i]);
        //        Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
        //        Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
        //        turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
        //        previousPoint = turnBoundaryPoint;

        //        if (waypoints[i].lookAround)
        //        {
        //            lookArounds[i] = true;
        //            lookNums[i] = waypoints[i].lookNum;
        //            lookForwards[i] = waypoints[i].transform.forward;
        //            lookPoints[i] = waypoints[i].transform.position;
        //        }
        //    }
        //}


        Vector2 V3ToV2(Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

        public void DrawWithGizmos()
        {

            Gizmos.color = Color.black;
            foreach (Vector3 p in lookPoints)
            {
                Gizmos.DrawCube(p + Vector3.up, Vector3.one);
            }

            Gizmos.color = Color.white;
            foreach (Line l in turnBoundaries)
            {
                l.DrawWithGizmos(10);
            }

        }

    }
}


