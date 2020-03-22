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

        float nodeDiameter;
        int gridSizeX, gridSizeY;

        int penaltyMin = int.MaxValue;
        int penaltyMax = int.MinValue;

        float offsetX, offsetY;

        List<Vector2> disappearBarrier = new List<Vector2>();

        PatrolManager patrolManager;

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

            CreateGrid();
            offsetX = transform.position.x;
            offsetY = transform.position.y;

            chooseOffset = Mathf.FloorToInt(obstacleProximityPenalty / (blurSize*blurSize));

            
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
                        areaNmae = hits[0].transform.name;
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
                int areaNum = 0;
                string[] lastAreaNmae = new string[2] { "E" , "E" } ;   //左,右
                int[] lastAreaX = new int[2] { int.MaxValue, int.MaxValue }; //左右

                //最左邊處理
                for (int x = -kernelExtents; x <= kernelExtents; x++)
                {
                    
                    int sampleX = Mathf.Clamp(x, 0, kernelExtents);

                    if (grid[0, y].walkable)  //如果最左邊可以走，要確認右邊區域
                    {
                        if (x < 0) penaltiesHorizontalPass[0, y] += obstacleProximityPenalty;
                        else
                        {
                            penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
                            //最左邊的只要確定右邊第一次碰撞區
                            if (areaNum == 0 && !grid[sampleX, y].walkable)
                            {
                                lastAreaNmae[1] = grid[sampleX, y].colliderName;
                                lastAreaX[1] = sampleX;
                                areaNum++;
                                //Debug.Log("0," + y + "  右邊有障礙物 " + lastAreaNmae[1]);
                                grid[0, y].AddArea(patrolManager.AddPatrolArea(lastAreaNmae[1]), 3);
                            }
                            //Debug.Log(x + "," + y + "  walkable " + grid[sampleX,y].walkable);
                        }
                        
                    }
                    else {//不能走，直接填右邊區域為自己碰撞物
                        if (areaNum == 0) {
                            lastAreaNmae[1] = grid[0, y].colliderName;
                            lastAreaX[1] = 0;
                            areaNum++;
                        }
                        if (x < 0) penaltiesHorizontalPass[0, y] += obstacleProximityPenalty;
                        else penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
                    }
                }

                for (int x = 1; x < gridSizeX; x++)
                {
                    
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                    if (x + kernelExtents >= gridSizeX && grid[addIndex, y].walkable) //如果會算到最右邊且可以走的格子，加上一點權重。不要太靠邊
                    {
                        penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + obstacleProximityPenalty;
                    }
                    else {
                        penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
                    }

                    if (!grid[x, y].walkable) continue;
                    // 從一障礙物邊界出來將原本在右邊的變左邊
                    if (lastAreaX[1] < x)
                    {
                        //if (lastAreaNmae[1].CompareTo(grid[x - 1, y].colliderName) != 0) {
                        //    //如果有兩個碰撞形成原本右邊的區域，將之後最左邊的名字填入
                        //    lastAreaNmae[1] = grid[x - 1, y].colliderName;
                        //    lastAreaX[1] = x - 1;
                        //}
                        lastAreaNmae[0] = grid[x - 1, y].colliderName;
                        lastAreaX[0] = x - 1;
                        lastAreaNmae[1] = "E";
                        lastAreaX[1] = int.MaxValue;
                        areaNum--;
                    }
                    else
                    { //只要不是從一區域剛出來，要看有沒有離開左邊區域
                        if (!grid[removeIndex, y].walkable && grid[removeIndex + 1, y].walkable && lastAreaX[0] <= removeIndex)
                        {
                            areaNum--;
                            lastAreaNmae[0] = "E";
                            lastAreaX[0] = int.MaxValue;
                        }
                    }

                    //右邊新的區域
                    if (!grid[addIndex, y].walkable && lastAreaNmae[1].CompareTo("E") == 0)
                    {
                        lastAreaNmae[1] = grid[addIndex, y].colliderName;
                        lastAreaX[1] = addIndex;
                        areaNum++;
                    }

                    if (lastAreaNmae[0].CompareTo("E") != 0)grid[x, y].AddArea(patrolManager.AddPatrolArea(lastAreaNmae[0]), 4);
                    if (lastAreaNmae[1].CompareTo("E") != 0) grid[x, y].AddArea(patrolManager.AddPatrolArea(lastAreaNmae[1]), 3);
                }
            }

            for (int x = 0; x < gridSizeX; x++)
            {
                int areaNum = 0;
                string[] lastAreaNmae = new string[2] { "E", "E" };   //上,下
                int[] lastAreaY = new int[2] { int.MaxValue, int.MaxValue }; //上,下

                //最下面處理
                for (int y = -kernelExtents; y <= kernelExtents; y++)
                {
                    int sampleY = Mathf.Clamp(y, 0, kernelExtents);

                    if (grid[x, 0].walkable)//如果最下面可以走，要確認上面區域
                    {
                        if (y < 0) penaltiesVerticalPass[x,0] += obstacleProximityPenalty * kernelExtents;
                        else
                        {
                            penaltiesVerticalPass[x,0] += penaltiesHorizontalPass[x,sampleY];
                            //最下面的只要確定上面第一次碰撞區
                            if (areaNum == 0 && !grid[x, sampleY].walkable)
                            {
                                lastAreaNmae[0] = grid[x,sampleY].colliderName;
                                lastAreaY[0] = sampleY;
                                areaNum++;
                                grid[x,0].AddArea(patrolManager.AddPatrolArea(lastAreaNmae[0]), 2);
                                Debug.Log(x + ",0" + "  上面有障礙物 " + lastAreaNmae[0]);
                            }
                            Debug.Log(x +"," + y + "  walkable " + grid[x, sampleY].walkable);
                        }
                    }
                    else //不能走，直接填上面區域為自己碰撞物
                    {
                        if (areaNum == 0)
                        {
                            lastAreaNmae[0] = grid[x,0].colliderName;
                            lastAreaY[0] = 0;
                            areaNum++;
                        }
                        if (y < 0) penaltiesVerticalPass[x, 0] += obstacleProximityPenalty*kernelExtents;
                        else penaltiesVerticalPass[x,0] += penaltiesHorizontalPass[x, sampleY];
                    }

                }

                //第一列加上先前水平的權重值定除以格數
                int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
                grid[x, 0].movementPenalty = blurredPenalty;

                for (int y = 1; y < gridSizeY; y++)
                {
                    
                    int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                    int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                    if (y + kernelExtents >= gridSizeY && grid[x, addIndex].walkable) //如果會算到最上面且可以走的格子，加上一點權重。不要太靠邊
                    {
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y-1] - penaltiesHorizontalPass[x, removeIndex] + obstacleProximityPenalty*kernelExtents;
                    }
                    else
                    {
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                    }

                    if (!grid[x, y].walkable) continue;
                    // 從一障礙物邊界出來將原本在上面的變下面
                    if (lastAreaY[0] < y)
                    {
                        //if (lastAreaNmae[1].CompareTo(grid[x, y-1].colliderName) != 0)
                        //{
                        //    //如果有兩個碰撞形成原本上面的區域，將之後下面那格的名字填入
                        //    lastAreaNmae[1] = grid[x, y-1].colliderName;
                        //    lastAreaY[1] = y - 1;
                        //}
                        lastAreaNmae[1] = grid[x, y - 1].colliderName;
                        lastAreaY[1] = y - 1;
                        lastAreaNmae[0] = "E";
                        lastAreaY[0] = int.MaxValue;
                        areaNum--;
                    }
                    else
                    { //只要不是從一區域剛出來，要看有沒有離開下面區域
                        if (!grid[x, removeIndex].walkable && grid[x, removeIndex + 1].walkable && lastAreaY[1] <= removeIndex)
                        {
                            areaNum--;
                            lastAreaNmae[1] = "E";
                            lastAreaY[1] = int.MaxValue;
                        }
                    }

                    //上面新的區域
                    if (!grid[x, addIndex].walkable && lastAreaNmae[0].CompareTo("E") == 0)
                    {
                        lastAreaNmae[0] = grid[x, addIndex].colliderName;
                        lastAreaY[0] = addIndex;
                        areaNum++;
                    }

                    if (lastAreaNmae[0].CompareTo("E") != 0) grid[x, y].AddArea(patrolManager.AddPatrolArea(lastAreaNmae[0]), 2);
                    if (lastAreaNmae[1].CompareTo("E") != 0) grid[x, y].AddArea(patrolManager.AddPatrolArea(lastAreaNmae[1]), 1);

                    blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                    grid[x, y].movementPenalty = blurredPenalty;

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
            float percentY = (worldPosition.z - offsetY + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            return grid[x, y];
        }

        public bool CheckInGrid(Vector3 worldPosition) {
            float percentX = (worldPosition.x - offsetX + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.z - offsetY + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            if (x < 0 || y < 0 || x >= gridSizeX || y >= gridSizeY) return false;
            else return true;
        }

        public void DisableCollider()
        {
            foreach (Vector2 xy in disappearBarrier)
            {
                grid[(int)(xy.x), (int)(xy.y)].walkable = true;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
            if (grid != null && displayGridGizmos)
            {
                foreach (Node n in grid)
                {
                    //Gizmos.color = Color.white;
                    //if (n.AreaNum > 0)
                    //{
                    //    if (n.AreaNum == 1)
                    //    {
                    //        if (n.dirr == 1) Gizmos.color = Color.blue;
                    //        else if(n.dirr == 2) Gizmos.color = Color.yellow;
                    //        else if (n.dirr == 3) Gizmos.color = Color.red;
                    //        else if (n.dirr == 4) Gizmos.color = Color.green;
                    //    }
                    //    else {
                    //        Gizmos.color = Color.grey;
                    //    }
                    //}
                    

                    Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                    Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
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


