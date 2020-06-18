using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathFinder;

public class PatrolManager : MonoBehaviour
{
    bool skip = false;
    int ID = 0;
    int gridX, gridY;
    List<string> areaNames = new List<string>();
    Dictionary<string, PatrolArea> areaDic = new Dictionary<string, PatrolArea>();   //碰撞名和地區的字典

    bool spreading;

    public float leastNarrow = 0;
    public float closeDstNum = 0;
    public int leastTurnDstNum = 0;
    public float connectTime = 0.1f;
    public float maxConnectAngle = 45.0f;
    public int minPatrolLength = 0;
    public int maxPatrolLength = 0;
    public int maxPathNum = 0;
    public int patrolCycleFailNum = 0;
    public int patrolRepeatNum = 3;
    public float turnDist;

    [HideInInspector]
    public int maxChoosenWeight = 0;
    [HideInInspector]
    public bool runConnectEnd = false;

    PathFindGrid pathFindGrid;

    [HideInInspector]
    public List<PatrolPath> patrolPathes = new List<PatrolPath>();

    public class SpreadNode {
        public bool stop = false;
        public bool choosen = false;
        public bool walkable = false;
        public List<string> fromArea = new List<string>();
        public bool current = false;
        public Vector2Int pos = new Vector2Int(-1, -1);
        public Vector2Int dir = new Vector2Int(0,0);
        public List<SpreadNode> neighbor = new List<SpreadNode>();

        public bool turnNode = false;
        public bool crossNode = false;
        public bool endNode = false;
        public int crossWeight = 0;
        public bool hasCouculate = false;
        public bool beenMerged = false;
        public SpreadNode mergeNode = null;
        public int mergeCount = 0;
        public bool mergeCouculate = false;
    }
    public SpreadNode[,] spreadGrid;
    public List<SpreadNode> tiltSpread = new List<SpreadNode>();
    public List<SpreadNode> choosenNode = new List<SpreadNode>();
    public List<PatrolGraphNode> ConfirmGraph = new List<PatrolGraphNode>();

    public Dictionary<Vector2Int, SpreadNode> choosenNodeDic = new Dictionary<Vector2Int, SpreadNode>();
    public Dictionary<Vector2Int, PatrolGraphNode> confirmGraphNodeDic = new Dictionary<Vector2Int, PatrolGraphNode>();

    public class PatrolGraphNode {
        public bool turnNode = false;
        public bool crossNode = false;
        public bool endNode = false;
        public int weight = 0;
        public int x;
        public int y;
        public Vector3 pos;
        public int detectNum = 0;
        public bool[] besideCouculate; 
        public Dictionary<PatrolGraphNode, float>besideNodes = new Dictionary<PatrolGraphNode, float>();
        public PatrolPath patrolPath;
        public PatrolGraphNode(int _x, int _y) {
            x = _x;
            y = _y;
        }
    }

    public SpreadNode sourceNode = null;
    public SpreadNode connectNeighbor = null;
    public List<SpreadNode> waitNodes = new List<SpreadNode>();
    List<SpreadNode> endNodes = new List<SpreadNode>();
    Dictionary<SpreadNode, List<SpreadNode>> mergeNodeDic = new Dictionary<SpreadNode, List<SpreadNode>>();

    bool skipRun = false;

