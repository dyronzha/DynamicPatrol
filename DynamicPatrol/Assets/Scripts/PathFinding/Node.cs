using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinder
{
    public class Node : IHeapItem<Node>
    {


        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;
        public int movementPenalty;
        public int extentPenalty = 0;

        public int gCost;
        public int hCost;
        public Node parent;
        int heapIndex;

        public string colliderName;
        public string allAreaName = "E";
        public int borderNum = 0;

        Dictionary<string, int> AreaInfo = new Dictionary<string, int>();  //1234 上下左右
        public Dictionary<string, int> AllAreaInfos {
            get {
                return AreaInfo;
            }
        }
        public int AreaNum{
            get { return AreaInfo.Count; }
        }
        public int AreaNumWithBorder {
            get { return AreaInfo.Count + borderNum; }
        }
        public int dirr = 0; //方便顯示位置顏色用，之後可以刪掉

        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty)
        {
            walkable = _walkable;
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
            movementPenalty = _penalty;
        }
        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty, string _area)
        {
            walkable = _walkable;
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
            movementPenalty = _penalty;
            colliderName = _area;
        }

        public int fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }

        public void AddPenalty(int value)
        {

            if (extentPenalty + value + movementPenalty <= 90)
            {
                extentPenalty += value;
            }
        }

        public void AddArea(string id, int dir) {
            if (!AreaInfo.ContainsKey(id))
            {
                if (AreaInfo.Count == 0) allAreaName = id;
                else allAreaName += id;
                AreaInfo.Add(id, dir);
                dirr = dir;
                //Debug.Log(gridX + "," + gridY + "  加入 " + id + " 總共有 " + allAreaName);
            }
            //else Debug.Log("已有 " + id + " 區域");
        }
        public void AddAreas(string allarea, Dictionary<string, int> newAreas)
        {
            //if (allarea.Contains(allAreaName)) return; //確定新區域都有包含
            foreach (KeyValuePair<string, int> item in newAreas)
            {
                if (!AreaInfo.ContainsKey(item.Key)) {
                    int dir = dirr;
                    if (walkable) {
                        dir = ((item.Value - gridY) > 0) ? 2 : 0;
                        AreaInfo.Add(item.Key, dir);
                        allAreaName += item.Key;
                    }
                    dirr = dir;
                }
            }
           
        }

        public void CheckAddAreas(ref List<string> allAreas) {
            foreach (KeyValuePair<string, int> item in AllAreaInfos)
            {
                if (!allAreas.Contains(item.Key))
                {
                    allAreas.Add(item.Key);
                }
            }
        }

        public void CheckRemoveAreas(Dictionary<string, int> newAreas, ref Dictionary<string, int> allAreas) {
            foreach (KeyValuePair<string, int> item in AllAreaInfos) {
                if (!newAreas.ContainsKey(item.Key)) {
                    allAreas.Remove(item.Key);
                }
            }
        }
    }

}

