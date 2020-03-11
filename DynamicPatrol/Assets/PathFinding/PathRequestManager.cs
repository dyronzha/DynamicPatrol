using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PathFinder
{
    public class PathRequestManager : MonoBehaviour
    {

        //Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        List<PathRequest> pathRequestList = new List<PathRequest>();

        PathRequest currentPathRequest;

        static PathRequestManager instance;
        PathFinding pathFinding;

        PathFinding[] pathFindings;

        bool isProcessingPath;

        void Awake()
        {
            instance = this;
            pathFinding = GetComponent<PathFinding>();
        }

        //基礎要求路徑
        public static PathRequest RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> _successCbk)
        {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, _successCbk);
            //instance.pathRequestQueue.Enqueue(newRequest);
            instance.pathRequestList.Add(newRequest);
            instance.TryProcessNext();
            //Debug.Log("add new finding path request   " + instance.pathRequestList.IndexOf(newRequest));
            return newRequest;
        }
        //比對舊路徑要求路徑
        public static PathRequest RequestPath(PathRequest oldRequest, Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> _successCbk)
        {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, _successCbk);

            if (instance.CheckProcessingRequest(oldRequest))   //如果目前正在搜尋且跟舊的一樣回傳null
            {
                return null;
            }

            if (oldRequest != null && instance.pathRequestList.Contains(oldRequest))   //有在排隊中更新為新的request
            {
                instance.pathRequestList[instance.pathRequestList.IndexOf(oldRequest)] = newRequest;
            }
            else
            {
                instance.pathRequestList.Add(newRequest);
            }
            instance.TryProcessNext();

            return newRequest;


            //instance.pathRequestQueue.Enqueue(newRequest);
        }

        //特定區域要求路徑
        public static PathRequest RequestPath(PathFinding pathfinding, PathRequest oldRequest, Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> _successCbk)
        {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, _successCbk, pathfinding);

            if (instance.CheckProcessingRequest(oldRequest))   //如果目前正在搜尋且跟舊的一樣回傳null
            {
                return null;
            }

            if (oldRequest != null && instance.pathRequestList.Contains(oldRequest))   //有在排隊中更新為新的request
            {
                instance.pathRequestList[instance.pathRequestList.IndexOf(oldRequest)] = newRequest;
            }
            else
            {
                instance.pathRequestList.Add(newRequest);
            }
            instance.TryProcessNext();

            return newRequest;
        }


        void TryProcessNext()
        {
            //if (!isProcessingPath && pathRequestQueue.Count > 0)
            //{
            //    currentPathRequest = pathRequestQueue.Dequeue();
            //    isProcessingPath = true;
            //    pathFinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
            //}
            if (!isProcessingPath && pathRequestList.Count > 0)
            {
                currentPathRequest = pathRequestList[0];
                pathRequestList.RemoveAt(0);
                isProcessingPath = true;
                pathFinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
            }
        }
        void AreaTryProcessNext()
        {
            //if (!isProcessingPath && pathRequestQueue.Count > 0)
            //{
            //    currentPathRequest = pathRequestQueue.Dequeue();
            //    isProcessingPath = true;
            //    pathFinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
            //}
            if (!isProcessingPath && pathRequestList.Count > 0)
            {
                currentPathRequest = pathRequestList[0];
                pathRequestList.RemoveAt(0);
                isProcessingPath = true;
                currentPathRequest.pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
            }
        }

        bool CheckProcessingRequest(PathRequest request)
        {  //如果要求是正在進行搜尋回傳true
            if (isProcessingPath && currentPathRequest == request) return true;
            else return false;
        }

        public static void ReplaceRequest(string name, PathRequest request)
        {
            instance.pathRequestList[instance.pathRequestList.IndexOf(request)] = null;

            //Debug.Log(name + " cancle finding path request   " + instance.pathRequestList.IndexOf(request));
            if (instance.pathRequestList.Contains(request)) instance.pathRequestList.Remove(request);
        }
        public static void CancleRequest(PathRequest request)
        {
            //Debug.Log("  cancle finding path request   " +  instance.pathRequestList.IndexOf(request));
            if (instance.pathRequestList.Contains(request)) instance.pathRequestList.Remove(request);
        }


        public void FinishedProcessingPath(Vector3[] path, bool success)
        {
            //Debug.Log("finisg find path");
            currentPathRequest.callback(path, success);
            isProcessingPath = false;
            TryProcessNext();
        }


        public static void ClearExtendPenalty()
        {
            instance.pathFinding.ClearGridExtendPenalty();
        }

        //public void FailProcessingPath() {
        //    isProcessingPath = false;
        //}

        public class PathRequest
        {
            public PathFinding pathfinding;
            public Vector3 pathStart;
            public Vector3 pathEnd;
            public Action<Vector3[], bool> callback;

            public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _successCbk)
            {
                pathStart = _start;
                pathEnd = _end;
                callback = _successCbk;
            }

            public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _successCbk, PathFinding _pathFinding)
            {
                pathStart = _start;
                pathEnd = _end;
                callback = _successCbk;
                pathfinding = _pathFinding;
            }
        }
    }
}


