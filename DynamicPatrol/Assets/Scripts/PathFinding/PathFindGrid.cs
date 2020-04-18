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

        public struct Area { 
            public string name;
            public int y;
        }
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

            chooseValue = Mathf.FloorToInt(obstacleProximityPenalty / (blurSize*blurSize)) + chooseOffset;

            
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
                        areaNmae = (hits[0].transform.parent == null)?hits[0].transform.name: hits[0].transform.parent.name;
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
                    grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty, areaNmae, patrolManager.CheckExistArea(areaNmae));


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
                //if (y == 0) grid[0, y].AddArea("Down", 1, true);
                //else if (y == gridSizeY - 1) grid[0, y].AddArea("Up", 2, true);
                grid[0, y].AddArea("Left", 4, true);

                //最左邊處理
                for (int x = -kernelExtents; x <= kernelExtents; x++)
                {
                    int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                    if (grid[0, y].walkable)  //如果最左邊可以走，要確認右邊區域
                    {
                        if (x < 0) penaltiesHorizontalPass[0, y] += obstacleProximityPenalty * kernelExtents;
                        else
                        {
                            penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
                            //最左邊的只要確定右邊第一次碰撞區
                            if (sampleX == 1 && !grid[sampleX, y].walkable)
                            {
                                grid[0, y].AddArea(grid[sampleX, y].AllAreaName, 3, false);
                            }
                        }
                    }
                    else {//不能走，增加權重
                        if (x < 0) penaltiesHorizontalPass[0, y] += obstacleProximityPenalty*kernelExtents;
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
                        penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
                    }

                    if (grid[x, y].walkable) {
                        //左邊有障礙
                        if (!grid[x - 1, y].walkable)
                        {
                            grid[x, y].AddArea(grid[x - 1, y].AllAreaName, 4, false);
                        }
                        //右邊有障礙
                        if (x + 1 <= gridSizeX - 1)
                        {
                            if (!grid[x + 1, y].walkable)
                            {
                                grid[x, y].AddArea(grid[x + 1, y].AllAreaName, 3, false);
                            }
                        }
                        else grid[x, y].AddArea("Right", 3, true);
                    }
                }
            }

            for (int x = 0; x < gridSizeX; x++)
            {
                grid[x, 0].AddArea("Down", 1, true);
                //最下面處理
                for (int y = -kernelExtents; y <= kernelExtents; y++)
                {
                    int sampleY = Mathf.Clamp(y, 0, kernelExtents);

                    if (grid[x, 0].walkable)//如果最下面可以走，要確認上面區域
                    {
                        if (y < 0) penaltiesVerticalPass[x, 0] += obstacleProximityPenalty * kernelExtents;//* kernelExtents;
                        else
                        {
                            penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
                            //最下面的只要確定上面第一次碰撞區
                            if (sampleY == 1 && !grid[x, sampleY].walkable)
                            {
                                grid[x, 0].AddArea(grid[x, sampleY].AllAreaName, 2, false);
                            }
                        }
                    }
                    else //不能走，直接填上面區域為自己碰撞物
                    {
                        if (y < 0) penaltiesVerticalPass[x, 0] += obstacleProximityPenalty * kernelExtents;// *kernelExtents;
                        else penaltiesVerticalPass[x,0] += penaltiesHorizontalPass[x, sampleY];
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
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + obstacleProximityPenalty * kernelExtents;// *kernelExtents;
                    }
                    else
                    {
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                    }

                    if (grid[x, y].walkable) {
                        //下面有障礙
                        if (!grid[x, y - 1].walkable)
                        {
                            grid[x, y].AddArea(grid[x, y - 1].AllAreaName, 1, false);
                        }
                        //上面有障礙
                        if (y + 1 <= gridSizeY - 1)
                        {
                            if (!grid[x, y + 1].walkable)
                                grid[x, y].AddArea(grid[x, y + 1].AllAreaName, 2, false);
                        }
                        else grid[x, y].AddArea("Up", 2, true);
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
                    //if (!n.walkable)
                    //{
                    //    Gizmos.color = Color.black;
                    //    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
                    //    continue;
                    //}
                    //Gizmos.color = Color.white;
                    //if (n.AreaNum > 0)
                    //{
                    //    if (n.AreaNum == 1)
                    //    {
                    //        if (n.dirr == 1) Gizmos.color = Color.blue;
                    //        else if (n.dirr == 2) Gizmos.color = Color.yellow;
                    //        else if (n.dirr == 3) Gizmos.color = Color.red;
                    //        else if (n.dirr == 4) Gizmos.color = Color.green;
                    //    }
                    //    else
                    //    {
                    //        Gizmos.color = Color.cyan;
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