    // Start is called before the first frame update
    void Start()
    {
        pathFindGrid = transform.GetComponent<PathFindGrid>();
        spreadGrid = new SpreadNode[gridX, gridY];
        for (int i = 0; i < gridX; i++) {
            for (int j = 0; j < gridY; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                if (!choosenNodeDic.ContainsKey(pos))
                {
                    spreadGrid[i, j] = new SpreadNode();
                    spreadGrid[i, j].walkable = pathFindGrid.Grid[i, j].walkable;
                    spreadGrid[i, j].pos = pos;
                }
                else {
                    spreadGrid[i, j] = choosenNodeDic[pos];
                    spreadGrid[i, j].walkable = true;
                    spreadGrid[i, j].pos = pos;
                    spreadGrid[i, j].choosen = true;
                }
            }
        }

        SpreadNode merge = new SpreadNode();

        SpreadNode eee = new SpreadNode();
        eee.pos = new Vector2Int(0, 0);
        eee.neighbor.Add(spreadGrid[10, 10]);
        eee.mergeNode = merge;

        SpreadNode fff = eee;
        fff.pos += new Vector2Int(100, 100);
        fff.neighbor.Add(spreadGrid[20, 20]);

        SpreadNode ggg = new SpreadNode();
        ggg.mergeNode = new SpreadNode();
        SpreadNode hhh = new SpreadNode();
        hhh.mergeNode = eee.mergeNode;
        SpreadNode iii = new SpreadNode();
        iii.mergeNode = fff.mergeNode;
        SpreadNode jjj = new SpreadNode();


        Debug.Log("fff  " + eee.mergeNode.Equals(fff.mergeNode));
        Debug.Log("ggg  " + eee.mergeNode.Equals(ggg.mergeNode));
        Debug.Log("hhh  " + eee.mergeNode.Equals(hhh.mergeNode));
        Debug.Log("iii  " + eee.mergeNode.Equals(iii.mergeNode));
        Debug.Log("jjj  " + eee.mergeNode.Equals(jjj.mergeNode));

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
            }
        }


        //第一層就加入的點設鄰居
        for (int n = 0; n < choosenNode.Count; n++)
        {
            for (int m = n + 1; m < choosenNode.Count; m++) {
                Vector2Int diff = choosenNode[n].pos - choosenNode[m].pos;
                if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                {
                    if(!choosenNode[m].neighbor.Contains(choosenNode[n])) choosenNode[m].neighbor.Add(choosenNode[n]);
                    if (!choosenNode[n].neighbor.Contains(choosenNode[m])) choosenNode[n].neighbor.Add(choosenNode[m]);
                }
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
                Debug.Log("CHOOSEN NUMBER " + choosenNode.Count);
                CouculateGrid();
                if (count > 10000) break;
            }
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            DeleteBranch();
        }
        if (Input.GetKeyDown(KeyCode.C)) {
            DeleteExtraNode();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            CouculateGraphCross();
            CouculateGraphTurn();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            StartCoroutine(ConactGraph());
        }
        if (Input.GetKeyDown(KeyCode.P)) {
            skipRun = true;
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
                node.fromArea = tiltSpread[i].fromArea;
                node.current = false;

                //加鄰居
                for (int n = choosenNode.Count - 1; n >= 0; n--)
                {
                    Vector2Int diff = choosenNode[n].pos - node.pos;
                    if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                    {
                        if(!node.neighbor.Contains(choosenNode[n]))node.neighbor.Add(choosenNode[n]);
                        if(!choosenNode[n].neighbor.Contains(node)) choosenNode[n].neighbor.Add(node);
                    }
                }
                choosenNode.Add(node);
                choosenNodeDic.Add(node.pos, node);
                tiltSpread[i].pos += tiltSpread[i].dir; 
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
                if (spreadGrid[currentNode.x, currentNode.y].choosen || spreadGrid[currentNode.x, currentNode.y].stop) //spreadGrid[currentNode.x, currentNode.y].stop || 
                {
                    spreadGrid[currentNode.x, currentNode.y].current = false;
                    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                    area.spreadGridNmae.RemoveAt(j);

                    //spreadGrid[currentNode.x, currentNode.y].choosen = false;
                    //if (choosenNodeDic.ContainsKey(new Vector2Int(currentNode.x, currentNode.y)))
                    //{
                    //    choosenNode.Remove(spreadGrid[currentNode.x, currentNode.y]);
                    //    choosenNodeDic.Remove(new Vector2Int(currentNode.x, currentNode.y));
                    //}

                }
                else
                {
                    //用於延伸方向碰到另一方向斜邊，新增延伸
                    bool extendTiltSpread = false;
                    Vector2Int extendTiltSpreadVec2 = new Vector2Int(0, 0);

                    //當下點也加入進整個地圖的參考
                    spreadGrid[currentNode.x, currentNode.y].dir = currentNode.direction;

                    int nextX = currentNode.x + currentNode.direction.x;
                    int nextY = currentNode.y + currentNode.direction.y;

                    //斜邊特別新增隔壁節點
                    if (currentNode.direction.sqrMagnitude > 1.5f)
                    {
                        //新增水平
                        //下一格超過範圍
                        if (nextX >= spreadGrid.GetLength(0) ||  nextX < 0 ) { 
                        
                        }
                        //下一格是被選點
                        else if (spreadGrid[nextX, currentNode.y].choosen)
                        {
                        }
                        //下一格是障礙
                        else if (!spreadGrid[nextX, currentNode.y].walkable)
                        {
                            spreadGrid[nextX, currentNode.y].stop = true;
                        }
                        //下一格是同名稱，且遇到方向是垂直的，通常是為了解決斜向擺放的方塊自己碰撞的問題
                        else if (spreadGrid[nextX, currentNode.y].fromArea.Contains(area.Name) && (spreadGrid[nextX, currentNode.y].dir.y != 0))
                        {
                            spreadGrid[nextX, currentNode.y].stop = true;
                        }
                        //新增隔壁x方向節點
                        //前進方向互相撞到，存下來
                        else if ((spreadGrid[nextX, currentNode.y].dir.sqrMagnitude > 0.5f))//  && spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) != 0 || ((spreadGrid[nextX, currentNode.y].dir.x + currentNode.direction.x == 0) && spreadGrid[nextX, currentNode.y].fromArea.CompareTo(area.Name) == 0)
                        {
                            spreadGrid[nextX, currentNode.y].choosen = true;
                            spreadGrid[nextX, currentNode.y].pos = new Vector2Int(nextX, currentNode.y);

                            //新增已選的鄰居
                            for (int n = choosenNode.Count - 1; n >= 0; n--)
                            {
                                Vector2Int diff = choosenNode[n].pos - spreadGrid[nextX, currentNode.y].pos;
                                if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                                {
                                    if (!spreadGrid[nextX, currentNode.y].neighbor.Contains(choosenNode[n])) spreadGrid[nextX, currentNode.y].neighbor.Add(choosenNode[n]);
                                    if (!choosenNode[n].neighbor.Contains(spreadGrid[nextX, currentNode.y])) choosenNode[n].neighbor.Add(spreadGrid[nextX, currentNode.y]);
                                }
                            }
                            choosenNode.Add(spreadGrid[nextX, currentNode.y]);
                            choosenNodeDic.Add(new Vector2Int(nextX, currentNode.y), spreadGrid[nextX, currentNode.y]);

                            //延伸方向碰到另一方向斜邊，新增延伸
                            if (spreadGrid[nextX, currentNode.y].dir.sqrMagnitude > 1.5f)
                            {
                                if (!extendTiltSpread && (spreadGrid[currentNode.x, nextY].dir + currentNode.direction).sqrMagnitude >= 1)
                                {
                                    extendTiltSpreadVec2 += spreadGrid[nextX, currentNode.y].dir + new Vector2Int(currentNode.direction.x, 0);
                                    extendTiltSpread = true;
                                }
                            }

                        }
                        //最後如果不是算過的，新增隔壁x方向節點
                        else
                        {
                            area.hasCouculateNode[nextX, nextY] = true;
                            area.couculateNodeDir[nextX, nextY] = new Vector2Int(currentNode.direction.x, 0);
                            spreadGrid[nextX, currentNode.y].dir = new Vector2Int(currentNode.direction.x, 0);
                            spreadGrid[nextX, currentNode.y].current = true;
                            if (!spreadGrid[nextX, currentNode.y].fromArea.Contains(area.Name)) spreadGrid[nextX, currentNode.y].fromArea.Add(area.Name);
                            area.AddSpreadGridTilt(nextX, currentNode.y, new Vector2Int(currentNode.direction.x, 0));
                        }

                        //新增垂直方向
                        //下一格超過範圍
                        if (nextY >= spreadGrid.GetLength(1) || nextY < 0)
                        {

                        }
                        //下一格是被選點
                        else if (spreadGrid[currentNode.x, nextY].choosen)
                        {

                        }
                        //下一格是障礙
                        else if (!spreadGrid[currentNode.x, nextY].walkable)
                        {
                            spreadGrid[currentNode.x, nextY].stop = true;
                        }
                        //下一格是同名稱，且遇到方向是水平的，通常是為了解決斜向擺放的方塊自己碰撞的問題
                        else if (spreadGrid[currentNode.x, nextY].fromArea.Contains(area.Name) && (spreadGrid[currentNode.x, nextY].dir.x != 0))
                        {
                            spreadGrid[currentNode.x, nextY].stop = true;
                        }
                        //新增隔壁y方向節點
                        //前進方向互相撞到，存下來
                        else if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude > 0.5f)
                        {
                            spreadGrid[currentNode.x, nextY].choosen = true;
                            spreadGrid[currentNode.x, nextY].pos = new Vector2Int(currentNode.x, nextY);

                            //新增已選的鄰居
                            for (int n = choosenNode.Count - 1; n >= 0; n--)
                            {
                                Vector2Int diff = choosenNode[n].pos - spreadGrid[currentNode.x, nextY].pos;
                                if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                                {
                                    if (!spreadGrid[currentNode.x, nextY].neighbor.Contains(choosenNode[n])) spreadGrid[currentNode.x, nextY].neighbor.Add(choosenNode[n]);
                                    if (!choosenNode[n].neighbor.Contains(spreadGrid[currentNode.x, nextY])) choosenNode[n].neighbor.Add(spreadGrid[currentNode.x, nextY]);

                                }
                            }
                            choosenNode.Add(spreadGrid[currentNode.x, nextY]);
                            choosenNodeDic.Add(new Vector2Int(currentNode.x, nextY), spreadGrid[currentNode.x, nextY]);

                            //延伸方向碰到另一方向斜邊，新增延伸
                            if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude > 1.5f)
                            {
                                if (!extendTiltSpread && (spreadGrid[currentNode.x, nextY].dir + currentNode.direction).sqrMagnitude >= 1)
                                {
                                    extendTiltSpreadVec2 += spreadGrid[currentNode.x, nextY].dir + new Vector2Int(0, currentNode.direction.y);
                                    extendTiltSpread = true;
                                }
                            }
                        }
                        //最後如果不是算過的，新增隔壁y方向節點
                        else // if (spreadGrid[currentNode.x, nextY].dir.sqrMagnitude < 0.5f)
                        {
                            area.hasCouculateNode[nextX, nextY] = true;
                            area.couculateNodeDir[nextX, nextY] = new Vector2Int(0, currentNode.direction.y);
                            spreadGrid[currentNode.x, nextY].dir = new Vector2Int(0, currentNode.direction.y);
                            spreadGrid[currentNode.x, nextY].current = true;
                            if (!spreadGrid[currentNode.x, nextY].fromArea.Contains(area.Name)) spreadGrid[currentNode.x, nextY].fromArea.Add(area.Name);
                            area.AddSpreadGridTilt(currentNode.x, nextY, new Vector2Int(0, currentNode.direction.y));

                        }
                    }

                    //下一格超過grid範圍
                    if (nextX >= spreadGrid.GetLength(0) || nextY >= spreadGrid.GetLength(1) || nextX < 0 || nextY < 0)
                    {
                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                        spreadGrid[currentNode.x, currentNode.y].stop = true;
                    }
                    //下一格是停止點
                    //else if (spreadGrid[nextX, nextY].stop)
                    //{
                    //    spreadGrid[currentNode.x, currentNode.y].current = false;
                    //    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                    //    area.spreadGridNmae.RemoveAt(j);
                    //}
                    //下一格是被選點
                    else if (spreadGrid[nextX, nextY].choosen)
                    {
                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                    }
                    //下一格是障礙
                    else if (!spreadGrid[nextX, nextY].walkable)
                    {
                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                        spreadGrid[currentNode.x, currentNode.y].stop = true;
                    }
                    //下一格同名稱且水平和垂直都沒有抵銷
                    else if (spreadGrid[nextX, nextY].fromArea.Contains(area.Name) && ((spreadGrid[nextX, nextY].dir.x + currentNode.direction.x != 0) && (spreadGrid[nextX, nextY].dir.y + currentNode.direction.y != 0))
                            )
                    {
                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                    }
                    else
                    {
                        //前進方向互相撞到，存下來
                        if ((spreadGrid[nextX, nextY].dir.sqrMagnitude > 0.5f)) 
                        {
                            spreadGrid[nextX, nextY].choosen = true;
                            spreadGrid[nextX, nextY].pos = new Vector2Int(nextX, nextY);

                            //新增鄰居
                            for (int n = choosenNode.Count - 1; n >= 0; n--)
                            {
                                Vector2Int diff = choosenNode[n].pos - spreadGrid[nextX, nextY].pos;
                                if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                                {
                                    if(!spreadGrid[nextX, nextY].neighbor.Contains(choosenNode[n])) spreadGrid[nextX, nextY].neighbor.Add(choosenNode[n]);
                                    if(!choosenNode[n].neighbor.Contains(spreadGrid[nextX, nextY])) choosenNode[n].neighbor.Add(spreadGrid[nextX, nextY]);

                                }
                            }
                            choosenNode.Add(spreadGrid[nextX, nextY]);
                            choosenNodeDic.Add(new Vector2Int(nextX, nextY), spreadGrid[nextX, nextY]);

                            area.spreadGrids.Remove(area.spreadGridNmae[j]);
                            area.spreadGridNmae.RemoveAt(j);

                            //互為斜方碰撞，延伸單一向
                            if (currentNode.direction.sqrMagnitude > 1.5f && spreadGrid[nextX, nextY].dir.sqrMagnitude > 1.5f)
                            {
                                //延伸方向
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
                                //if(!node.fromArea.Contains(area.Name))node.fromArea.Add(area.Name);
                                //if(spreadGrid[nextX, nextY].fromArea.Count > 0) node.fromArea.Add(spreadGrid[nextX, nextY].fromArea[0]);
                                tiltSpread.Add(node);
                                Debug.Log("延伸  " + node.pos + "   dir:" + node.dir);
                            }
                        }
                        else {
                            //延伸方向碰到另一方向斜邊，新增延伸
                            if (extendTiltSpread)
                            {

                                //有兩個方向，等於有三個斜邊接壤，直接設為選擇點
                                if (extendTiltSpreadVec2.sqrMagnitude > 1)
                                {
                                    spreadGrid[nextX, nextY].choosen = true;
                                    spreadGrid[nextX, nextY].pos = new Vector2Int(nextX, nextY);
                                    spreadGrid[nextX, nextY].dir = currentNode.direction;

                                    //新增鄰居
                                    for (int n = choosenNode.Count - 1; n >= 0; n--)
                                    {
                                        Vector2Int diff = choosenNode[n].pos - spreadGrid[nextX, nextY].pos;
                                        if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                                        {
                                            if (!spreadGrid[nextX, nextY].neighbor.Contains(choosenNode[n])) spreadGrid[nextX, nextY].neighbor.Add(choosenNode[n]);
                                            if (!choosenNode[n].neighbor.Contains(spreadGrid[nextX, nextY])) choosenNode[n].neighbor.Add(spreadGrid[nextX, nextY]);

                                        }
                                    }
                                    choosenNode.Add(spreadGrid[nextX, nextY]);
                                    choosenNodeDic.Add(new Vector2Int(nextX, nextY), spreadGrid[nextX, nextY]);
                                    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                                    area.spreadGridNmae.RemoveAt(j);

                                }
                                else
                                {
                                    //只碰到單一斜邊，將下一點方向改成延伸
                                    spreadGrid[nextX, nextY].choosen = true;
                                    spreadGrid[nextX, nextY].pos = new Vector2Int(nextX, nextY);
                                    spreadGrid[nextX, nextY].dir = extendTiltSpreadVec2;

                                    currentNode.direction = extendTiltSpreadVec2;
                                    int newX = nextX + extendTiltSpreadVec2.x;
                                    int newY = nextY + extendTiltSpreadVec2.y;
                                    SpreadNode node = new SpreadNode();
                                    node.pos = new Vector2Int(newX, newY);
                                    node.dir = extendTiltSpreadVec2;
                                    //if(!node.fromArea.Contains(area.Name))node.fromArea.Add(area.Name);
                                    //if(spreadGrid[nextX, nextY].fromArea.Count > 0) node.fromArea.Add(spreadGrid[nextX, nextY].fromArea[0]);
                                    tiltSpread.Add(node);
                                    Debug.Log("第2種延伸  " + nextX + "," + nextY + "   dir:" + currentNode.direction);

                                    //新增鄰居
                                    for (int n = choosenNode.Count - 1; n >= 0; n--)
                                    {
                                        Vector2Int diff = choosenNode[n].pos - spreadGrid[nextX, nextY].pos;
                                        if (diff.sqrMagnitude > 0 && diff.sqrMagnitude <= 2)
                                        {
                                            if (!spreadGrid[nextX, nextY].neighbor.Contains(choosenNode[n])) spreadGrid[nextX, nextY].neighbor.Add(choosenNode[n]);
                                            if (!choosenNode[n].neighbor.Contains(spreadGrid[nextX, nextY])) choosenNode[n].neighbor.Add(spreadGrid[nextX, nextY]);

                                        }
                                    }
                                    choosenNode.Add(spreadGrid[nextX, nextY]);
                                    choosenNodeDic.Add(new Vector2Int(nextX, nextY), spreadGrid[nextX, nextY]);
                                    area.spreadGrids.Remove(area.spreadGridNmae[j]);
                                    area.spreadGridNmae.RemoveAt(j);
                                }

                            }
                            //一般傳播點給下一格方向
                            else {
                                spreadGrid[nextX, nextY].dir = currentNode.direction;
                                spreadGrid[nextX, nextY].fromArea.Add(area.Name);
                                spreadGrid[nextX, nextY].current = true;
                            } 
                        }


                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        currentNode.x = nextX;
                        currentNode.y = nextY;

                        

                        //hasC[nextX, nextY] = true;
                        //area.hasCouculateNode[nextX, nextY] = true;
                        //area.couculateNodeDir[nextX, nextY] = currentNode.direction;
                    }
                }

            }
        }
        if (skip)
        {
            //CouculateGraphCross();
            //CouculateGraphTurn();
        }

    }

    //刪除自角落延伸的點
    void DeleteBranch() {
        Debug.Log("計算末端點  " + choosenNode.Count);
        List<Vector2Int> couculatedNodes = new List<Vector2Int>();
        List<SpreadNode> probablyEndNodes = new List<SpreadNode>();

        //遍歷被選點
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            SpreadNode node = choosenNode[i];
            SpreadNode endNode = null;
            int count = 0;
            int xNum = 0;
            int yNum = 0;
            //Debug.Log("計算  " + choosenNode[i].pos + "  " + node.neighbor.Count);

            //已經被算過是有碰撞的末端點，捨棄
            if (couculatedNodes.Contains(node.pos))
            {
                //Debug.Log("計算過  " + node.pos + " 捨棄");
                continue;
            }

            //遍歷鄰居計算方向
            for (int j = 0; j < node.neighbor.Count; j++)
            {
                xNum += (node.neighbor[j].pos.x - node.pos.x);
                yNum += (node.neighbor[j].pos.y - node.pos.y);
                count++;
                Debug.Log("鄰居有  " + node.neighbor[j].pos);
            }
            if (count == 0)
            {
                //沒有鄰居，列入以計算點，之後刪除
                couculatedNodes.Add(node.pos);
            }
            else if (count == 1)
            {
                //只有一鄰居，為末端點
                endNode = node;

                //Debug.Log("個數1 末端 ");
            }
            //有多個鄰居的末端，方向不會抵銷
            else if (count == 2 && (Mathf.Abs(xNum) >= 2 || Mathf.Abs(yNum) >= 2))
            {
                //兩個鄰居，但位於同方向，為末端點
                endNode = node;
                
                //Debug.Log("個數多 末端 ");
            }

            //如果是末端點，開始往回推
            int breakNum = 0;
            while (endNode != null)
            {
                //Debug.Log(" 開始計算 " + endNode.pos + " 的分支");
                breakNum++;
                if (breakNum > 9999) break;

                if (endNode.neighbor.Count <= 1)
                {

                    if (endNode.neighbor.Count == 0)
                    {
                        //Debug.Log("分支盡頭");
                        couculatedNodes.Add(endNode.pos);
                        endNode = null;
                    }
                    else
                    {
                        //只有一個鄰居
                        //Debug.Log(endNode.pos + " 有一個鄰居 " + endNode.neighbor[0].pos);
                        endNode.neighbor[0].neighbor.Remove(endNode);
                        couculatedNodes.Add(endNode.pos);
                        if (!probablyEndNodes.Contains(endNode.neighbor[0]))
                        {
                            //如果那個鄰居沒有在交錯點列表，可以將它列為下一個判斷
                            //Debug.Log("鄰居 " + endNode.neighbor[0].pos + "  鄰居為下一個計算點");
                            endNode = endNode.neighbor[0];
                        }
                        else
                        {
                            //如果那個鄰居在交錯點列表，結束這個分支計算
                            //Debug.Log("鄰居 " + endNode.neighbor[0].pos + "  為分支末端");
                            endNode = null;
                        }
                    }
                }
                else
                {
                    //如果有多個鄰居，需要計算方向
                    //Debug.Log(endNode.pos + " 有多個鄰居");
                    count = 0;
                    xNum = 0;
                    yNum = 0;
                    int neighborNum = 0; //用於確認自己是不是鄰居唯一的接壤點，刪除可能會造成斷路
                    for (int j = endNode.neighbor.Count - 1; j >= 0; j--)
                    {
                        if (!couculatedNodes.Contains(endNode.neighbor[j].pos))
                        {
                            xNum += (endNode.neighbor[j].pos.x - endNode.pos.x);
                            yNum += (endNode.neighbor[j].pos.y - endNode.pos.y);
                            count++;
                            neighborNum += (endNode.neighbor[j].neighbor.Count - 1);
                            //Debug.Log(endNode.pos + " 有多個鄰居  " + endNode.neighbor[j].pos);
                        }
                    }
                    //確認自己是鄰居唯一的接壤點，刪除可能會造成斷路，直接結束分支
                    if (neighborNum == endNode.neighbor.Count) {
                        if (!probablyEndNodes.Contains(endNode)) probablyEndNodes.Add(endNode);
                        endNode = null;
                        break;
                    }
                    if ((Mathf.Abs(xNum) >= 2 || Mathf.Abs(yNum) >= 2))
                    {
                        //Debug.Log("兩個鄰居形成末端  " + endNode.pos);
                        couculatedNodes.Add(endNode.pos);
                        for (int j = endNode.neighbor.Count - 1; j >= 0; j--)
                        {
                            if (!couculatedNodes.Contains(endNode.neighbor[j].pos))
                            {
                                endNode.neighbor[j].neighbor.Remove(endNode);
                                //將鄰居都納入可能清單
                                if (!probablyEndNodes.Contains(endNode.neighbor[j])) probablyEndNodes.Add(endNode.neighbor[j]);
                                //將鄰居從自己鄰居清單移除
                                if (endNode.neighbor[j].neighbor.Count == 0) endNode.neighbor.RemoveAt(j);
                            }
                        }
                    }
                    else
                    {
                        //如果有多個鄰居，且方向計算後不為末端，將自己加入可能清單
                        if (!probablyEndNodes.Contains(endNode)) probablyEndNodes.Add(endNode);
                    }
                    endNode = null;
                    //一有多個鄰居就結束該分支
                }
            }
        }

        //將計算過的點捨棄
        for (int i = couculatedNodes.Count - 1; i >= 0; i--)
        {
            //Debug.Log("計算過  " + couculatedNodes[i] + " 捨棄");
            choosenNodeDic[couculatedNodes[i]].choosen = false;
            if (probablyEndNodes.Contains(choosenNodeDic[couculatedNodes[i]])) probablyEndNodes.Remove(choosenNodeDic[couculatedNodes[i]]);
            choosenNode.Remove(choosenNodeDic[couculatedNodes[i]]);
            choosenNodeDic.Remove(couculatedNodes[i]);

        }
        couculatedNodes.Clear();
        waitNodes.Clear();

        //遍歷可能清單，為各分支的來源交錯點
        for (int i = probablyEndNodes.Count - 1; i >= 0; i--)
        {
            if (couculatedNodes.Contains(probablyEndNodes[i].pos)) continue;

            //Debug.Log("新支線!!!!!!!!!!!!!!!!!!!!!!!!!");

            //可能是末端點的交錯點
            SpreadNode endNode = probablyEndNodes[i];
            int breakNum = 0;
            while (endNode != null)
            {
                breakNum++;
                if (breakNum > 9999) break;
                Vector3 pos = pathFindGrid.GetNodePos(endNode.pos.x, endNode.pos.y);
                Vector3 VStart = pos + new Vector3(0, 0, closeDstNum);
                Vector3 VEnd = pos + new Vector3(0, 0, -closeDstNum);
                Vector3 HStart = pos + new Vector3(closeDstNum, 0, 0);
                Vector3 HEnd = pos + new Vector3(-closeDstNum, 0, 0);
                //Debug.Log("交錯末端點 ~~~~~~~!!!!!!!!! " + endNode.pos + "     position: ");
                //Debug.Log("左邊 " + HEnd);
                //Debug.Log("右邊 " + HStart);
                //Debug.Log("下面 " + VEnd);
                //Debug.Log("上面 " + VStart);

                //如果周圍有障礙繼續執行刪除分支
                if (Physics.Linecast(VStart, VEnd, 1 << LayerMask.NameToLayer("Obstacle")) || Physics.Linecast(HStart, HEnd, 1 << LayerMask.NameToLayer("Obstacle")) ||
                        VStart.z > pathFindGrid.MaxBorderPoint.z || VEnd.z < pathFindGrid.MinBorderPoint.z || HStart.x > pathFindGrid.MaxBorderPoint.x || HEnd.x < pathFindGrid.MinBorderPoint.x)
                {
                    //Debug.Log("周圍有障礙 ");
                    int count = 0;
                    int xNum = 0;
                    int yNum = 0;
                    int neighborNum = 0; //確認自己是不是鄰居唯一的接壤點，刪除可能會造成斷路
                    if (endNode.neighbor.Count == 0)
                    {
                        //Debug.Log("是末端  捨棄");
                        couculatedNodes.Add(endNode.pos);
                        endNode = null;
                    }
                    else if (endNode.neighbor.Count == 1)
                    {
                        //Debug.Log("是末端  捨棄");
                        //剩下的鄰居有在等待清單，將其從等待清單移除
                        if (waitNodes.Contains(endNode.neighbor[0])) waitNodes.Remove(endNode.neighbor[0]);
                        couculatedNodes.Add(endNode.pos);
                        endNode.neighbor[0].neighbor.Remove(endNode);
                        endNode = endNode.neighbor[0];
                    }
                    else
                    {
                        //多個鄰居，需要計算方向
                        for (int j = 0; j < endNode.neighbor.Count; j++)
                        {
                            xNum += (endNode.neighbor[j].pos.x - endNode.pos.x);
                            yNum += (endNode.neighbor[j].pos.y - endNode.pos.y);
                            count++;
                            neighborNum += (endNode.neighbor[j].neighbor.Count - 1);
                        }
                        //確認自己是鄰居唯一的接壤點，刪除可能會造成斷路
                        if (neighborNum == endNode.neighbor.Count)
                        {
                            endNode = null;
                            break;
                        }
                        if ((Mathf.Abs(xNum) >= 2 || Mathf.Abs(yNum) >= 2))
                        {
                            //Debug.Log("是末端  捨棄");
                            couculatedNodes.Add(endNode.pos);
                            for (int j = 0; j < endNode.neighbor.Count; j++)
                            {
                                endNode.neighbor[j].neighbor.Remove(endNode);
                                if (!couculatedNodes.Contains(endNode.neighbor[j].pos) && !waitNodes.Contains(endNode.neighbor[j]))
                                {
                                    //Debug.Log("加~~~~進等待  " + endNode.neighbor[j].pos);
                                    waitNodes.Add(endNode.neighbor[j]);
                                }
                            }
                        }
                        if (waitNodes.Count > 0)
                        {
                            endNode = waitNodes[0];
                            waitNodes.RemoveAt(0);
                        }
                        else
                        {
                            endNode = null;
                        }
                    }
                }
                else
                {
                    //如果周圍沒有障礙，停止執行刪除分支
                    endNode = null;
                }
            }
        }
        waitNodes.Clear();
        for (int i = couculatedNodes.Count - 1; i >= 0; i--)
        {
            //已經被算過是有碰撞的末端點，捨棄
            //Debug.Log("計算過  " + couculatedNodes[i] + " 捨棄");
            choosenNodeDic[couculatedNodes[i]].choosen = false;
            choosenNode.Remove(choosenNodeDic[couculatedNodes[i]]);
            choosenNodeDic.Remove(couculatedNodes[i]);
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
            //bool hasCross = false;
            bool hasTurn = false;
            int crossWeight = 0;

            for (int cy = 1; cy >= -1; cy--) {
                for (int cx = -1; cx <= 1; cx++) {
                    Vector2Int detectPos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy); 
                    
                    if ((cy == 0 && cx == 0)  || detectPos.x < 0 || detectPos.x >= gridX || detectPos.y < 0 || detectPos.y >= gridY || crossWeight >= 10) continue; // || hasCross
                    Debug.Log(choosenNode[i].pos + " detect  " + detectPos.x + "," + detectPos.y + "  walkable" + spreadGrid[detectPos.x, detectPos.y].walkable + "   choosen" + spreadGrid[detectPos.x, detectPos.y].choosen);
                    //hasCross = (hasCross || (confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].crossNode));
                    hasTurn = (hasTurn || (confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].turnNode));
                    if ((confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].weight > crossWeight)) crossWeight = confirmGraphNodeDic[detectPos].weight;
                    if (spreadGrid[detectPos.x, detectPos.y].walkable && spreadGrid[detectPos.x, detectPos.y].choosen) {
                        
                        count++;
                        xNum += cx;
                        yNum += cy;
                        couculateNum += Mathf.Abs(cx);
                        couculateNum += Mathf.Abs(cy);
                        Debug.Log(choosenNode[i].pos + "couculate   " + detectPos.x + "," + detectPos.y + " num " + xNum + "," + yNum);
                    }
                }
            }
            //if (hasCross) continue;
            if (count == 1) {
                //PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //ConfirmGraph.Add(node);
                //confirmGraphNodeDic.Add(choosenNode[i].pos, node);
            }
            else if (xNum != 0 || yNum != 0)
            {
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //x或y其一不抵銷，斜邊數量多列為交錯點，判斷為交錯點的權重比較小
                if (couculateNum > 4)
                { //&& couculateNum % 2 == 0
                    for (int cy = 1; cy >= -1; cy--)
                    {
                        for (int cx = -1; cx <= 1; cx++)
                        {
                            if (cx == 0 && cy == 0) continue;
                            Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                            if (confirmGraphNodeDic.ContainsKey(pos)) //!confirmGraphNodeDic[pos].crossNode
                            {
                                confirmGraphNodeDic[pos].turnNode = false;
                                ConfirmGraph.Remove(confirmGraphNodeDic[pos]);
                                confirmGraphNodeDic.Remove(pos);
                            }
                        }
                    }
                    node.crossNode = true;
                    node.weight = 7;
                    ConfirmGraph.Add(node);
                    confirmGraphNodeDic.Add(choosenNode[i].pos, node);

                }

                else if(!hasTurn && crossWeight < 5){
                    //一般列為轉折點
                    node.turnNode = true;
                    node.weight = 5;
                    ConfirmGraph.Add(node);
                    confirmGraphNodeDic.Add(choosenNode[i].pos, node);
                }

            }
            else
            {

                //x或y都抵銷且周圍數量不為偶數，列為交錯點，不然就不列入
                if (count == 4 || (count > 0 && count % 2 != 0))
                {
                    PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                    node.crossNode = true;
                    node.weight = 10;
                    ConfirmGraph.Add(node);
                    confirmGraphNodeDic.Add(choosenNode[i].pos, node);

                    for (int cy = 1; cy >= -1; cy--)
                    {
                        for (int cx = -1; cx <= 1; cx++)
                        {
                            if (cx == 0 && cy == 0) continue;
                            Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                            if (confirmGraphNodeDic.ContainsKey(pos))  //&& !confirmGraphNodeDic[pos].crossNode
                            {
                                //比上方判斷交錯點的權重大，周圍權重不大於10就刪掉
                                if (confirmGraphNodeDic[pos].weight > 5)
                                {
                                    confirmGraphNodeDic[pos].crossNode = false;
                                    confirmGraphNodeDic[pos].weight = 0;
                                }
                                confirmGraphNodeDic[pos].turnNode = false;
                                ConfirmGraph.Remove(confirmGraphNodeDic[pos]);
                                confirmGraphNodeDic.Remove(pos);
                            }
                        }
                    }
                }
            }
        }
    }

    //如果自己的鄰居可以互相連接，或是寬度小於一定的道路，可以刪掉
    void DeleteExtraNode() {

        //第一次遍歷先找出過窄的道路，刪除
        for (int i = choosenNode.Count - 1; i >= 0; i--) {
            if (leastNarrow > .0f)
            {
                Vector3 pos = pathFindGrid.GetNodePos(choosenNode[i].pos.x, choosenNode[i].pos.y);
                Vector3 VStart = pos + new Vector3(0, 0, leastNarrow);
                Vector3 VEnd = pos + new Vector3(0, 0, -leastNarrow);
                Vector3 HStart = pos + new Vector3(leastNarrow, 0, 0);
                Vector3 HEnd = pos + new Vector3(-leastNarrow, 0, 0);
                if (Physics.Linecast(VStart, VEnd, 1 << LayerMask.NameToLayer("Obstacle")) || Physics.Linecast(HStart, HEnd, 1 << LayerMask.NameToLayer("Obstacle")) ||
                        VStart.z > pathFindGrid.MaxBorderPoint.z || VEnd.z < pathFindGrid.MinBorderPoint.z || HStart.x > pathFindGrid.MaxBorderPoint.x || HEnd.x < pathFindGrid.MinBorderPoint.x)
                {
                    for (int j = 0; j < choosenNode[i].neighbor.Count; j++)
                    {
                        choosenNode[i].neighbor[j].neighbor.Remove(choosenNode[i]);
                    }
                    choosenNode[i].neighbor.Clear();
                    choosenNodeDic.Remove(choosenNode[i].pos);
                    choosenNode.RemoveAt(i);
                }
            }
        }

        //第二次遍歷，如果自己的鄰居可以互相連接，可以刪除
        for (int i = choosenNode.Count - 1; i >= 0; i--) {
            //鄰居大於2的，計算
            if (choosenNode[i].neighbor.Count >= 2)
            {
                bool[] couculate = new bool[choosenNode[i].neighbor.Count];
                int couculateNum = 0;
                //計算每個鄰居之間有沒有連接，連接數++
                for (int j = 0; j < choosenNode[i].neighbor.Count; j++)
                {

                    if (couculate[j]) continue;
                    for (int k = 0; k < choosenNode[i].neighbor.Count; k++)
                    {
                        if (j == k) continue;
                        if ((choosenNode[i].neighbor[j].pos - choosenNode[i].neighbor[k].pos).sqrMagnitude <= 2)
                        {
                            couculate[j] = true;
                            couculate[k] = true;
                            couculateNum++;
                        }
                    }
                }
                //如果連接數大於鄰居數量-1，就代表鄰居可以不經過自己走到連到其他鄰居，可以把自己刪掉
                if (couculateNum >= choosenNode[i].neighbor.Count - 1)
                {
                    for (int j = 0; j < choosenNode[i].neighbor.Count; j++)
                    {
                        choosenNode[i].neighbor[j].neighbor.Remove(choosenNode[i]);
                    }
                    choosenNode[i].neighbor.Clear();
                    choosenNodeDic.Remove(choosenNode[i].pos);
                    choosenNode.RemoveAt(i);
                }
            }
            //鄰居數量0的，把自己刪掉
            else if (choosenNode[i].neighbor.Count == 0) {
                for (int j = 0; j < choosenNode[i].neighbor.Count; j++)
                {
                    choosenNode[i].neighbor[j].neighbor.Remove(choosenNode[i]);
                }
                choosenNode[i].neighbor.Clear();
                choosenNodeDic.Remove(choosenNode[i].pos);
                choosenNode.RemoveAt(i);
            }
        }
    }

    //計算交錯點，跟把交錯點合成合併點
    void CouculateGraphCross() {
        
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            int count = 0;
            int xNum = 0, yNum = 0;

            SpreadNode mergeNode = new SpreadNode();

            //如果自己沒被合併且鄰居數大於2，檢查可不可以合併
            if (!choosenNode[i].beenMerged && choosenNode[i].neighbor.Count > 2)
            {
                choosenNode[i].crossNode = true;

                List<SpreadNode> waitMergeNodes = new List<SpreadNode>();
                List<SpreadNode> changeNeighbor = new List<SpreadNode>();

                //遍歷鄰居
                for (int j = 0; j < choosenNode[i].neighbor.Count; j++)
                {
                    //如果鄰居的鄰居數大於2，他也是交錯點，可以開始合併
                    if (choosenNode[i].neighbor[j].neighbor.Count > 2)
                    {
                        //檢查鄰居尚未被合併，且等待合併清單也沒有它，進行合併
                        if (!choosenNode[i].neighbor[j].beenMerged && !waitMergeNodes.Contains(choosenNode[i].neighbor[j])) {
                            //第一次先初始化合併點
                            if (!choosenNode[i].beenMerged) {
                                choosenNode[i].mergeNode = mergeNode;
                                choosenNode[i].beenMerged = true;
                                mergeNode.crossNode = true;
                                mergeNode.pos = choosenNode[i].pos;
                                mergeNode.mergeCount++;
                                List<SpreadNode> allMergeNode = new List<SpreadNode>();
                                allMergeNode.Add(choosenNode[i]);
                                mergeNodeDic.Add(mergeNode, allMergeNode);

                                //變化鄰居點，將鄰居點的鄰居改成是合併點
                                for (int n = 0; n < changeNeighbor.Count; n++) {
                                    changeNeighbor[n].neighbor.Remove(choosenNode[i]);
                                    changeNeighbor[n].neighbor.Add(mergeNode);
                                }
                            }
                            //Debug.Log(choosenNode[i].pos + "  開始為合併點 " + choosenNode[i].neighbor[j].pos);
                            
                            choosenNode[i].neighbor[j].mergeNode = mergeNode;
                            choosenNode[i].neighbor[j].beenMerged= true;
                            mergeNode.pos += choosenNode[i].neighbor[j].pos;
                            mergeNode.mergeCount++;
                            mergeNodeDic[mergeNode].Add(choosenNode[i].neighbor[j]);
                            waitMergeNodes.Add(choosenNode[i].neighbor[j]);
                        }
                    }
                    else
                    {
                        //鄰居為一般點，不合併，存到合併點鄰居
                        if (!mergeNode.neighbor.Contains(choosenNode[i].neighbor[j]))
                        {
                            //如果合併了，改鄰居的鄰居為合併點，如果還沒合併，遇到一般鄰居，將他們存到變化清單
                            mergeNode.neighbor.Add(choosenNode[i].neighbor[j]);
                            if (!choosenNode[i].beenMerged) changeNeighbor.Add(choosenNode[i].neighbor[j]);
                            else {
                                choosenNode[i].neighbor[j].neighbor.Remove(choosenNode[i]);
                                choosenNode[i].neighbor[j].neighbor.Add(mergeNode);
                            }
                        }
                    }

                }

                int breakNum = 0;
                while (waitMergeNodes.Count > 0) {
                    breakNum++;
                    if (breakNum >= 99999) break;
                    
                    //遍歷等待清單
                    for (int n = 0; n < waitMergeNodes[0].neighbor.Count; n++) {
                        //鄰居大於2是交錯點，合併進合併點
                        if ( waitMergeNodes[0].neighbor[n].neighbor.Count > 2)
                        {
                            if (!waitMergeNodes[0].neighbor[n].beenMerged && !waitMergeNodes.Contains(waitMergeNodes[0].neighbor[n])) {
                                mergeNode.pos += waitMergeNodes[0].pos;
                                mergeNode.mergeCount++;
                                waitMergeNodes[0].neighbor[n].beenMerged = true;
                                waitMergeNodes[0].neighbor[n].mergeNode = mergeNode;
                                mergeNodeDic[mergeNode].Add(waitMergeNodes[0].neighbor[n]);
                                waitMergeNodes.Add(waitMergeNodes[0].neighbor[n]);
                            }
                        }
                        //鄰居小於等於2是一般點，加進合併點鄰居
                        else
                        {
                            if (!mergeNode.neighbor.Contains(waitMergeNodes[0].neighbor[n]))
                            {
                                mergeNode.neighbor.Add(waitMergeNodes[0].neighbor[n]);
                                waitMergeNodes[0].neighbor[n].neighbor.Remove(waitMergeNodes[0]);
                                waitMergeNodes[0].neighbor[n].neighbor.Add(mergeNode);
                            }
                        }
                    }
                    waitMergeNodes.RemoveAt(0);
                }

                //將合併點內的所有交錯點等於合併點數值
                if (mergeNodeDic.ContainsKey(mergeNode)) {
                    mergeNode.pos = new Vector2Int(Mathf.RoundToInt((float)mergeNode.pos.x / (float)mergeNode.mergeCount), Mathf.RoundToInt((float)mergeNode.pos.y / (float)mergeNode.mergeCount));
                    for (int n = 0; n < mergeNodeDic[mergeNode].Count; n++)
                    {
                        mergeNodeDic[mergeNode][n].neighbor = mergeNode.neighbor;
                        mergeNodeDic[mergeNode][n].crossNode = true;
                        mergeNodeDic[mergeNode][n].beenMerged = true;
                        mergeNodeDic[mergeNode][n].pos = mergeNode.pos;
                    }
                }
            }

            //Debug用
            //if (choosenNode[i].beenMerged)
            //{
            //    foreach (SpreadNode node in choosenNode[i].mergeNode.neighbor)
            //    {
            //        Debug.Log(choosenNode[i].pos + "merge  neighbor " + node.pos);
            //    }
            //}
        }
    }

    //確認轉折點或末端點
    void CouculateGraphTurn()
    {
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            //將沒有choosen的移除
            if (!choosenNode[i].choosen) {
                choosenNodeDic.Remove(choosenNode[i].pos);
                choosenNode.RemoveAt(i);
                continue;
            }

            //遍歷鄰居決定轉折點或末端點
            int xNum = 0, yNum = 0;
            bool hasTurn = false;
            int count = 0;
            for (int j = 0; j < choosenNode[i].neighbor.Count; j++) {
                Vector2Int detectPos = choosenNode[i].neighbor[j].pos;
                Vector2Int diff = detectPos - choosenNode[i].pos;
                count++;
                xNum += diff.x;
                yNum += diff.y;
                hasTurn = hasTurn || choosenNode[i].neighbor[j].turnNode || choosenNode[i].neighbor[j].crossNode;
            }
            if (choosenNode[i].neighbor.Count == 1)
            {
                //末端點
                choosenNode[i].endNode = true;
            }
            if (!hasTurn && (xNum != 0 || yNum != 0) && count == 2)
            {
                //一般列為轉折點
                choosenNode[i].turnNode = true;
            }
           
        }
    }

    //遍歷所有被選點來連接
    public IEnumerator ConactGraph() {

        PatrolGraphNode fromNode = null;
        int connectLength = 0;

        //先找出一個交錯點為開始點
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            sourceNode = (choosenNode[i].beenMerged)? choosenNode[i].mergeNode: choosenNode[i];
            if (sourceNode.crossNode){
                //Debug.Log("第一個點 " + sourceNode.pos);
                fromNode = new PatrolGraphNode(sourceNode.pos.x, sourceNode.pos.y);
                fromNode.crossNode = true;
                fromNode.pos = pathFindGrid.GetNodePos(fromNode.x, fromNode.y);
                ConfirmGraph.Add(fromNode);
                confirmGraphNodeDic.Add(sourceNode.pos, fromNode);

                connectNeighbor = sourceNode.neighbor[0];
                sourceNode.neighbor.RemoveAt(0);
                connectNeighbor.neighbor.Remove(sourceNode);
                //將第一個交錯點列入清單
                waitNodes.Add(sourceNode);

                //Debug.Log("第一個鄰居點 " + connectNeighbor.pos + "  是否交錯點" + connectNeighbor.crossNode);
                break;
            } 
        }

        SpreadNode detectNode = null;
        SpreadNode lastTurnNode = null;
        Vector2Int lastDir = new Vector2Int(0,0);
        int lastLength = 0;
        int currentLength = 0;
        int breakNum = 0;
        while (connectNeighbor != null) {


            if (!skipRun) yield return new WaitForSeconds(connectTime);

            breakNum++;
            if (breakNum > 100000) break;

            connectLength++;
            currentLength++;

            //判斷鄰居是不是交錯點
            Debug.Log("計算鄰居點 " + connectNeighbor.pos);
            if (connectNeighbor.crossNode)
            {
                PatrolGraphNode nextNode;

                //新的交錯點與上個轉折點中有障礙物，將上一個轉折點加入
                //確認如果有上一轉折點，查看中間有無障礙物，有的話先連接來源點與上一轉折點

                Vector3 sourcePos = pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y);
                Vector3 connectPos = pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y);
                Vector3 center = 0.5f * (sourcePos + connectPos);
                Vector3 halfExtent = new Vector3((center - sourcePos).magnitude, 1.0f, leastNarrow);
                Collider[] hits = Physics.OverlapBox(center, halfExtent, Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, Vector3.Angle(Vector3.left, (connectPos - sourcePos)), 0), 1 << LayerMask.NameToLayer("Obstacle"));
                //(Physics.Linecast(pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y), pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y), 1 << LayerMask.NameToLayer("Obstacle")) 
                if (lastTurnNode != null &&
                    ((hits == null ? false : (hits.Length > 0)) ||
                    (Vector2.Angle(lastDir, connectNeighbor.pos - detectNode.pos) > maxConnectAngle) && (connectLength - lastLength) > leastTurnDstNum))
                {
                    sourcePos = pathFindGrid.GetNodePos(lastTurnNode.pos.x, lastTurnNode.pos.y);
                    hits = Physics.OverlapBox(center, halfExtent, Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, Vector3.Angle(Vector3.left, (connectPos - sourcePos)), 0), 1 << LayerMask.NameToLayer("Obstacle"));
                    if (hits == null ? false : (hits.Length > 0)) {
                        lastTurnNode = detectNode;
                        lastLength += currentLength;
                        connectLength -= currentLength;
                    }
                    if (!confirmGraphNodeDic.ContainsKey(lastTurnNode.pos))
                    {
                        Debug.Log("尚未有該轉折點");
                        nextNode = new PatrolGraphNode(lastTurnNode.pos.x, lastTurnNode.pos.y);
                        nextNode.turnNode = true;
                        nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                        ConfirmGraph.Add(nextNode);
                        confirmGraphNodeDic.Add(lastTurnNode.pos, nextNode);
                    }
                    //已有
                    else
                    {
                        Debug.Log("已有該轉折點");
                        nextNode = confirmGraphNodeDic[lastTurnNode.pos];
                    }
                    float dist = (confirmGraphNodeDic[sourceNode.pos].pos - nextNode.pos).magnitude;
                    //將來源點加進上個轉折點的連接點裡
                    nextNode.besideNodes.Add(confirmGraphNodeDic[sourceNode.pos], dist);
                    //將上個轉折點加進來原點的連接點裡
                    confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(nextNode, dist);
                    Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + dist);
                    connectLength -= lastLength;

                    lastDir = new Vector2Int(connectNeighbor.pos.x - lastTurnNode.pos.x, connectNeighbor.pos.y - lastTurnNode.pos.y);
                    sourceNode = lastTurnNode;
                }
                //else if (lastTurnNode != null)
                //{
                //    sourceNode = lastTurnNode;
                //}

                //新增鄰居點進圖，設鄰居點為來源點的隔壁點
                //未在圖裡，新的點
                if (!confirmGraphNodeDic.ContainsKey(connectNeighbor.pos))
                {
                    nextNode = new PatrolGraphNode(connectNeighbor.pos.x, connectNeighbor.pos.y);
                    nextNode.crossNode = true;
                    nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                    ConfirmGraph.Add(nextNode);
                    confirmGraphNodeDic.Add(connectNeighbor.pos, nextNode);

                }
                //已有
                else
                {
                    nextNode = confirmGraphNodeDic[connectNeighbor.pos];
                }
                float dst = (confirmGraphNodeDic[sourceNode.pos].pos - nextNode.pos).magnitude;
                //將來源點加進鄰居的連接點裡
                nextNode.besideNodes.Add(confirmGraphNodeDic[sourceNode.pos], dst);
                //將鄰居點加進自己的連接點裡
                confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(nextNode,dst);
                Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + connectLength);
                connectLength = 0;
                currentLength = 0;
                lastLength = 0;
                lastTurnNode = null;

                //鄰居點的鄰居數大於0，加入等待清單
                if (connectNeighbor.neighbor.Count > 0)
                {
                    Debug.Log("判斷交錯點  新增進清單 " + connectNeighbor.pos);
                    if (!connectNeighbor.beenMerged && !waitNodes.Contains(connectNeighbor)) waitNodes.Add(connectNeighbor);
                    else if (connectNeighbor.beenMerged && !waitNodes.Contains(connectNeighbor.mergeNode)) waitNodes.Add(connectNeighbor.mergeNode);
                    //if(!connectNeighbor.neighbor.Contains(connectNeighbor))waitNodes.Add(connectNeighbor);
                }
                Debug.Log("判斷交錯點  回清單第0個繼續下一分支");
                //確認清單中的點有鄰居
                bool newSource = false;
                while (waitNodes.Count > 0)
                {
                    Debug.Log("清單有  " + waitNodes[0].pos + "  有鄰居個數" + waitNodes[0].neighbor.Count);
                    sourceNode = waitNodes[0];
                    if (sourceNode.neighbor.Count > 0)
                    {
                        Debug.Log("來原點 " + sourceNode.pos + "   新分支 " + sourceNode.neighbor[0].pos);
                        connectNeighbor = sourceNode.neighbor[0];
                        sourceNode.neighbor.RemoveAt(0);
                        Debug.Log( "   新分支 " + connectNeighbor.pos + "移除  " + sourceNode.pos + "--------> " + connectNeighbor.neighbor.Contains(sourceNode));
                        connectNeighbor.neighbor.Remove(sourceNode);

                        if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                        newSource = true;
                        break;
                    }
                    else waitNodes.RemoveAt(0);
                }
                if (!newSource) connectNeighbor = null;
            }
            //判斷鄰居是不是轉折點
            else if (connectNeighbor.turnNode)
            {
                PatrolGraphNode nextNode;
                //新增鄰居點進圖，設鄰居點為來源點的隔壁點
                //未在圖裡，新的點
                Debug.Log("新增轉折點");

                if (lastTurnNode == null)
                {
                    lastDir = new Vector2Int(connectNeighbor.pos.x - sourceNode.pos.x, connectNeighbor.pos.y - sourceNode.pos.y);
                    lastTurnNode = connectNeighbor;
                    lastLength = connectLength;
                    Debug.Log("第一個轉折點");
                    //第一個轉折點還要確認距離有沒有夠長
                    //if (connectLength > leastTurnDstNum)
                    //{
                    //}
                }
                else {
                    //與來原點之間有碰撞，或與上一個轉折點角度太大，加入上一個轉折點
                    //Vector3 center = 0.5f*(pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y) + pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y));
                    //float length = (pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y) - pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y))
                    //Physics.OverlapBox(center, new Vector3( ,1.0f, leastNarrow))
                    Debug.Log("source node  " + sourceNode.pos);
                    Debug.Log(" angle " + (Vector2.Angle(lastDir, connectNeighbor.pos - lastTurnNode.pos)));
                    Debug.Log(" connect length " + (connectLength - lastLength));
                    Debug.Log(" lastLength " + lastLength);

                    Vector3 sourcePos = pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y);
                    Vector3 connectPos = pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y);
                    Vector3 center = 0.5f * (sourcePos + connectPos);
                    Vector3 halfExtent = new Vector3((center - sourcePos).magnitude, 1.0f, leastNarrow);
                    Collider[] hits = Physics.OverlapBox(center, halfExtent, Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, Vector3.Angle(Vector3.left, (connectPos - sourcePos)), 0), 1 << LayerMask.NameToLayer("Obstacle"));
                    //Physics.Linecast(pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y), pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y), 1 << LayerMask.NameToLayer("Obstacle")) 
                    Debug.Log(" hit wall  ：" + (hits == null ? false : (hits.Length > 0)));

                    if ((hits == null ? false : (hits.Length > 0))||
                        (Vector2.Angle(lastDir, connectNeighbor.pos - lastTurnNode.pos) > maxConnectAngle && (connectLength - lastLength) > leastTurnDstNum && (lastLength > leastTurnDstNum))) //
                    {
                        sourcePos = pathFindGrid.GetNodePos(lastTurnNode.pos.x, lastTurnNode.pos.y);
                        hits = Physics.OverlapBox(center, halfExtent, Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, Vector3.Angle(Vector3.left, (connectPos - sourcePos)), 0), 1 << LayerMask.NameToLayer("Obstacle"));
                        if (hits == null ? false : (hits.Length > 0))
                        {
                            lastTurnNode = detectNode;
                            lastLength += currentLength;
                            connectLength -= currentLength;
                        }

                        if (!confirmGraphNodeDic.ContainsKey(lastTurnNode.pos))
                        {
                            Debug.Log("尚未有該轉折點");
                            nextNode = new PatrolGraphNode(lastTurnNode.pos.x, lastTurnNode.pos.y);
                            nextNode.turnNode = true;
                            nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                            ConfirmGraph.Add(nextNode);
                            confirmGraphNodeDic.Add(lastTurnNode.pos, nextNode);
                        }
                        //已有
                        else
                        {
                            Debug.Log("已有該轉折點");
                            nextNode = confirmGraphNodeDic[lastTurnNode.pos];
                        }
                        float dst = (confirmGraphNodeDic[sourceNode.pos].pos - nextNode.pos).magnitude;
                        Debug.Log("新連接 " + sourceNode.pos + " ---> " + lastTurnNode.pos + "  length" + dst);
                        //將來源點加進上個轉折點的連接點裡
                        nextNode.besideNodes.Add(confirmGraphNodeDic[sourceNode.pos], dst);
                        //將上個轉折點加進來原點的連接點裡
                        confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(nextNode, dst);
                        connectLength -= lastLength;
                        lastDir = new Vector2Int(connectNeighbor.pos.x - detectNode.pos.x, connectNeighbor.pos.y - detectNode.pos.y);
                        sourceNode = lastTurnNode;
                        lastTurnNode = connectNeighbor;
                        //if(connectLength > leastTurnDstNum) lastTurnNode = connectNeighbor;
                        //else lastTurnNode = null;
                        lastLength = connectLength;
                        Debug.Log("轉折點改為    " + lastTurnNode.pos);
                    }
                    else if ((connectLength - lastLength) > leastTurnDstNum || lastLength < leastTurnDstNum)
                    {
                        lastDir = new Vector2Int(connectNeighbor.pos.x - sourceNode.pos.x, connectNeighbor.pos.y - sourceNode.pos.y);
                        lastTurnNode = connectNeighbor;
                        lastLength = connectLength;
                        Debug.Log("往後推最後轉折點  " + lastTurnNode.pos);
                    }
                }
                detectNode = connectNeighbor;

                //鄰居點的鄰居數大於0
                if (connectNeighbor.neighbor.Count > 0)
                {
                    Debug.Log("轉折點 鄰居大於0");

                    SpreadNode temp = connectNeighbor.neighbor[0];
                   temp.neighbor.Remove(connectNeighbor);
                    connectNeighbor.neighbor.RemoveAt(0);
                    connectNeighbor = temp;
                }
                //鄰居點沒有鄰居
                else
                {
                    Debug.Log("轉折點 鄰居小於0  從清單中選");
                    //確認清單中的點有鄰居
                    bool newSource = false;
                    while (waitNodes.Count > 0)
                    {
                        Debug.Log("清單有  " + waitNodes[0].pos + "  有鄰居個數" + waitNodes[0].neighbor.Count);
                        sourceNode = waitNodes[0];
                        if (sourceNode.neighbor.Count > 0)
                        {
                            Debug.Log("來原點 " + sourceNode.pos + "   新分支 " + sourceNode.neighbor[0].pos);
                            connectNeighbor = sourceNode.neighbor[0];
                            sourceNode.neighbor.RemoveAt(0);
                            connectNeighbor.neighbor.Remove(sourceNode);
                            if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                            newSource = true;
                            break;
                        }
                        else waitNodes.RemoveAt(0);
                    }
                    if(!newSource)connectNeighbor = null;
                }
                currentLength = 0;
            }
            //鄰居為一般點
            else {
                connectNeighbor.hasCouculate = true;
                //先決定鄰居，並移除其他鄰居
                //Debug.Log("一般點    有 " + connectNeighbor.neighbor.Count + " 個鄰居");
                SpreadNode neighbor = null;

                if (connectNeighbor.neighbor.Count > 0)
                {
                    //Debug.Log("下個點為 " + connectNeighbor.neighbor[0].pos + "  為合併點" + connectNeighbor.neighbor[0].beenMerged);
                    SpreadNode temp = connectNeighbor.neighbor[0];
                    connectNeighbor.neighbor[0].neighbor.Remove(connectNeighbor);
                    connectNeighbor.neighbor.RemoveAt(0);
                    //if (connectNeighbor.neighbor.Count > 0) Debug.Log("剩鄰居 " + connectNeighbor.neighbor[0].pos);
                    connectNeighbor = temp; ;
                    
                }
                //沒有其他鄰居點
                else
                {
                    PatrolGraphNode nextNode;

                    Vector3 sourcePos = pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y);
                    Vector3 connectPos = pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y);
                    Vector3 center = 0.5f * ( sourcePos + connectPos);
                    Vector3 halfExtent = new Vector3((center - sourcePos).magnitude, 1.0f, leastNarrow);
                    Collider[] hits = Physics.OverlapBox(center, halfExtent, Quaternion.LookRotation(Vector3.forward)* Quaternion.Euler(0,Vector3.Angle(Vector3.left, (connectPos - sourcePos)),0), 1 << LayerMask.NameToLayer("Obstacle"));
                    //Physics.Linecast(pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y), pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y), 1 << LayerMask.NameToLayer("Obstacle"))
                    Debug.Log(" hit wall  ：" + (hits == null ? false : (hits.Length > 0)));
                    if (lastTurnNode != null &&
                    ( (hits == null?false:(hits.Length > 0))||
                    (Vector2.Angle(lastDir, connectNeighbor.pos - lastTurnNode.pos) > maxConnectAngle && (connectLength - lastLength) > leastTurnDstNum)))
                    {
                        sourcePos = pathFindGrid.GetNodePos(lastTurnNode.pos.x, lastTurnNode.pos.y);
                        hits = Physics.OverlapBox(center, halfExtent, Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, Vector3.Angle(Vector3.left, (connectPos - sourcePos)), 0), 1 << LayerMask.NameToLayer("Obstacle"));
                        if (hits == null ? false : (hits.Length > 0))
                        {
                            lastTurnNode = detectNode;
                            lastLength += currentLength;
                            connectLength -= currentLength;
                        }
                        if (!confirmGraphNodeDic.ContainsKey(lastTurnNode.pos))
                        {
                            Debug.Log("尚未有該轉折點");
                            nextNode = new PatrolGraphNode(lastTurnNode.pos.x, lastTurnNode.pos.y);
                            nextNode.turnNode = true;
                            nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                            ConfirmGraph.Add(nextNode);
                            confirmGraphNodeDic.Add(lastTurnNode.pos, nextNode);
                        }
                        //已有
                        else
                        {
                            Debug.Log("已有該轉折點");
                            nextNode = confirmGraphNodeDic[lastTurnNode.pos];
                        }
                        float dst = (confirmGraphNodeDic[sourceNode.pos].pos - nextNode.pos).magnitude;
                        //將來源點加進上個轉折點的連接點裡
                        nextNode.besideNodes.Add(confirmGraphNodeDic[sourceNode.pos],dst);
                        //將上個轉折點加進來原點的連接點裡
                        confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(nextNode,dst);
                        Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + dst);
                        connectLength -= lastLength;

                        lastDir = new Vector2Int(connectNeighbor.pos.x - lastTurnNode.pos.x, connectNeighbor.pos.y - lastTurnNode.pos.y);
                        sourceNode = lastTurnNode;
                    }

                    //確認為末端點，加入
                    if (connectNeighbor.endNode)
                    {
                        //Debug.Log(" 末端點");
                        if (!confirmGraphNodeDic.ContainsKey(connectNeighbor.pos))
                        {
                            nextNode = new PatrolGraphNode(connectNeighbor.pos.x, connectNeighbor.pos.y);
                            nextNode.endNode = true;
                            nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                            ConfirmGraph.Add(nextNode);
                            confirmGraphNodeDic.Add(connectNeighbor.pos, nextNode);
                        }
                        //已有
                        else
                        {
                            nextNode = confirmGraphNodeDic[connectNeighbor.pos];
                        }

                        float dst = (confirmGraphNodeDic[sourceNode.pos].pos - nextNode.pos).magnitude;
                        //將來源點加進鄰居的連接點裡
                        nextNode.besideNodes.Add(confirmGraphNodeDic[sourceNode.pos],dst);
                        //將鄰居點加進自己的連接點裡
                        confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(nextNode,dst);
                        Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + dst);
                        connectLength = 0;
                        currentLength = 0;
                        lastLength = 0;
                    }
                    lastTurnNode = null;

                    //從清單中選，確認清單中的點有鄰居
                    bool newSource = false;
                    while (waitNodes.Count > 0)
                    {
                       // Debug.Log("清單有  " + waitNodes[0].pos + "  有鄰居個數" + waitNodes[0].neighbor.Count);
                        sourceNode = waitNodes[0];
                        if (sourceNode.neighbor.Count > 0)
                        {
                            //Debug.Log("來原點 " + sourceNode.pos + "   新分支 " + sourceNode.neighbor[0].pos);
                            connectNeighbor = sourceNode.neighbor[0];
                            sourceNode.neighbor.RemoveAt(0);
                            connectNeighbor.neighbor.Remove(sourceNode);
                            if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                            newSource = true;
                            break;
                        }
                        else waitNodes.RemoveAt(0);
                    }
                    if (!newSource) connectNeighbor = null;
                }
            }
        }

        runConnectEnd = true;
        StartCoroutine(CreatePath());
        Debug.Log("跑完連接~~~~~");
        
        //for (int i = 0; i < ConfirmGraph.Count; i++) {
        //    int turnNum = 0;
        //    int endNum = 0;
        //    int turnX = 0, turnY = 0;
        //    int endX = 0, endY = 0;
        //    Vector3 turnVec = new Vector3(0, 0, 0);
        //    Vector3 endVec = new Vector3(0, 0, 0);
        //    for (int j = 0; j < ConfirmGraph[i].besideNodes.Count; j++) {
        //        if (ConfirmGraph[i].besideNodes[j].node.turnNode) {
        //            turnX += ConfirmGraph[i].besideNodes[j].node.x;
        //            turnY += ConfirmGraph[i].besideNodes[j].node.y;
        //            turnNum++;
        //        }
        //        else if(ConfirmGraph[i].besideNodes[j].node.endNode) {
        //            endX += ConfirmGraph[i].besideNodes[j].node.x;
        //            endY += ConfirmGraph[i].besideNodes[j].node.y;
        //            endNum++;
        //        }
        //    }
        //    if (turnNum == 2) {
        //        turnX = Mathf.RoundToInt(turnX * 0.5f);
        //        turnY = Mathf.RoundToInt(turnY * 0.5f);
        //        CheckNearObstacle()
        //        pathFindGrid.GetNodePos(turnX, turnY);
        //    }
        //    if (endNum == 2) {
        //        endX = Mathf.RoundToInt(endX * 0.5f);
        //        endY = Mathf.RoundToInt(endY * 0.5f);

        //    }
        //}

    }

    IEnumerator CreatePath() {
        Debug.Log("開始創建路線 ~~~~~~~~~");
        bool cycle = true;
        int cycleFail = 0;
        //連接出maxPathNum數量之路線
        for (int pathNum = 0; pathNum < maxPathNum;)
        {
            Debug.Log("第 " + (pathNum+1) + "  條路線");
            yield return null;
            float currentLength = 0;
            int repeatNum = 0;
            //繞圈和來回有不同做法
            //繞圈，分兩條路線並接回來
            if (cycle)
            {
                List<int> couculateID = new List<int>();
                List<List<PatrolGraphNode>> hasCouculateBeside = new List<List<PatrolGraphNode>>();//紀錄目前的節點那些連接點算過了
                List<PatrolGraphNode> uselessNode = new List<PatrolGraphNode>();  //紀錄封閉型不會用的點
                List<PatrolGraphNode> patrolGraph = new List<PatrolGraphNode>();  //紀錄存的點
                List<Vector3> patrolPoint = new List<Vector3>();  //記錄存的點位置，產生path
                PatrolGraphNode currentNode = null;

                ////路線開頭點不能為用過，且可連點大於1
                //int id = Random.Range(0, ConfirmGraph.Count);
                //while (ConfirmGraph[id].detectNum > 0 || ConfirmGraph[id].besideNodes.Count <= 1)
                //{
                //    uselessNode.Add(ConfirmGraph[id]);
                //    id = Random.Range(0, ConfirmGraph.Count);
                //}
                ////將第一點加進路線中
                //couculateID.Add(id);
                //ConfirmGraph[id].detectNum++;
                //patrolGraph.Add(ConfirmGraph[id]);
                //patrolPoint.Add(ConfirmGraph[id].pos);
                //ConfirmGraph[id].detectNum++;
                //hasCouculateNum.Add(1);

                //foreach (KeyValuePair<PatrolManager.PatrolGraphNode, float> item in patrolGraph[0].besideNodes)
                //{
                //    if (item.Key.besideNodes.Count > 1 && (currentLength + item.Value) < maxPatrolLength)
                //    {
                //        currentNode = item.Key;
                //        currentLength += item.Value;
                //        break;
                //    }
                //    else
                //    {
                //        if (!uselessNode.Contains(ConfirmGraph[id])) uselessNode.Add(ConfirmGraph[id]);
                //    }
                //}

                bool add = true;
                while (add)
                {
                    yield return null;

                    //路線第一點
                    if (currentNode == null)
                    {
                        Debug.Log("路線第一點 ");
                        int id = Random.Range(0, ConfirmGraph.Count);

                        //路線開頭點不能為用過，且可連點大於1，且沒在無用清單
                        while (ConfirmGraph[id].detectNum > 0 || ConfirmGraph[id].besideNodes.Count <= 1 || uselessNode.Contains(ConfirmGraph[id]))
                        {
                            Debug.Log("路線開頭點為用過，且可連點小於1，且在無用清單 ");
                            yield return null;
                            if (!uselessNode.Contains(ConfirmGraph[id])) uselessNode.Add(ConfirmGraph[id]);
                            id = Random.Range(0, ConfirmGraph.Count);
                        }
                        //將第一點加進路線中
                        couculateID.Add(id);
                        patrolGraph.Add(ConfirmGraph[id]);
                        patrolPoint.Add(ConfirmGraph[id].pos);
                        currentNode = ConfirmGraph[id];
                        //hasCouculateBeside.Add(new Dictionary<PatrolGraphNode, bool>() new bool[currentNode.besideNodes.Count]);
                        hasCouculateBeside.Add(new List<PatrolGraphNode>());
                        Debug.Log("路線第一點 加入清單   " + currentNode.pos);
                    }
                    else
                    {
                        Debug.Log("路線 第 " + patrolGraph.Count + " 個：" + currentNode.pos);
                        int connectEndID = -1;
                        PatrolGraphNode node = null;

                        //遍歷每個連接點
                        foreach (KeyValuePair<PatrolManager.PatrolGraphNode, float> item in currentNode.besideNodes)
                        {
                           
                            //先存，等到遍歷完，確認有沒有連接點可以接成封閉路線
                            //node = item.Key;
                            
                            Debug.Log("遍歷 第" + (hasCouculateBeside.Count + 1) + " 個連接點：" + item.Key.pos);

                            if (hasCouculateBeside[hasCouculateBeside.Count - 1].Count >= currentNode.besideNodes.Count)
                            {
                                Debug.Log("該連接點已經全算過算過分支 換下一個");
                                break;
                            }
                            //如果這次比記錄過的值小，代表已經算過
                            if (hasCouculateBeside[hasCouculateBeside.Count - 1].Contains(item.Key)) //hasCouculateBeside[hasCouculateBeside.Count - 1][count - 1]
                            {
                                Debug.Log("該連接點已經算過 換下一個");
                                continue;
                            } 

                            //如果連接點鄰居不超過1，繞圈的不會需要末端點，記錄進沒用清單
                            if (item.Key.besideNodes.Count <= 1)
                            {
                                Debug.Log(item.Key.pos + " 該連接點鄰居不超過1， 記錄進沒用清單  換下一個");
                                //hasCouculateBeside[hasCouculateBeside.Count - 1][count - 1] = true;
                                hasCouculateBeside[hasCouculateBeside.Count - 1].Add(item.Key);
                                uselessNode.Add(item.Key);
                                continue;
                            }

                            if (!((currentLength + item.Value) < maxPatrolLength)) {
                                Debug.Log(currentLength + item.Value + " 距離超過  ");
                            }
                            if (!(item.Key.detectNum <= 0 ? true : repeatNum < patrolRepeatNum - 1)) {
                                Debug.Log("重複點數量超過 ");
                            }
                            if (uselessNode.Contains(item.Key)) {
                                Debug.Log("在無用清單 ");
                            }
                            if (!((patrolGraph.Count < 2) ? true : !item.Key.Equals(patrolGraph[patrolGraph.Count - 2]))) {
                                Debug.Log("是上一個點 ");
                            }

                            //該連接點距離不能超過最大距離，且如果已經是其他路徑的點，需要計算有沒有超過最大重複點數量，且連接點不能在沒用清單裡，且連接點不能是上一個節點
                            if ((currentLength + item.Value) < maxPatrolLength && (item.Key.detectNum <= 0 ? true : repeatNum < 1) &&
                                !uselessNode.Contains(item.Key) && ((patrolGraph.Count < 2) ? true : !item.Key.Equals(patrolGraph[patrolGraph.Count - 2])) )
                            {

                                //接回已有的點
                                if (patrolGraph.Contains(item.Key))
                                {
                                    Debug.Log(" 接回已有的點");
                                    //檢查接回去的點之間的距離
                                    int startID = -1;
                                    float length = item.Value;
                                    for (int i = patrolGraph.Count - 1; i >= 1; i--)
                                    {
                                        //查目前的點在已有點的第幾個
                                        if (!patrolGraph[i].Equals(item.Key))
                                        {
                                            Debug.Log(" 判斷是不是接回已有的點  為第 " + i + "個 " + patrolGraph[i - 1].pos);
                                            length += patrolGraph[i].besideNodes[patrolGraph[i - 1]];
                                            startID = i - 1;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    if (length >= minPatrolLength)
                                    {
                                        Debug.Log(" 路線大於最小值 結束");
                                        connectEndID = startID;
                                        node = item.Key;
                                        break;
                                    }
                                    else
                                    {
                                        //連接距離不夠長，當作無用分支
                                        Debug.Log(length + "  連接距離不夠長，當作無用分支");
                                        //node = null;
                                        int lastID = hasCouculateBeside.Count - 1;
                                        //hasCouculateBeside[lastID][count - 1] = true;
                                        hasCouculateBeside[lastID].Add(item.Key);
                                    }
                                }
                                else {
                                    node = item.Key;
                                    Debug.Log("一般點");
                                } 
                            }
                            else
                            {
                                Debug.Log(" 不需要的點 ");
                                //hasCouculateBeside[hasCouculateBeside.Count - 1][count - 1] = true;
                                hasCouculateBeside[hasCouculateBeside.Count - 1].Add(item.Key);
                            }
                        }

                        //連接點都不符合
                        if (node == null)
                        {
                            int lastID = hasCouculateBeside.Count - 1;

                            //代表沒有接到下一點，移除本身，退回上個點
                            if (lastID - 1 >= 0)
                            {
                                Debug.Log(" 沒有接到下一點，移除本身，退回上個點  ");
                                currentLength -= currentNode.besideNodes[patrolGraph[lastID - 1]];
                                hasCouculateBeside.RemoveAt(lastID);
                                patrolGraph.RemoveAt(lastID);
                                patrolPoint.RemoveAt(lastID);
                                uselessNode.Add(currentNode);
                                Debug.Log(currentNode.pos + " 加進無用清單  ");

                                currentNode = patrolGraph[lastID - 1];
                                if (currentNode.detectNum > 0) repeatNum=1;
                                else repeatNum = 0;
                                Debug.Log(" 退回上個點  " + currentNode.pos);
                            }
                            else {
                                //如果退到該分支最後，重新選起始點
                                Debug.Log(" 退到底，開新分支  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ ~~~~~~~第 " + cycleFail + " 次");
                                currentNode = null;
                                couculateID.Clear();
                                hasCouculateBeside.Clear();
                                uselessNode.Clear();
                                patrolGraph.Clear();
                                patrolPoint.Clear();
                                cycleFail++;
                                if (cycleFail > patrolCycleFailNum) {
                                    cycle = false;
                                    break;
                                }
                            }
                            
                        }
                        //從接下去的連接點繼續判斷
                        else
                        {
                            Debug.Log(" 從接下去的連接點繼續判斷  " + node.pos);
                            //hasCouculateBeside[hasCouculateBeside.Count - 1][count - 1] = true;
                            hasCouculateBeside[hasCouculateBeside.Count - 1].Add(node);
                            if (node.detectNum > 0) repeatNum++;
                            else repeatNum = 0;
                            currentLength += currentNode.besideNodes[node];
                            currentNode = node;
                            patrolGraph.Add(currentNode);
                            patrolPoint.Add(currentNode.pos);
                            hasCouculateBeside.Add(new List<PatrolGraphNode>());
                            hasCouculateBeside[hasCouculateBeside.Count - 1].Add(patrolGraph[patrolGraph.Count-2]);
                            if (connectEndID >= 0)
                            {
                                //連接路線點
                                Debug.Log("完成路線 ");
                                for (int i = 0; i < connectEndID; i++)
                                {
                                    Debug.Log("移除前面  " + i);
                                    patrolGraph.RemoveAt(0);
                                    patrolPoint.RemoveAt(0);
                                }
                                PatrolPath path = new PatrolPath(true, patrolPoint, turnDist);
                                for (int i = 0; i < patrolGraph.Count-1; i++)
                                {
                                    Debug.Log("路線點 " + patrolGraph[i].pos +"  &  " + patrolPoint[i]);
                                    patrolGraph[i].detectNum++;
                                    patrolGraph[i].patrolPath = path;
                                }
                                patrolPathes.Add(path);
                                currentNode = null;
                                cycleFail = 0;
                                pathNum++;
                                break;
                            }
                        }
                    }
                }

            }
            //來回
            else
            {
                int lastNodeID = 0;
                List<int> couculateID = new List<int>();
                List<List<PatrolGraphNode>> hasCouculateBeside = new List<List<PatrolGraphNode>>();//紀錄目前的節點那些連接點算過了
                List<PatrolGraphNode> patrolGraph = new List<PatrolGraphNode>();  //紀錄存的點
                List<Vector3> patrolPoint = new List<Vector3>();  //記錄存的點位置，產生path
                PatrolGraphNode currentNode = null;
                PatrolGraphNode firstNode = null;

                bool add = true;
                while (add)
                {
                    yield return null;

                    //路線第一點
                    if (currentNode == null)
                    {
                        Debug.Log("路線第一點 ");
                        int id = Random.Range(0, ConfirmGraph.Count);

                        //路線開頭點不能為用過，且可連點大於1，且沒在無用清單
                        while (ConfirmGraph[id].detectNum > 0)
                        {
                            Debug.Log("路線開頭點為用過 ");
                            yield return null;
                            id = Random.Range(0, ConfirmGraph.Count);
                        }
                        //將第一點加進路線中
                        couculateID.Add(id);
                        patrolGraph.Add(ConfirmGraph[id]);
                        patrolPoint.Add(ConfirmGraph[id].pos);
                        currentNode = ConfirmGraph[id];
                        firstNode = currentNode;
                        hasCouculateBeside.Add(new List<PatrolGraphNode>());
                        Debug.Log("路線第一點 加入清單   " + currentNode.pos);
                    }
                    else
                    {
                        Debug.Log("路線 第 " + patrolGraph.Count + " 個：" + currentNode.pos);
                        PatrolGraphNode node = null;
                        int lastdetectNum = 10;
                        //遍歷每個連接點
                        foreach (KeyValuePair<PatrolManager.PatrolGraphNode, float> item in currentNode.besideNodes)
                        {

                            //如果這次比記錄過的值小，代表已經算過
                            //if (count < hasCouculateNum[hasCouculateNum.Count - 1]) continue;
                            int id = (firstNode != null ? hasCouculateBeside.Count - 1 : 0);
                            Debug.Log("遍歷 第" + (hasCouculateBeside[id].Count+1) + " 個連接點：" + item.Key.pos);

                            if (hasCouculateBeside[id].Count >= currentNode.besideNodes.Count) {
                                Debug.Log("該連接點已經全算過算過分支 換下一個");
                                break;
                            }
                            if (hasCouculateBeside[id].Contains(item.Key))
                            {
                                Debug.Log("該連接點已經算過 換下一個");
                                continue;
                            }

                            //如果連接點鄰居不超過1，到達一末端，改從頭連接  繞圈的不會需要末端點，記錄進沒用清單
                            //if (item.Key.besideNodes.Count <= 1)
                            //{
                            //    Debug.Log(item.Key.pos + " 該連接點鄰居不超過1， 記錄進沒用清單  換下一個");
                            //    hasCouculateBeside[hasCouculateBeside.Count - 1][count - 1] = true;
                            //    continue;
                            //}

                            if (!((currentLength + item.Value) < maxPatrolLength))
                            {
                                Debug.Log(currentLength + item.Value + " 距離超過  ");
                            }
                            if (!(item.Key.detectNum <= 0 ? true : repeatNum < 1))
                            {
                                Debug.Log("重複點數量超過 ");
                            }
                            if (!((patrolGraph.Count < 2) ? true : !item.Key.Equals(patrolGraph[patrolGraph.Count - 2])))
                            {
                                Debug.Log("是上一個點 ");
                            }

                            //該連接點距離不能超過最大距離，且如果已經是其他路徑的點，需要計算有沒有超過最大重複點數量，且連接點不能在沒用清單裡，且連接點不能是上一個節點
                            if ((currentLength + item.Value) < maxPatrolLength && (item.Key.detectNum <= 0 ? true : repeatNum < 1) && !patrolGraph.Contains(item.Key)
                                && ((patrolGraph.Count < 2) ? true : !item.Key.Equals(patrolGraph[patrolGraph.Count - 2])))
                            {
                                if (item.Key.detectNum <= lastdetectNum) {
                                    node = item.Key;
                                    lastdetectNum = item.Key.detectNum;
                                } 
                            }
                            else
                            {
                                Debug.Log(" 不需要的點 ");
                                //hasCouculateBeside[id]++;
                                hasCouculateBeside[id].Add(item.Key);
                            }
                        }

                        //連接點都不符合
                        if (node == null)
                        {
                           
                            if (currentLength > minPatrolLength) {
                                //連接路線點
                                Debug.Log("完成路線 ");
                                PatrolPath path = new PatrolPath(true, patrolPoint, turnDist);
                                for (int i = 0; i < patrolGraph.Count; i++)
                                {
                                    Debug.Log("路線點 " + patrolGraph[i].pos + "  &  " + patrolPoint[i]);
                                    patrolGraph[i].detectNum++;
                                    patrolGraph[i].patrolPath = path;
                                }
                                Debug.Log("路線點 " + patrolGraph[patrolGraph.Count - 1].pos + "  &  " + patrolPoint[patrolGraph.Count - 1]);
                                patrolPathes.Add(path);
                                currentNode = null;
                                pathNum++;
                                break;
                            }

                            int lastID = hasCouculateBeside.Count - 1;
                            if (firstNode != null)
                            {
                                if (hasCouculateBeside[0].Count < firstNode.besideNodes.Count)
                                {
                                    Debug.Log("該路線起始點有其他方向 改成從頭開始接  ");
                                    currentNode = firstNode;
                                    firstNode = null;
                                }
                                else
                                {
                                    //代表沒有接到下一點，移除本身，退回上個點
                                    if (lastID - 1 >= 0)
                                    {
                                        Debug.Log(" 沒有接到下一點，移除本身，退回上個點  ");
                                        currentLength -= currentNode.besideNodes[patrolGraph[lastID - 1]];
                                        hasCouculateBeside.RemoveAt(lastID);
                                        patrolGraph.RemoveAt(lastID);
                                        patrolPoint.RemoveAt(lastID);

                                        currentNode = patrolGraph[lastID - 1];
                                        if (currentNode.detectNum > 0) repeatNum = 1;
                                        else repeatNum = 0;
                                        Debug.Log(" 退回上個點  " + currentNode.pos);
                                    }
                                    else
                                    {
                                        //如果退到該分支最後，重新選起始點
                                        Debug.Log(" 退到底，開新分支  ");
                                        currentNode = null;
                                        couculateID.Clear();
                                        hasCouculateBeside.Clear();
                                        patrolGraph.Clear();
                                        patrolPoint.Clear();
                                    }
                                }
                            }
                            else
                            {
                                //代表沒有接到下一點，移除本身，退回上個點
                                if (lastID - 1 >= 0)
                                {
                                    Debug.Log("起始點分支線， 沒有接到下一點，移除本身，退回上個點  ");
                                    currentLength -= currentNode.besideNodes[patrolGraph[1]];
                                    hasCouculateBeside.RemoveAt(0);
                                    patrolGraph.RemoveAt(0);
                                    patrolPoint.RemoveAt(0);

                                    currentNode = patrolGraph[0];
                                    if (currentNode.detectNum > 0) repeatNum = 1;
                                    else repeatNum = 0;
                                    Debug.Log("起始點分支線， 退回上個點  " + currentNode.pos);
                                }
                                else
                                {
                                    //如果退到該分支最後，重新選起始點
                                    Debug.Log("起始點分支線， 退到底，開新分支  ");
                                    currentNode = null;
                                    couculateID.Clear();
                                    hasCouculateBeside.Clear();
                                    patrolGraph.Clear();
                                    patrolPoint.Clear();
                                }
                            }

                        }
                        //從接下去的連接點繼續判斷
                        else
                        {
                            Debug.Log(" 從接下去的連接點繼續判斷  " + node.pos);
                            if (node.detectNum > 0) repeatNum++;
                            else repeatNum = 0;
                            currentLength += currentNode.besideNodes[node];
                            currentNode = node;
                            if (firstNode != null)
                            {
                                //hasCouculateBeside[hasCouculateBeside.Count - 1]++;
                                hasCouculateBeside[hasCouculateBeside.Count - 1].Add(currentNode);
                                patrolGraph.Add(currentNode);
                                patrolPoint.Add(currentNode.pos);
                                hasCouculateBeside.Add(new List<PatrolGraphNode>());
                                hasCouculateBeside[hasCouculateBeside.Count - 1].Add(patrolGraph[patrolGraph.Count - 2]);
                            }
                            else {
                                //hasCouculateBeside[0]++;
                                hasCouculateBeside[0].Add(currentNode);
                                patrolGraph.Insert(0, currentNode);
                                patrolPoint.Insert(0, currentNode.pos);
                                hasCouculateBeside.Insert(0, new List<PatrolGraphNode>());
                                hasCouculateBeside[0].Add(patrolGraph[1]);
                            }

                            Debug.Log(" 路徑長度  " + currentLength);
                            if (currentLength > minPatrolLength && Random.Range(.0f, 1.0f) > 0.3f)
                            {
                                //連接路線點
                                Debug.Log("完成路線 ");
                                PatrolPath path = new PatrolPath(false, patrolPoint, turnDist);
                                for (int i = 0; i < patrolGraph.Count; i++)
                                {
                                    Debug.Log("路線點 " + patrolGraph[i].pos + "  &  " + patrolPoint[i]);
                                    patrolGraph[i].detectNum++;
                                    patrolGraph[i].patrolPath = path;
                                }
                                Debug.Log("路線點 " + patrolGraph[patrolGraph.Count - 1].pos + "  &  " + patrolPoint[patrolGraph.Count - 1]);
                                patrolPathes.Add(path);
                                currentNode = null;
                                pathNum++;
                                break;
                            }
                        }
                    }
                }


                //路線開頭點不能為用過
                //int id = Random.Range(0, ConfirmGraph.Count);
                //while (ConfirmGraph[id].detectNum > 0)
                //{
                //    id = Random.Range(0, ConfirmGraph.Count);
                //}

                //PatrolGraphNode node = ConfirmGraph[id];
                //List<PatrolGraphNode> patrolGraph = new List<PatrolGraphNode>();
                //List<Vector3> patrolPoint = new List<Vector3>();
                //patrolGraph.Add(node);
                //node.detectNum++;
                //patrolPoint.Add(node.pos);
                //int giveUpNum = 0;
                //bool add = true;
                //while (add)
                //{
                //    PatrolGraphNode.ConnectGraphNode connectNode = node.besideNodes[Random.Range(0, node.besideNodes.Count)];
                //    int newLength = currentLength + connectNode.length;

                //    //新的點不能在路線裡，新點的使用數不能超過3，長度不超過最大長度，重複點數量不超過3
                //    if (!patrolGraph.Contains(connectNode.node) && connectNode.node.detectNum <= 3 && newLength <= maxPatrolLength && (newLength < minPatrolLength && connectNode.node.besideNodes.Count > 1))
                //    {
                //        giveUpNum = 0;
                //        //新點是重複點，數量++
                //        if (connectNode.node.detectNum > 0) {
                //            repeatNum++;
                //            if (repeatNum > 3) {
                //                Debug.Log("因為重複點太多 來回 捨棄 ");
                //                for (int i = 0; i < patrolGraph.Count; i++)
                //                {
                //                    patrolGraph[i].detectNum--;
                //                }
                //                patrolGraph.Clear();
                //                patrolPoint.Clear();
                //                pathNum--;
                //                break;
                //            }
                //        } 

                //        currentLength = newLength;
                //        node = connectNode.node;
                //        patrolGraph.Add(node);
                //        patrolPoint.Add(node.pos);
                //        node.detectNum++;

                //        //超過最低長度且0.1機率，不再加新的點，行成路線
                //        if (newLength >= minPatrolLength && Random.Range(0, 100) < 10)
                //        {
                //            add = false;
                //            Path path = new Path(patrolPoint, turnDist);
                //            patrolPathes.Add(path);
                //        }
                //    }
                //    else
                //    {
                //        //連續重複失敗10次，捨棄這條路線
                //        giveUpNum++;
                //        if (giveUpNum >= 10)
                //        {
                //            Debug.Log("來回 捨棄 ");
                //            for (int i = 0; i < patrolGraph.Count; i++)
                //            {
                //                patrolGraph[i].detectNum--;
                //            }
                //            patrolGraph.Clear();
                //            patrolPoint.Clear();
                //            pathNum--;
                //            break;
                //        }
                //    }

                //}
            }
        }
    }



    //int CheckTurnCrossNode(SpreadNode node) {
    //    // 0:一般點 // 1:轉折點 // 2:交錯點
    //    int count = 0;
    //    int couculateNum = 0;
    //    int xNum = 0, yNum = 0;
    //    bool hasTurn = false;
    //    int crossWeight = 0;
    //    for (int cy = 1; cy >= -1; cy--)
    //    {
    //        for (int cx = -1; cx <= 1; cx++)
    //        {
    //            Vector2Int detectPos = new Vector2Int(node.pos.x + cx, node.pos.y + cy);

    //            if ((cy == 0 && cx == 0) || detectPos.x < 0 || detectPos.x >= gridX || detectPos.y < 0 || detectPos.y >= gridY || crossWeight >= 10) continue; // || hasCross
    //            Debug.Log(node.pos + " detect  " + detectPos.x + "," + detectPos.y + "  walkable" + spreadGrid[detectPos.x, detectPos.y].walkable + "   choosen" + spreadGrid[detectPos.x, detectPos.y].choosen);
    //            //hasCross = (hasCross || (confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].crossNode));
    //            hasTurn = (hasTurn || (confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].turnNode));
    //            if ((confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].weight > crossWeight)) crossWeight = confirmGraphNodeDic[detectPos].weight;
    //            if (spreadGrid[detectPos.x, detectPos.y].walkable && spreadGrid[detectPos.x, detectPos.y].choosen &&
    //                Mathf.InverseLerp(0, maxChoosenWeight, spreadGrid[detectPos.x, detectPos.y].choosenWeight) >= choosenRate)
    //            {
    //                count++;
    //                xNum += cx;
    //                yNum += cy;
    //                couculateNum += Mathf.Abs(cx);
    //                couculateNum += Mathf.Abs(cy);
    //                Debug.Log(choosenNode[i].pos + "couculate   " + detectPos.x + "," + detectPos.y + " num " + xNum + "," + yNum);
    //            }
    //        }
    //    }
    //    //if (hasCross) continue;
    //    if (count == 1)
    //    {
    //        //PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
    //        //ConfirmGraph.Add(node);
    //        //confirmGraphNodeDic.Add(choosenNode[i].pos, node);
    //    }
    //    else if (xNum != 0 || yNum != 0)
    //    {
    //        PatrolGraphNode grapgNode = new PatrolGraphNode(node.pos.x, choosenNode[i].pos.y);
    //        //x或y其一不抵銷，斜邊數量多列為交錯點，判斷為交錯點的權重比較小
    //        if (couculateNum > 4)
    //        { //&& couculateNum % 2 == 0
    //            for (int cy = 1; cy >= -1; cy--)
    //            {
    //                for (int cx = -1; cx <= 1; cx++)
    //                {
    //                    if (cx == 0 && cy == 0) continue;
    //                    Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
    //                    if (confirmGraphNodeDic.ContainsKey(pos) && confirmGraphNodeDic[pos].weight < 10) //!confirmGraphNodeDic[pos].crossNode
    //                    {
    //                        ConfirmGraph.Remove(confirmGraphNodeDic[pos]);
    //                        confirmGraphNodeDic.Remove(pos);
    //                    }
    //                }
    //            }
    //            node.crossNode = true;
    //            node.weight = 7;
    //            ConfirmGraph.Add(node);
    //            confirmGraphNodeDic.Add(choosenNode[i].pos, node);

    //        }

    //        else if (!hasTurn && crossWeight < 5)
    //        {
    //            //一般列為轉折點
    //            node.turnNode = true;
    //            node.weight = 5;
    //            ConfirmGraph.Add(node);
    //            confirmGraphNodeDic.Add(choosenNode[i].pos, node);
    //        }

    //    }
    //    else
    //    {

    //        //x或y都抵銷且周圍數量不為偶數，列為交錯點，不然就不列入
    //        if (count == 4 || (count > 0 && count % 2 != 0))
    //        {
    //            PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
    //            node.crossNode = true;
    //            node.weight = 10;
    //            ConfirmGraph.Add(node);
    //            confirmGraphNodeDic.Add(choosenNode[i].pos, node);

    //            for (int cy = 1; cy >= -1; cy--)
    //            {
    //                for (int cx = -1; cx <= 1; cx++)
    //                {
    //                    if (cx == 0 && cy == 0) continue;
    //                    Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
    //                    if (confirmGraphNodeDic.ContainsKey(pos))  //&& !confirmGraphNodeDic[pos].crossNode
    //                    {
    //                        //比上方判斷交錯點的權重大，周圍權重不大於10就刪掉
    //                        if (confirmGraphNodeDic[pos].weight < 10)
    //                        {
    //                            confirmGraphNodeDic[pos].crossNode = false;
    //                            confirmGraphNodeDic[pos].weight = 0;
    //                        }
    //                        ConfirmGraph.Remove(confirmGraphNodeDic[pos]);
    //                        confirmGraphNodeDic.Remove(pos);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

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
    }
    public void SetSpreadStop(int x, int y) {
        spreadGrid[x, y].stop = true;
    }
}

