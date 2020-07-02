using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    float colliderRadius = .0f;
    bool visible = true;
    public bool Visible {
        get { return visible; }
    }

    public float moveSpeed = 5.0f;
    public float collidResolution = 1.2f;
    public LayerMask obstacleMask;

    Transform selfTransform;
    public Vector3 position {
        get { return selfTransform.position; }
    }

    // Start is called before the first frame update
    private void Awake()
    {
        selfTransform = transform;
        colliderRadius = transform.localScale.x*0.5f;

    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        if (Input.GetKeyDown(KeyCode.Space)) visible = !visible;
    }
    void Move() {
        int hMove = 0;
        int vMove = 0;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            hMove = -1;
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            if (hMove < 0) hMove = 0;
            else hMove = 1;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            vMove = -1;
        }
        if (Input.GetKey(KeyCode.UpArrow)) {
            if (vMove < 0) vMove = 0;
            else vMove = 1;
        }

        Vector3 perDiff = Vector3.zero;
        float detectLength = Time.deltaTime * moveSpeed * collidResolution;

        Collider[] hHits = Physics.OverlapSphere(selfTransform.position + detectLength * new Vector3(hMove, 0, 0), colliderRadius, obstacleMask);
        if (Mathf.Abs(hMove) > 0 && (hHits == null || hHits.Length == 0)) 
            perDiff += hMove * new Vector3(1, 0, 0);
        Collider[] vHits = Physics.OverlapSphere(selfTransform.position + detectLength * new Vector3(0, 0, vMove), colliderRadius, obstacleMask);
        if (Mathf.Abs(vMove) > 0 && (vHits == null || vHits.Length == 0)) 
            perDiff += vMove * new Vector3(0, 0, 1);
        //Debug.Log(perDiff);

        Vector3 detectPos = selfTransform.position + detectLength * perDiff;
        Vector3 nextPos = selfTransform.position + Time.deltaTime * moveSpeed * perDiff.normalized;
        Collider[] hits = Physics.OverlapSphere(detectPos, colliderRadius, obstacleMask);
        if (hits == null || hits.Length == 0) {
            selfTransform.position = nextPos;
        }
    }
}
