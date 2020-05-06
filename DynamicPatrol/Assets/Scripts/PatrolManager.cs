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

    public int NotBorderWeight = 50;
    public int BorderAndNormalCornerWeight = 50;
    public int PerSpreadWeight = 10;
    public float sameAreaDisRate = 0.3f;
    [Range(0.0f, 1.0f)]
    public float choosenRate = .0f;

    [HideInInspector]
    public int maxChoosenWeight = 0;

    public class SpreadNode {
        public bool stop = false;
        public bool choosen = false;
        public int choseNum = 0;
        public bool walkable = false;
        public List<string> fromArea = new List<string>();
        public bool current = false;
        public Vector2Int pos = new Vector2Int(-1, -1);
        public Vector2Int dir = new Vector2Int(0,0);
        public int choosenWeight = 0;
        public List<SpreadNode> neighbor = new List<SpreadNode>();
        public List<bool> hasCountNeighbor = new List<bool>();
    }
    public SpreadNode[,] spreadGrid;
    public List<SpreadNode> tiltSpread = new List<SpreadNode>();
    public List<SpreadNode> choosenNode = new List<SpreadNode>();
    public List<PatrolGraphNode> ConfirmGraph = new List<PatrolGraphNode>();

    public Dictionary<Vector2Int, SpreadNode> choosenNodeDic = new Dictionary<Vector2Int, SpreadNode>();
    public Dictionary<Vector2Int, PatrolGraphNode> confirmGraphNodeDic = new Dictionary<Vector2Int, PatrolGraphNode>();

    public class PatrolGraphNode {
        public bool crossNode = false;
        public int x;
        public int y;
        public PatrolGraphNode UpNode;
        public PatrolGraphNode DownNode;
        public PatrolGraphNode LeftNode;
        public PatrolGraphNode RightNode;
        public PatrolGraphNode(int _x, int _y) {
            x = _x;
            y = _y;
        }
    }

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
                    spreadGrid[i, j].walkable = true;
                    spreadGrid[i, j].pos = pos;
                    spreadGrid[i, j].choosen = true;
                    Debug.Log(i + "," + j + "  choosen weight" + spreadGrid[i, j].choosenWeight);
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
                spreadGrid[currentNode.x, currentNode.y].fromArea.Add(area.Name);
                spreadGrid[currentNode.x, currentNode.y].dir = currentNode.direction;
                currentNode.choosenWeight += PerSpreadWeight;
            }
        }
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
        if (Input.GetKeyDown(KeyCode.X)) {
            AddGraph();
        }
    }

    void CouculateGrid() {
        skip = true;
        bool[,] hasC = new bool[gridX, gridY];

         //計算斜邊互相碰撞的衍伸點
        for (int i = tiltSpread.Count-1; i >= 0; i--) {
            SpreadNode node = spreadGrid[tiltSpread[i].pos.x, tiltSpread[i].pos.y];
            if (tiltSpread[i].pos.x < 0 || tiltSpread[i].pos.x >= gridX || tiltSpread[i].pos.y < 0 || tiltSpread[i].pos.y >= gridY ||
                node.choosen || !node.walkable || node.fromArea.Count > 0)
            {
                Debug.Log("移除衍伸點  " + node.pos.x + "," + node.pos.y);
                tiltSpread.RemoveAt(i);
            }
            else {
                Debug.Log("衍伸點  " + node.pos.x + "," + node.pos.y + "  填充選擇點");
                node.pos = tiltSpread[i].pos;
                node.dir = tiltSpread[i].dir;
                node.choosen = true;
                node.choosenWeight += tiltSpread[i].choosenWeight;

                for (int n = choosenNode.Count; n >= 0; n--) {
                    Vector2Int diff = choosenNode[n].pos - node.pos;
                    if (diff.sqrMagnitude <= 2) {
                        node.neighbor.Add(choosenNode[n]);
                        choosenNode[n].neighbor.Add(node);
                        choosenNode[n].hasCountNeighbor.Add(false);
                    }
                }

                choosenNode.Add(node);
                choosenNodeDic.Add(node.pos, node);
                tiltSpread[i].pos += tiltSpread[i].dir;
                tiltSpread[i].choosenWeight += PerSpreadWeight;

                
            }
        }

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
                spreadGrid[currentNode.x, currentNode.y].fromArea.Add(area.Name);

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
                        if (!spreadGrid[nextX, nextY].fromArea.Contains(area.Name)) spreadGrid[nextX, nextY].choseNum++;
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

                    //同名稱不是正左右或正上下遇到
                    else if (spreadGrid[nextX, nextY].fromArea.Contains(area.Name) && ((spreadGrid[nextX, nextY].dir.x + currentNode.direction.x != 0) && (spreadGrid[nextX, nextY].dir.y + currentNode.direction.y != 0))
                            ) {

                    }
                    else
                    {

                        //斜邊特別新增隔壁節點
                        if (currentNode.direction.sqrMagnitude > 1.5f)
                        {

                            //下一格是被選點
                            if (spreadGrid[nextX, currentNode.y].choosen)
                            {
                                if (!spreadGrid[nextX, currentNode.y].fromArea.Contains(area.Name)) spreadGrid[nextX, currentNode.y].choseNum++;
                            }
                            //下一格是障礙
                            else if (!spreadGrid[nextX, currentNode.y].walkable)
                            {
                                //spreadGrid[nextX, currentNode.y].stop = true;
                            }
                            else if (spreadGrid[nextX, currentNode.y].fromArea.Contains(area.Name) && (spreadGrid[nextX, currentNode.y].dir.y != 0))
                            {

                            }
                            //新增隔壁x方向節點
                            //前進方向互相撞到，存下來
                            else if ((spreadGrid[nextX, currentNode.y].dir.sqrMagnitude > 0.5f))//  && spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) != 0 || ((spreadGrid[nextX, currentNode.y].dir.x + currentNode.direction.x == 0) && spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) == 0)
                            {
                                spreadGrid[nextX, currentNode.y].choosen = true;
                                if (!spreadGrid[nextX, currentNode.y].fromArea.Contains(area.Name)) {
                                    //if(currentNode.direction.sqrMagnitude > 1.5f) spreadGrid[nextX, currentNode.y].choseNum+=2;
                                    //else spreadGrid[nextX, currentNode.y].choseNum++;
                                    spreadGrid[nextX, currentNode.y].choseNum++;
                                }
                                spreadGrid[nextX, currentNode.y].pos = new Vector2Int(nextX, currentNode.y);
                                for (int n = choosenNode.Count; n >= 0; n--)
                                {
                                    Vector2Int diff = choosenNode[n].pos - spreadGrid[nextX, currentNode.y].pos;
                                    if (diff.sqrMagnitude <= 2)
                                    {
                                        spreadGrid[nextX, currentNode.y].neighbor.Add(choosenNode[n]);
                                        choosenNode[n].neighbor.Add(spreadGrid[nextX, currentNode.y]);
                                        choosenNode[n].hasCountNeighbor.Add(false);
                                    }
                                }
                                choosenNode.Add(spreadGrid[nextX, currentNode.y]);
                                choosenNodeDic.Add(new Vector2Int(nextX, currentNode.y), spreadGrid[nextX, currentNode.y]);
                                //Debug.Log(nextX + "," + currentNode.y + "   " + area.Name + "  hit " + spreadGrid[nextX, currentNode.y].fromArea[0] + "   " + spreadGrid[nextX, currentNode.y].choosenWeight);
                                
                                //if (spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) != 0) spreadGrid[nextX, currentNode.y].choosenWeight += (currentNode.choosenWeight + PerSpreadWeight);
                                //else spreadGrid[nextX, currentNode.y].choosenWeight = Mathf.RoundToInt(sameAreaDisRate * (spreadGrid[nextX, currentNode.y].choosenWeight + currentNode.choosenWeight + PerSpreadWeight));
                                if (currentNode.direction.sqrMagnitude > 0.5f && (currentNode.direction.x + spreadGrid[nextX, currentNode.y].dir.x == 0 || currentNode.direction.y + spreadGrid[nextX, currentNode.y].dir.y == 0))
                                    spreadGrid[nextX, currentNode.y].choosenWeight += (currentNode.choosenWeight + PerSpreadWeight);
                                else
                                    spreadGrid[nextX, currentNode.y].choosenWeight = Mathf.RoundToInt(sameAreaDisRate * (spreadGrid[nextX, currentNode.y].choosenWeight + currentNode.choosenWeight + PerSpreadWeight));
                                if (spreadGrid[nextX, currentNode.y].choosenWeight > maxChoosenWeight) maxChoosenWeight = spreadGrid[nextX, currentNode.y].choosenWeight;
                                //Debug.Log(nextX + "," + currentNode.y + "  " + (currentNode.choosenWeight + PerSpreadWeight) + " ---> " + spreadGrid[nextX, currentNode.y].choosenWeight);
                            }
                            //最後如果不是算過的，新增隔壁x方向節點
                            else //if (spreadGrid[nextX, currentNode.y].dir.sqrMagnitude < 0.5f) 
                            {
                                area.hasCouculateNode[nextX, nextY] = true;
                                area.couculateNodeDir[nextX, nextY] = new Vector2Int(currentNode.direction.x, 0);
                                spreadGrid[nextX, currentNode.y].dir = new Vector2Int(currentNode.direction.x, 0);
                                spreadGrid[nextX, currentNode.y].current = true;
                                spreadGrid[nextX, currentNode.y].fromArea.Add(area.Name);
                                area.AddSpreadGridTilt(nextX, currentNode.y, new Vector2Int(currentNode.direction.x, 0), currentNode.choosenWeight + PerSpreadWeight);
                                spreadGrid[nextX, currentNode.y].choosenWeight = currentNode.choosenWeight + PerSpreadWeight;
                                //Debug.Log("add new  " + nextX + "," + currentNode.y + "     " + currentNode.choosenWeight + PerSpreadWeight);
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
                                if (!spreadGrid[currentNode.x, nextY].fromArea.Contains(area.Name)) spreadGrid[currentNode.x, nextY].choseNum++;
                            }
                            //下一格是障礙
                            else if (!spreadGrid[currentNode.x, nextY].walkable)
                            {
                                //spreadGrid[currentNode.x, nextY].stop = true;
                            }
                            else if (spreadGrid[currentNode.x, nextY].fromArea.Contains(area.Name) && (spreadGrid[currentNode.x, nextY].dir.x != 0))
                            {

                            }
                            //新增隔壁y方向節點
                            //前進方向互相撞到，存下來
                            else if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude > 0.5f) //&& spreadGrid[currentNode.x, nextY].fromArea.CompareTo(area.Name) != 0) ||  ((spreadGrid[currentNode.x, nextY].dir.y + currentNode.direction.y == 0) && spreadGrid[currentNode.x, nextY].fromArea.CompareTo(area.Name) == 0)
                            {
                                spreadGrid[currentNode.x, nextY].choosen = true;
                                if (!spreadGrid[currentNode.x, nextY].fromArea.Contains(area.Name)) {
                                    //if(currentNode.direction.sqrMagnitude > 1.5f) spreadGrid[currentNode.x, nextY].choseNum+=2;
                                    //else spreadGrid[currentNode.x, nextY].choseNum++;
                                    spreadGrid[currentNode.x, nextY].choseNum++;
                                }
                                spreadGrid[currentNode.x, nextY].pos = new Vector2Int(currentNode.x, nextY);
                                for (int n = choosenNode.Count; n >= 0; n--)
                                {
                                    Vector2Int diff = choosenNode[n].pos - spreadGrid[currentNode.x, nextY].pos;
                                    if (diff.sqrMagnitude <= 2)
                                    {
                                        spreadGrid[currentNode.x, nextY].neighbor.Add(choosenNode[n]);
                                        choosenNode[n].neighbor.Add(spreadGrid[currentNode.x, nextY]);
                                        choosenNode[n].hasCountNeighbor.Add(false);
                                    }
                                }
                                choosenNode.Add(spreadGrid[currentNode.x, nextY]);
                                choosenNodeDic.Add(new Vector2Int(currentNode.x, nextY), spreadGrid[currentNode.x, nextY]);
                                //if (spreadGrid[currentNode.x, nextY].fromArea.CompareTo(area.Name) != 0) spreadGrid[currentNode.x, nextY].choosenWeight += (currentNode.choosenWeight + PerSpreadWeight);
                                //else spreadGrid[currentNode.x, nextY].choosenWeight = Mathf.RoundToInt(sameAreaDisRate * (spreadGrid[currentNode.x, nextY].choosenWeight + currentNode.choosenWeight + PerSpreadWeight));
                                if (currentNode.direction.sqrMagnitude > 0.5f && (currentNode.direction.x + spreadGrid[currentNode.x, nextY].dir.x == 0 || currentNode.direction.y + spreadGrid[currentNode.x, nextY].dir.y == 0))
                                    spreadGrid[currentNode.x, nextY].choosenWeight += (currentNode.choosenWeight + PerSpreadWeight);
                                else
                                    spreadGrid[currentNode.x, nextY].choosenWeight = Mathf.RoundToInt(sameAreaDisRate * (spreadGrid[currentNode.x, nextY].choosenWeight + currentNode.choosenWeight + PerSpreadWeight));
                                if (spreadGrid[currentNode.x, nextY].choosenWeight > maxChoosenWeight) maxChoosenWeight = spreadGrid[currentNode.x, nextY].choosenWeight;
                            }
                            //最後如果不是算過的，新增隔壁y方向節點
                            else // if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude < 0.5f)
                            {
                                area.hasCouculateNode[nextX, nextY] = true;
                                area.couculateNodeDir[nextX, nextY] = new Vector2Int(0, currentNode.direction.y);
                                spreadGrid[currentNode.x, nextY].dir = new Vector2Int(0, currentNode.direction.y);
                                spreadGrid[currentNode.x, nextY].current = true;
                                spreadGrid[currentNode.x, nextY].fromArea.Add(area.Name);
                                area.AddSpreadGridTilt(currentNode.x, nextY, new Vector2Int(0, currentNode.direction.y), currentNode.choosenWeight + PerSpreadWeight);
                                spreadGrid[currentNode.x, nextY].choosenWeight = currentNode.choosenWeight + PerSpreadWeight;
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
                        bool sameArea = spreadGrid[nextX, nextY].fromArea.Contains(area.Name);
                        if ((spreadGrid[nextX, nextY].dir.sqrMagnitude > 0.5f)// && spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name)!=0 ||
                            ) //(((spreadGrid[nextX, nextY].dir.x + currentNode.direction.x == 0 && currentNode.direction.x != 0) || (spreadGrid[nextX, nextY].dir.y + currentNode.direction.y == 0 && currentNode.direction.y != 0)) && spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name) == 0)
                        {
                            //Debug.Log(nextX + "," + nextY + "   " + area.Name + "  hit " + spreadGrid[nextX, nextY].fromArea[0] + "   " + spreadGrid[nextX, nextY].choosenWeight);
                            currentNode.choosenWeight += PerSpreadWeight;
                            
                            //if (spreadGrid[nextX, nextY].fromArea.CompareTo(area.Name) != 0) spreadGrid[nextX, nextY].choosenWeight += currentNode.choosenWeight;
                            //else spreadGrid[nextX, nextY].choosenWeight = Mathf.RoundToInt(sameArea * (spreadGrid[nextX, nextY].choosenWeight + currentNode.choosenWeight));
                            
                            if (currentNode.direction.sqrMagnitude > 0.5f && (currentNode.direction.x + spreadGrid[nextX, nextY].dir.x == 0 || currentNode.direction.y + spreadGrid[nextX, nextY].dir.y == 0))
                                spreadGrid[nextX, nextY].choosenWeight += (currentNode.choosenWeight);
                            else
                                spreadGrid[nextX, nextY].choosenWeight = Mathf.RoundToInt(sameAreaDisRate * (spreadGrid[nextX, nextY].choosenWeight + currentNode.choosenWeight));
                            if (spreadGrid[nextX, nextY].choosenWeight > maxChoosenWeight) maxChoosenWeight = spreadGrid[nextX, nextY].choosenWeight;
                            //Debug.Log(nextX + "," + nextY + "  " + (currentNode.choosenWeight) + " ---> " + spreadGrid[nextX, nextY].choosenWeight);
                            spreadGrid[nextX, nextY].choosen = true;
                            if (!sameArea) {
                                spreadGrid[nextX, nextY].choseNum++;
                            }
                            spreadGrid[nextX, nextY].pos = new Vector2Int(nextX, nextY);
                            for (int n = choosenNode.Count; n >= 0; n--)
                            {
                                Vector2Int diff = choosenNode[n].pos - spreadGrid[nextX, nextY].pos;
                                if (diff.sqrMagnitude <= 2)
                                {
                                    spreadGrid[nextX, nextY].neighbor.Add(choosenNode[n]);
                                    choosenNode[n].neighbor.Add(spreadGrid[nextX, nextY]);
                                    choosenNode[n].hasCountNeighbor.Add(false);
                                }
                            }
                            choosenNode.Add(spreadGrid[nextX, nextY]);
                            choosenNodeDic.Add(new Vector2Int(nextX, nextY), spreadGrid[nextX, nextY]);
                            area.spreadGrids.Remove(area.spreadGridNmae[j]);
                            area.spreadGridNmae.RemoveAt(j);

                            //互為斜方碰撞，延伸單一向
                            if (currentNode.direction.sqrMagnitude > 1.5f && spreadGrid[nextX, nextY].dir.sqrMagnitude > 1.5f)
                            {
                                Vector2Int dir = Vector2Int.zero;
                                if ((currentNode.direction.x + spreadGrid[nextX, nextY].dir.x) != 0) {
                                    dir = new Vector2Int(currentNode.direction.x, 0);
                                }
                                if ((currentNode.direction.y + spreadGrid[nextX, nextY].dir.y) != 0) {
                                    dir = new Vector2Int(0, currentNode.direction.y);
                                }
                                int newX = nextX + dir.x;
                                int newY = nextY + dir.y;
                                SpreadNode node = new SpreadNode();
                                node.pos = new Vector2Int(newX, newY);
                                node.dir = dir;
                                node.choosenWeight = currentNode.choosenWeight*2 + PerSpreadWeight*2;
                                tiltSpread.Add(node);
                                Debug.Log("延伸  " + node.pos + "   dir:" + node.dir);
                            }
                        }
                        else {
                            
                            currentNode.choosenWeight += PerSpreadWeight;
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
                        spreadGrid[nextX, nextY].fromArea.Add(area.Name);
                        spreadGrid[nextX, nextY].current = true;

                        area.hasCouculateNode[nextX, nextY] = true;
                        area.couculateNodeDir[nextX, nextY] = currentNode.direction;
                    }
                }

            }
        }

    }

    void AddGraph() {
        if (ConfirmGraph.Count > 0) {
            ConfirmGraph.Clear();
            confirmGraphNodeDic.Clear();
        }
        Debug.Log(gridX + "," + gridY);
        for (int i = choosenNode.Count-1; i >= 0; i--) {

            int count = 0;
            int couculateNum = 0;
            int xNum = 0, yNum = 0;
            bool hasCross = false;
            bool hasTurn = false;
            if (Mathf.InverseLerp(0, maxChoosenWeight, choosenNode[i].choosenWeight) < choosenRate) continue;
            for (int cy = 1; cy >= -1; cy--) {
                for (int cx = -1; cx <= 1; cx++) {
                    Vector2Int detectPos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy); 
                    
                    if ((cy == 0 && cx == 0)  || detectPos.x < 0 || detectPos.x >= gridX || detectPos.y < 0 || detectPos.y >= gridY || hasCross) continue;
                    Debug.Log(choosenNode[i].pos + " detect  " + detectPos.x + "," + detectPos.y + "  walkable" + spreadGrid[detectPos.x, detectPos.y].walkable + "   choosen" + spreadGrid[detectPos.x, detectPos.y].choosen);
                    hasCross = (hasCross || (confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].crossNode));
                    if (spreadGrid[detectPos.x, detectPos.y].walkable && spreadGrid[detectPos.x, detectPos.y].choosen && 
                        Mathf.InverseLerp(0, maxChoosenWeight, spreadGrid[detectPos.x, detectPos.y].choosenWeight) >= choosenRate) {
                        
                        count++;
                        xNum += cx;
                        yNum += cy;
                        couculateNum += Mathf.Abs(cx);
                        couculateNum += Mathf.Abs(cy);
                        Debug.Log(choosenNode[i].pos + "couculate   " + detectPos.x + "," + detectPos.y + " num " + xNum + "," + yNum);
                    }
                }
            }
            if (hasCross) continue;
            if (count == 1) {
                //PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //ConfirmGraph.Add(node);
                //confirmGraphNodeDic.Add(choosenNode[i].pos, node);
            }
            else if (xNum != 0 || yNum != 0)
            {
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);

                //x或y其一不抵銷，斜邊數量多列為交錯點
                if (couculateNum > 4 ) { //&& couculateNum % 2 == 0
                    node.crossNode = true;
                    for (int cy = 1; cy >= -1; cy--)
                    {
                        for (int cx = -1; cx <= 1; cx++)
                        {
                            if (cx == 0 && cy == 0) continue;
                            Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                            if (confirmGraphNodeDic.ContainsKey(pos) && !confirmGraphNodeDic[pos].crossNode)
                            {
                                ConfirmGraph.Remove(confirmGraphNodeDic[pos]);
                                confirmGraphNodeDic.Remove(pos);
                            }
                        }
                    }
                }
                
                //一般列為轉折點
                ConfirmGraph.Add(node);
                confirmGraphNodeDic.Add(choosenNode[i].pos, node);
            }
            else
            {
                //x或y都抵銷且周圍數量不為偶數，列為交錯點，不然就不列入
                if (count == 4 || (count > 0 && count % 2 != 0))
                {
                    PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                    node.crossNode = true;
                    ConfirmGraph.Add(node);
                    confirmGraphNodeDic.Add(choosenNode[i].pos, node);

                    for (int cy = 1; cy >= -1; cy--)
                    {
                        for (int cx = -1; cx <= 1; cx++)
                        {
                            if (cx == 0 && cy == 0) continue;
                            Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                            if (confirmGraphNodeDic.ContainsKey(pos) && !confirmGraphNodeDic[pos].crossNode) {
                                ConfirmGraph.Remove(confirmGraphNodeDic[pos]);
                                confirmGraphNodeDic.Remove(pos);
                            }
                        }
                    }
                }
            }
        }
    }
    void ConactGraph() {
        List<SpreadNode> waitNodes = new List<SpreadNode>();
        SpreadNode node;
        SpreadNode neighbor;
        int neighborCount = 0;

        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            node = choosenNode[i];
            if (CheckCrossNode(node)) {
                break;
            } 
        }

        if (node.neighbor.Count > 0)
        {
            neighbor = node.neighbor[node.neighbor.Count - 1];
            waitNodes.Add(neighbor);
        }

        

    }

    bool CheckCrossNode(SpreadNode node) {
        int count = 0;
        int couculateNum = 0;
        int xNum = 0, yNum = 0;
        for (int cy = 1; cy >= -1; cy--)
        {
            for (int cx = -1; cx <= 1; cx++)
            {
                Vector2Int detectPos = new Vector2Int(node.pos.x + cx, node.pos.y + cy);

                if ((cy == 0 && cx == 0) || detectPos.x < 0 || detectPos.x >= gridX || detectPos.y < 0 || detectPos.y >= gridY) continue;
                if (spreadGrid[detectPos.x, detectPos.y].walkable && spreadGrid[detectPos.x, detectPos.y].choosen &&
                    Mathf.InverseLerp(0, maxChoosenWeight, spreadGrid[detectPos.x, detectPos.y].choosenWeight) >= choosenRate)
                {
                    count++;
                    xNum += cx;
                    yNum += cy;
                    couculateNum += Mathf.Abs(cx);
                    couculateNum += Mathf.Abs(cy);
                }
            }
        }
        if (xNum != 0 || yNum != 0)
        {
            //x或y其一不抵銷，斜邊數量多列為交錯點
            if (couculateNum > 4) //&& couculateNum % 2 == 0
            {
                return true;
            }
            return false;
        }
        else
        {
            //x或y都抵銷且周圍數量不為偶數，列為交錯點，不然就不列入
            if (count == 4 || (count > 0 && count % 2 != 0))
            {
                return true;
            }
            return false;
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
