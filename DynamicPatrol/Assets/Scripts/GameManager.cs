using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Camera MainCamera;
    public EnemyManager enemyManager;
    public Player player;
    public Animator canvasAnimator;

    bool init = false;
    int mapCount = 0;
    Transform[] gameMaps;
    Vector3[] startPos;
    PatrolManager[] mapPatrolManager;

    public static bool pause = true;

    // Start is called before the first frame update
    private void Awake()
    {
        gameMaps = new Transform[transform.childCount];
        startPos = new Vector3[transform.childCount];
        mapPatrolManager = new PatrolManager[transform.childCount];
        for (int i = 0; i < gameMaps.Length; i++) {
            gameMaps[i] = transform.GetChild(i);
            startPos[i] = gameMaps[i].Find("StartPos").position;
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!init)
        {
            init = true;
            mapPatrolManager[mapCount].SpawnEnemy();
            canvasAnimator.Play("BlackFadeIn");
        }
        else { 
            
        }
    }
}
