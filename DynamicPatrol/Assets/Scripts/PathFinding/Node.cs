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

        public bool nearBorder = false;

        int areaID;
        public int AreaID{
            get { return areaID; }
        }

        public bool canChoose = true;
        string colliderName;
        public string ColliderName { get { return colliderName; } }
        string lastAreaName = string.Empty;
        public string LastAreaName{
            get {
                return lastAreaName;
            }
        }


        //23 11 24
        //12    13
        //26 14 27
        int direction = 0;
        public int Direction { get { return direction; } }
        public string locNmae;

        //Dictionary<string, int> AreaInfo = new Dictionary<string, int>();
        //public Dictionary<string, int> AllAreaInfos
        //{
        //    get
        //    {
        //        return AreaInfo;
        //    }
        //}


        //public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty)
        //{
        //    walkable = _walkable;
        //    worldPosition = _worldPos;
        //    gridX = _gridX;
        //    gridY = _gridY;
        //    movementPenalty = _penalty;
        //}
        public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty, string _name)
        {
            walkable = _walkable;
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
            movementPenalty = _penalty;
            colliderName= _name;
            locNmae = gridX + "," + gridY;
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

        public void AddArea(string name, int dir, PatrolManager patrolManager) {
            if (lastAreaName.Length == 0)
            {
                //未有區域，新增
                direction = dir;
                lastAreaName = name;
                patrolManager.FindAreaInDic(name).AddSpreadGrid(gridX, gridY, dir);
            }
            else {
                //一個以上，且名字不同，互相覆蓋，移除
                if (lastAreaName.CompareTo(name) != 0)
                {
                    patrolManager.FindAreaInDic(lastAreaName).RemoveSpreadGrid(gridX, gridY);
                    direction = 0;
                    lastAreaName = string.Empty;
                    canChoose = false;

                    Vector2Int pos = new Vector2Int(gridX, gridY);
                    if (!patrolManager.choosenNodeDic.ContainsKey(pos)) {
                        PatrolManager.SpreadNode node = new PatrolManager.SpreadNode();
                        if(name.CompareTo("Border") == 0 || lastAreaName.CompareTo("Border") == 0) node.choosenWeight = 10;
                        else node.choosenWeight = 80;
                        node.choosen = true;
                        node.choseNum++;
                        patrolManager.choosenNode.Add(node);
                        patrolManager.choosenNodeDic.Add(pos, node);
                    }
                    
                }
                //一個以上，名字相同，但同時有上下左右，互相覆蓋，移除
                else
                {
                    if (dir < 20 && direction < 20) {
                        patrolManager.FindAreaInDic(lastAreaName).RemoveSpreadGrid(gridX, gridY);
                        direction = 0;
                        lastAreaName = string.Empty;
                        canChoose = false;
                    }
                }

            }
        }

        public void OldAddArea(string name, int dir, bool _nearBorder, PatrolManager patrolManager) {
            if (lastAreaName.Length > 0)
            {
                if (nearBorder && _nearBorder)
                {
                    
                    direction += dir;
                    patrolManager.FindAreaInDic(name).UpdateSpreadGrid(gridX, gridY, direction);
                    Debug.Log(gridX + "," + gridY + "  corner " + direction);
                }
                else {
                    if (name.CompareTo(lastAreaName) == 0)
                    {
                        direction += dir;
                        patrolManager.FindAreaInDic(lastAreaName).UpdateSpreadGrid(gridX, gridY, direction);
                    }
                    else {
                        patrolManager.FindAreaInDic(lastAreaName).RemoveSpreadGrid(gridX, gridY);
                        lastAreaName += ("," + name);
                        direction = 0;
                        canChoose = false;
                    }

                } 
            }
            else {
                lastAreaName = name;
                direction = dir;
                nearBorder = _nearBorder;
                patrolManager.FindAreaInDic(name).AddSpreadGrid(gridX, gridY, dir);
            }
            
        }

        public void RemoveArea(PatrolManager patrolManager) {
            patrolManager.FindAreaInDic(lastAreaName).RemoveSpreadGrid(gridX, gridY);
            direction = 0;
            lastAreaName = string.Empty;
        }

    }

}

