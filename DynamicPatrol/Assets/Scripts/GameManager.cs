using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Camera MainCamera;
    public EnemyManager enemyManager;
    public Player player;
    public int playerTotalLife = 3;
    public Transform GameMap;
    public UnityEngine.UI.Text canvasInfo;
    public UnityEngine.UI.Image canvasInfoBG;
    public UnityEngine.UI.Text playerLife;
    public LayerMask goalLayerMask;

    bool init = false;
    int mapCount = 0, allMapNum = 0;
    Transform[] gameMaps;
    Vector3[] startPos, exitPos;
    PatrolManager[] mapPatrolManager;
    Animator canvasAnimator;

    bool hasStart = false;
    public static bool pause = true;
    public static int hasCreatPath = 0;

    int playerDeathCount = 0;

    System.Action blackEndCBK, blackShowCBK;
    void Blank() { }

    [HeaderAttribute("Test Value")]
    public bool InTest = false;
    public Enemy testEnemy;
    public Player testPlayer;

    // Start is called before the first frame update
    private void Awake()
    {
        canvasAnimator = GetComponent<Animator>();

        int activeMapCount = 0;
        for (int i = 0; i < GameMap.childCount; i++) {
            if (!GameMap.GetChild(i).gameObject.activeSelf) continue;
            activeMapCount++;
        }

        gameMaps = new Transform[activeMapCount];
        startPos = new Vector3[gameMaps.Length];
        exitPos = new Vector3[gameMaps.Length];
        mapPatrolManager = new PatrolManager[gameMaps.Length];
        for (int i = 0; i < GameMap.childCount; i++) {
            if (!GameMap.GetChild(i).gameObject.activeSelf) continue;
            Debug.Log(GameMap.GetChild(i).name);
            gameMaps[allMapNum] = GameMap.GetChild(i);
            startPos[allMapNum] = gameMaps[allMapNum].Find("StartPos").position;
            exitPos[allMapNum] = gameMaps[allMapNum].Find("Exit").position;
            mapPatrolManager[allMapNum] = gameMaps[allMapNum].Find("PathfindGrid").GetComponent<PatrolManager>();
            mapPatrolManager[allMapNum].InTest = InTest;
            mapPatrolManager[allMapNum].startPos = startPos[allMapNum];
            allMapNum++;
        }
        blackEndCBK = Blank; //ShowInfo;
        blackShowCBK = Blank;
        canvasInfo.enabled = false;
        canvasInfoBG.enabled = false;
    }
    void Start()
    {
        player.transform.position = startPos[0];
        MainCamera.transform.position = new Vector3(mapPatrolManager[mapCount].transform.position.x, MainCamera.transform.position.y, mapPatrolManager[mapCount].transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
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
                DetectPlayerGoal();
            }
            
        }

        if (Input.GetKeyDown(KeyCode.Q)) NextRound();

        if (InTest) {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                //RequestDynamicPatrol(new DynamicPatrolRequest(false, new Vector3(0,0,0), testEnemy),testPlayer.position, testEnemy);
                if (testEnemy != null) testEnemy.InTestSetSearch(testPlayer.position);
            }
            if (Input.GetKeyDown(KeyCode.W)) {
                if (enemyManager.CheckEnemyChangePathCount())
                {
                    enemyManager.ChangeAllEnemyPath(testEnemy);
                }
            }
        }
    }

    void DetectPlayerGoal() {
        if (pause) return;
        Collider[] hits = Physics.OverlapSphere(player.transform.position, 0.5f, goalLayerMask);
        if (hits != null && hits.Length > 0) {
            if (mapCount < gameMaps.Length)
            {
                pause = true;
                canvasInfo.text = "Next Round";
                canvasInfo.enabled = true;
                canvasInfoBG.enabled = true;
                blackShowCBK = SkipToNextRound;
                canvasAnimator.Play("BlackFadeOut");
            }
            else { 
                //遊戲結束
            }
            
        }
    }

    void NextRound() {
        playerDeathCount = 0;
        pause = true;
        canvasInfo.text = "Next Round";
        canvasInfo.enabled = true;
        canvasInfoBG.enabled = true;
        blackShowCBK = SkipToNextRound;
        //blackEndCBK = BlackEndPlay;
        canvasAnimator.Play("BlackFadeOut");
    }

    public void CountPlayerDead() {
        playerDeathCount++;
        playerLife.text = "Life: " + (playerTotalLife - playerDeathCount).ToString();
        if (playerDeathCount >= playerTotalLife)
        {
            playerDeathCount = 0;
            pause = true;
            canvasInfo.text = "Next Round";
            canvasInfo.enabled = true;
            canvasInfoBG.enabled = true;
            blackShowCBK = SkipToNextRound;
            //blackEndCBK = BlackEndPlay;
            canvasAnimator.Play("BlackFadeOut");
            
        }
        else {
            pause = true;
            blackShowCBK = PlayerDeadReset;
            blackEndCBK = Blank;
            canvasAnimator.Play("BlackFadeOut");
            
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
