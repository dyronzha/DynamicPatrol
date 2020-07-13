using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwthing : MonoBehaviour
{
    bool stop;
    float speed, slower, lifeTime;
    Vector3 flyDir;
    LineRenderer lineRender;
    MeshRenderer mesh;

    Player player;
    EnemyManager enemyManager;


    public float maxSpeed = 5.0f;
    public float slowSpeed = 1.0f;
    public float totalLifeTime = 1.0f;
    public float senseRange = 2.5f;
    public LayerMask obstacleMask;


    // Start is called before the first frame update
    private void Awake()
    {
        lineRender = GetComponent<LineRenderer>();
        player = GameObject.Find("Player").GetComponent<Player>();
        enemyManager = GameObject.Find("EnemyManager").GetComponent<EnemyManager>();
        mesh = GetComponent<MeshRenderer>();
    }
    void Start()
    {
        lineRender.enabled = false;
        speed = maxSpeed;
        slower = slowSpeed;
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!stop)
        {
            speed -= slower * Time.deltaTime;
            slower += 2.0f * Time.deltaTime;
            transform.position += speed * Time.deltaTime * flyDir;

            Collider[] hits = Physics.OverlapSphere(transform.position, 0.25f, obstacleMask);
            if (hits != null && hits.Length > 0)
            {
                lifeTime = .0f;
                //speed = maxSpeed;
                stop = true;
                lineRender.enabled = true;
                mesh.enabled = false;

                for (int i = 0; i < 12; i++)
                {
                    Vector3 pos;
                    Vector3 rayDir = (Quaternion.Euler(0, 30.0f * i, 0) * Vector3.forward);
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, rayDir, out hit, senseRange, obstacleMask))
                    {
                        pos = hit.point;
                    }
                    else pos = transform.position + senseRange * rayDir;
                    lineRender.SetPosition(i, pos);
                }
                enemyManager.ThrowAttention(transform.position, senseRange);
            }
            else {
                if (speed < 10f)
                {
                    //speed = maxSpeed;
                    lineRender.enabled = true;
                    //mesh.enabled = false;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 pos;
                        Vector3 rayDir = (Quaternion.Euler(0, 30.0f * i, 0) * Vector3.forward);
                        RaycastHit hit;
                        if (Physics.Raycast(transform.position, rayDir, out hit, senseRange, obstacleMask))
                        {
                            pos = hit.point;
                        }
                        else pos = transform.position + senseRange * rayDir;
                        lineRender.SetPosition(i, pos);
                    }
                    enemyManager.ThrowAttention(transform.position, senseRange);
                    if(speed < 2.0f) stop = true;
                }
            }
            
        }
        else {
            for (int i = 0; i < 12; i++)
            {
                Vector3 pos;
                Vector3 rayDir = (Quaternion.Euler(0, 30.0f * i, 0) * Vector3.forward);
                RaycastHit hit;
                if (Physics.Raycast(transform.position, rayDir, out hit, senseRange, obstacleMask))
                {
                    pos = hit.point;
                }
                else pos = transform.position + senseRange * rayDir;
                lineRender.SetPosition(i, pos);
            }
            enemyManager.ThrowAttention(transform.position, senseRange);

            lifeTime += Time.deltaTime;
            if (lifeTime > totalLifeTime) {
                speed = maxSpeed;
                lifeTime = .0f;
                stop = false;
                lineRender.enabled = false;
                slower = slowSpeed;
                gameObject.SetActive(false);
                player.RecycleThrowThing(this);
            }
        }
        
    }

    public void SetThrow(Vector3 pos, Vector3 dir) {
        mesh.enabled = true;
        transform.position = pos;
        gameObject.SetActive(true);
        stop = false;
        flyDir = dir;
        lifeTime = .0f;
    }

    public void ForceRecycle() {
        speed = maxSpeed;
        lifeTime = .0f;
        lineRender.enabled = false;
        mesh.enabled = true;
        gameObject.SetActive(false);
        stop = false;
        slower = slowSpeed;
    }
}
