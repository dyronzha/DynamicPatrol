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
                    grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty, patrolManager.CheckExistArea(areaNmae));


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
                string[] lastAreaNmae = new string[2] { string.Empty , string.Empty } ;   //左,右
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
                                grid[0, y].AddArea(lastAreaNmae[1], 3);
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
                //grid[0, y].borderNum++; //最左邊 +1 border

                for (int x = 1; x < gridSizeX; x++)
                {
                    
                    int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX-1); //x - kernelExtents - 1
                    int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                    if (x + kernelExtents >= gridSizeX && grid[addIndex, y].walkable) //如果會算到最右邊且可以走的格子，加上一點權重。不要太靠邊
                    {
                        penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + obstacleProximityPenalty;
                        //grid[x, y].borderNum++; //最右邊 +1 border
                    }
                    else {
                        penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
                    }

                    if (!grid[x, y].walkable) {
                        //所在格子在障礙物裡要判斷右邊有沒有新障礙物
                        if (!grid[addIndex, y].walkable && lastAreaNmae[1].CompareTo(grid[addIndex, y].AllAreaName) != 0)
                        {
                            // 將原本在右邊的變左邊
                            lastAreaNmae[0] = lastAreaNmae[1];
                            lastAreaX[0] = x;
                            areaNum--;
                            
                            //右邊填入新的
                            lastAreaNmae[1] = grid[addIndex, y].AllAreaName;
                            lastAreaX[1] = addIndex;
                            areaNum++;

                            if (lastAreaNmae[0].CompareTo(string.Empty) != 0) grid[x, y].AddArea(lastAreaNmae[0], 4);
                            if (lastAreaNmae[1].CompareTo(string.Empty) != 0) grid[x, y].AddArea(lastAreaNmae[1], 3);
                        }
                        continue;
                    } 

                    // 從一障礙物邊界出來將原本在右邊的變左邊
                    if (lastAreaX[1] < x)
                    {
                        lastAreaNmae[0] = grid[x - 1, y].colliderName;
                        lastAreaX[0] = x - 1;
                        lastAreaNmae[1] = string.Empty;
                        lastAreaX[1] = int.MaxValue;
                        areaNum--;
                    }
                    else
                    { //只要不是從一區域剛出來，要看有沒有離開左邊區域
                        if (!grid[removeIndex, y].walkable && grid[removeIndex + 1, y].walkable && lastAreaX[0] <= removeIndex)
                        {
                            areaNum--;
                            lastAreaNmae[0] = string.Empty;
                            lastAreaX[0] = int.MaxValue;
                        }
                    }

                    //右邊新的區域
                    if (!grid[addIndex, y].walkable && lastAreaNmae[1].Length == 0)
                    {
                        lastAreaNmae[1] = grid[addIndex, y].colliderName;
                        lastAreaX[1] = addIndex;
                        areaNum++;
                    }

                    if (lastAreaNmae[0].Length > 0) {
                        string t = lastAreaNmae[0];
                        Debug.Log("左邊有 " + t);
                        grid[x, y].AddArea(t, 4);
                    }
                    
                    if (lastAreaNmae[1].Length > 0) grid[x, y].AddArea(lastAreaNmae[1], 3);
                }
            }

            for (int x = 0; x < gridSizeX; x++)
            {
                int areaNum = 0;
                string[] lastAreaNmae = new string[2] { string.Empty, string.Empty };   //上,下
                int[] lastAreaY = new int[2] { int.MaxValue, int.MaxValue }; //上,下
                string compareTiltArea = string.Empty;
                string lastTiltArea = string.Empty;
                Dictionary<string, int> lastAllAreas = new Dictionary<string, int>();
                List<int> lastAreaTiltY = new List<int>();

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
                            if (areaNum == 0) {
                                if (!grid[x, sampleY].walkable) {
                                    lastAreaNmae[0] = grid[x, sampleY].colliderName;
                                    lastAreaY[0] = sampleY;
                                    areaNum++;
                                    grid[x, 0].AddArea(lastAreaNmae[0], 2);
                                    Debug.Log(x + ",0" + "  在" + sampleY + "有障礙物 " + lastAreaNmae[0]);
                                }
                            }

                            //Debug.Log(x + "," + y + "  walkable " + grid[x, sampleY].walkable);
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

                    //斜方區域，判斷上方新區域
                    Debug.Log(x + "," + sampleY +  "  new " + grid[x, sampleY].AllAreaName + "  last" + lastTiltArea);
                    if (compareTiltArea.CompareTo(grid[x, sampleY].AllAreaName) != 0)  //grid[x, sampleY].walkable && 
                    {
                        compareTiltArea = grid[x, sampleY].AllAreaName;
                        if (!lastTiltArea.Contains(grid[x, sampleY].AllAreaName))
                        {
                            //確認新區域沒有在lastAllAreas
                            foreach (KeyValuePair<string, int> item in grid[x, sampleY].AllAreaInfos)
                            {
                                if (!lastAllAreas.ContainsKey(item.Key))
                                {
                                    lastAllAreas.Add(item.Key, sampleY);
                                    if (lastTiltArea.Length == 0) lastTiltArea = item.Key;
                                    else lastTiltArea += item.Key;
                                }
                            }
                        }
                        //新區域都有被包含，紀錄位置
                        else lastAreaTiltY.Add(sampleY);

                    }
                }
                //判斷新區域的區域有哪些是有的
                grid[x, 0].AddAreas(lastTiltArea, lastAllAreas);  
                //grid[x, 0].borderNum++; //最下面 +1 border

                //第一列加上先前水平的權重值定除以格數
                int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
                grid[x, 0].movementPenalty = (grid[x,0].AreaNum > 1)?blurredPenalty/ grid[x, 0].AreaNum: blurredPenalty;
                

                for (int y = 1; y < gridSizeY; y++)
                {
                    int upY = y + kernelExtents;
                    int downY = y - kernelExtents;
                    int removeIndex = Mathf.Clamp(downY - 1, 0, gridSizeY-1); //y - kernelExtents - 1
                    int addIndex = Mathf.Clamp(upY, 0, gridSizeY - 1);

                    if (upY >= gridSizeY && grid[x, addIndex].walkable) //如果會算到最上面且可以走的格子，加上一點權重。不要太靠邊
                    {
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y-1] - penaltiesHorizontalPass[x, removeIndex] + obstacleProximityPenalty*kernelExtents;
                        //grid[x, y].borderNum++; //最上面 +1 border
                    }
                    else
                    {
                        penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                    }

                    //斜方區域，判斷下方離開區域
                    if (downY >= 0 && (lastAreaTiltY.Count>0 && removeIndex >= lastAreaTiltY[0] - 1))
                    {
                        Debug.Log(x + "," + y + "判斷移除 " + removeIndex);
                        lastAreaTiltY.RemoveAt(0);
                        grid[x, removeIndex].CheckRemoveAreas(grid[x, y].AllAreaInfos, ref lastAllAreas, ref lastTiltArea);
                        foreach (KeyValuePair<string, int> item in lastAllAreas)
                        {
                            Debug.Log("剩下 " + item.Key);
                        }
                    }
                    //斜方區域，判斷上方新區域，先跟舊的lastAreaTilt比，看是不是遇到新區域
                    Debug.Log(x + "," + addIndex + "  new " + grid[x, addIndex].AllAreaName + "  last" + compareTiltArea);
                    if (upY < gridSizeY && compareTiltArea.CompareTo(grid[x, addIndex].AllAreaName) != 0) // grid[x, addIndex].walkable &&
                    {
                        compareTiltArea = grid[x, addIndex].AllAreaName;
                        Debug.Log(lastTiltArea + " contain " + grid[x, addIndex].AllAreaName + "  is " + lastTiltArea.Contains(grid[x, addIndex].AllAreaName));
                        if (!lastTiltArea.Contains(grid[x, addIndex].AllAreaName))
                        {
                            //確認新區域有沒有在lastAllAreas
                            foreach (KeyValuePair<string, int> item in grid[x, addIndex].AllAreaInfos)
                            {
                                if (!lastAllAreas.ContainsKey(item.Key))
                                {
                                    lastAllAreas.Add(item.Key, addIndex);
                                    if (lastTiltArea.Length == 0) lastTiltArea = item.Key;
                                    else lastTiltArea += item.Key;
                                }
                            }
                            Debug.Log("目前包含區域 " + lastTiltArea);
                        }
                        //新區域都有被包含，紀錄位置
                        else {
                            Debug.Log(addIndex + "為 removeIndex");
                            lastAreaTiltY.Add(addIndex);
                        } 
                    }
                    //判斷新區域的區域有哪些是有的 377

                    if (!grid[x, y].walkable) {
                        //所在格子在障礙物裡要判斷上面有沒有新障礙物
                        if (!grid[x, addIndex].walkable)
                        {
                            Debug.Log(x + "," + y + "------- add " + grid[x, addIndex].AllAreaName);
                            if (lastAreaNmae[0].CompareTo(grid[x, addIndex].AllAreaName) != 0)
                            {
                                //將原本在上面的變下面
                                lastAreaNmae[1] = lastAreaNmae[0];
                                lastAreaY[1] = y;
                                areaNum--;

                                lastAreaNmae[0] = grid[x, addIndex].colliderName;
                                lastAreaY[0] = addIndex;
                                areaNum++;

                                if (lastAreaNmae[0].CompareTo(string.Empty) != 0) grid[x, y].AddArea(lastAreaNmae[0], 2);
                                if (lastAreaNmae[1].CompareTo(string.Empty) != 0) grid[x, y].AddArea(lastAreaNmae[1], 1);
                            }
                            //if (grid[x, addIndex].allAreaName.CompareTo(lastAreaNmae[0]) != 0) {
                            //    lastAreaNmae[0] = grid[x, addIndex].allAreaName;
                            //}
                        }   
                        continue;
                    }

                    //判斷新區域的區域有哪些是有的
                    //傳入新的lastAreaTilt，可以快速比較跟目前x,y的一不一樣
                    grid[x, y].AddAreas(lastTiltArea, lastAllAreas);

                    // 從一障礙物邊界出來將原本在上面的變下面
                    if (lastAreaY[0] < y)
                    {
                        lastAreaNmae[1] = grid[x, y - 1].colliderName;
                        lastAreaY[1] = y - 1;
                        lastAreaNmae[0] = string.Empty;
                        lastAreaY[0] = int.MaxValue;
                        areaNum--;
                    }
                    else
                    { //只要不是從一區域剛出來，要看有沒有離開下面區域
                        if (!grid[x, removeIndex].walkable && grid[x, removeIndex + 1].walkable && lastAreaY[1] <= removeIndex)
                        {
                            areaNum--;
                            lastAreaNmae[1] = string.Empty;
                            lastAreaY[1] = int.MaxValue;
                        }
                    }

                    //上面新的區域
                    if (!grid[x, addIndex].walkable && lastAreaNmae[0].Length == 0)
                    {
                        lastAreaNmae[0] = grid[x, addIndex].colliderName;
                        lastAreaY[0] = addIndex;
                        areaNum++;
                    }

                    if (lastAreaNmae[0].Length != 0) grid[x, y].AddArea(lastAreaNmae[0], 2);
                    if (lastAreaNmae[1].Length != 0) grid[x, y].AddArea(lastAreaNmae[1], 1);

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
                    if (!n.walkable)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
                        continue;
                    }
                    Gizmos.color = Color.white;
                    if (n.AreaNum > 0)
                    {
                        if (n.AreaNum == 1)
                        {
                            if (n.dirr == 1) Gizmos.color = Color.blue;
                            else if (n.dirr == 2) Gizmos.color = Color.yellow;
                            else if (n.dirr == 3) Gizmos.color = Color.red;
                            else if (n.dirr == 4) Gizmos.color = Color.green;
                        }
                        else
                        {
                            Gizmos.color = Color.cyan;
                        }
                    }


                    //Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                    //Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;
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