/*
 void CouculateGraphCross() {
        List<PatrolGraphNode> graphNodes = new List<PatrolGraphNode>();
        Dictionary<Vector2Int, PatrolGraphNode> graphNodeDic = new Dictionary<Vector2Int, PatrolGraphNode>();
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            int count = 0;
            int couculateNum = 0;
            int xNum = 0, yNum = 0;
            //bool hasCross = false;
            //bool hasTurn = false;
            int crossWeight = 0;
            if (Mathf.InverseLerp(0, maxChoosenWeight, choosenNode[i].choosenWeight) < choosenRate) continue;
            for (int cy = 1; cy >= -1; cy--)
            {
                for (int cx = -1; cx <= 1; cx++)
                {
                    Vector2Int detectPos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);

                    if ((cy == 0 && cx == 0) || detectPos.x < 0 || detectPos.x >= gridX || detectPos.y < 0 || detectPos.y >= gridY || crossWeight >= 10) continue; // || hasCross
                    Debug.Log(choosenNode[i].pos + " detect  " + detectPos.x + "," + detectPos.y + "  walkable" + spreadGrid[detectPos.x, detectPos.y].walkable + "   choosen" + spreadGrid[detectPos.x, detectPos.y].choosen);
                    //hasCross = (hasCross || (confirmGraphNodeDic.ContainsKey(detectPos) && confirmGraphNodeDic[detectPos].crossNode));
                    //hasTurn = (hasTurn || (graphNodeDic.ContainsKey(detectPos) && graphNodeDic[detectPos].turnNode));
                    if ((graphNodeDic.ContainsKey(detectPos) && graphNodeDic[detectPos].weight > crossWeight)) crossWeight = graphNodeDic[detectPos].weight;
                    if (spreadGrid[detectPos.x, detectPos.y].walkable && spreadGrid[detectPos.x, detectPos.y].choosen &&
                        Mathf.InverseLerp(0, maxChoosenWeight, spreadGrid[detectPos.x, detectPos.y].choosenWeight) >= choosenRate)
                    {
                        count++;
                        xNum += cx;
                        yNum += cy;
                        couculateNum += Mathf.Abs(cx);
                        couculateNum += Mathf.Abs(cy);
                        Debug.Log(choosenNode[i].pos + "couculate   " + detectPos.x + "," + detectPos.y + " num " + xNum + "," + yNum);
                    }
                }
            }
            if (crossWeight >= 10) continue;
            if (count == 1)
            {
            }
            else if (xNum != 0 || yNum != 0) // (Mathf.Abs(xNum) + Mathf.Abs(yNum)) == 1
            {
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //x或y其一不抵銷，斜邊數量多列為交錯點，判斷為交錯點的權重比較小
               
                if (count > 2)//couculateNum > 4 && couculateNum % 2 == 0
                {
                    //只剩一個方向，權重最小
                    if (count <= 3) //(Mathf.Abs(xNum) + Mathf.Abs(yNum)) == 1
                    {
                        int tempW = 0;
                        for (int cy = 1; cy >= -1; cy--)
                        {
                            for (int cx = -1; cx <= 1; cx++)
                            {
                                if (cx == 0 && cy == 0) continue;
                                Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                                if (graphNodeDic.ContainsKey(pos))
                                {
                                    if ((graphNodeDic[pos].weight > tempW)) tempW = graphNodeDic[pos].weight;
                                    if (graphNodeDic[pos].weight < 7) {
                                        graphNodes.Remove(graphNodeDic[pos]);
                                        graphNodeDic.Remove(pos);
                                        //choosenNodeDic[pos].turnNode = false;
                                        choosenNodeDic[pos].crossNode = false;
                                    }
                                }
                            }
                        }
                        if (tempW > 5) continue;
                        node.crossNode = true;
                        node.weight = 5;
                        graphNodes.Add(node);
                        graphNodeDic.Add(choosenNode[i].pos, node);
                        choosenNode[i].crossNode = true;
                    }
                    else
                    {
                        int tempW = 0;
                        for (int cy = 1; cy >= -1; cy--)
                        {
                            for (int cx = -1; cx <= 1; cx++)
                            {
                                if (cx == 0 && cy == 0) continue;
                                Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                                if (graphNodeDic.ContainsKey(pos))
                                {
                                    if ((graphNodeDic[pos].weight > tempW)) tempW = graphNodeDic[pos].weight;
                                    if (graphNodeDic[pos].weight < 10) {
                                        graphNodes.Remove(graphNodeDic[pos]);
                                        graphNodeDic.Remove(pos);
                                        //choosenNodeDic[pos].turnNode = false;
                                        choosenNodeDic[pos].crossNode = false;
                                    }

                                }
                            }
                        }
                        if (tempW > 7) continue;
                        node.crossNode = true;
                        node.weight = 7;
                        graphNodes.Add(node);
                        graphNodeDic.Add(choosenNode[i].pos, node);
                        choosenNode[i].crossNode = true;
                    }
                   
                }
                //else if (!hasTurn && crossWeight < 5)
                //{
                //    //一般列為轉折點
                //    node.turnNode = true;
                //    node.weight = 5;
                //    graphNodes.Add(node);
                //    graphNodeDic.Add(choosenNode[i].pos, node);
                //    choosenNode[i].turnNode = true;
                //}
            }
            else
            {
                //x或y都抵銷且周圍數量不為偶數，列為交錯點，不然就不列入
                if (count > 2) //count == 4 || (count > 0 && count % 2 != 0)
                {
                    PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                    node.crossNode = true;
                    node.weight = 10;
                    graphNodes.Add(node);
                    graphNodeDic.Add(choosenNode[i].pos, node);
                    choosenNode[i].crossNode = true;

                    for (int cy = 1; cy >= -1; cy--)
                    {
                        for (int cx = -1; cx <= 1; cx++)
                        {
                            if (cx == 0 && cy == 0) continue;
                            Vector2Int pos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);
                            if (graphNodeDic.ContainsKey(pos))  //&& !confirmGraphNodeDic[pos].crossNode
                            {
                                //比上方判斷交錯點的權重大，周圍權重不大於10就刪掉
                                if (graphNodeDic[pos].weight > 3)
                                {
                                    graphNodeDic[pos].crossNode = false;
                                    graphNodeDic[pos].weight = 0;
                                    choosenNodeDic[pos].crossNode= false;
                                }
                                //graphNodeDic[pos].turnNode = false;
                                graphNodes.Remove(graphNodeDic[pos]);
                                graphNodeDic.Remove(pos);
                                choosenNodeDic[pos].turnNode = false;
                            }
                        }
                    }
                }
            }
        }
    }
    void CouculateGraphTurn()
    {
        List<PatrolGraphNode> graphNodes = new List<PatrolGraphNode>();
        Dictionary<Vector2Int, PatrolGraphNode> graphNodeDic = new Dictionary<Vector2Int, PatrolGraphNode>();
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            int xNum = 0, yNum = 0;
            bool hasTurn = false;
            int count = 0;
            if (Mathf.InverseLerp(0, maxChoosenWeight, choosenNode[i].choosenWeight) < choosenRate) continue;
            for (int cy = 1; cy >= -1; cy--)
            {
                for (int cx = -1; cx <= 1; cx++)
                {
                    Vector2Int detectPos = new Vector2Int(choosenNode[i].pos.x + cx, choosenNode[i].pos.y + cy);

                    if ((cy == 0 && cx == 0) || detectPos.x < 0 || detectPos.x >= gridX || detectPos.y < 0 || detectPos.y >= gridY) continue;
                    Debug.Log(choosenNode[i].pos + " detect  " + detectPos.x + "," + detectPos.y + "  walkable" + spreadGrid[detectPos.x, detectPos.y].walkable + "   choosen" + spreadGrid[detectPos.x, detectPos.y].choosen);
                    hasTurn = (hasTurn || graphNodeDic.ContainsKey(detectPos) || spreadGrid[detectPos.x, detectPos.y].crossNode);
                    if (spreadGrid[detectPos.x, detectPos.y].walkable && spreadGrid[detectPos.x, detectPos.y].choosen) //&& Mathf.InverseLerp(0, maxChoosenWeight, spreadGrid[detectPos.x, detectPos.y].choosenWeight) >= choosenRate
                    {
                        count++;
                        xNum += cx;
                        yNum += cy;
                        Debug.Log(choosenNode[i].pos + "couculate   " + detectPos.x + "," + detectPos.y + " num " + xNum + "," + yNum);
                    }
                    
                }
            }
            if (count == 1)
            {
                //末端點
                choosenNode[i].endNode = true;
            }
            if (!hasTurn && (xNum != 0 || yNum != 0) && count > 1 && count < 3)
            {
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //一般列為轉折點
                node.turnNode = true;
                node.weight = 3;
                graphNodes.Add(node);
                graphNodeDic.Add(choosenNode[i].pos, node);
                choosenNode[i].turnNode = true;
            }
           
        }
    }
     */
