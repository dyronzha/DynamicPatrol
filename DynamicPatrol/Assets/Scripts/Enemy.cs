using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    int stateStep = 0;
    float sightRadius, sightAngle;
    float moveSpeed, chaseSpeed, moveRotateSpeed, lookRotateSpeed;
    float lookAroundAngle;

    EnemyManager enemyManager;

    public enum EnemyState {
        Patrol, lookAround, Search, Chase, backRoute
    }
    EnemyState curState = EnemyState.Patrol;

    int curLookNum = 0, lookAroundNum = 0;
    Vector3 nextPatrolPos, moveFWD;
    PatrolPath patrolPath, oringinPatrol;
    public PatrolPath OringinPatrolPath {
        get { return oringinPatrol; }
    }
    PathFinder.Path backRoutePath;

    bool lefRot = true, patrolEnd = false;
    float lookAroundTime = .0f;
    float lookClockWay = 1.0f;
    Quaternion preLookAround;
    Vector3 RSideLookDir, LSideLookDir;

    public MeshFilter viewMeshFilter;
    Mesh viewMesh;

    // Start is called before the first frame update
    private void Awake()
    {

    }
    void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = transform.name + "ViewMesh";
        viewMeshFilter.mesh = viewMesh;
    }

    // Update is called once per frame
    void Update()
    {

        float a = Vector3.SignedAngle(transform.forward, new Vector3(-1, 0, 0), Vector3.up);
        if (Input.GetKey(KeyCode.A)) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(a) * 90.0f * Time.deltaTime, 0);
        if (Input.GetKey(KeyCode.D)) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(a) * -90.0f * Time.deltaTime, 0);
        switch (curState) {
            case EnemyState.Patrol:
                Patroling();
                break;
            case EnemyState.lookAround:
                LookingAround();
                break;
        }
    }
    private void LateUpdate()
    {
        DrawFieldofView();
    }

    public void InitInfo(EnemyManager manager, float _moveSpeed, float _chaseSpeed, float _moveRotateSpeed, float _lookRotateSpeed, float _sightRadius, float _sightAngle) {
        enemyManager = manager;
        moveSpeed = _moveSpeed;
        chaseSpeed = _chaseSpeed;
        moveRotateSpeed = _moveRotateSpeed;
        lookRotateSpeed = _lookRotateSpeed;
        sightRadius = _sightRadius;
        sightAngle = _sightAngle;
        Debug.Log(enemyManager);
    }
    public void SetPatrolPath(PatrolPath path) {
        patrolPath = path;
        oringinPatrol = path;
        transform.position = path.startPos;

        Debug.Log("startttttttttttt  pos " + transform.position);
        gameObject.SetActive(true);
        LSideLookDir = new Vector3(-transform.forward.z, 0, transform.forward.x);
        RSideLookDir = new Vector3(transform.forward.z, 0, -transform.forward.x);
    }

    public void SearchUpdatePatrolPath(PatrolPath path) {
        patrolPath = path;
        transform.position = path.startPos;
        ChangeState(EnemyState.Patrol);
        patrolEnd = false;
    }

    void ChangeState(EnemyState state) {
        stateStep = 0;
        curLookNum = 0;
        curState = state;
    }

    void Patroling() {
        //return;
        if (patrolEnd) {
            float angle = Vector3.SignedAngle(transform.forward, moveFWD, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(angle) * lookRotateSpeed *1.5f * Time.deltaTime, 0);
            else {
                transform.rotation = Quaternion.LookRotation(moveFWD);
                patrolEnd = false;
            } 
            return;
        }

        Vector3 targetPos = new Vector3(0, 0, 0);
        if (patrolPath.MoveInPatrolRoute(transform.position, ref targetPos, ref lookAroundNum)) {
            //Debug.Log("goalllll reach " + targetPos);
            moveFWD = new Vector3(targetPos.x - transform.position.x, 0, targetPos.z - transform.position.z).normalized;
            if (lookAroundNum > 0)
            {
                preLookAround = transform.rotation;
                LSideLookDir = new Vector3(-transform.forward.z, 0, transform.forward.x);
                RSideLookDir = new Vector3(transform.forward.z, 0, -transform.forward.x);
                ChangeState(EnemyState.lookAround);
                lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up));
            }
            else {
                float angle = Vector3.Angle(transform.forward, moveFWD);
                if (angle > 120.0f)
                {
                    patrolEnd = true;
                }
                else {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
                    transform.position += transform.forward * moveSpeed * Time.deltaTime;
                }

                //ransform.position += moveFWD * moveSpeed * Time.deltaTime;
                //if (Mathf.Abs(angle) >= rotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(angle) * rotateSpeed * Time.deltaTime, 0);
                //else transform.rotation = Quaternion.LookRotation(moveFWD);
            }

        }
        else {
            //Debug.Log("notttttt reach " + targetPos);
            moveFWD = new Vector3(targetPos.x - transform.position.x, 0, targetPos.z - transform.position.z).normalized;
            float angle = Vector3.SignedAngle(transform.forward, moveFWD, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            //transform.position += moveFWD * moveSpeed * Time.deltaTime;
            //if (Mathf.Abs(angle) >= rotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(angle) * rotateSpeed * Time.deltaTime, 0);
           // else transform.rotation = Quaternion.LookRotation(moveFWD);
        }
    }

    void LookingAround() {
        if (stateStep == 0)
        {
            //第一階段先轉到固定位
            float angle = Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(LSideLookDir);
                stateStep = 1;
            }
            //Debug.Log("第一階段 轉向  angle " + angle + "   " + LSideLookDir) ;
        }
        else if (stateStep == 1)
        {
            float angle = Vector3.SignedAngle(transform.forward, RSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, -lookClockWay * lookRotateSpeed * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(RSideLookDir);
                stateStep = 2;
                curLookNum++;
                if (curLookNum > lookAroundNum) stateStep = 3;
            }
            //Debug.Log("第二階段 轉向  angle " + angle + "   " + RSideLookDir);
        }
        else if (stateStep == 2) {
            float angle = Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(LSideLookDir);
                stateStep = 1;
                curLookNum++;
                if (curLookNum > lookAroundNum) stateStep = 3;
            }
        }
        else
        {
            float angle = Vector3.SignedAngle(transform.forward, moveFWD, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(angle) * lookRotateSpeed * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(moveFWD);
                curLookNum = 0;
                ChangeState(EnemyState.Patrol);
            }
            //Debug.Log("第三階段 轉向  angle " + angle + "   " + moveFWD);
        }
    }

    Vector3 DirFromAngle(float angle) {
        //angle += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo{
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;
        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle) {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 a, Vector3 b){
            pointA = a;
            pointB = b;
        }
    }

    void DrawFieldofView() {
        int stepCount = Mathf.RoundToInt(sightAngle* enemyManager.sightResolution);
        float stepAngleSize = sightAngle / stepCount;
        List<Vector3> viewPoint = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for (int i = 0; i < stepCount; i++) {
            float angle = transform.eulerAngles.y - sightAngle * 0.5f + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0) {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > enemyManager.edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded)) {
                    EdgeInfo edge = FindEdege(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero) {
                        viewPoint.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoint.Add(edge.pointB);
                    }
                }
            }
            viewPoint.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoint.Count + 1;
        Vector3[] vertices= new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount-2)*3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount-1; i++) {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoint[i]);

            if (i < vertexCount - 2) {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    EdgeInfo FindEdege(ViewCastInfo minViewCast, ViewCastInfo maxViewCast) {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < enemyManager.edegeResolveIteration; i++) {
            float angle = (minAngle + maxAngle) * 0.5f;
            ViewCastInfo newViewCast = ViewCast(angle);
            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > enemyManager.edgeDstThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint,maxPoint);
    }

    ViewCastInfo ViewCast (float globalAngle) {
        Vector3 dir = DirFromAngle(globalAngle);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dir, out hit, sightRadius,  enemyManager.obstacleMask)) {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else return new ViewCastInfo(false, transform.position+dir*sightRadius, sightRadius, globalAngle);
    }
}
