using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float chaseSpeed = 8.0f;
    public float moveRotateSpeed = 8.0f;
    public float lookRotateSpeed = 180.0f;
    public float sightRadius = 5.0f;
    public float sightAngle = 70.0f;
    public float senseRadius= 1.5f;
    public float sightResolution = 0.5f;
    public int edegeResolveIteration = 3;
    public float edgeDstThreshold = 0.5f;
    public float reflectTime = 0.3f;
    public LayerMask obstacleMask;

    List<Enemy> freeEnemy = new List<Enemy>();
    List<Enemy> usedEnemy = new List<Enemy>();

    public Player player;

    public ConversationManager conversationManager;
    public GameManager gameManager;

    int changePathNum = 0;
    bool allChange = false;

    // Start is called before the first frame update
    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++) {
            freeEnemy.Add(transform.GetChild(i).GetComponent<Enemy>());
            freeEnemy[i].InitInfo(this);
            freeEnemy[i].gameObject.SetActive(false);
        }
        conversationManager = GameObject.Find("ConversationManager").GetComponent<ConversationManager>();
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    public Enemy SpawnEnemyInPatrol(PatrolPath path, PatrolManager patrolManager) {
        Debug.Log("spppppppppppppppppawn  ");
        Enemy enemy = freeEnemy[0];
        freeEnemy.RemoveAt(0);
        usedEnemy.Add(enemy);
        enemy.SetPatrolPath(path, patrolManager);
        return enemy;
    }

    public bool CheckEnemyChangePathCount() {
        changePathNum++;
        if (changePathNum >= 5)
        {
            return true;
        }
        else return false;

    }
    public void ChangeAllEnemyPath(Enemy caller) {
        changePathNum = 0;
        Debug.Log("Change all enemy path  ");
        caller.BeginRenewPatrol(true);
        for (int i = 0; i < usedEnemy.Count; i++)
        {
            if (!usedEnemy[i].Equals(caller)) usedEnemy[i].BeginRenewPatrol(false);
        }
    }

    public void RecycleAllEnemy() {
        changePathNum = 0;
        for (int i = usedEnemy.Count - 1; i >= 0; i--) {
            usedEnemy[i].RecycleReset();
            freeEnemy.Add(usedEnemy[i]);
            usedEnemy.RemoveAt(i);
        }
    }

    public void CatchPlayer() {
        changePathNum = 0;
        gameManager.CountPlayerDead();
    }

    public void ThrowAttention(Vector3 point, float range) {
        float leastDst = float.MaxValue;
        Enemy enemy = null;
        for (int i = 0; i < usedEnemy.Count; i++) {
            float dst = (usedEnemy[i].transform.position - point).magnitude;
            if (dst < range && !Physics.Linecast(point, usedEnemy[i].transform.position, obstacleMask) && 
                (usedEnemy[i].CurrentState == Enemy.EnemyState.Patrol || usedEnemy[i].CurrentState == Enemy.EnemyState.lookAround || usedEnemy[i].CurrentState == Enemy.EnemyState.GoBackRoute)
                ) {
                usedEnemy[i].SuspectByThrow(point);
                if ( dst < leastDst) {
                    leastDst = dst;
                    enemy = usedEnemy[i];
                }
            }
        }
        if(enemy != null)enemy.AttentionByThrow(point);

    }
}
