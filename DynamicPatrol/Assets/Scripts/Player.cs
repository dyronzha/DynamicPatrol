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

    bool inThrow = false;
    int throwNum = 0;
    public int GetHasThrowNum {
        get { return (maxThrowNum - throwNum); }
    }

    Vector3 throwDir;
    List<Throwthing> usedThrowthings = new List<Throwthing>();
    List<Throwthing> freeThrowthings = new List<Throwthing>();

    Camera mainCamera;

    public float moveSpeed = 5.0f;
    public float collidResolution = 1.2f;
    public LayerMask obstacleMask;
    public LayerMask playerMask;
    public Transform throwArrow;
    SpriteRenderer throwArrowSprite;
    public int maxThrowNum = 5;
    public UnityEngine.UI.Text throwNumTxt;

    Transform selfTransform;
    public Vector3 position {
        get { return selfTransform.position; }
    }

    Vector3 minBorder;
    Vector3 maxBorder;

    //PathFinder.PathFindGrid grid;

    // Start is called before the first frame update
    private void Awake()
    {
        selfTransform = transform;
        colliderRadius = transform.localScale.x*0.5f;

        Transform throwthings = GameObject.Find("Throwthings").transform;
        for (int i = 0; i < throwthings.childCount; i++) {
            freeThrowthings.Add(throwthings.GetChild(i).GetComponent<Throwthing>());
        }
        throwArrowSprite = throwArrow.GetChild(0).GetComponent<SpriteRenderer>();
        throwArrowSprite.enabled = false;
        mainCamera = Camera.main;
        //grid = GameObject.Find("PathfindGrid").GetComponent<PathFinder.PathFindGrid>();

    }
    void Start()
    {
        throwNum = maxThrowNum;
        throwNumTxt.text = "x" + throwNum.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("    mouse " + mainCamera.ScreenToWorldPoint(Input.mousePosition));
        if (GameManager.pause) return;
        Move();

        if (!inThrow)
        {
            if (Input.GetMouseButtonDown(0)) {
                if (throwNum > 0 && freeThrowthings.Count > 0) {
                    inThrow = true;
                    Vector3 mPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    throwDir = new Vector3(mPos.x, 0, mPos.z) - transform.position;
                    Debug.Log("throw  " + throwDir + "    mouse " + Input.mousePosition);
                    if (throwDir.sqrMagnitude <= 0.25f) throwDir = new Vector3(0, 0, 1);
                    throwArrow.rotation = Quaternion.LookRotation(throwDir);
                    throwArrowSprite.enabled = true;
                }
            }
        }
        else {
            Vector3 mPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newDir = new Vector3(mPos.x, 0, mPos.z) - transform.position;
            if (newDir.sqrMagnitude > 0.25f) throwDir = newDir.normalized;
            throwArrow.rotation = Quaternion.LookRotation(throwDir);

            if (Input.GetMouseButtonDown(1)) {
                inThrow = false;
                throwArrowSprite.enabled = false;
            }
            if (Input.GetMouseButtonUp(0)) {
                inThrow = false;
                throwArrowSprite.enabled = false;
                throwNum--;
                freeThrowthings[0].SetThrow(transform.position, throwDir);
                usedThrowthings.Add(freeThrowthings[0]);
                freeThrowthings.RemoveAt(0);
                throwNumTxt.text = "x" + throwNum.ToString();
            }
        }

        //if (Input.GetKeyDown(KeyCode.Z)) visible = !visible;
    }
    void Move() {
        int hMove = 0;
        int vMove = 0;
        if (Input.GetKey(KeyCode.A))
        {
            hMove = -1;
        }
        if (Input.GetKey(KeyCode.D)) {
            if (hMove < 0) hMove = 0;
            else hMove = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            vMove = -1;
        }
        if (Input.GetKey(KeyCode.W)) {
            if (vMove < 0) vMove = 0;
            else vMove = 1;
        }

        Vector3 perDiff = Vector3.zero;
        float detectLength = Time.deltaTime * moveSpeed * collidResolution;

        Collider[] hHits = Physics.OverlapSphere(selfTransform.position + detectLength * new Vector3(hMove, 0, 0), colliderRadius, obstacleMask);
        if (Mathf.Abs(hMove) > 0 && (hHits == null || hHits.Length == 0) && 
            (selfTransform.position.x+ hMove*detectLength)> minBorder.x && (selfTransform.position.x + hMove*detectLength) < maxBorder.x) 
            perDiff += hMove * new Vector3(1, 0, 0);
        Collider[] vHits = Physics.OverlapSphere(selfTransform.position + detectLength * new Vector3(0, 0, vMove), colliderRadius, obstacleMask);
        if (Mathf.Abs(vMove) > 0 && (vHits == null || vHits.Length == 0) &&
            (selfTransform.position.z + vMove*detectLength) > minBorder.z && (selfTransform.position.z + vMove*detectLength) < maxBorder.z) 
            perDiff += vMove * new Vector3(0, 0, 1);
        //Debug.Log(perDiff);

        Vector3 detectPos = selfTransform.position + detectLength * perDiff;
        Vector3 nextPos = selfTransform.position + Time.deltaTime * moveSpeed * perDiff.normalized;
        Collider[] hits = Physics.OverlapSphere(detectPos, colliderRadius, obstacleMask);
        if (hits == null || hits.Length == 0) {
            selfTransform.position = nextPos;
        }
    }


    public void SetBorder(Vector3 min, Vector3 max) {
        minBorder = min;
        maxBorder = max;
    }

    public void RecycleThrowThing(Throwthing t) {
        freeThrowthings.Add(t);
        usedThrowthings.Remove(t);
    }
    public void ResetThrowthing() {
        throwNum = maxThrowNum;
        throwArrowSprite.enabled = false;
        throwNumTxt.text = "x" + throwNum.ToString();
        for (int i = usedThrowthings.Count-1; i >= 0; i--) {
            usedThrowthings[i].ForceRecycle();
            freeThrowthings.Add(usedThrowthings[i]);
            usedThrowthings.RemoveAt(i);
        }
    }
}
