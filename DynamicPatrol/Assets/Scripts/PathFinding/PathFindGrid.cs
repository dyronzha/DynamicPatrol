using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PathFinder
{
    public class PathFindGrid : MonoBehaviour
    {

        public bool displayGridGizmos;
        public LayerMask unwalkableMask;
        public Vector2 gridWorldSize;
        public float nodeRadius;
        public TerrainType[] walkableRegions;
        public int obstacleProximityPenalty = 10;
        public int blurSize = 3;
        public int chooseOffset;
        
        
        int chooseValue;
        
        Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
        LayerMask walkableMask;

        Node[,] grid;
        public Node[,] Grid { get { return grid; } }

        float nodeDiameter;
        int gridSizeX, gridSizeY;
        public int GridSizeX { get { return gridSizeX; } }
        public int GridSizeY { get { return gridSizeY; } }

        int penaltyMin = int.MaxValue;
        int penaltyMax = int.MinValue;

        float offsetX, offsetZ;

        List<Vector2> disappearBarrier = new List<Vector2>();

        PatrolManager patrolManager;

        [HideInInspector]
        public Vector3 MaxBorderPoint;
        [HideInInspector]
        public Vector3 MinBorderPoint;

        public enum DrawType { 
            Weight, BeforeSpread, AfterSpread, GraphConnect
        }
        public DrawType drawType; 
        
        void Awake()
        {
            patrolManager = transform.GetComponent<PatrolManager>();
            nodeDiameter = nodeRadius * 2.0f;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

            foreach (TerrainType region in walkableRegions)
            {
                walkableMask.value |= region.terrainMask.value;
                walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
            }

            patrolManager.InitGridSize(gridSizeX, gridSizeY);
            //patrolManager.CheckExistArea("Border");
            patrolManager.CheckExistArea("UpBorder");
            patrolManager.CheckExistArea("DownBorder");
            patrolManager.CheckExistArea("LeftBorder");
            patrolManager.CheckExistArea("RightBorder");
            CreateGrid();

            offsetX = transform.position.x;
            offsetZ = transform.position.z;

            chooseValue = Mathf.FloorToInt(obstacleProximityPenalty / (blurSize*blurSize)) + chooseOffset;

            MaxBorderPoint = transform.position + 0.5f*new Vector3(gridWorldSize.x,0, gridWorldSize.y);
            MinBorderPoint = transform.position - 0.5f * new Vector3(gridWorldSize.x, 0, gridWorldSize.y);
            Debug.Log(MaxBorderPoint + "   " + MinBorderPoint);
        }
        public void Start()
        {
            
        }

        public int MaxSize
        {
            get
            {
                return gridSizeX * gridSizeY;
            }
        }


        void CreateGrid()
        {
            grid = new Node[gridSizeX, gridSizeY];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

            string areaNmae = string.Empty;
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    //bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                    bool walkable = true;
                    Collider[] hits = Physics.OverlapSphere(worldPoint, nodeRadius, unwalkableMask);
                    if (hits != null && hits.Length > 0)
                    {
                        walkable = false;
                        areaNmae = hits[0].transform.name;//(hits[0].transform.parent == null)?hits[0].transform.name: hits[0].transform.parent.name;
                        if (hits.Length == 0 && hits[0].tag == "disappearBarrier")
                        {
                            disappearBarrier.Add(new Vector2(x, y));
                        }
                    }

                    int movementPenalty = 0;


                    Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100, walkableMask))
                    {
                        walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                    }

                    if (!walkable)
                    {
                        movementPenalty += obstacleProximityPenalty;
                        
                    }
                    grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty, areaNmae);
                    patrolManager.CheckExistArea(areaNmae);

                }
            }

            BlurPenaltyMap();

        }

        void BlurPenaltyMap()
        {
            int kernelSize = blurSize * 2 + 1;
            int kernelExtents = (kernelSize - 1) / 2;

            int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
            int[,] penaltiesVerticalPass = new int[gridSizeX, gridSizeY];


            for (int y = 0; y < gridSizeY; y++)
            {
                //將邊界視為區域
                //if (grid[0, y].walkable) {
                //    grid[0, y].AddArea("Border", 13, patrolManager);
                //    右邊
                //    if (!grid[1, y].walkable) {
                //        grid[0, y].AddArea(grid[1, y].ColliderName, 12, patrolManager);
                //    }
                //    上面
                //    if (y < gridSizeY-1)
                //    {
                //        if (!grid[0, y + 1].walkable) {
                //            grid[0, y].AddArea(grid[0, y + 1].ColliderName, 14, patrolManager);
                //        }
                //        右上
                //        if (!grid[1, y + 1].walkable)
                //        {
                //            grid[0, y].AddArea(grid[1, y + 1].ColliderName, 26, patrolManager);
                //        }
                //    }
                //    else {
                //        grid[0, y].AddArea("Border", 27, patrolManager);
                //    }
                //    下面
                //    if (y > 0)
                //    {
                //        if (!grid[0, y - 1].walkable)
                //        {
                //            grid[0, y].AddArea(grid[0, y - 1].ColliderName, 11, patrolManager);
                //        }
                //        右下
                //        if (!grid[1, y - 1].walkable)
                //        {
                //            grid[0, y].AddArea(grid[1, y - 1].ColliderName, 23, patrolManager);
                //        }
                //    }
                //    else {
                //        grid[0, y].AddArea("Border", 24, patrolManager);
                //    }
                //}
                if (grid[0, y].walkable) {
                    if(y >= 0 && y <= gridSizeY - 1) grid[0, y].AddArea("LeftBorder", 13, patrolManager);
                    //if (y == 0) grid[0, y].AddArea("Border", 24, patrolManager);
                    //else if (y == gridSizeY - 1) grid[0, y].AddArea("Border", 27, patrolManager);
                    //else grid[0, y].AddArea("Border", 13, patrolManager);
                }

                //最左邊處理
                for (int x = -kernelExtents; x <= kernelExtents; x++)
                {
                    int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                    if (grid[0, y].walkable)  //如果最左邊可以走，要確認右邊區域
                    {
                        if (x < 0) penaltiesHorizontalPass[0, y] += obstacleProximityPenalty;// * kernelExtents;
                        else
                        {
                            penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
                        }
                    }
                    else
                    {//不能走，增加權重
                        if (x < 0) penaltiesHorizontalPass[0, y] += obstacleProximityPenalty;
                        else penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
                    }

                }
                for (int x = 1; x < gridSizeX; x++)
                {
                    
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX-1); //x - kernelExtents - 1
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                    if (x + kernelExtents >= gridSizeX) //如果會算到最右邊且可以走的格子，加上一點權重。不要太靠邊
                    {
                        penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + obstacleProximityPenalty;
                    }
                    else {
                        if((x - kernelExtents - 1) < 0) penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - obstacleProximityPenalty + grid[addIndex, y].movementPenalty;
                        else penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
                    }

                    //if (grid[x, y].walkable) {
                    //    for (int i = -1; i <= 1; i++) {
                    //        if (x + i >= gridSizeX)
                    //        {
                    //            if (y == 0) grid[x, y].AddArea("Border", 23, patrolManager);
                    //            else if (y == gridSizeY - 1) grid[x, y].AddArea("Border", 26, patrolManager);
                    //            else grid[x, y].AddArea("Border", 12, patrolManager);
                    //        }
                    //        else {
                    //            for (int j = -1; j <= 1; j++)
                    //            {
                    //                if ((i == 0 && j == 0) || y+j < 0 || y+j > gridSizeY-1) continue;
                    //                if (!grid[x + i, y + j].walkable) {
                    //                    if (i == -1)
                    //                    {
                    //                        if (j == -1) grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 24, patrolManager);
                    //                        else if (j == 0) grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 13, patrolManager);
                    //                        else grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 27, patrolManager);
                    //                    }
                    //                    else if (i == 0)
                    //                    {
                    //                        if (j == -1) grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 11, patrolManager);
                    //                        else grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 14, patrolManager);
                    //                    }
                    //                    else {
                    //                        if (j == -1) grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 23, patrolManager);
                    //                        else if (j == 0) grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 12, patrolManager);
                    //                        else grid[x, y].AddArea(grid[x + i, y + j].ColliderName, 26, patrolManager);
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //    //if ((dir >= 11 && dir <= 14) || (dir >= 23 && dir <= 27 && dir != 25))
                    //    //{
                    //    //    grid[x, y].AddArea(area, dir, false, patrolManager);
                    //    //}
                    //    //else if(grid[x, y].LastAreaName.Length > 0)
                    //    //{
                    //    //    grid[x, y].RemoveArea(patrolManager);
                    //    //}
                    //}

                    if (grid[x, y].walkable && grid[x, y].canChoose) {
                        //左邊有障礙
                        if (!grid[x - 1, y].walkable)
                        {
                            grid[x, y].AddArea(grid[x - 1, y].ColliderName, 13, patrolManager);
                        }
                        //右邊有障礙
                        if (x < gridSizeX - 1)
                        {
                            if (!grid[x + 1, y].walkable)
                            {
                                grid[x, y].AddArea(grid[x + 1, y].ColliderName, 12, patrolManager);
                            }
                        }
                        else {
                            if(y >= 0 && y <= gridSizeY - 1) grid[x, y].AddArea("RightBorder", 12, patrolManager);
                            //if (y == 0) grid[x, y].AddArea("Border", 23, patrolManager);
                            //else if (y == gridSizeY - 1) grid[x, y].AddArea("Border", 26, patrolManager);
                            //else grid[x, y].AddArea("Border", 12, patrolManager);
                        } 
                    }
                }
            }

            for (int x = 0; x < gridSizeX; x++)
            {
                if (grid[x, 0].walkable && grid[x, 0].canChoose)
                {
                    if(x >= 0 && x <= gridSizeX-1) grid[x, 0].AddArea("DownBorder", 11, patrolManager);
                    if (!grid[x, 1].walkable)
                    {
                        grid[x, 0].AddArea(grid[x, 1].ColliderName, 14, patrolManager);
                    }
                }

                //最下面處理
                for (int y = -kernelExtents; y <= kernelExtents; y++)
                {
                    int sampleY = Mathf.Clamp(y, 0, kernelExtents);

                    if (grid[x, 0].walkable)//如果最下面可以走，要確認上面區域
                    {
                        if (y < 0) penaltiesVerticalPass[x, 0] += obstacleProximityPenalty* kernelExtents*2+obstacleProximityPenalty;
                        else
                        {
                            penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
                        }
                    }
                    else //不能走，直接填上面區域為自己碰撞物
                    {
                        if (y < 0) penaltiesVerticalPass[x, 0] += obstacleProximityPenalty * kernelExtents * 2 + obstacleProximityPenalty;// *kernelExtents;
                        else penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
                    }
                }

                //第一列加上先前水平的權重值定除以格數
                int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
                grid[x, 0].movementPenalty = blurredPenalty;
                

                for (int y = 1; y < gridSizeY; y++)
                {
                    int upY = y + kernelExtents;
                    int downY = y - kernelExtents;
                    int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY-1); //y - kernelExtents - 1
                    int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                    if (y + kernelExtents >= gridSizeY) //如果會算到最上面且可以走的格子，加上一點權重。不要太靠邊
                    {
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + obstacleProximityPenalty*kernelExtents*2+ obstacleProximityPenalty;
                    }
                    else
                    {
                        if((y - kernelExtents - 1) < 0) penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - (obstacleProximityPenalty * kernelExtents * 2 + obstacleProximityPenalty) + penaltiesHorizontalPass[x, addIndex];
                        else penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                    }

                    if (grid[x, y].walkable && grid[x, y].canChoose)
                    {
                        //下面有障礙
                        if (!grid[x, y - 1].walkable)
                        {
                            grid[x, y].AddArea(grid[x, y - 1].ColliderName, 11, patrolManager);
                        }
                        //上面有障礙
                        if (y < gridSizeY - 1)
                        {
                            if (!grid[x, y + 1].walkable)
                            {
                                grid[x, y].AddArea(grid[x, y + 1].ColliderName, 14, patrolManager);
                            }
                        }
                        else
                        {
                            if(x >= 0 && x <= gridSizeX - 1) grid[x, y].AddArea("UpBorder", 14, patrolManager);
                        }

                        //斜方判斷
                        if (grid[x, y].canChoose) {
                            for (int i = -1; i <= 1; i += 2)
                            {
                                if (!grid[x, y].canChoose) break;
                                for (int j = -1; j <= 1; j += 2)
                                {
                                    if (!grid[x, y].canChoose) break;
                                    int _x = x + i;
                                    int _y = y + j;
                                    if (_x >= 0 && _x <= gridSizeX - 1 && _y >= 0 && _y <= gridSizeY - 1 && !grid[_x, _y].walkable)
                                    {
                                        if (i == -1)
                                        {
                                            if (j == -1) grid[x, y].AddArea(grid[_x, _y].ColliderName, 24, patrolManager);
                                            else grid[x, y].AddArea(grid[_x, _y].ColliderName, 27, patrolManager);
                                        }
                                        else
                                        {
                                            if (j == -1) grid[x, y].AddArea(grid[_x, _y].ColliderName, 23, patrolManager);
                                            else grid[x, y].AddArea(grid[_x, _y].ColliderName, 26, patrolManager);
                                        }
                                    }
                                }
                            }
                        }
                        
                    }

                    blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                    grid[x, y].movementPenalty = blurredPenalty;
                    //grid[x, y].movementPenalty = (grid[x, y].AreaNum > 1) ? blurredPenalty / grid[x, y].AreaNum : blurredPenalty;

                    if (blurredPenalty > penaltyMax)
                    {
                        penaltyMax = blurredPenalty;
                    }
                    if (blurredPenalty < penaltyMin)
                    {
                        penaltyMin = blurredPenalty;
                    }
                }
            }

        }

        public string GetColliderName(Vector2Int pos) {
            return grid[pos.x, pos.y].ColliderName;
        }

        public void ClearExtendPenalty()
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    grid[x, y].extentPenalty = 0;
                }
            }
        }

        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }


        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            float percentX = (worldPosition.x - offsetX + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.z - offsetZ + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            return grid[x, y];
        }

        public bool CheckInGrid(Vector3 worldPosition) {
            float percentX = (worldPosition.x - offsetX + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.z - offsetZ + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            if (x < 0 || y < 0 || x >= gridSizeX || y >= gridSizeY) return false;
            else return true;
        }

        public Vector3 GetNodePos(int x, int y) {
            return grid[x, y].worldPosition;
        }

        public void DisableCollider()
        {
            foreach (Vector2 xy in disappearBarrier)
            {
                grid[(int)(xy.x), (int)(xy.y)].walkable = true;
            }
        }

        private void Update()
        {
            
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
            if (grid != null && displayGridGizmos)
            {
                foreach (Node n in grid)
                {
                    if (drawType == DrawType.Weight)
                    {
                        Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                        Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;
                        //Gizmos.color = (n.walkable) ? Color.white : Color.red;
                    }
                    else if (drawType == DrawType.BeforeSpread)
                    {
                        if (!n.walkable)
                        {
                            //Gizmos.color = Color.black;
                            Gizmos.color = Color.red;
                            Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
                            continue;
                        }
                        Gizmos.color = Color.white;
                        //Debug.Log(n.locNmae + "  " +  n.Direction);
                        if (n.Direction == 11) Gizmos.color = new Color(1, 0, 0);
                        else if (n.Direction == 12) Gizmos.color = new Color(0, 0, 1);
                        else if (n.Direction == 13) Gizmos.color = new Color(0, 1, 0);
                        else if (n.Direction == 14) Gizmos.color = new Color(0.5f, 0, 0);
                        else if (n.Direction == 23) Gizmos.color = new Color(1, 0, 1);
                        else if (n.Direction == 24) Gizmos.color = new Color(1, 1, 0);
                        else if (n.Direction == 26) Gizmos.color = new Color(0.5f, 0, 0.5f);
                        else if (n.Direction == 27) Gizmos.color = new Color(0.5f, 0.5f, 0f);
                        //if (n.Direction == 11) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 12) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 13) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 14) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 23) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 24) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 26) Gizmos.color = Color.cyan;
                        //else if (n.Direction == 27) Gizmos.color = Color.cyan;
                    }
                    else if (drawType == DrawType.AfterSpread)
                    {
                        Gizmos.color = Color.white;
                        if (!n.walkable) Gizmos.color = Color.black;
                        if (patrolManager.spreadGrid[n.gridX, n.gridY].current) {
                            Gizmos.color = Color.gray;
                            if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(0, 1)) Gizmos.color = new Color(1, 0, 0);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(-1, 0)) Gizmos.color = new Color(0, 0, 1);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(1, 0)) Gizmos.color = new Color(0, 1, 0);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(0, -1)) Gizmos.color = new Color(0.5f, 0, 0);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(-1, 1)) Gizmos.color = new Color(1, 0, 1);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(1, 1)) Gizmos.color = new Color(1, 1, 0);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(-1, -1)) Gizmos.color = new Color(0.5f, 0, 0.5f);
                            else if (patrolManager.spreadGrid[n.gridX, n.gridY].dir == new Vector2Int(1, -1)) Gizmos.color = new Color(0.5f, 0.5f, 0f);
                        } 
                        //if (patrolManager.spreadGrid[n.gridX, n.gridY].close) Gizmos.color = Color.black;
                        if (patrolManager.choosenNodeDic.ContainsKey(new Vector2Int(n.gridX, n.gridY))) {

                            if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count > 2)
                            {
                                if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 3) Gizmos.color = new Color(1, 0, 1);
                                else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 4) Gizmos.color = new Color(0.5f, 0, 0);
                                else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 5) Gizmos.color = new Color(0, 0, 0.5f);
                                else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 6) Gizmos.color = new Color(0.2f, 0.2f, 0.2f);
                                else Gizmos.color = new Color(0, 0, 0);
                            }
                            else {
                                if(patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 2) Gizmos.color = new Color(0, 1, 1);
                                else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 1) Gizmos.color = new Color(0, 0.5f, 0);
                                else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count == 0) Gizmos.color = new Color(0, 0.3f, 0);
                            } 
                        }
                       
                        //if (patrolManager.confirmGraphNodeDic.ContainsKey(new Vector2Int(n.gridX, n.gridY)))
                        //{
                        //    if (patrolManager.confirmGraphNodeDic[new Vector2Int(n.gridX, n.gridY)].crossNode) Gizmos.color = new Color(1, 0, 1);
                        //    else Gizmos.color = Color.red;
                        //}
                    }
                    else if (drawType == DrawType.GraphConnect) {
                        Gizmos.color = Color.white;
                        if (!n.walkable) Gizmos.color = Color.black;
                        //if (patrolManager.spreadGrid[n.gridX, n.gridY].choosen) Gizmos.color = Color.gray;
                        if (patrolManager.choosenNodeDic.ContainsKey(new Vector2Int(n.gridX, n.gridY))) {

                            if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count <= 0)
                            {
                                Gizmos.color = Color.blue;
                            }
                            else {
                                Gizmos.color = new Color(0, 1, 1);
                            }
                            if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].crossNode || patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].beenMerged) Gizmos.color = new Color(1, 0, 1);
                            else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].turnNode) Gizmos.color = Color.yellow;
                            else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].endNode) Gizmos.color = Color.green;
                            //else if (patrolManager.choosenNodeDic[new Vector2Int(n.gridX, n.gridY)].neighbor.Count < 2) Gizmos.color = Color.green;
                        }

                        //if (patrolManager.confirmGraphNodeDic.ContainsKey(new Vector2Int(n.gridX, n.gridY)))
                        //{
                        //    Gizmos.color = Color.red;
                        //}
                        if (patrolManager.connectNeighbor!= null && n.gridX == patrolManager.connectNeighbor.pos.x && n.gridY == patrolManager.connectNeighbor.pos.y) Gizmos.color = Color.gray;
                    }

                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));

                }
               
            }

            if (patrolManager != null) {
                float height = 3;
                for (int i = 0; i < patrolManager.ConfirmGraph.Count; i++)
                {
                    Gizmos.color = Color.red;
                    PatrolManager.PatrolGraphNode node = patrolManager.ConfirmGraph[i];
                    Vector3 from = new Vector3(node.pos.x, height, node.pos.z);

                    foreach (KeyValuePair<PatrolManager.PatrolGraphNode, float> item in node.besideNodes)
                    {
                        Vector3 to = new Vector3(item.Key.pos.x, height, item.Key.pos.z);
                        Gizmos.DrawLine(from, to);
                    }
                    height += 1.0f;
                }
               
                height += 1.0f;
                for (int i = 0; i < patrolManager.patrolPathes.Count; i++) {
                    Gizmos.color = Color.cyan;
                    for (int j = 1; j < patrolManager.patrolPathes[i].CurrentPath.lookPoints.Length; j++)
                    {
                        Vector3 from = new Vector3(patrolManager.patrolPathes[i].CurrentPath.lookPoints[j].x, height, patrolManager.patrolPathes[i].CurrentPath.lookPoints[j].z);
                        Vector3 to = new Vector3(patrolManager.patrolPathes[i].CurrentPath.lookPoints[j-1].x, height, patrolManager.patrolPathes[i].CurrentPath.lookPoints[j-1].z);
                        Gizmos.DrawLine(from, to);
                        if (patrolManager.patrolPathes[i].LookAroundPoints(j) > 0)
                        {
                            Gizmos.DrawSphere(from, 0.5f);
                        }
                    }

                    height += 1.0f;
                }
            }
           
        }

        [System.Serializable]
        public class TerrainType
        {
            public LayerMask terrainMask;
            public int terrainPenalty;
        }

    }
}


