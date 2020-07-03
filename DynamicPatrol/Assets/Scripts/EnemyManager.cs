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
    public float sightResolution = 0.5f;
    public int edegeResolveIteration = 3;
    public float edgeDstThreshold = 0.5f;
    public float reflectTime = 0.3f;
    public LayerMask obstacleMask;

    List<Enemy> freeEnemy = new List<Enemy>();
    List<Enemy> usedEnemy = new List<Enemy>();

    public Player player;

    public ConversationManager conversationManager;



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
}
