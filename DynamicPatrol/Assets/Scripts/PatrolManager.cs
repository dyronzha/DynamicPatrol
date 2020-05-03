using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    bool skip = false;
    int ID = 0;
    int gridX, gridY;
    List<string> areaNames = new List<string>();
    Dictionary<string, PatrolArea> areaDic = new Dictionary<string, PatrolArea>();   //碰撞名和地區的字典

    bool spreading;
    public int maxChoosenWeight = 0;

    public class SpreadNode {
        public bool stop = false;
        public bool choosen = false;
        public int choseNum = 0;
        public bool walkable = false;
        public string fromArea = string.Empty;
        public bool current = false;
        public Vector2Int dir = new Vector2Int(0,0);
        public int choosenWeight = 0;
    }
    public SpreadNode[,] spreadGrid;
    public List<SpreadNode> choosenNode = new List<SpreadNode>();

    public Dictionary<Vector2Int, SpreadNode> choosenNodeDic = new Dictionary<Vector2Int, SpreadNode>();


    // Start is called before the first frame update
    void Start()
    {
        PathFinder.PathFindGrid pathFindGrid = transform.GetComponent<PathFinder.PathFindGrid>();
        spreadGrid = new SpreadNode[gridX, gridY];
        for (int i = 0; i < gridX; i++) {
            for (int j = 0; j < gridY; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                if (!choosenNodeDic.ContainsKey(pos))
                {
                    spreadGrid[i, j] = new SpreadNode();
                    spreadGrid[i, j].walkable = pathFindGrid.Grid[i, j].walkable;
                }
                else {
                    spreadGrid[i, j] = choosenNodeDic[pos];
                }
            }
        }


        for (int i = areaNames.Count - 1; i >= 0; i--)
        {
            //輪巡有的區域
            PatrolArea area = areaDic[areaNames[i]];
            for (int j = area.spreadGridNmae.Count - 1; j >= 0; j--)
            {
                //輪巡區域內的擴散格
                PatrolArea.SpreadGridNode currentNode = area.spreadGrids[area.spreadGridNmae[j]];
                spreadGrid[currentNode.x, currentNode.y].current = true;
                spreadGrid[currentNode.x, currentNode.y].fromArea = area.Name;
                spreadGrid[currentNode.x, currentNode.y].dir = currentNode.direction;
                currentNode.choosenWeight += 10;
            }
        }
        List<string> test = new List<string>();
        test.Add("aaa");
        test.Add("bbb");
        test.Add("ccc");
        test.Add("ddd");
        test.Add("eee");
        test.Add("aaa");
        Debug.Log(test.Contains("ccc"));
        Debug.Log(test[0]);
        Debug.Log(test[5]);
        Debug.Log(test.Contains("aaa"));
    }

    // Update is called once per frame
    void Update()
    {
        //return;
        
        if (Input.GetKeyDown(KeyCode.Space)) {
            CouculateGrid();
        }
        if (Input.GetKeyDown(KeyCode.Z)) {
            int count = 0;
            while (!skip) {
                count++;
                CouculateGrid();
                if (count > 10000) break;
            }
            
        }
    }

    void CouculateGrid() {
        skip = true;
        bool[,] hasC = new bool[gridX, gridY];
        for (int i = areaNames.Count - 1; i >= 0; i--)
        {
            //輪巡有的區域
            PatrolArea area = areaDic[areaNames[i]];
            for (int j = area.spreadGridNmae.Count - 1; j >= 0; j--)
            {
                skip = false;
                //輪巡區域內的擴散格
                PatrolArea.SpreadGridNode currentNode = area.spreadGrids[area.spreadGridNmae[j]];


                spreadGrid[currentNode.x, currentNode.y].current = true;
                spreadGrid[currentNode.x, currentNode.y].fromArea = area.Name;

                //當下這格已經是停止點 或 被選點
                if (spreadGrid[currentNode.x, currentNode.y].choosen) //spreadGrid[currentNode.x, currentNode.y].stop || 
                {
                    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                    area.spreadGridNmae.RemoveAt(j);
                }
                else
                {
                    //當下點也加入進整個地圖的參考
                    spreadGrid[currentNode.x, currentNode.y].dir = currentNode.direction;

                    int nextX = currentNode.x + currentNode.direction.x;
                    int nextY = currentNode.y + currentNode.direction.y;
                    //下一格超過grid範圍
                    if (nextX >= spreadGrid.GetLength(0) || nextY >= spreadGrid.GetLength(1) || nextX < 0 || nextY < 0)
                    {
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                        spreadGrid[currentNode.x, currentNode.y].stop = true;
                    }
                    //下一格是停止點
                    //else if (spreadGrid[nextX, nextY].stop)
                    //{
                    //    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                    //    area.spreadGridNmae.RemoveAt(j);
                    //}
                    //下一格是被選點
                    else if (spreadGrid[nextX, nextY].choosen)
                    {
                        spreadGrid[nextX, nextY].choseNum++;
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                    }
                    //下一格是障礙
                    else if (!spreadGrid[nextX, nextY].walkable)
                    {
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                        spreadGrid[currentNode.x, currentNode.y].stop = true;
                    }
                    //下一格已經算過
                    //else if (spreadGrid[nextX, nextY].dir.sqrMagnitude > 0.5f && spreadGrid[nextX, nextY].dir.x + currentNode.direction.x != 0 && spreadGrid[nextX, nextY].dir.y + currentNode.direction.y != 0)
                    //{
                    //    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                    //    area.spreadGridNmae.RemoveAt(j);
                    //}
                    //else if (area.hasCouculateNode[nextX, nextY] && (area.couculateNodeDir[nextX, nextY] - currentNode.direction).sqrMagnitude < 1.5f)
                    //{
                    //    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                    //    area.spreadGridNmae.RemoveAt(j);
                    //}
                    else
                    {

                        //斜邊特別新增隔壁節點
                        if (currentNode.direction.sqrMagnitude > 1.5f)
                        {
                           
                            //下一格是被選點
                            if (spreadGrid[nextX, currentNode.y].choosen)
                            {
                                spreadGrid[nextX, currentNode.y].choseNum++;
                            }
                            //下一格是障礙
                            else if (!spreadGrid[nextX, currentNode.y].walkable)
                            {
                                //spreadGrid[nextX, currentNode.y].stop = true;
                            }
                            //新增隔壁x方向節點
                            //前進方向互相撞到，存下來
                            else if ((spreadGrid[nextX, currentNode.y].dir.sqrMagnitude > 0.5f))//  && spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) != 0 || ((spreadGrid[nextX, currentNode.y].dir.x + currentNode.direction.x == 0) && spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) == 0)
                            {
                                spreadGrid[nextX, currentNode.y].choosen = true;
                                spreadGrid[nextX, currentNode.y].choseNum++;
                                choosenNode.Add(spreadGrid[nextX, currentNode.y]);
                                choosenNodeDic.Add(new Vector2Int(nextX, currentNode.y), spreadGrid[nextX, currentNode.y]);
                                Debug.Log(nextX + "," + currentNode.y + "   " + area.Name + "  hit " + spreadGrid[nextX, currentNode.y].fromArea + "   " + spreadGrid[nextX, currentNode.y].choosenWeight);
                                if (spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) != 0) spreadGrid[nextX, currentNode.y].choosenWeight += (currentNode.choosenWeight + 10);
                                else spreadGrid[nextX, currentNode.y].choosenWeight = Mathf.RoundToInt(0.3f * (spreadGrid[nextX, currentNode.y].choosenWeight + currentNode.choosenWeight + 10));
                                if (spreadGrid[nextX, currentNode.y].choosenWeight > maxChoosenWeight) maxChoosenWeight = spreadGrid[nextX, currentNode.y].choosenWeight;
                                Debug.Log(nextX + "," + currentNode.y + "  " + (currentNode.choosenWeight + 10) + " ---> " + spreadGrid[nextX, currentNode.y].choosenWeight);
                            }
                            //最後如果不是算過的，新增隔壁x方向節點
                            else //if (spreadGrid[nextX, currentNode.y].dir.sqrMagnitude < 0.5f) 
                            {
                                area.hasCouculateNode[nextX, nextY] = true;
                                area.couculateNodeDir[nextX, nextY] = new Vector2Int(currentNode.direction.x, 0);
                                spreadGrid[nextX, currentNode.y].dir = new Vector2Int(currentNode.direction.x, 0);
                                spreadGrid[nextX, currentNode.y].current = true;
                                spreadGrid[nextX, currentNode.y].fromArea = area.Name;
                                area.AddSpreadGridTilt(nextX, currentNode.y, new Vector2Int(currentNode.direction.x, 0), currentNode.choosenWeight+10);
                                spreadGrid[nextX, currentNode.y].choosenWeight = currentNode.choosenWeight + 10;
                            }
                            //else if (!(area.hasCouculateNode[nextX, currentNode.y] && (area.couculateNodeDir[nextX, currentNode.y] - currentNode.direction).sqrMagnitude < 1.5f))
                            //{
                            //    area.hasCouculateNode[nextX, nextY] = true;
                            //    area.couculateNodeDir[nextX, nextY] = new Vector2Int(currentNode.direction.x, 0);
                            //    spreadGrid[nextX, currentNode.y].dir = new Vector2Int(currentNode.direction.x, 0);
                            //    area.AddSpreadGridTilt(nextX, currentNode.y, new Vector2Int(currentNode.direction.x, 0));
                            //}

                            
                            //下一格是被選點
                            if (spreadGrid[currentNode.x, nextY].choosen)
                            {
                                spreadGrid[currentNode.x, nextY].choseNum++;
                            }
                            //下一格是障礙
                            else if (!spreadGrid[currentNode.x, nextY].walkable)
                            {
                                //spreadGrid[currentNode.x, nextY].stop = true;
                            }
                            //新增隔壁y方向節點
                            //前進方向互相撞到，存下來
                            else if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude > 0.5f) //&& spreadGrid[currentNode.x, nextY].fromArea.CompareTo(area.Name) != 0) ||  ((spreadGrid[currentNode.x, nextY].dir.y + currentNode.direction.y == 0) && spreadGrid[currentNode.x, nextY].fromArea.CompareTo(area.Name) == 0)
                            {
                                spreadGrid[currentNode.x, nextY].choosen = true;
                                spreadGrid[currentNode.x, nextY].choseNum++;
                                choosenNode.Add(spreadGrid[currentNode.x, nextY]);
                                choosenNodeDic.Add(new Vector2Int(currentNode.x, nextY), spreadGrid[currentNode.x, nextY]);
                                Debug.Log(currentNode.x + "," + nextY + "   " + area.Name + "  hit " + spreadGrid[nextX, currentNode.y].fromArea + "  " + spreadGrid[currentNode.x, nextY].choosenWeight);
                                if (spreadGrid[currentNode.x, nextY].fromArea.CompareTo(area.Name) != 0) spreadGrid[currentNode.x, nextY].choosenWeight += (currentNode.choosenWeight + 10);
                                else spreadGrid[currentNode.x, nextY].choosenWeight = Mathf.RoundToInt(0.3f * (spreadGrid[currentNode.x, nextY].choosenWeight + currentNode.choosenWeight + 10));
                                if (spreadGrid[currentNode.x, nextY].choosenWeight > maxChoosenWeight) maxChoosenWeight = spreadGrid[currentNode.x, nextY].choosenWeight;
                                Debug.Log(currentNode.x + "," + nextY + "  " + (currentNode.choosenWeight + 10) + " ---> " + spreadGrid[currentNode.x, nextY].choosenWeight);
                            }
                            //最後如果不是算過的，新增隔壁y方向節點
                            else // if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude < 0.5f)
                            {
                                area.hasCouculateNode[nextX, nextY] = true;
                                area.couculateNodeDir[nextX, nextY] = new Vector2Int(0, currentNode.direction.y);
                                spreadGrid[currentNode.x, nextY].dir = new Vector2Int(0, currentNode.direction.y);
                                spreadGrid[currentNode.x, nextY].current = true;
                                spreadGrid[currentNode.x, nextY].fromArea = area.Name;
                                area.AddSpreadGridTilt(currentNode.x, nextY, new Vector2Int(0, currentNode.direction.y), currentNode.choosenWeight+10);
                                spreadGrid[currentNode.x, nextY].choosenWeight = currentNode.choosenWeight + 10;
                                Debug.Log("add new  " + currentNode.x + "," + nextY + "     " + currentNode.choosenWeight + 10);
                            }
                            //else if (!(area.hasCouculateNode[currentNode.x, nextY] && (area.couculateNodeDir[currentNode.x, nextY] - currentNode.direction).sqrMagnitude < 1.5f))
                            //{
                            //    area.hasCouculateNode[nextX, nextY] = true;
                            //    area.couculateNodeDir[nextX, nextY] = new Vector2Int(0, currentNode.direction.y);
                            //    spreadGrid[currentNode.x, nextY].dir = new Vector2Int(0, currentNode.direction.y);
                            //    area.AddSpreadGridTilt(currentNode.x, nextY, new Vector2Int(0, currentNode.direction.y));
                            //}
                        }

                        //前進方向互相撞到，存下來
                        int sameArea = spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name);
                        if ((spreadGrid[nextX, nextY].dir.sqrMagnitude > 0.5f)// && spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name)!=0 ||
                            ) //(((spreadGrid[nextX, nextY].dir.x + currentNode.direction.x == 0 && currentNode.direction.x != 0) || (spreadGrid[nextX, nextY].dir.y + currentNode.direction.y == 0 && currentNode.direction.y != 0)) && spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name) == 0)
                        {
                            Debug.Log(nextX + "," + nextY + "   " + area.Name + "  hit " + spreadGrid[nextX,nextY].fromArea + "   " + spreadGrid[nextX, nextY].choosenWeight);
                            currentNode.choosenWeight += 10;
                            if (spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name) != 0) spreadGrid[nextX, nextY].choosenWeight += currentNode.choosenWeight;
                            else spreadGrid[nextX, nextY].choosenWeight = Mathf.RoundToInt(0.3f * (spreadGrid[nextX, nextY].choosenWeight + currentNode.choosenWeight));
                            if (spreadGrid[nextX, nextY].choosenWeight > maxChoosenWeight) maxChoosenWeight = spreadGrid[nextX, nextY].choosenWeight;
                            Debug.Log(nextX + "," + nextY + "  " + (currentNode.choosenWeight) + " ---> " + spreadGrid[nextX, nextY].choosenWeight);
                            spreadGrid[nextX, nextY].choosen = true;
                            spreadGrid[nextX, nextY].choseNum++;
                            choosenNode.Add(spreadGrid[nextX, nextY]);
                            choosenNodeDic.Add(new Vector2Int(nextX, nextY), spreadGrid[nextX, nextY]);
                            area.spreadGrids.Remove(area.spreadGridNmae[j]);
                            area.spreadGridNmae.RemoveAt(j);
                        }
                        else {
                            currentNode.choosenWeight += 10;
                            spreadGrid[nextX, nextY].choosenWeight = currentNode.choosenWeight;
                        }

                        if (!hasC[currentNode.x, currentNode.y])
                        {
                            spreadGrid[currentNode.x, currentNode.y].current = false;
                        }
                        hasC[nextX, nextY] = true;

                        
                        currentNode.x = nextX;
                        currentNode.y = nextY;
                        spreadGrid[nextX, nextY].dir = currentNode.direction;
                        spreadGrid[nextX, nextY].fromArea = area.Name;
                        spreadGrid[nextX, nextY].current = true;
                        
                        area.hasCouculateNode[nextX, nextY] = true;
                        area.couculateNodeDir[nextX, nextY] = currentNode.direction;
                    }
                }

            }
        }
    }

    public void InitGridSize(int x, int y) {
        gridX = x;
        gridY = y;
    }

    public void CheckExistArea(string name)
    {
        if (name.Length > 0 && !areaDic.ContainsKey(name))
        {
            areaNames.Add(name);
            PatrolArea area = new PatrolArea(name, this, gridX, gridY);
            areaDic.Add(name, area);
        }

    }
    public PatrolArea FindAreaInDic(string name) {
        if (areaDic.ContainsKey(name)) {
            return areaDic[name];
        }
        return null;
    }

    public bool CheckSpreadNodeChoosen(int x, int y) {
        return spreadGrid[x, y].choosen;
    }
    public bool CheckSpreadNodeStop(int x, int y) { 
        return spreadGrid[x, y].stop;
    }
    public void SetSpreadChoosen(int x, int y) {
        spreadGrid[x, y].choosen = true;
        spreadGrid[x, y].choseNum++;
    }
    public void SetSpreadStop(int x, int y) {
        spreadGrid[x, y].stop = true;
    }
}
