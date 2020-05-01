using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    int ID = 0;
    int gridX, gridY;
    List<string> areaNames = new List<string>();
    Dictionary<string, PatrolArea> areaDic = new Dictionary<string, PatrolArea>();   //碰撞名和地區的字典

    bool spreading;

    public class SpreadNode {
        public bool stop = false;
        public bool choosen = false;
        public int choseNum = 0;
        public bool walkable = false;
        public bool corner = false;

        public bool current = false;
        public Vector2Int dir = new Vector2Int(0,0);
    }
    public SpreadNode[,] spreadGrid;
    List<SpreadNode> choosenNode = new List<SpreadNode>();

    public Dictionary<string, SpreadNode> choosenNodeDic = new Dictionary<string, SpreadNode>();


    // Start is called before the first frame update
    void Start()
    {
        PathFinder.PathFindGrid pathFindGrid = transform.GetComponent<PathFinder.PathFindGrid>();
        spreadGrid = new SpreadNode[gridX, gridY];
        for (int i = 0; i < gridX; i++) {
            for (int j = 0; j < gridY; j++)
            {
                spreadGrid[i, j] = new SpreadNode();
                spreadGrid[i, j].walkable = pathFindGrid.Grid[i, j].walkable;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //return;
        if (Input.GetKeyDown(KeyCode.Space)) {
            for (int i = areaNames.Count - 1; i >= 0; i--)
            {
                //輪巡有的區域
                PatrolArea area = areaDic[areaNames[i]];
                for (int j = area.spreadGridNmae.Count - 1; j >= 0; j--)
                {
                    //輪巡區域內的擴散格
                    Debug.Log(area.spreadGridNmae[j] + "  " + j);
                    PatrolArea.SpreadGridNode currentNode = area.spreadGrids[area.spreadGridNmae[j]];

                    spreadGrid[currentNode.x, currentNode.y].current = true;

                    //當下這格已經是停止點 或 被選點
                    if (spreadGrid[currentNode.x, currentNode.y].stop || spreadGrid[currentNode.x, currentNode.y].choosen)
                    {
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                    }
                    else
                    {
                        int nextX = currentNode.x + currentNode.direction.x;
                        int nextY = currentNode.y + currentNode.direction.y;
                        //下一格超過grid範圍
                        if (nextX > spreadGrid.GetLength(0) || nextY > spreadGrid.GetLength(1)) {
                            area.spreadGrids.Remove(area.spreadGridNmae[j]);
                            area.spreadGridNmae.RemoveAt(j);
                            spreadGrid[currentNode.x, currentNode.y].stop = true;
                        }
                        //下一格是停止點
                        else if (spreadGrid[nextX, nextY].stop)
                        {
                            area.spreadGrids.Remove(area.spreadGridNmae[j]);
                            area.spreadGridNmae.RemoveAt(j);
                        }
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
                        //下一格已經算過且方向一樣
                        else if ((area.couculateNodeDir[currentNode.x, currentNode.y] - currentNode.direction).sqrMagnitude < 1.5f)
                        {
                            area.spreadGrids.Remove(area.spreadGridNmae[j]);
                            area.spreadGridNmae.RemoveAt(j);
                        }
                        else
                        {

                            //前進方向互相撞到，存下來
                            if (currentNode.direction.x + spreadGrid[nextX, nextY].dir.x == 0 || currentNode.direction.y + spreadGrid[nextX, nextY].dir.y == 0)
                            {
                                currentNode.x = nextX;
                                currentNode.y = nextY;
                                spreadGrid[nextX, nextY].choosen = true;
                                spreadGrid[nextX, nextY].choseNum++;
                                choosenNode.Add(spreadGrid[nextX, nextY]);
                                choosenNodeDic.Add(nextX + "," + nextY, spreadGrid[nextX, nextY]);
                                area.spreadGrids.Remove(area.spreadGridNmae[j]);
                                area.spreadGridNmae.RemoveAt(j);
                            }
                            else
                            {
                                //沒有撞到又是斜邊才特別新增隔壁節點
                                if (currentNode.direction.sqrMagnitude > 1.5f)
                                {
                                    //新增隔壁x方向節點
                                    //前進方向互相撞到，存下來
                                    if (currentNode.direction.x + spreadGrid[nextX, currentNode.y].dir.x == 0)
                                    {
                                        spreadGrid[nextX, currentNode.y].choosen = true;
                                        spreadGrid[nextX, currentNode.y].choseNum++;
                                        choosenNode.Add(spreadGrid[nextX, currentNode.y]);
                                        choosenNodeDic.Add(nextX + "," + currentNode.y, spreadGrid[nextX, currentNode.y]);
                                    }
                                    //下一格是被選點
                                    else if (spreadGrid[nextX, currentNode.y].choosen)
                                    {
                                        spreadGrid[nextX, nextY].choseNum++;
                                    }
                                    //下一格是障礙
                                    else if (!spreadGrid[nextX, currentNode.y].walkable)
                                    {
                                        //spreadGrid[nextX, currentNode.y].stop = true;
                                    }
                                    //最後如果不是算過的，新增隔壁x方向節點
                                    else if (!(area.hasCouculateNode[nextX, currentNode.y] && (area.couculateNodeDir[currentNode.x, currentNode.y] - currentNode.direction).sqrMagnitude < 1.5f))
                                    {
                                        area.AddSpreadGridTilt(nextX, currentNode.y, new Vector2Int(currentNode.direction.x, 0));
                                    }

                                    //新增隔壁y方向節點
                                    //前進方向互相撞到，存下來
                                    if (currentNode.direction.y + spreadGrid[currentNode.x, nextY].dir.x == 0)
                                    {
                                        spreadGrid[currentNode.x, nextY].choosen = true;
                                        spreadGrid[currentNode.x, nextY].choseNum++;
                                        choosenNode.Add(spreadGrid[currentNode.x, nextY]);
                                        choosenNodeDic.Add(currentNode.x + "," + nextY, spreadGrid[currentNode.x, nextY]);
                                    }
                                    //下一格是被選點
                                    else if (spreadGrid[currentNode.x, nextY].choosen)
                                    {
                                        spreadGrid[currentNode.x, nextY].choseNum++;
                                    }
                                    //下一格是障礙
                                    else if (!spreadGrid[currentNode.x, nextY].walkable)
                                    {
                                        //spreadGrid[currentNode.x, nextY].stop = true;
                                    }
                                    //最後如果不是算過的，新增隔壁y方向節點
                                    else if (!(area.hasCouculateNode[currentNode.x, nextY] && (area.couculateNodeDir[currentNode.x, nextY] - currentNode.direction).sqrMagnitude < 1.5f))
                                    {
                                        area.AddSpreadGridTilt(currentNode.x, nextY, new Vector2Int(0,currentNode.direction.y));
                                    }
                                }
                            }

                            area.hasCouculateNode[nextX, nextY] = true;
                        }
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
