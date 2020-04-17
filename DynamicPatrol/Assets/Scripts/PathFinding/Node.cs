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

        public string colliderName = string.Empty;
        string allAreaName = string.Empty;
        string unwalkableAreaName = string.Empty;
        public string AllAreaName{
            get {
                if (walkable) return allAreaName;
                else return unwalkableAreaName;
            }
        }
        public int borderNum = 0;

        Dictionary<string, int> AreaInfo = new Dictionary<string, int>();  //1234 上下左右
        Dictionary<string, int> UnwalkableAreaInfo = new Dictionary<string, int>();
        public Dictionary<string, int> AllAreaInfos
        {
            get
            {
                if (walkable) return AreaInfo;
                else return UnwalkableAreaInfo;
            }
        }


        public int AreaNum{
            get { return AreaInfo.Count; }
        }
        public int AreaNumWithBorder {
            get { return AreaInfo.Count + borderNum; }
        }
        public int dirr = 0; //方便顯示位置顏色用，之後可以刪掉

        //public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty)
        //{
        //    walkable = _walkable;
        //    worldPosition = _worldPos;
        //    gridX = _gridX;
        //    gridY = _gridY;
        //    movementPenalty = _penalty;
        //}
        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty, string _area)
        {
            walkable = _walkable;
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
            movementPenalty = _penalty;
            colliderName = _area;
            if (!walkable)
            {
                UnwalkableAreaInfo.Add(_area, 0);
                unwalkableAreaName = _area;
            }
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
            if (walkable)
            {
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
            else {
                if (!UnwalkableAreaInfo.ContainsKey(id))
                {
                    if (UnwalkableAreaInfo.Count == 0) unwalkableAreaName = id;
                    else unwalkableAreaName += id;
                    UnwalkableAreaInfo.Add(id, dir);
                    dirr = dir;
                    //Debug.Log(gridX + "," + gridY + "  加入 " + id + " 總共有 " + allAreaName);
                }
            }
        }
        public void AddAreas(string allarea, Dictionary<string, int> newAreas)
        {
            if (allAreaName.Contains(allarea)) return; //確定自己有包含新區域
            foreach (KeyValuePair<string, int> item in newAreas)
            {
                Debug.Log(gridX + "," + gridY +  "  確認 " + item.Key + "有沒有");
                if (!AreaInfo.ContainsKey(item.Key))
                {
                    Debug.Log("新增 " + item.Key);
                    int dir = dirr;
                    if (walkable)
                    {
                        dir = ((item.Value - gridY) > 0) ? 2 : 1;
                        AreaInfo.Add(item.Key, dir);
                        allAreaName += item.Key;
                    }
                    dirr = dir;
                }
                else {
                    Debug.Log("已有 " + item.Key);
                }
            }
           
        }

        public void CheckAddAreas(ref List<string> allAreas) {
            foreach (KeyValuePair<string, int> item in AreaInfo)
            {
                if (!allAreas.Contains(item.Key))
                {
                    allAreas.Add(item.Key);
                }
            }
        }

        public void CheckRemoveAreas(Dictionary<string, int> upAreas, ref Dictionary<string, int> allAreas, ref string lastArea) {
            foreach (KeyValuePair<string, int> item in upAreas) {
                Debug.Log("與現在的比 " + item.Key);
            }
            foreach (KeyValuePair<string, int> item in AreaInfo)
            {
                Debug.Log(gridX + "," + gridY + "有 " + item.Key + " 比較 " + lastArea);
                //指判斷刪除橫向的，且沒有的區域
                if (item.Value > 2 && !upAreas.ContainsKey(item.Key) && allAreas.ContainsKey(item.Key))
                {
                    allAreas.Remove(item.Key);

                    Debug.Log("移除在" + lastArea.IndexOf(item.Key) + "的" + item.Key);
                    lastArea = lastArea.Remove(lastArea.IndexOf(item.Key), item.Key.Length);
                    Debug.Log("  剩下" + lastArea + "。");
                }
            }
            if (!walkable)
            {
                Debug.Log(gridX + "," + gridY + "有 " + colliderName + " 比較 " + lastArea);
                //指判斷刪除橫向的，且沒有的區域
                if (!upAreas.ContainsKey(colliderName) && allAreas.ContainsKey(colliderName))
                {
                    allAreas.Remove(colliderName);

                    Debug.Log("移除在" + lastArea.IndexOf(colliderName) + "的" + colliderName);
                    lastArea = lastArea.Remove(lastArea.IndexOf(colliderName), colliderName.Length);
                    Debug.Log("  剩下" + lastArea + "。");
                }
            }

        }
    }

}

