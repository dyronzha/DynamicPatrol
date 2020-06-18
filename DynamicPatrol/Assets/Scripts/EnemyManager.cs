using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    List<Enemy> freeEnemy = new List<Enemy>();
    List<Enemy> usedEnemy = new List<Enemy>();


    // Start is called before the first frame update
    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++) {
            freeEnemy.Add(transform.GetChild(i).GetComponent<Enemy>());
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
