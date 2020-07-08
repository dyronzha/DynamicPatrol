using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool InTest = false;
    public Camera MainCamera;
    public EnemyManager enemyManager;
    public Player player;
    public Transform GameMap;
    public UnityEngine.UI.Text canvasInfo;
    public UnityEngine.UI.Image canvasInfoBG;


    bool init = false;
    int mapCount = 0;
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

    // Start is called before the first frame update
    private void Awake()
    {
        canvasAnimator = GetComponent<Animator>();

        gameMaps = new Transform[GameMap.childCount];
        startPos = new Vector3[gameMaps.Length];
        exitPos = new Vector3[gameMaps.Length];
        mapPatrolManager = new PatrolManager[gameMaps.Length];
        for (int i = 0; i < gameMaps.Length; i++) {
            Debug.Log(GameMap.GetChild(i).name);
            gameMaps[i] = GameMap.GetChild(i);
            startPos[i] = gameMaps[i].Find("StartPos").position;
            Debug.Log(gameMaps[i].Find("Exit").name);
            exitPos[i] = gameMaps[i].Find("Exit").position;
            mapPatrolManager[i] = gameMaps[i].Find("PathfindGrid").GetComponent<PatrolManager>();
            mapPatrolManager[i].InTest = InTest;
        }
        blackEndCBK = ShowInfo;
        blackShowCBK = Blank;
        canvasInfo.enabled = false;
        canvasInfoBG.enabled = false;
    }
    void Start()
    {
        blackEndCBK();
    }

    // Update is called once per frame
    void Update()
    {
        if (!init)
        {
            if (hasCreatPath >= gameMaps.Length) {
                init = true;
                mapPatrolManager[mapCount].SpawnEnemy();
                player.SetBorder(mapPatrolManager[mapCount].PathGrid.MinBorderPoint, mapPatrolManager[mapCount].PathGrid.MaxBorderPoint);
                canvasAnimator.Play("BlackFadeIn");
            }

        }
        else {
            if (!hasStart)
            {
                if (Input.GetKeyDown(KeyCode.Space))
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
    }

    void DetectPlayerGoal() {
        Collider[] hits = Physics.OverlapBox(exitPos[mapCount], new Vector3(1.2f, 2.0f, 0.8f), Quaternion.identity, player.playerMask);
        if (hits != null && hits.Length > 0) {
            pause = true;
            canvasInfo.text = "Next Round";
            canvasInfo.enabled = true;
            canvasInfoBG.enabled = true;
            blackShowCBK = SkipToNextRound;
            canvasAnimator.Play("BlackFadeOut");
            
        }
    }

    public void CountPlayerDead() {
        playerDeathCount++;
        if (playerDeathCount >= 3)
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
        hasStart = false;
    }

}
