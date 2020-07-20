using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int dataNum = 0;
    public Camera MainCamera;
    public EnemyManager enemyManager;
    public Player player;
    public int playerTotalLife = 3;
    public Transform GameMap;
    public UnityEngine.UI.Text canvasInfo;
    public UnityEngine.UI.Image canvasInfoBG;
    public UnityEngine.UI.Text playerLife;
    public LayerMask goalLayerMask;
    public LayerMask startLayerMask;
    
    bool init = false, dynamicFirst = true, hasGoal = false;
    int mapCount = 0, allMapNum = 0;
    Transform[] gameMaps;
    Vector3[] startPos, exitPos;
    PatrolManager[] mapPatrolManager;
    GameObject[] exitObject;
    Animator canvasAnimator;

    bool hasStart = false;
    public static bool pause = true;
    public static int dynamicPatolChange = 0;
    public int hasCreatPath = 0;

    int playerDeathCount = 0;

    System.Action blackEndCBK, blackShowCBK;
    void Blank() { }

    [HeaderAttribute("Test Value")]
    public bool InTest = false;
    public Enemy testEnemy;
    public Player testPlayer;

    PlayInfomation playInfomation;
    float roundTime = .0f;
    int roundDeadNum = 0;

    // Start is called before the first frame update
    private void Awake()
    {
        canvasAnimator = GetComponent<Animator>();

        Transform dynamicMaps = GameMap.Find("DynamicMaps");
        Transform normalMaps = GameMap.Find("NormalMaps");
        int activeMapCount = 0;
        for (int i = 0; i < dynamicMaps.childCount; i++) {
            if (!dynamicMaps.GetChild(i).gameObject.activeSelf) continue;
            activeMapCount++;
        }
        for (int i = 0; i < normalMaps.childCount; i++)
        {
            if (!normalMaps.GetChild(i).gameObject.activeSelf) continue;
            activeMapCount++;
        }

        gameMaps = new Transform[activeMapCount];
        startPos = new Vector3[gameMaps.Length];
        exitPos = new Vector3[gameMaps.Length];
        mapPatrolManager = new PatrolManager[gameMaps.Length];
        exitObject = new GameObject[gameMaps.Length];

        bool r = (Random.Range(0, 10) >= 5) ? true : false;// false; //= (Random.Range(0, 10) >= 5) ? true : false;
        if (r)
        {
            for (int i = 0; i < dynamicMaps.childCount; i++)
            {
                if (!dynamicMaps.GetChild(i).gameObject.activeSelf) continue;
                Debug.Log(GameMap.GetChild(i).name);
                gameMaps[allMapNum] = dynamicMaps.GetChild(i);
                startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
                exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
                exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
                mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
                mapPatrolManager[allMapNum].InTest = InTest;
                mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
                allMapNum++;

                if (!normalMaps.GetChild(i).gameObject.activeSelf) continue;
                Debug.Log(GameMap.GetChild(i).name);
                gameMaps[allMapNum] = normalMaps.GetChild(i);
                startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
                exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
                exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
                mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
                mapPatrolManager[allMapNum].InTest = InTest;
                mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
                allMapNum++;
            }
            //for (int i = 0; i < dynamicMaps.childCount; i++)
            //{
            //    if (!dynamicMaps.GetChild(i).gameObject.activeSelf) continue;
            //    Debug.Log(GameMap.GetChild(i).name);
            //    gameMaps[allMapNum] = dynamicMaps.GetChild(i);
            //    startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
            //    exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
            //    exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
            //    mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
            //    mapPatrolManager[allMapNum].InTest = InTest;
            //    mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
            //    allMapNum++;
            //}
            //for (int i = 0; i < normalMaps.childCount; i++)
            //{
            //    if (!normalMaps.GetChild(i).gameObject.activeSelf) continue;
            //    Debug.Log(GameMap.GetChild(i).name);
            //    gameMaps[allMapNum] = normalMaps.GetChild(i);
            //    startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
            //    exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
            //    exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
            //    mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
            //    mapPatrolManager[allMapNum].InTest = InTest;
            //    mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
            //    allMapNum++;
            //}
        }
        else {
            for (int i = 0; i < dynamicMaps.childCount; i++)
            {
                if (!normalMaps.GetChild(i).gameObject.activeSelf) continue;
                Debug.Log(GameMap.GetChild(i).name);
                gameMaps[allMapNum] = normalMaps.GetChild(i);
                startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
                exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
                exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
                mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
                mapPatrolManager[allMapNum].InTest = InTest;
                mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
                allMapNum++;
                if (!dynamicMaps.GetChild(i).gameObject.activeSelf) continue;
                Debug.Log(GameMap.GetChild(i).name);
                gameMaps[allMapNum] = dynamicMaps.GetChild(i);
                startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
                exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
                exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
                mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
                mapPatrolManager[allMapNum].InTest = InTest;
                mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
                allMapNum++;
            }
            //for (int i = 0; i < normalMaps.childCount; i++)
            //{
            //    if (!normalMaps.GetChild(i).gameObject.activeSelf) continue;
            //    Debug.Log(GameMap.GetChild(i).name);
            //    gameMaps[allMapNum] = normalMaps.GetChild(i);
            //    startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
            //    exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
            //    exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
            //    mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
            //    mapPatrolManager[allMapNum].InTest = InTest;
            //    mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
            //    allMapNum++;
            //}
            //for (int i = 0; i < dynamicMaps.childCount; i++)
            //{
            //    if (!dynamicMaps.GetChild(i).gameObject.activeSelf) continue;
            //    Debug.Log(GameMap.GetChild(i).name);
            //    gameMaps[allMapNum] = dynamicMaps.GetChild(i);
            //    startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
            //    exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
            //    exitObject[allMapNum] = gameMaps[allMapNum].Find("Exit").GetChild(0).gameObject;
            //    mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
            //    mapPatrolManager[allMapNum].InTest = InTest;
            //    mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
            //    allMapNum++;
            //}
        }
        blackEndCBK = Blank; //ShowInfo;
        blackShowCBK = Blank;
        canvasInfo.enabled = false;
        canvasInfoBG.enabled = false;

        playInfomation = new PlayInfomation(gameMaps.Length/2);
        playInfomation.dynamicFirst = r;
    }
    void Start()
    {
        if (InTest) {
            pause = false;
            canvasAnimator.Play("BlackFadeIn");
        }

        player.transform.position = startPos[mapCount];
        MainCamera.transform.position = new Vector3(mapPatrolManager[mapCount].transform.position.x, MainCamera.transform.position.y, mapPatrolManager[mapCount].transform.position.z);
        playerLife.text = "Life: " + (playerTotalLife - playerDeathCount).ToString();

        //playInfomation.SetTime(true, 0, 50.0f);
        //playInfomation.SetTime(false, 0, 10.0f);
        //playInfomation.CountDeadNum(true, 1);
        //playInfomation.CountDeadNum(false, 1);
        //playInfomation.GetThrowNum(true, 2,1);
        //playInfomation.GetThrowNum(false,2,1);
        //string potion = JsonUtility.ToJson(playInfomation, true);
        //System.IO.File.WriteAllText(Application.dataPath + "/StreamingAssets" + "/PlayInfomation" + dataNum.ToString() + ".json", potion);
    }

    // Update is called once per frame
    void Update()
    {
        if (InTest)
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                //RequestDynamicPatrol(new DynamicPatrolRequest(false, new Vector3(0,0,0), testEnemy),testPlayer.position, testEnemy);
                if (testEnemy != null) testEnemy.InTestSetSearch(testPlayer.position);
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (enemyManager.CheckEnemyChangePathCount())
                {
                    enemyManager.ChangeAllEnemyPath(testEnemy);
                }
            }
            if (!init)
            {
                if (hasCreatPath >= allMapNum)
                {
                    init = true;
                    mapPatrolManager[mapCount].SpawnEnemy();
                }
            }
            if(Input.GetKeyDown(KeyCode.Space)) player.SetBorder(mapPatrolManager[mapCount].PathGrid.MinBorderPoint, mapPatrolManager[mapCount].PathGrid.MaxBorderPoint);
            if (Input.GetKeyDown(KeyCode.Escape) && SceneManager.GetActiveScene().buildIndex == 0) {
                pause = true;
                SceneManager.LoadScene(1);
            }
            return;
        }

        if (!init)
        {
            if (hasCreatPath >= allMapNum) {
                init = true;
                mapPatrolManager[mapCount].SpawnEnemy();
                player.SetBorder(mapPatrolManager[mapCount].PathGrid.MinBorderPoint, mapPatrolManager[mapCount].PathGrid.MaxBorderPoint);
                canvasAnimator.Play("BlackFadeIn");
                canvasInfo.enabled = true;
                canvasInfoBG.enabled = true;
            }
        }
        else {
            if (!hasStart)
            {
                if (Input.GetKeyDown(KeyCode.Space) || InTest)
                {
                    hasStart = true;
                    pause = false;
                    canvasInfo.enabled = false;
                    canvasInfoBG.enabled = false;
                }
            }
            else {
                if (Input.GetKeyDown(KeyCode.Escape)) pause = !pause;
                if (pause) return;
                DetectPlayerGoal();
                roundTime += Time.deltaTime;
            }
            
        }
        if (Input.GetKeyDown(KeyCode.I)) NextRound();

       
    }

    void DetectPlayerGoal()
    {
        if (!hasGoal)
        {
            Collider[] hits = Physics.OverlapSphere(player.transform.position, 0.5f, goalLayerMask);
            if (hits != null && hits.Length > 0)
            {
                hasGoal = true;
                exitObject[mapCount].SetActive(false);
                player.ResetThrowthing();
            }
        }
        else
        {
            Collider[] hits = Physics.OverlapSphere(player.transform.position, 0.5f, startLayerMask);
            if (hits != null && hits.Length > 0)
            {
                if (mapCount < gameMaps.Length - 1)
                {
                    pause = true;
                    hasGoal = false;
                    canvasInfo.text = "Next Round";
                    canvasInfo.enabled = true;
                    canvasInfoBG.enabled = true;
                    blackShowCBK = SkipToNextRound;
                    canvasAnimator.Play("BlackFadeOut");
                    playInfomation.SetTime(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, roundTime);
                    roundTime = .0f;
                    playerDeathCount = 0;
                    playInfomation.GetThrowNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, player.GetHasThrowNum);
                    player.ResetThrowthing();
                }
                else
                {
                    //遊戲結束
                    pause = true;
                    hasGoal = false;
                    canvasInfo.text = "Congratulations";
                    blackShowCBK = Blank;
                    canvasInfo.enabled = true;
                    canvasInfoBG.enabled = true;
                    //blackShowCBK = SkipToNextRound;
                    canvasAnimator.Play("BlackFadeOut");
                    playInfomation.SetTime(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, roundTime);
                    roundTime = .0f;
                    playerDeathCount = 0;
                    playInfomation.GetThrowNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, player.GetHasThrowNum);
                    player.ResetThrowthing();
                    playInfomation.SetDynamicPatrolNum(dynamicPatolChange);

                    string potion = JsonUtility.ToJson(playInfomation, true);
                    System.IO.File.WriteAllText(Application.dataPath + "/StreamingAssets" + "/PlayInfomation_2-" + dataNum.ToString() + ".json", potion);
                }
            }
        }
    }

    void NextRound() {
        if (mapCount < gameMaps.Length - 1)
        {
            pause = true;
            hasGoal = false;
            canvasInfo.text = "Next Round";
            canvasInfo.enabled = true;
            canvasInfoBG.enabled = true;
            blackShowCBK = SkipToNextRound;
            canvasAnimator.Play("BlackFadeOut");
            playInfomation.SetTime(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, roundTime);
            roundTime = .0f;
            playerDeathCount = 0;
            playInfomation.GetThrowNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, player.GetHasThrowNum);
            player.ResetThrowthing();
        }
        else {
            //遊戲結束
            pause = true;
            hasGoal = false;
            canvasInfo.text = "Congratulations";
            blackShowCBK = Blank;
            canvasInfo.enabled = true;
            canvasInfoBG.enabled = true;
            //blackShowCBK = SkipToNextRound;
            canvasAnimator.Play("BlackFadeOut");
            playInfomation.SetTime(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, roundTime);
            roundTime = .0f;
            playerDeathCount = 0;
            playInfomation.GetThrowNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, player.GetHasThrowNum);
            player.ResetThrowthing();
            playInfomation.SetDynamicPatrolNum(dynamicPatolChange);

            string potion = JsonUtility.ToJson(playInfomation, true);
            System.IO.File.WriteAllText(Application.dataPath + "/StreamingAssets" + "/PlayInfomation_2-" + dataNum.ToString() + ".json", potion);
        }
    }

    public void CountPlayerDead() {
        playerDeathCount++;
        playerLife.text = "Life: " + (playerTotalLife - playerDeathCount).ToString();
        if (playerDeathCount >= playerTotalLife)
        {
            if (mapCount < gameMaps.Length - 1) {
                pause = true;
                hasGoal = false;
                exitObject[mapCount].SetActive(true);
                canvasInfo.text = "Next Round";
                canvasInfo.enabled = true;
                canvasInfoBG.enabled = true;
                blackShowCBK = SkipToNextRound;
                //blackEndCBK = BlackEndPlay;
                canvasAnimator.Play("BlackFadeOut");
                playInfomation.SetTime(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, -roundTime);
                roundTime = .0f;
                playInfomation.CountDeadNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount);
                playerDeathCount = 0;
                playInfomation.GetThrowNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, player.GetHasThrowNum);
                player.ResetThrowthing();
            }
            else
            {
                pause = true;
                canvasInfo.text = "Congratulations";
                blackShowCBK = Blank;
                canvasInfo.enabled = true;
                canvasInfoBG.enabled = true;
                canvasAnimator.Play("BlackFadeOut");
                playInfomation.SetTime(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, -roundTime);
                roundTime = .0f;
                playInfomation.CountDeadNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount);
                playerDeathCount = 0;
                playInfomation.GetThrowNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount, player.GetHasThrowNum);
                player.ResetThrowthing();
                playInfomation.SetDynamicPatrolNum(dynamicPatolChange);

                string potion = JsonUtility.ToJson(playInfomation, true);
                System.IO.File.WriteAllText(Application.dataPath + "/StreamingAssets" + "/PlayInfomation_2-" + dataNum.ToString() + ".json", potion);
            }

        }
        else {
            pause = true;
            hasGoal = false;
            exitObject[mapCount].SetActive(true);
            blackShowCBK = PlayerDeadReset;
            blackEndCBK = Blank;
            canvasAnimator.Play("BlackFadeOut");
            playInfomation.CountDeadNum(mapPatrolManager[mapCount].dynamicPatrolSystem, mapCount);

        }
    }

    public void BlackShowCBK() {
        blackShowCBK();
    }
    public void BlackEndCBK() {
        blackEndCBK();
    }
    void ShowInfo() {
        canvasInfo.enabled = true;
        canvasInfoBG.enabled = true;
    }
    void PlayerDeadReset() {
        pause = false;
        mapPatrolManager[mapCount].ResetMap();
        player.ResetThrowthing();
        player.transform.position = startPos[mapCount];
    }
    void SkipToNextRound() {
        mapCount++;
        enemyManager.RecycleAllEnemy();
        mapPatrolManager[mapCount].SpawnEnemy();
        player.SetBorder(mapPatrolManager[mapCount].PathGrid.MinBorderPoint, mapPatrolManager[mapCount].PathGrid.MaxBorderPoint);
        MainCamera.transform.position = new Vector3(mapPatrolManager[mapCount].transform.position.x, MainCamera.transform.position.y, mapPatrolManager[mapCount].transform.position.z);
        player.transform.position = new Vector3(startPos[mapCount].x, 0, startPos[mapCount].z);
        canvasInfo.text = "PRESS  SPACE  TO  START";
        playerLife.text = "Life: " + (playerTotalLife).ToString();
        hasStart = false;
    }

}
