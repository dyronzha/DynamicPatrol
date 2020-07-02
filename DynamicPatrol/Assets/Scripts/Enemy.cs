using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    bool newPatrol = false, changingPath = false, searchRoute = false;
    int changingPathConnectID = -1;
    int stateStep = 0;
    float sightRadius, sightAngle;
    float moveSpeed, chaseSpeed, moveRotateSpeed, lookRotateSpeed;
    float lookAroundAngle;
    float seePlayerTime = .0f, loosePlayerTime = .0f, reflectTime;

    EnemyManager enemyManager;

    public enum EnemyState {
        Patrol, lookAround, Search, Chase, GoBackRoute, Suspect
    }
    EnemyState curState = EnemyState.Patrol;
    EnemyState lastState;

    int curLookNum = 0, lookAroundNum = 0;
    Vector3 nextPatrolPos, moveFWD, lastPathPoint, playerLastPos, playerDir;
    PatrolManager patrolManager;
    PatrolPath patrolPath, newPatrolPath;
    PatrolPath oringinPath;
    public PatrolPath OringinPath {
        get { return oringinPath; }
    }
    List<PatrolManager.PatrolGraphNode> oringinPatrolGraphNode;
    public List<PatrolManager.PatrolGraphNode> OringinPatrolGraphNode {
        get { return oringinPatrolGraphNode; }
    }
    PathFinder.Path backRoutePath;

    bool lefRot = true, patrolEnd = false;
    float lookAroundTime = .0f;
    float lookClockWay = 1.0f;
    Quaternion preLookAround;
    Vector3 RSideLookDir, LSideLookDir;

    public MeshFilter viewMeshFilter;
    Mesh viewMesh;

    System.Action<PatrolPath, PatrolPath> DynamicChangingPathCBK = null;

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
                if (DetectPlayer())
                {
                    seePlayerTime += Time.deltaTime;
                    if (seePlayerTime >= reflectTime)
                    {
                        ChangeState(EnemyState.Suspect);
                        seePlayerTime = .0f;
                    }
                }
                else { 
                    seePlayerTime -= Time.deltaTime;
                    if (seePlayerTime <= .0f) seePlayerTime = .0f;
                } 
                break;

            case EnemyState.lookAround:
                LookingAround();
                if (DetectPlayer())
                {
                    seePlayerTime += Time.deltaTime;
                    if (seePlayerTime >= reflectTime)
                    {
                        ChangeState(EnemyState.Suspect);
                        seePlayerTime = .0f;
                    }
                }
                else
                {
                    seePlayerTime -= Time.deltaTime;
                    if (seePlayerTime <= .0f) seePlayerTime = .0f;
                }
                break;

            case EnemyState.Chase:
                if (DetectPlayer()) Chasing();
                else {
                    ChangeState(EnemyState.Search);
                    StartCoroutine(patrolManager.DynamicPatrol(playerLastPos, this));
                } 
                break;

            case EnemyState.Search:
                Searching(DetectPlayer());
                break;

            case EnemyState.Suspect:
                Suspecting();
                if (DetectPlayer())
                {
                    seePlayerTime += Time.deltaTime;
                    if (seePlayerTime >= reflectTime)
                    {
                        ChangeState(EnemyState.Chase);
                        seePlayerTime = .0f;
                    }
                }
                else
                {
                    seePlayerTime -= Time.deltaTime;
                    if (seePlayerTime <= .0f) seePlayerTime = .0f;
                    loosePlayerTime += Time.deltaTime;
                    if (loosePlayerTime > Random.Range(1.5f, 3.0f)) {
                        loosePlayerTime = .0f;
                        ChangeState(lastState);
                    }
                }
                break;

            case EnemyState.GoBackRoute:
                break;
        }
        lastState = curState;
    }
    private void LateUpdate()
    {
        DrawFieldofView();
    }

    public void InitInfo(EnemyManager manager) {
        enemyManager = manager;
        moveSpeed = manager.moveSpeed;
        chaseSpeed = manager.chaseSpeed;
        moveRotateSpeed = manager.moveRotateSpeed;
        lookRotateSpeed = manager.lookRotateSpeed;
        sightRadius = manager.sightRadius;
        sightAngle = manager.sightAngle;
        reflectTime = manager.reflectTime;
        Debug.Log(enemyManager);
    }
    public void SetPatrolPath(PatrolPath path, PatrolManager _patrolManager) {
        patrolManager = _patrolManager;
        patrolPath = path;
        oringinPath = path;
        oringinPatrolGraphNode = path.pathPatrolGraphNode;
        transform.position = path.startPos;
        lastPathPoint = path.startPos;
        transform.rotation = Quaternion.LookRotation(path.GetPathPoint(1) - lastPathPoint);

        Debug.Log("startttttttttttt  pos " + transform.position);
        gameObject.SetActive(true);
        LSideLookDir = new Vector3(-transform.forward.z, 0, transform.forward.x);
        RSideLookDir = new Vector3(transform.forward.z, 0, -transform.forward.x);
    }

    public void TestDynamicPatrol(PatrolPath path) {
        patrolPath = path;
        transform.position = path.startPos;
        ChangeState(EnemyState.Patrol);
        patrolEnd = false;
    }

    public void SearchUpdatePatrolPath(PatrolPath path, Enemy enemy, System.Action<PatrolPath, PatrolPath> pathChangingCBK) {
        
        if (this.Equals(enemy))
        {
            newPatrol = true;
            newPatrolPath = path;
            patrolEnd = false;
            DynamicChangingPathCBK = pathChangingCBK;
            //ChangeState(EnemyState.Patrol);
            //patrolPath = path;
            //transform.position = path.startPos;
        }
        else {
            newPatrol = true;
            newPatrolPath = path;
            patrolEnd = false;
            DynamicChangingPathCBK = pathChangingCBK;
        }
    }

    void ChangeState(EnemyState state) {
        if(state != EnemyState.Suspect) curLookNum = 0;
        stateStep = 0;
        curState = state;
    }

    bool DetectPlayer() {
        return false;
        if (enemyManager.player.Visible) {
            Vector3 playerPos = enemyManager.player.position;
            if (!Physics.Linecast(playerPos, transform.position, enemyManager.obstacleMask))
            {
                playerDir = playerPos - transform.position;
                if (playerDir.sqrMagnitude <= sightRadius * sightRadius)
                {
                    float angle = Vector3.Angle(transform.forward, playerDir);
                    if (angle <= sightAngle * 0.5f)
                    {
                        playerLastPos = playerPos;

                        return true;
                    }
                }
            }
        }
        return false;
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

        Vector3 targetPos = new Vector3(-100, 0, -100);
        if (patrolPath.MoveInPatrolRoute(transform.position, ref targetPos, ref lookAroundNum)) {
            Debug.Log("goalllll reach " + lastPathPoint);

            //檢查有沒有動態變化路線，並在走到變化路線上時變更路線和ids
            if (newPatrol) {
                if (!changingPath)
                {
                    Debug.Log("channnnge targettttttttt " + targetPos);
                    if (newPatrolPath.pathPoints.Contains(targetPos))
                    {
                        Debug.Log("channnnge targettttttttt " + targetPos + "    " + newPatrolPath.pathPoints.IndexOf(targetPos));
                        changingPath = true;
                    }
                }
                else {
                    if (!patrolPath.Reverse) changingPathConnectID = newPatrolPath.pathPoints.IndexOf(lastPathPoint);
                    else
                    {
                        newPatrolPath.SetPathReverse();
                        //if(newPatrolPath.TBranch)
                        //    changingPathConnectID = newPatrolPath.pathPoints.Count - 1 - newPatrolPath.pathPoints.IndexOf(lastPathPoint) - newPatrolPath.newBranchGraphNode.Count * 2;
                        //else
                        //    changingPathConnectID = newPatrolPath.pathPoints.Count - 1 - newPatrolPath.pathPoints.IndexOf(lastPathPoint);
                        changingPathConnectID = newPatrolPath.pathPoints.Count - 1 - newPatrolPath.pathPoints.IndexOf(lastPathPoint);
                        Debug.Log(newPatrolPath.TBranch + "   new path count" + (newPatrolPath.pathPoints.Count - 1));
                        Debug.Log("new path id " + newPatrolPath.pathPoints.IndexOf(lastPathPoint));
                        Debug.Log("new path branch " + newPatrolPath.newBranchGraphNode.Count);
                    }
                    newPatrol = false;
                    changingPath = false;
                    newPatrolPath.SetPatrolPathID(changingPathConnectID + 1);
                    targetPos = newPatrolPath.GetPathPoint(changingPathConnectID + 1);
                    Debug.Log(" ChangingPathConnectID  " + (changingPathConnectID+1) + "    " + targetPos);
                    lookAroundNum = newPatrolPath.LookAroundPoints(changingPathConnectID);
                    DynamicChangingPathCBK(patrolPath, newPatrolPath);
                    patrolPath = newPatrolPath;
                }
            }
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
            lastPathPoint = targetPos;
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

    void Suspecting() {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(playerDir), Time.deltaTime * moveRotateSpeed);
    }

    void Chasing() {
        Vector3 moveFWD = playerDir.normalized;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
        transform.position += transform.forward * chaseSpeed * Time.deltaTime;
    }

    void Searching(bool findPlayer) {
        if (findPlayer) {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
            seePlayerTime += Time.deltaTime;
            if (seePlayerTime > reflectTime) ChangeState(EnemyState.Chase);
            return;
        }

        Vector3 diff = new Vector3(0,0,0);
        if (stateStep == 0)
        {
            diff = (playerLastPos - transform.position);
            if (diff.sqrMagnitude > 0.5f)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
                transform.position += transform.forward * chaseSpeed * Time.deltaTime;
            }
            else
            {
                stateStep++;
                LSideLookDir = new Vector3(-transform.forward.z, 0, transform.forward.x);
                RSideLookDir = new Vector3(transform.forward.z, 0, -transform.forward.x);
                lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, (Random.Range(.0f,1.0f) >= 0.5f ? LSideLookDir : RSideLookDir), Vector3.up));

            }
        }
        else if (stateStep == 1)
        {
            //第一階段先轉到固定位
            float angle = Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * 1.2f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(LSideLookDir);
                stateStep = 2;
            }
            //Debug.Log("第一階段 轉向  angle " + angle + "   " + LSideLookDir) ;
        }
        else if (stateStep == 2)
        {
            float angle = Vector3.SignedAngle(transform.forward, RSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, -lookClockWay * lookRotateSpeed * 1.2f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(RSideLookDir);
                stateStep = 2;
                curLookNum++;
                if (curLookNum >= 3) {
                    lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, -diff, Vector3.up));
                    stateStep = 4;
                } 
            }
            //Debug.Log("第二階段 轉向  angle " + angle + "   " + RSideLookDir);
        }
        else if (stateStep == 3)
        {
            float angle = Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * 1.2f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(LSideLookDir);
                stateStep = 1;
                curLookNum++;
                if (curLookNum >= 3) {
                    lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, -diff , Vector3.up));
                    stateStep = 4;
                } 
            }
        }
        else {
            ChangeState(EnemyState.GoBackRoute);
            //float angle = Vector3.SignedAngle(transform.forward, -diff, Vector3.up);
            //if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * Time.deltaTime, 0);
            //else
            //{
            //    transform.rotation = Quaternion.LookRotation(-diff);
            //}
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
