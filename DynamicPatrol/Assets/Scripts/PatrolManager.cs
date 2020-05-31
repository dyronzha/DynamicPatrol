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

    public int NotBorderWeight = 50;
    public int BorderAndNormalCornerWeight = 50;
    public int PerSpreadWeight = 10;
    public float sameAreaDisRate = 0.3f;
    [Range(0.0f, 1.0f)]
    public float choosenRate = .0f;
    public float closeDstNum = 0;
    public float connectTime = 0.1f;
    public float maxConnectAngle = 45.0f;
    public int minPatrolLength = 0;
    public int maxPatrolLength = 0;
    public int maxPathNum = 0;
    public int patrolCycleNum = 0;
    public float turnDist;

    [HideInInspector]
    public int maxChoosenWeight = 0;
    [HideInInspector]
    public bool runConnectEnd = false;

    PathFindGrid pathFindGrid;

    [HideInInspector]
    public List<Path> patrolPathes = new List<Path>();

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

        public struct ConnectGraphNode {
            public int length;
            public PatrolGraphNode node;
        }
        public List<ConnectGraphNode> besideNodes;
        
        public PatrolGraphNode(int _x, int _y) {
            x = _x;
            y = _y;
        }
    }

    public SpreadNode sourceNode = null;
    public SpreadNode connectNeighbor = null;
    public List<SpreadNode> waitNodes = new List<SpreadNode>();
    List<SpreadNode> endNodes = new List<SpreadNode>();

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
                currentNode.choosenWeight += PerSpreadWeight;
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
        if (Input.GetKeyDown(KeyCode.C)) {
            CouculateGraphCross();
            CouculateGraphTurn();
            StartCoroutine(ConactGraph());
        }
        if (Input.GetKeyDown(KeyCode.P)) {
            skipRun = true;
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
                node.fromArea = tiltSpread[i].fromArea;

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
                if (spreadGrid[currentNode.x, currentNode.y].choosen) //spreadGrid[currentNode.x, currentNode.y].stop || 
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
                            //spreadGrid[nextX, currentNode.y].stop = true;
                        }
                        //下一格是同名稱，且遇到方向是垂直的，通常是為了解決斜向擺放的方塊自己碰撞的問題
                        else if (spreadGrid[nextX, currentNode.y].fromArea.Contains(area.Name) && (spreadGrid[nextX, currentNode.y].dir.y != 0))
                        {

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
                            area.AddSpreadGridTilt(nextX, currentNode.y, new Vector2Int(currentNode.direction.x, 0), currentNode.choosenWeight + PerSpreadWeight);
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
                            //spreadGrid[currentNode.x, nextY].stop = true;
                        }
                        //下一格是同名稱，且遇到方向是水平的，通常是為了解決斜向擺放的方塊自己碰撞的問題
                        else if (spreadGrid[currentNode.x, nextY].fromArea.Contains(area.Name) && (spreadGrid[currentNode.x, nextY].dir.x != 0))
                        {

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
                            area.AddSpreadGridTilt(currentNode.x, nextY, new Vector2Int(0, currentNode.direction.y), currentNode.choosenWeight + PerSpreadWeight);

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
                            ) {
                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        area.spreadGrids.Remove(area.spreadGridNmae[j]);
                        area.spreadGridNmae.RemoveAt(j);
                    }
                    else
                    {
                        //前進方向互相撞到，存下來
                        if ((spreadGrid[nextX, nextY].dir.sqrMagnitude > 0.5f)) 
                        {
                            currentNode.choosenWeight += PerSpreadWeight;
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
                            if (extendTiltSpread) {

                                //有兩個方向，等於有三個斜邊接壤，直接設為選擇點
                                if (extendTiltSpreadVec2.sqrMagnitude > 1)
                                {
                                    currentNode.choosenWeight += PerSpreadWeight;
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
                                else {
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
                            else spreadGrid[nextX, nextY].dir = currentNode.direction;
                            currentNode.choosenWeight += PerSpreadWeight;
                        }

                        //if (!hasC[currentNode.x, currentNode.y])
                        //{
                        //    spreadGrid[currentNode.x, currentNode.y].current = false;
                        //}
                        spreadGrid[currentNode.x, currentNode.y].current = false;
                        hasC[nextX, nextY] = true;
                        currentNode.x = nextX;
                        currentNode.y = nextY;
                        //spreadGrid[nextX, nextY].dir = currentNode.direction;
                        spreadGrid[nextX, nextY].fromArea.Add(area.Name);
                        spreadGrid[nextX, nextY].current = true;

                        area.hasCouculateNode[nextX, nextY] = true;
                        area.couculateNodeDir[nextX, nextY] = currentNode.direction;
                    }
                }

            }
        }
        //if (skip)
        //{
        //    Debug.Log("計算末端點  " + choosenNode.Count);
        //    List<Vector2Int> couculatedNodes = new List<Vector2Int>();
        //    for (int i = choosenNode.Count - 1; i >= 0; i--)
        //    {

        //        SpreadNode node = choosenNode[i];
        //        SpreadNode endNode = null;

        //        //已經被算過是有碰撞的末端點，捨棄
        //        if (couculatedNodes.Contains(node.pos))
        //        {
        //            Debug.Log("計算過  " + node.pos + " 捨棄");
        //            node.choosen = false;
        //            choosenNode.Remove(choosenNodeDic[node.pos]);
        //            choosenNodeDic.Remove(node.pos);
        //            couculatedNodes.Remove(node.pos);
        //            continue;
        //        }

        //        Debug.Log("計算  " + choosenNode[i].pos + "  " + node.neighbor.Count);

        //        int count = 0;
        //        int xNum = 0;
        //        int yNum = 0;
        //        for (int j = node.neighbor.Count - 1; j >= 0; j--)
        //        {
        //            xNum += (node.neighbor[j].pos.x - node.pos.x);
        //            yNum += (node.neighbor[j].pos.y - node.pos.y);
        //            count++;
        //            Debug.Log("鄰居有  " + node.neighbor[j].pos);
        //        }
        //        if (count == 0)
        //        {
        //            choosenNode.RemoveAt(i);
        //            choosenNodeDic.Remove(node.pos);
        //        }
        //        else if (count == 1)
        //        {
        //            Debug.Log("個數1 末端 ");
        //            endNode = node;
        //        }
        //        //有多個鄰居的末端，方向不會抵銷
        //        else if (count == 2 && (Mathf.Abs(xNum) >= 2 || Mathf.Abs(yNum) >= 2))
        //        {
        //            Debug.Log("個數多 末端 ");
        //            endNode = node;
        //        }

        //        List<SpreadNode> waitNodes = new List<SpreadNode>();
        //        int breakNum = 0;
        //        while (endNode != null)
        //        {
        //            breakNum++;
        //            if (breakNum > 9999) break;

        //            if (endNode.neighbor.Count > 2)
        //            {
        //                for (int j = endNode.neighbor.Count - 1; j >= 0; j--)
        //                {
        //                    if (!couculatedNodes.Contains(endNode.neighbor[j].pos)) {
        //                        xNum += (endNode.neighbor[j].pos.x - endNode.pos.x);
        //                        yNum += (endNode.neighbor[j].pos.y - endNode.pos.y);
        //                    }
        //                    xNum += (endNode.neighbor[j].pos.x - endNode.pos.x);
        //                    yNum += (endNode.neighbor[j].pos.y - endNode.pos.y);
        //                    count++;
        //                    Debug.Log("鄰居有  " + endNode.neighbor[j].pos);
        //                    if (!waitNodes.Contains(endNode.neighbor[j]) && !couculatedNodes.Contains(endNode.neighbor[j].pos)) waitNodes.Add(endNode.neighbor[j]);
        //                }
        //                if (Mathf.Abs(xNum) >= 2 || Mathf.Abs(yNum) >= 2)
        //                {
        //                    couculatedNodes.Add(endNode.pos);
        //                }
        //                endNode = null;
        //            }
        //            else
        //            {
        //                Vector3 pos = pathFindGrid.GetNodePos(endNode.pos.x, endNode.pos.y);
        //                Vector3 VStart = pos + new Vector3(0, 0, closeDstNum);
        //                Vector3 VEnd = pos + new Vector3(0, 0, -closeDstNum);
        //                Vector3 HStart = pos + new Vector3(closeDstNum, 0, 0);
        //                Vector3 HEnd = pos + new Vector3(-closeDstNum, 0, 0);
        //                Debug.Log(endNode.pos + " :  " + VStart + " ---- " + VEnd + "  " + Physics.Linecast(VStart, VEnd, 1 << LayerMask.NameToLayer("Obstacle")));
        //                Debug.Log(endNode.pos + " :  " + HStart + " ---- " + HEnd + "  " + Physics.Linecast(VStart, VEnd, 1 << LayerMask.NameToLayer("Obstacle")));

        //                couculatedNodes.Add(endNode.pos);
        //                for (int w = 0; w < endNode.neighbor.Count; w++)
        //                {
        //                    if (!waitNodes.Contains(endNode.neighbor[w]) && !couculatedNodes.Contains(endNode.neighbor[w].pos)) waitNodes.Add(endNode.neighbor[w]);
        //                }
        //                if (waitNodes.Count > 0)
        //                {
        //                    endNode = waitNodes[0];
        //                    waitNodes.RemoveAt(0);
        //                }
        //                else
        //                {
        //                    endNode = null;
        //                }

        //                //if (Physics.Linecast(VStart, VEnd, 1 << LayerMask.NameToLayer("Obstacle")) || Physics.Linecast(HStart, HEnd, 1 << LayerMask.NameToLayer("Obstacle")))
        //                //{
        //                //    couculatedNodes.Add(endNode.pos);
        //                //    for (int w = 0; w < endNode.neighbor.Count; w++)
        //                //    {
        //                //        if (!waitNodes.Contains(endNode.neighbor[w]) && !couculatedNodes.Contains(endNode.neighbor[w].pos)) waitNodes.Add(endNode.neighbor[w]);
        //                //    }
        //                //    if (waitNodes.Count > 0)
        //                //    {
        //                //        endNode = waitNodes[0];
        //                //        waitNodes.RemoveAt(0);
        //                //    }
        //                //    else
        //                //    {
        //                //        endNode = null;
        //                //    }
        //                //}
        //                //else
        //                //{
        //                //    endNode.endNode = true;
        //                //    endNode = null;
        //                //}
        //            }

        //        }
        //    }
        //    for (int i = couculatedNodes.Count - 1; i >= 0; i--)
        //    {
        //        //已經被算過是有碰撞的末端點，捨棄
        //        Debug.Log("計算過  " + couculatedNodes[i] + " 捨棄");
        //        choosenNodeDic[couculatedNodes[i]].choosen = false;
        //        choosenNode.Remove(choosenNodeDic[couculatedNodes[i]]);
        //        choosenNodeDic.Remove(couculatedNodes[i]);
        //    }
        //}

        if (skip)
        {
            CouculateGraphCross();
            CouculateGraphTurn();
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

    void CouculateGraphCross() {
        List<PatrolGraphNode> graphNodes = new List<PatrolGraphNode>();
        Dictionary<Vector2Int, PatrolGraphNode> graphNodeDic = new Dictionary<Vector2Int, PatrolGraphNode>();
        bool hasMergeNeighbor = false;
        
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            int count = 0;
            int couculateNum = 0;
            int xNum = 0, yNum = 0;

            SpreadNode mergeNode = new SpreadNode();

            if (!choosenNode[i].beenMerged && choosenNode[i].neighbor.Count > 2)
            {
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                node.crossNode = true;
                node.weight = 10;
                graphNodes.Add(node);
                graphNodeDic.Add(choosenNode[i].pos, node);
                choosenNode[i].crossNode = true;

                for (int j = 0; j < choosenNode[i].neighbor.Count; j++)
                {
                    if (choosenNode[i].pos.x == 31 && choosenNode[i].pos.y == 73) Debug.Log("neighbor  " + choosenNode[i].neighbor[j].pos);

                    Vector2Int detectPos = choosenNode[i].neighbor[j].pos;
                    Vector2Int diff = detectPos - choosenNode[i].pos;
                    count++;
                    xNum += diff.x;
                    yNum += diff.y;
                    couculateNum += Mathf.Abs(diff.x);
                    couculateNum += Mathf.Abs(diff.y);
                    Debug.Log(choosenNode[i].pos + "couculate  第 " + j +"個 "  + detectPos.x + "," + detectPos.y + " num " + xNum + "," + yNum);

                    //鄰居是交錯點
                    if (choosenNode[i].neighbor[j].neighbor.Count > 2 || choosenNode[i].neighbor[j].beenMerged)
                    {
                        choosenNode[i].neighbor[j].crossNode = true;
                        //鄰居是單純交錯點
                        if (!choosenNode[i].neighbor[j].beenMerged)
                        {
                            //自己不是合併點，建立新合併點
                            if (!choosenNode[i].beenMerged)
                            {
                                Debug.Log("自己不是合併點，建立新合併點  加入 " + choosenNode[i].neighbor[j].pos);
                                choosenNode[i].beenMerged = true;
                                choosenNode[i].neighbor[j].beenMerged = true;
                                mergeNode = new SpreadNode();
                                mergeNode.mergeCount = 2;
                                mergeNode.pos += choosenNode[i].pos;
                                mergeNode.pos += choosenNode[i].neighbor[j].pos;
                                choosenNode[i].mergeNode = mergeNode;
                                choosenNode[i].neighbor[j].mergeNode = mergeNode;

                                //先將合併點鄰居清單設為鄰居的鄰居清單
                                //mergeNode.neighbor = choosenNode[i].neighbor[j].neighbor;

                                bool notCross = false;
                                //增加鄰居的鄰居進合併點鄰居，鄰居必須不為被合併且不為交錯點
                                for (int m = 0; m < choosenNode[i].neighbor[j].neighbor.Count; m++)
                                {

                                    if (choosenNode[i].neighbor[j].neighbor[m].beenMerged)
                                    {

                                    }
                                    else {
                                        //將沒加如過的加進鄰居清單
                                        if (choosenNode[i].neighbor[j].neighbor[m].neighbor.Count <= 2 && !choosenNode[i].neighbor[j].neighbor[m].beenMerged &&
                                            !mergeNode.neighbor.Contains(choosenNode[i].neighbor[j].neighbor[m]))
                                        {
                                            mergeNode.neighbor.Add(choosenNode[i].neighbor[j].neighbor[m]);
                                        }
                                    }
                                   
                                }
                                //增加自己的鄰居進合併點鄰居
                                for (int m = 0; m < choosenNode[i].neighbor.Count; m++)
                                {
                                    if (!choosenNode[i].neighbor[m].beenMerged && choosenNode[i].neighbor[m].neighbor.Count <= 2)
                                    {
                                        if (!mergeNode.neighbor.Contains(choosenNode[i].neighbor[m]))
                                        {
                                            mergeNode.neighbor.Add(choosenNode[i].neighbor[m]);
                                        }
                                        //同為兩合併點的鄰居
                                        else
                                        {
                                            //沒有其他分支，不為交錯點，移除合併點和該點
                                            Debug.Log("移除 " + choosenNode[i].neighbor[m].pos);
                                            SpreadNode removeNode = choosenNode[i].neighbor[m];

                                            choosenNode[i].crossNode = false;
                                            choosenNode[i].beenMerged = false;

                                            choosenNode[i].neighbor[j].crossNode = false;
                                            choosenNode[i].neighbor[j].beenMerged = false;

                                            choosenNode[i].neighbor[m].choosen = false;

                                            choosenNode[i].neighbor[j].neighbor.Remove(choosenNode[i].neighbor[m]);
                                            choosenNode[i].neighbor.RemoveAt(m);

                                            mergeNode.mergeCount = 0;
                                            mergeNode.neighbor.Clear();

                                            notCross = true;

                                            Debug.Log(choosenNode[i].pos + " 剩餘鄰居 " + choosenNode[i].neighbor.Count);
                                            Debug.Log(choosenNode[i].neighbor[j].pos + " 剩餘鄰居 " + choosenNode[i].neighbor[j].neighbor.Count);
                                            break;
                                        }
                                    }
                                }
                                if (notCross) break;

                                //移除合併點鄰居中的自己
                                if (mergeNode.neighbor.Contains(choosenNode[i])) mergeNode.neighbor.Remove(choosenNode[i]);

                                //choosenNode[i].neighbor = mergeNode.neighbor;
                                choosenNode[i].neighbor[j].neighbor = mergeNode.neighbor;
                            }
                            //自己是合併點，納入鄰居
                            else
                            {
                                Debug.Log("自己是合併點，納入鄰居  加入 " + choosenNode[i].neighbor[j]);
                                choosenNode[i].neighbor[j].beenMerged = true;
                                mergeNode = choosenNode[i].mergeNode;
                                mergeNode.mergeCount++;
                                mergeNode.pos += choosenNode[i].neighbor[j].pos;
                                choosenNode[i].neighbor[j].mergeNode = mergeNode;
                                //增加鄰居的鄰居清單進合併點鄰居
                                for (int m = 0; m < choosenNode[i].neighbor[j].neighbor.Count; m++)
                                {
                                    if (choosenNode[i].neighbor[j].neighbor[m].neighbor.Count <= 2 && !choosenNode[i].neighbor[j].neighbor[m].beenMerged &&
                                        !mergeNode.neighbor.Contains(choosenNode[i].neighbor[j].neighbor[m]))
                                    {
                                        mergeNode.neighbor.Add(choosenNode[i].neighbor[j].neighbor[m]);
                                    }
                                }
                                //移除合併點鄰居清單中的鄰居
                                if (mergeNode.neighbor.Contains(choosenNode[i].neighbor[j])) mergeNode.neighbor.Remove(choosenNode[i].neighbor[j]);

                                //choosenNode[i].neighbor = mergeNode.neighbor;
                                choosenNode[i].neighbor[j].neighbor = mergeNode.neighbor;
                            }
                        }
                        //鄰居是合併點
                        else
                        {
                            //自己不是合併點，加進合併點
                            if (!choosenNode[i].beenMerged)
                            {
                                Debug.Log("自己不是合併點，加進合併點");
                                mergeNode = choosenNode[i].neighbor[j].mergeNode;
                                mergeNode.mergeCount++;
                                mergeNode.pos += choosenNode[i].pos;
                                choosenNode[i].mergeNode = mergeNode;
                                choosenNode[i].beenMerged = true;

                                //移除合併點鄰居中的自己
                                if (mergeNode.neighbor.Contains(choosenNode[i])) mergeNode.neighbor.Remove(choosenNode[i]);
                                //增加自己的鄰居進合併點鄰居
                                for (int m = 0; m < choosenNode[i].neighbor.Count; m++)
                                {
                                    if (choosenNode[i].neighbor[m].neighbor.Count <= 2 && !choosenNode[i].neighbor[m].beenMerged &&
                                        !mergeNode.neighbor.Contains(choosenNode[i].neighbor[m]))
                                    {
                                        mergeNode.neighbor.Add(choosenNode[i].neighbor[m]);
                                    }
                                }

                                //choosenNode[i].neighbor = mergeNode.neighbor;
                                choosenNode[i].neighbor[j].neighbor = mergeNode.neighbor;
                            }
                            else
                            {
                                Debug.Log("自己是合併點，並確認是不是不同的合併點，將兩個合併點整合");
                                //自己是合併點，並確認是不是不同的合併點，將兩個合併點整合
                                if (!choosenNode[i].neighbor[j].mergeNode.Equals(choosenNode[i].mergeNode))
                                {
                                    mergeNode = choosenNode[i].neighbor[j].mergeNode;
                                    mergeNode.mergeCount += choosenNode[i].mergeNode.mergeCount;
                                    mergeNode.pos += choosenNode[i].mergeNode.pos;
                                    //移除合併點鄰居中的自己
                                    if (mergeNode.neighbor.Contains(choosenNode[i])) mergeNode.neighbor.Remove(choosenNode[i]);
                                    //增加自己的合併點鄰居進合併點鄰居
                                    for (int m = 0; m < choosenNode[i].mergeNode.neighbor.Count; m++)
                                    {
                                        if (choosenNode[i].mergeNode.neighbor[m].neighbor.Count <= 2 && !choosenNode[i].mergeNode.neighbor[m].beenMerged &&
                                            !mergeNode.neighbor.Contains(choosenNode[i].mergeNode.neighbor[m]))
                                        {
                                            mergeNode.neighbor.Add(choosenNode[i].mergeNode.neighbor[m]);
                                        }
                                    }
                                    //choosenNode[i].neighbor = mergeNode.neighbor;
                                    choosenNode[i].neighbor[j].neighbor = mergeNode.neighbor;
                                    choosenNode[i].mergeNode.pos = mergeNode.pos;
                                    choosenNode[i].mergeNode.mergeCount = mergeNode.mergeCount;
                                    choosenNode[i].mergeNode.neighbor = mergeNode.neighbor;
                                }
                            }
                        }
                    }
                    else Debug.Log("鄰居不為交錯點");
                }
                //將自己的鄰居設為合併點鄰居
                if(mergeNode.mergeCount > 0)choosenNode[i].neighbor = mergeNode.neighbor;
            }

            if (choosenNode[i].beenMerged)
            {
                foreach (SpreadNode node in choosenNode[i].mergeNode.neighbor)
                {
                    Debug.Log(choosenNode[i].pos + "merge  neighbor " + node.pos);
                }
            }

            else continue;

            continue;

            List<SpreadNode> tempNeighbor = new List<SpreadNode>();
            if (count == 1)
            {
            }
            else if (xNum != 0 || yNum != 0) // (Mathf.Abs(xNum) + Mathf.Abs(yNum)) == 1
            {
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //x或y其一不抵銷，斜邊數量多列為交錯點，判斷為交錯點的權重比較小
                if (count > 2)//couculateNum > 4 && couculateNum % 2 == 0
                {
                    //for (int n = 0; n < choosenNode[i].neighbor.Count; n++)
                    //{
                    //    tempNeighbor.Add(choosenNode[i].neighbor[n]);
                    //    //自己不是合併點
                    //    if (!choosenNode[i].beenMerged)
                    //    {
                    //        //鄰居是合併點，將鄰居的mergeNode與目前點的鄰居整合
                    //        if (choosenNode[i].neighbor[n].beenMerged) {
                    //            mergeNode = choosenNode[i].neighbor[n].mergeNode;
                    //            mergeNode.pos += choosenNode[i].pos;
                    //            mergeNode.mergeCount++;
                    //            if (mergeNode.neighbor.Contains(choosenNode[i])) mergeNode.neighbor.Remove(choosenNode[i]);
                    //            choosenNode[i].beenMerged = true;
                    //            for (int m = 0; m < choosenNode[i].neighbor.Count; m++) {
                    //                if (!mergeNode.neighbor.Contains(choosenNode[i].neighbor[m])) mergeNode.neighbor.Add(choosenNode[i].neighbor[m]);
                    //            }
                    //        }
                    //        //鄰居是一般交錯點，結合成一新合併點
                    //        else if (choosenNode[i].neighbor[n].crossNode) {
                    //            choosenNode[i].beenMerged = true;
                    //            choosenNode[i].neighbor[n].beenMerged = true;
                    //            choosenNode[i].mergeNode = new SpreadNode();
                    //            choosenNode[i].neighbor[n].mergeNode = mergeNode;
                    //            mergeNode.mergeCount = 2;
                    //            mergeNode.pos += choosenNode[i].pos;
                    //            mergeNode.pos += choosenNode[i].neighbor[n].pos;
                    //            mergeNode.neighbor = choosenNode[i].neighbor[n].neighbor;
                    //            if (mergeNode.neighbor.Contains(choosenNode[i])) mergeNode.neighbor.Remove(choosenNode[i]);
                    //            for (int m = 0; m < choosenNode[i].neighbor.Count; m++)
                    //            {
                    //                if (!mergeNode.neighbor.Contains(choosenNode[i].neighbor[m])) mergeNode.neighbor.Add(choosenNode[i].neighbor[m]);
                    //            }
                    //        }
                    //    }
                    //    //自己是合併點
                    //    else {
                    //        //鄰居是合併點，將鄰居的mergeNode與目前點的鄰居整合
                    //        if (choosenNode[i].neighbor[n].beenMerged)
                    //        {
                    //            if (!choosenNode[i].neighbor[n].mergeNode.Equals(choosenNode[i].mergeNode))
                    //            {
                    //                mergeNode = choosenNode[i].mergeNode;
                    //                mergeNode.pos += choosenNode[i].neighbor[n].pos;
                    //                mergeNode.mergeCount += ;

                    //                for (int m = 0; m < choosenNode[i].neighbor.Count; m++)
                    //                {
                    //                    if (!mergeNode.neighbor.Contains(choosenNode[i].neighbor[m])) mergeNode.neighbor.Add(choosenNode[i].neighbor[m]);
                    //                }
                    //            }
                    //        }
                    //        //鄰居是一般交錯點，併入自己合併點
                    //        else if (choosenNode[i].neighbor[n].crossNode) { 
                                
                    //        }
                    //    }
                    //    Vector2Int pos = choosenNode[i].neighbor[n].pos;
                    //    if (graphNodeDic.ContainsKey(pos))
                    //    {
                    //        //if ((graphNodeDic[pos].weight > tempW)) tempW = graphNodeDic[pos].weight;
                    //    }
                    //}

                    //只剩一個方向，且鄰居數等於三  權重最小
                    if (count == 3) //(Mathf.Abs(xNum) + Mathf.Abs(yNum)) == 1
                    {
                        int tempW = 0;
                        for (int n = 0; n < choosenNode[i].neighbor.Count; n++) {
                            Vector2Int pos = choosenNode[i].neighbor[n].pos;
                            if (graphNodeDic.ContainsKey(pos))
                            {
                                if ((graphNodeDic[pos].weight > tempW)) tempW = graphNodeDic[pos].weight;
                            }
                        }

                        if (tempW > 5) continue;
                        node.crossNode = true;
                        node.weight = 5;
                        graphNodes.Add(node);
                        graphNodeDic.Add(choosenNode[i].pos, node);
                        choosenNode[i].crossNode = true;
                    }
                    //只剩一個方向，且鄰居數大於三  權重第二
                    else
                    {
                        int tempW = 0;
                        for (int n = 0; n < choosenNode[i].neighbor.Count; n++) {
                            Vector2Int pos = choosenNode[i].neighbor[n].pos;
                            if (graphNodeDic.ContainsKey(pos))
                            {
                                if ((graphNodeDic[pos].weight > tempW)) tempW = graphNodeDic[pos].weight;
                                if (graphNodeDic[pos].weight < 7)
                                {
                                    //移除權重不大於自己的鄰居
                                    graphNodes.Remove(graphNodeDic[pos]);
                                    graphNodeDic.Remove(pos);
                                    //choosenNodeDic[pos].turnNode = false;
                                    choosenNodeDic[pos].crossNode = false;
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

                    for (int n = 0; n < choosenNode[i].neighbor.Count; n++) {
                        Vector2Int pos = choosenNode[i].neighbor[n].pos;
                        //比上方判斷交錯點的權重大，周圍權重不大於10就刪掉
                        if (graphNodeDic.ContainsKey(pos) && graphNodeDic[pos].weight < 10)  //&& !confirmGraphNodeDic[pos].crossNode
                        {
                            if (graphNodeDic[pos].weight > 3)
                            {
                                graphNodeDic[pos].crossNode = false;
                                graphNodeDic[pos].weight = 0;
                                choosenNodeDic[pos].crossNode = false;
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
    void CouculateGraphTurn()
    {
        //List<PatrolGraphNode> graphNodes = new List<PatrolGraphNode>();
        //Dictionary<Vector2Int, PatrolGraphNode> graphNodeDic = new Dictionary<Vector2Int, PatrolGraphNode>();
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            if (choosenNode[i].crossNode && choosenNode[i].beenMerged) {
                //mergeNode的pos只能除一次
                if(!choosenNode[i].mergeNode.mergeCouculate) choosenNode[i].mergeNode.pos = new Vector2Int(Mathf.RoundToInt(choosenNode[i].mergeNode.pos.x / choosenNode[i].mergeNode.mergeCount), Mathf.RoundToInt(choosenNode[i].mergeNode.pos.y / choosenNode[i].mergeNode.mergeCount));
                choosenNode[i].mergeNode.mergeCouculate = true;
                choosenNode[i].neighbor = choosenNode[i].mergeNode.neighbor;
                choosenNode[i].pos = choosenNode[i].mergeNode.pos;
                continue;
            }

            if (!choosenNode[i].choosen) {
                choosenNodeDic.Remove(choosenNode[i].pos);
                choosenNode.RemoveAt(i);
                continue;
            }

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
                PatrolGraphNode node = new PatrolGraphNode(choosenNode[i].pos.x, choosenNode[i].pos.y);
                //一般列為轉折點
                node.turnNode = true;
                node.weight = 3;
                //graphNodes.Add(node);
                //graphNodeDic.Add(choosenNode[i].pos, node);
                choosenNode[i].turnNode = true;
            }
           
        }
    }

    public IEnumerator ConactGraph() {

        PatrolGraphNode fromNode = null;
        int neighborCount = 0;
        int connectLength = 0;

        //先找出一個交錯點為開始點
        for (int i = choosenNode.Count - 1; i >= 0; i--)
        {
            sourceNode = choosenNode[i];
            if (sourceNode.crossNode){
                //if (sourceNode.beenMerged) {
                //    sourceNode.pos = sourceNode.mergeNode.pos;
                //    sourceNode.neighbor = sourceNode.mergeNode.neighbor;
                //}

                fromNode= new PatrolGraphNode(sourceNode.pos.x, sourceNode.pos.y);
                fromNode.besideNodes = new List<PatrolGraphNode.ConnectGraphNode>();
                fromNode.crossNode = true;
                fromNode.pos = pathFindGrid.GetNodePos(fromNode.x, fromNode.y);
                ConfirmGraph.Add(fromNode);
                confirmGraphNodeDic.Add(sourceNode.pos, fromNode);
                connectNeighbor = sourceNode.neighbor[0];
                sourceNode.neighbor.RemoveAt(0);
                connectNeighbor.neighbor.Remove(sourceNode);
                //將第一個交錯點列入清單
                waitNodes.Add(sourceNode);
                Debug.Log("第一個點 " + sourceNode.pos);
                Debug.Log("第一個鄰居點 " + connectNeighbor.pos + "  是否交錯點" + connectNeighbor.crossNode);
                break;
            } 
        }

        SpreadNode lastTurnNode = null;
        Vector2Int lastDir = new Vector2Int(0,0);
        int lastLength = 0;
        int breakNum = 0;
        while (connectNeighbor != null) {


            if (!skipRun) yield return new WaitForSeconds(connectTime);

            breakNum++;
            if (breakNum > 100000) break;

            connectLength++;


            //判斷鄰居是不是交錯點
            Debug.Log("計算鄰居點 " + connectNeighbor.pos);
            if (connectNeighbor.crossNode)
            {
                PatrolGraphNode nextNode;
                PatrolGraphNode.ConnectGraphNode srcNode;
                PatrolGraphNode.ConnectGraphNode besideNode;

                //if (connectNeighbor.beenMerged) {
                //    connectNeighbor.pos = connectNeighbor.mergeNode.pos;
                //    connectNeighbor.neighbor = connectNeighbor.mergeNode.neighbor;
                //}

                //新的交錯點與上個轉折點中有障礙物，將上一個轉折點加入
                if (lastTurnNode != null && 
                    (Physics.Linecast(pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y), pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y), 1 << LayerMask.NameToLayer("Obstacle")) ||
                    Vector2.Angle(lastDir, connectNeighbor.pos - lastTurnNode.pos) > maxConnectAngle))
                {
                    if (!confirmGraphNodeDic.ContainsKey(lastTurnNode.pos))
                    {
                        Debug.Log("尚未有該轉折點");
                        nextNode = new PatrolGraphNode(lastTurnNode.pos.x, lastTurnNode.pos.y);
                        nextNode.besideNodes = new List<PatrolGraphNode.ConnectGraphNode>();
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
                    //將來源點加進上個轉折點的連接點裡
                    srcNode = new PatrolGraphNode.ConnectGraphNode();
                    srcNode.node = confirmGraphNodeDic[sourceNode.pos];
                    srcNode.length = lastLength;
                    nextNode.besideNodes.Add(srcNode);
                    //將上個轉折點加進來原點的連接點裡
                    besideNode = new PatrolGraphNode.ConnectGraphNode();
                    besideNode.node = nextNode;
                    besideNode.length = lastLength;
                    confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(besideNode);
                    Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + lastLength);
                    connectLength -= lastLength;

                    lastDir = new Vector2Int(connectNeighbor.pos.x - lastTurnNode.pos.x, connectNeighbor.pos.y - lastTurnNode.pos.y);
                    sourceNode = lastTurnNode;
                    //lastTurnNode = connectNeighbor;
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
                    nextNode.besideNodes = new List<PatrolGraphNode.ConnectGraphNode>();
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
                //將來源點加進鄰居的連接點裡
                srcNode = new PatrolGraphNode.ConnectGraphNode();
                srcNode.node = confirmGraphNodeDic[sourceNode.pos];
                srcNode.length = connectLength;
                nextNode.besideNodes.Add(srcNode);
                //將鄰居點加進自己的連接點裡
                besideNode = new PatrolGraphNode.ConnectGraphNode();
                besideNode.node = nextNode;
                besideNode.length = connectLength;
                confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(besideNode);
                Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + connectLength);
                connectLength = 0;

                lastTurnNode = null;

                //檢查已接進圖的鄰居數有沒有大於二，因為有些情況一般點會誤判成交錯點
                if (confirmGraphNodeDic[connectNeighbor.pos].besideNodes.Count < 2)
                {

                }


                //將鄰居交錯點的所有鄰居的鄰居移除自己
                //for (int i = 0; i < connectNeighbor.neighbor.Count; i++) {
                //    Debug.Log();
                //    if (connectNeighbor.neighbor[i].neighbor.Contains(connectNeighbor)) {
                //        connectNeighbor.neighbor[i].neighbor.Remove(connectNeighbor);
                //    }
                //}

                //鄰居點的鄰居數大於0，加入等待清單
                if (connectNeighbor.neighbor.Count > 0)
                {
                    if(!connectNeighbor.neighbor.Contains(connectNeighbor))waitNodes.Add(connectNeighbor);
                }
                else
                {
                    //鄰居點沒有鄰居
                }

                //來源點鄰居數大於0，鄰居點改為來原點的另一個鄰居
                if (sourceNode.neighbor.Count > 0)
                {
                    Debug.Log("判斷交錯點  來源點還有鄰居");
                    //如果有交錯點，優先選交錯點
                    for (int n = sourceNode.neighbor.Count - 1; n >= 0; n--)
                    {
                        Debug.Log("鄰居有 " + connectNeighbor.pos);
                        connectNeighbor = sourceNode.neighbor[n];
                        if (connectNeighbor.crossNode) {
                            //if (connectNeighbor.beenMerged)
                            //{
                            //    connectNeighbor.pos = connectNeighbor.mergeNode.pos;
                            //    connectNeighbor.neighbor = connectNeighbor.mergeNode.neighbor;
                            //}
                            break;
                        } 
                        Debug.Log("鄰居有 " + connectNeighbor.pos);
                    }
                    sourceNode.neighbor.Remove(connectNeighbor);
                    connectNeighbor.neighbor.Remove(sourceNode);
                   
                }
                //來源點的鄰居都計算完，來原點改為等待清單的頭，鄰居點改為等待清單的鄰居
                else
                {
                    Debug.Log("判斷交錯點  來源點的鄰居都計算完");

                    //確認清單中的點有鄰居
                    while (waitNodes.Count > 0)
                    {
                        Debug.Log("清單有  " + waitNodes[0].pos + "  有鄰居個數" + waitNodes[0].neighbor.Count);
                        sourceNode = waitNodes[0];
                        if (sourceNode.neighbor.Count > 0)
                        {
                            for (int n = sourceNode.neighbor.Count - 1; n >= 0; n--)
                            {
                                connectNeighbor = sourceNode.neighbor[n];
                                if (connectNeighbor.crossNode) {
                                    //if (connectNeighbor.beenMerged)
                                    //{
                                    //    connectNeighbor.pos = connectNeighbor.mergeNode.pos;
                                    //    connectNeighbor.neighbor = connectNeighbor.mergeNode.neighbor;
                                    //}
                                    break;
                                }
                            }
                            sourceNode.neighbor.Remove(connectNeighbor);
                            connectNeighbor.neighbor.Remove(sourceNode);
                            break;
                        }
                        else waitNodes.RemoveAt(0);
                    }
                    //清單中無點
                    if (waitNodes.Count <= 0)
                    {
                        break;
                    }
                    else
                    {
                        //清單中的點被選後鄰居變為0，從清單移除
                        if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                    }
                }
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

                    //與第一個轉折點之間沒有碰撞，先記住
                    //if (!Physics.Linecast(new Vector3(sourceNode.pos.x, 0, sourceNode.pos.y), new Vector3(connectNeighbor.pos.x, 0, connectNeighbor.pos.y), 1 << LayerMask.NameToLayer("Obstacle"))) {
                        
                    //}
                    
                }
                else {
                    //與來原點之間有碰撞，或與上一個轉折點角度太大，加入上一個轉折點
                    if (Physics.Linecast(pathFindGrid.GetNodePos(sourceNode.pos.x, sourceNode.pos.y), pathFindGrid.GetNodePos(connectNeighbor.pos.x, connectNeighbor.pos.y), 1 << LayerMask.NameToLayer("Obstacle")) || 
                        Vector2.Angle(lastDir, connectNeighbor.pos - lastTurnNode.pos) > maxConnectAngle)
                    {
                        if (!confirmGraphNodeDic.ContainsKey(lastTurnNode.pos))
                        {
                            Debug.Log("尚未有該轉折點");
                            nextNode = new PatrolGraphNode(lastTurnNode.pos.x, lastTurnNode.pos.y);
                            nextNode.besideNodes = new List<PatrolGraphNode.ConnectGraphNode>();
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
                        //將來源點加進上個轉折點的連接點裡
                        PatrolGraphNode.ConnectGraphNode srcNode = new PatrolGraphNode.ConnectGraphNode();
                        srcNode.node = confirmGraphNodeDic[sourceNode.pos];
                        srcNode.length = lastLength;
                        nextNode.besideNodes.Add(srcNode);
                        //將上個轉折點加進來原點的連接點裡
                        PatrolGraphNode.ConnectGraphNode besideNode = new PatrolGraphNode.ConnectGraphNode();
                        besideNode.node = nextNode;
                        besideNode.length = lastLength;
                        confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(besideNode);
                        Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + lastLength);
                        connectLength -= lastLength;

                        lastDir = new Vector2Int(connectNeighbor.pos.x - lastTurnNode.pos.x, connectNeighbor.pos.y - lastTurnNode.pos.y);
                        sourceNode = lastTurnNode;
                    }
                }

                lastTurnNode = connectNeighbor;
                lastLength = connectLength;

                //鄰居點的鄰居數大於0
                if (connectNeighbor.neighbor.Count > 0)
                {
                    Debug.Log("轉折點 鄰居大於0");
                    SpreadNode tempNode = null;
                    for (int n = connectNeighbor.neighbor.Count - 1; n >= 0; n--)
                    {
                        tempNode = connectNeighbor.neighbor[n];
                        if (tempNode.crossNode) break;
                    }
                    connectNeighbor.neighbor.Remove(tempNode);
                    tempNode.neighbor.Remove(connectNeighbor);
                    connectNeighbor = tempNode;
                }
                //鄰居點沒有鄰居
                else
                {
                    Debug.Log("轉折點 鄰居小於0");
                    //確認清單中的點有鄰居
                    while (waitNodes.Count > 0)
                    {
                        sourceNode = waitNodes[0];
                        if (sourceNode.neighbor.Count > 0)
                        {
                            for (int n = sourceNode.neighbor.Count - 1; n >= 0; n--)
                            {
                                connectNeighbor = sourceNode.neighbor[n];
                                if (connectNeighbor.crossNode) {
                                    if (connectNeighbor.beenMerged)
                                    {
                                        connectNeighbor.pos = connectNeighbor.mergeNode.pos;
                                        connectNeighbor.neighbor = connectNeighbor.mergeNode.neighbor;
                                    }
                                    break;
                                }
                            }
                            sourceNode.neighbor.Remove(connectNeighbor);
                            connectNeighbor.neighbor.Remove(sourceNode);
                            break;
                        }
                        else waitNodes.RemoveAt(0);
                    }
                    //清單中無點
                    if (waitNodes.Count <= 0)
                    {
                        break;
                    }
                    else
                    {
                        //清單中的點被選後鄰居變為0，從清單移除
                        if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                    }
                }

                //if (!confirmGraphNodeDic.ContainsKey(connectNeighbor.pos))
                //{
                //    Debug.Log("尚未有該轉折點");
                //    nextNode = new PatrolGraphNode(connectNeighbor.pos.x, connectNeighbor.pos.y);
                //    nextNode.besideNodes = new List<PatrolGraphNode.ConnectGraphNode>();
                //    nextNode.turnNode = true;
                //    nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                //    ConfirmGraph.Add(nextNode);
                //    confirmGraphNodeDic.Add(connectNeighbor.pos, nextNode);
                //}
                ////已有
                //else
                //{
                //    Debug.Log("已有該轉折點");
                //    nextNode = confirmGraphNodeDic[connectNeighbor.pos];
                //}
                ////將來源點加進鄰居的連接點裡
                //PatrolGraphNode.ConnectGraphNode srcNode = new PatrolGraphNode.ConnectGraphNode();
                //srcNode.node = confirmGraphNodeDic[sourceNode.pos];
                //srcNode.length = connectLength;
                //nextNode.besideNodes.Add(srcNode);
                ////將鄰居點加進自己的連接點裡
                //PatrolGraphNode.ConnectGraphNode besideNode = new PatrolGraphNode.ConnectGraphNode();
                //besideNode.node = nextNode;
                //besideNode.length = connectLength;
                //confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(besideNode);
                //Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + connectLength);
                //connectLength = 0;

                //鄰居點的鄰居數大於0，鄰居點變為來源點
                //if (connectNeighbor.neighbor.Count > 0)
                //{
                //    Debug.Log("轉折點 鄰居大於0");
                //    sourceNode = connectNeighbor;
                //    for (int n = sourceNode.neighbor.Count - 1; n >= 0; n--)
                //    {
                //        connectNeighbor = sourceNode.neighbor[n];
                //        if (connectNeighbor.crossNode) break;
                //    }
                //    sourceNode.neighbor.Remove(connectNeighbor);
                //    connectNeighbor.neighbor.Remove(sourceNode);
                //}
                ////鄰居點沒有鄰居
                //else
                //{
                //    Debug.Log("轉折點 鄰居小於0");
                //    //確認清單中的點有鄰居
                //    while (waitNodes.Count > 0)
                //    {
                //        sourceNode = waitNodes[0];
                //        if (sourceNode.neighbor.Count > 0)
                //        {
                //            for (int n = sourceNode.neighbor.Count - 1; n >= 0; n--)
                //            {
                //                connectNeighbor = sourceNode.neighbor[n];
                //                if (connectNeighbor.crossNode) break;
                //            }
                //            sourceNode.neighbor.Remove(connectNeighbor);
                //            connectNeighbor.neighbor.Remove(sourceNode);
                //            break;
                //        }
                //        else waitNodes.RemoveAt(0);
                //    }
                //    //清單中無點
                //    if (waitNodes.Count <= 0)
                //    {
                //        break;
                //    }
                //    else
                //    {
                //        //清單中的點被選後鄰居變為0，從清單移除
                //        if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                //    }
                //}
            }
            //鄰居為一般點
            else {
                connectNeighbor.hasCouculate = true;
                //先決定鄰居，並移除其他鄰居
                Debug.Log("一般點    有 " + connectNeighbor.neighbor.Count + " 個鄰居");
                SpreadNode neighbor = null;
                for (int n = connectNeighbor.neighbor.Count - 1; n >= 0; n--)
                {
                    //已經被尋過，移除
                    //if (connectNeighbor.neighbor[n].hasCouculate)
                    //{
                    //    Debug.Log("新鄰居為算過的一般點  移除新鄰居");
                    //    connectNeighbor.neighbor.RemoveAt(n);
                    //    continue;
                    //}

                    ////新的鄰居點不能走回來源點
                    //if (!connectNeighbor.neighbor[n].neighbor.Contains(sourceNode))
                    //{

                    //    if (neighbor == null && (connectNeighbor.neighbor[n].crossNode || connectNeighbor.neighbor[n].turnNode || n == 0))
                    //    {
                    //        neighbor = connectNeighbor.neighbor[n];
                    //        //break;
                    //    }
                    //    else
                    //    {
                    //        //為了怕一般點連多個點，有詢過的將自己從鄰居移除
                    //        //connectNeighbor.neighbor[n].neighbor.Remove(connectNeighbor);
                    //    }
                    //}
                    //else {
                    //    Debug.Log("新鄰居會連回來原點 互相移除");
                    //    connectNeighbor.neighbor[n].neighbor.Remove(connectNeighbor);
                    //    connectNeighbor.neighbor.RemoveAt(n);
                    //    continue;
                    //}

                    if (connectNeighbor.neighbor[n].beenMerged)
                    {
                        if (!connectNeighbor.neighbor[n].mergeNode.Equals(sourceNode)){
                            if (neighbor == null)
                            {
                                neighbor = connectNeighbor.neighbor[n];
                            }
                            else
                            {
                                if (connectNeighbor.neighbor[n].crossNode)
                                {
                                    if (neighbor.beenMerged && neighbor.mergeNode != sourceNode)
                                    {
                                        neighbor = connectNeighbor.neighbor[n];
                                        neighbor.pos = neighbor.mergeNode.pos;
                                        neighbor.neighbor = neighbor.mergeNode.neighbor;
                                    }
                                }
                                else if (connectNeighbor.neighbor[n].turnNode)
                                {
                                    if (!neighbor.crossNode) neighbor = connectNeighbor.neighbor[n];
                                }
                            }
                        }
                    }
                    else {
                        if (!connectNeighbor.neighbor[n].neighbor.Contains(sourceNode)) { 
                            
                        }
                    }

                    if (!connectNeighbor.neighbor[n].neighbor.Contains(sourceNode)) {
                        if (neighbor == null)
                        {
                            neighbor = connectNeighbor.neighbor[n];
                        }
                        else
                        {
                            if (connectNeighbor.neighbor[n].crossNode)
                            {
                                if (neighbor.beenMerged && neighbor.mergeNode != sourceNode) {
                                    neighbor = connectNeighbor.neighbor[n];
                                    neighbor.pos = neighbor.mergeNode.pos;
                                    neighbor.neighbor = neighbor.mergeNode.neighbor;
                                }
                            }
                            else if (connectNeighbor.neighbor[n].turnNode)
                            {
                                if (!neighbor.crossNode) neighbor = connectNeighbor.neighbor[n];
                            }
                        }
                    }
                    connectNeighbor.neighbor[n].neighbor.Remove(connectNeighbor);
                    connectNeighbor.neighbor.RemoveAt(n);
                }
                if (neighbor != null)
                {
                    connectNeighbor = neighbor;
                }
                //沒有其他鄰居點
                else {
                    PatrolGraphNode nextNode;
                    PatrolGraphNode.ConnectGraphNode srcNode;
                    PatrolGraphNode.ConnectGraphNode besideNode;
                    //確認為末端點，加入
                    if (connectNeighbor.endNode) {
                        if (!confirmGraphNodeDic.ContainsKey(connectNeighbor.pos))
                        {
                            nextNode = new PatrolGraphNode(connectNeighbor.pos.x, connectNeighbor.pos.y);
                            nextNode.besideNodes = new List<PatrolGraphNode.ConnectGraphNode>();
                            nextNode.endNode= true;
                            nextNode.pos = pathFindGrid.GetNodePos(nextNode.x, nextNode.y);
                            ConfirmGraph.Add(nextNode);
                            confirmGraphNodeDic.Add(connectNeighbor.pos, nextNode);
                        }
                        //已有
                        else
                        {
                            nextNode = confirmGraphNodeDic[connectNeighbor.pos];
                        }
                        //將來源點加進鄰居的連接點裡
                        srcNode = new PatrolGraphNode.ConnectGraphNode();
                        srcNode.node = confirmGraphNodeDic[sourceNode.pos];
                        srcNode.length = connectLength;
                        nextNode.besideNodes.Add(srcNode);
                        //將鄰居點加進自己的連接點裡
                        besideNode = new PatrolGraphNode.ConnectGraphNode();
                        besideNode.node = nextNode;
                        besideNode.length = connectLength;
                        confirmGraphNodeDic[sourceNode.pos].besideNodes.Add(besideNode);
                        Debug.Log("新連接 " + sourceNode.pos + " ---> " + nextNode.pos + "  length" + connectLength);
                        connectLength = 0;
                    }
                    lastTurnNode = null;
                    
                    //從清單中選，確認清單中的點有鄰居
                    while (waitNodes.Count > 0)
                    {
                        sourceNode = waitNodes[0];
                        if (sourceNode.neighbor.Count > 0)
                        {
                            for (int n = sourceNode.neighbor.Count - 1; n >= 0; n--)
                            {
                                connectNeighbor = sourceNode.neighbor[n];
                                if (connectNeighbor.crossNode) {
                                    if (connectNeighbor.beenMerged)
                                    {
                                        connectNeighbor.pos = connectNeighbor.mergeNode.pos;
                                        connectNeighbor.neighbor = connectNeighbor.mergeNode.neighbor;
                                    }
                                    break;
                                }
                            }
                            sourceNode.neighbor.Remove(connectNeighbor);
                            connectNeighbor.neighbor.Remove(sourceNode);
                            break;
                        }
                        else waitNodes.RemoveAt(0);
                    }
                    //清單中無點
                    if (waitNodes.Count <= 0)
                    {
                        break;
                    }
                    else {
                        //清單中的點被選後鄰居變為0，從清單移除
                        if (sourceNode.neighbor.Count <= 0) waitNodes.RemoveAt(0);
                    }

                }
                
            }
        }

        Debug.Log("跑完連接~~~~~");
        runConnectEnd = true;

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


        //連接出maxPathNum數量之路線
        for (int pathNum = 0; pathNum < maxPathNum; pathNum++)
        {
            yield return null;
            int currentLength = 0;
            int repeatNum = 0;

            //繞圈和來回有不同做法
            //繞圈
            if (pathNum < patrolCycleNum)
            {
                List<PatrolGraphNode> patrolGraph1 = new List<PatrolGraphNode>();
                List<PatrolGraphNode> patrolGraph2 = new List<PatrolGraphNode>();
                List<Vector3> patrolPoint1 = new List<Vector3>();
                List<Vector3> patrolPoint2 = new List<Vector3>();

                //路線開頭點不能為用過
                int id = Random.Range(0, ConfirmGraph.Count);
                while (ConfirmGraph[id].detectNum > 0 || ConfirmGraph[id].besideNodes.Count <= 1)
                {
                    id = Random.Range(0, ConfirmGraph.Count);
                }
                ConfirmGraph[id].detectNum++;
                patrolGraph1.Add(ConfirmGraph[id]);
                patrolGraph1.Add(ConfirmGraph[id]);
                patrolPoint2.Add(ConfirmGraph[id].pos);
                patrolPoint2.Add(ConfirmGraph[id].pos);

                PatrolGraphNode node1 = ConfirmGraph[id].besideNodes[0].node;
                currentLength += ConfirmGraph[id].besideNodes[0].length;
                patrolGraph1.Add(node1);
                node1.detectNum++;
                patrolPoint1.Add(node1.pos);

                PatrolGraphNode node2 = ConfirmGraph[id].besideNodes[1].node;
                currentLength += ConfirmGraph[id].besideNodes[1].length;
                patrolGraph2.Add(node2);
                node2.detectNum++;
                patrolPoint2.Add(node2.pos);

                PatrolGraphNode lastNode1 = ConfirmGraph[id];
                PatrolGraphNode lastNode2 = ConfirmGraph[id];

                int giveUpNum1 = 0;
                int giveUpNum2 = 0;
                bool add = true;
                while (add)
                {
                    //第一條支線
                    PatrolGraphNode.ConnectGraphNode connectNode1 = node1.besideNodes[Random.Range(0, node1.besideNodes.Count)];
                    int newLength = currentLength + connectNode1.length;

                    //新的點不能在支線裡，新點的使用數不能超過3，長度不超過最大長度，重複點數量不超過3
                    if (!patrolGraph1.Contains(connectNode1.node) && connectNode1.node.detectNum <= 3 && newLength <= maxPatrolLength && repeatNum <= 3 && connectNode1.node.besideNodes.Count > 1)
                    {
                        giveUpNum1 = 0;
                        //新點是重複點，數量++
                        if (connectNode1.node.detectNum > 0) repeatNum++;
                        lastNode1 = node1;
                        currentLength = newLength;
                        node1 = connectNode1.node;
                        patrolGraph1.Add(node1);
                        patrolPoint1.Add(node1.pos);
                        node1.detectNum++;

                        if (patrolGraph2.Contains(connectNode1.node) && currentLength >= minPatrolLength) {

                            int nextID = patrolGraph2.IndexOf(connectNode1.node);
                            for (int i = nextID-1; i >= 0; i--) {
                                patrolPoint1.Add(patrolPoint2[i]);
                            }
                            Path path = new Path(patrolPoint1, turnDist);
                            patrolPathes.Add(path);
                            break;
                        }
                    }
                    else
                    {
                        //連續重複失敗10次，捨棄這條路線
                        giveUpNum1++;
                        if (giveUpNum1 >= 10)
                        {
                            Debug.Log("cycle 捨棄 支線1");

                            if (patrolGraph1.Contains(connectNode1.node)) Debug.Log("碰到有的點");
                            if (connectNode1.node.detectNum > 3) Debug.Log("點已被太多路線共用");
                            if (newLength > maxPatrolLength) Debug.Log("路徑太長");
                            if(repeatNum > 3) Debug.Log("重複點太多");
                            if(connectNode1.node.besideNodes.Count <= 1) Debug.Log("點沒有鄰居");

                            for (int i = 0; i < patrolGraph1.Count; i++)
                            {
                                patrolGraph1[i].detectNum--;
                            }
                            for (int i = 0; i < patrolGraph2.Count; i++)
                            {
                                patrolGraph2[i].detectNum--;
                            }
                            pathNum--;
                            break;
                        }

                    }

                    //第二條支線
                    PatrolGraphNode.ConnectGraphNode connectNode2 = node2.besideNodes[Random.Range(0, node2.besideNodes.Count)];
                    newLength = currentLength + connectNode2.length;

                    //新的點不能是上個點，新點的使用數不能超過3，長度不超過最大長度，重複點數量不超過3
                    if (connectNode2.node != lastNode2 && connectNode2.node.detectNum <= 3 && newLength <= maxPatrolLength && repeatNum <= 3 && connectNode2.node.besideNodes.Count > 1)
                    {
                        giveUpNum2 = 0;
                        //新點是重複點，數量++
                        if (connectNode2.node.detectNum > 0) repeatNum++;
                        lastNode1 = node2;
                        currentLength = newLength;
                        node2 = connectNode2.node;
                        patrolGraph2.Add(node2);
                        patrolPoint2.Add(node2.pos);
                        node2.detectNum++;

                        if (patrolGraph1.Contains(connectNode2.node) && currentLength >= minPatrolLength)
                        {

                            int nextID = patrolGraph1.IndexOf(connectNode2.node);
                            for (int i = nextID - 1; i >= 0; i--)
                            {
                                patrolPoint2.Add(patrolPoint1[i]);
                            }
                            Path path = new Path(patrolPoint2, turnDist);
                            patrolPathes.Add(path);
                            break;
                        }
                    }
                    else
                    {
                        //連續重複失敗10次，捨棄這條路線
                        giveUpNum2++;
                        if (giveUpNum2 >= 10)
                        {
                            Debug.Log("cycle 捨棄 支線2");

                            if (patrolGraph2.Contains(connectNode1.node)) Debug.Log("碰到有的點");
                            if (connectNode2.node.detectNum > 3) Debug.Log("點已被太多路線共用");
                            if (newLength > maxPatrolLength) Debug.Log("路徑太長");
                            if (repeatNum > 3) Debug.Log("重複點太多");
                            if (connectNode2.node.besideNodes.Count <= 1) Debug.Log("點沒有鄰居");
                            for (int i = 0; i < patrolGraph1.Count; i++)
                            {
                                patrolGraph1[i].detectNum--;
                            }
                            for (int i = 0; i < patrolGraph2.Count; i++)
                            {
                                patrolGraph2[i].detectNum--;
                            }
                            pathNum--;
                            break;
                        }
                    }
                }
            }
            //來回
            else {
                //路線開頭點不能為用過
                int id = Random.Range(0, ConfirmGraph.Count);
                while (ConfirmGraph[id].detectNum > 0)
                {
                    id = Random.Range(0, ConfirmGraph.Count);
                }

                PatrolGraphNode node = ConfirmGraph[id];
                List<PatrolGraphNode> patrolGraph = new List<PatrolGraphNode>();
                List<Vector3> patrolPoint = new List<Vector3>();
                patrolGraph.Add(node);
                node.detectNum++;
                patrolPoint.Add(node.pos);
                int giveUpNum = 0;
                bool add = true;
                while (add)
                {
                    PatrolGraphNode.ConnectGraphNode connectNode = node.besideNodes[Random.Range(0, node.besideNodes.Count)];
                    int newLength = currentLength + connectNode.length;

                    //新的點不能在路線裡，新點的使用數不能超過3，長度不超過最大長度，重複點數量不超過3
                    if (!patrolGraph.Contains(connectNode.node) && connectNode.node.detectNum <= 3 && newLength <= maxPatrolLength && (newLength < minPatrolLength && connectNode.node.besideNodes.Count > 1))
                    {
                        giveUpNum = 0;
                        //新點是重複點，數量++
                        if (connectNode.node.detectNum > 0) {
                            repeatNum++;
                            if (repeatNum > 3) {
                                Debug.Log("因為重複點太多 來回 捨棄 ");
                                for (int i = 0; i < patrolGraph.Count; i++)
                                {
                                    patrolGraph[i].detectNum--;
                                }
                                patrolGraph.Clear();
                                patrolPoint.Clear();
                                pathNum--;
                                break;
                            }
                        } 

                        currentLength = newLength;
                        node = connectNode.node;
                        patrolGraph.Add(node);
                        patrolPoint.Add(node.pos);
                        node.detectNum++;

                        //超過最低長度且0.1機率，不再加新的點，行成路線
                        if (newLength >= minPatrolLength && Random.Range(0, 100) < 10)
                        {
                            add = false;
                            Path path = new Path(patrolPoint, turnDist);
                            patrolPathes.Add(path);
                        }
                    }
                    else
                    {
                        //連續重複失敗10次，捨棄這條路線
                        giveUpNum++;
                        if (giveUpNum >= 10)
                        {
                            Debug.Log("來回 捨棄 ");
                            for (int i = 0; i < patrolGraph.Count; i++)
                            {
                                patrolGraph[i].detectNum--;
                            }
                            patrolGraph.Clear();
                            patrolPoint.Clear();
                            pathNum--;
                            break;
                        }
                    }

                }
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
