﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    bool newPatrol = false, changingPath = false, findRoute = false, waitProcess = false, dynamicPatrol = false;
    bool renewPatroling = false;
    int changingPathConnectID = -1;
    int stateStep = 0;
    float sightRadius, sightAngle;
    float moveSpeed, chaseSpeed, moveRotateSpeed, lookRotateSpeed;
    float lookAroundAngle;
    float seePlayerTime = .0f, loosePlayerTime = .0f, reflectTime;

    Enemy passEnemy = null;
    PatrolPath passPath;
    System.Action<Enemy,PatrolPath> passCBK;
    Vector3 passPoint;
    EnemyManager enemyManager;

    public enum EnemyState {
        Patrol, lookAround, Search, Chase, GoBackRoute, Suspect
    }
    EnemyState curState = EnemyState.Patrol;
    EnemyState lastState;

    int curLookNum = 0, lookAroundNum = 0;
    Vector3 nextPatrolPos, moveFWD, lastPathPoint, playerLastPos, playerDir;
    PatrolManager patrolManager;
    PatrolPath patrolPath, newPatrolPath, lastPatrolPath;
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


    System.Action<Enemy,PatrolPath> DynamicChangingPathCBK = null;

    bool conversation = false;

    DynamicPatrolRequest curRequest = null;

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
                    Debug.Log("lllllllllllllllllost  player  " + transform.forward);
                    //Debug.Break();
                    ChangeState(EnemyState.Search);
                } 
                break;

            case EnemyState.Search:
                Searching(DetectPlayer());
                break;

            case EnemyState.Suspect:
                if (DetectPlayer())
                {
                    seePlayerTime += Time.deltaTime;
                    if (seePlayerTime >= reflectTime)
                    {
                        Debug.Log("suspecting ffind   player  " + transform.forward);
                        //Debug.Break();
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
                        ChangeState(EnemyState.Patrol);
                    }
                }
                Suspecting();
                break;

            case EnemyState.GoBackRoute:
                GoingBackRoute();
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
        lastPatrolPath = path;
        oringinPatrolGraphNode = path.pathPatrolGraphNode;
        transform.position = path.startPos;
        lastPathPoint = path.startPos;
        transform.rotation = Quaternion.LookRotation(path.GetPathPoint(1) - lastPathPoint);

        Debug.Log("startttttttttttt  pos " + transform.position);
        gameObject.SetActive(true);
        LSideLookDir = new Vector3(-transform.forward.z, 0, transform.forward.x);
        RSideLookDir = new Vector3(transform.forward.z, 0, -transform.forward.x);
    }
    public void BeginRenewPatrol(bool hasStart) {
        if (hasStart) {
            patrolManager.RenewAllPatrol();
            patrolManager.RequestNewPatrol(this, transform.position);
        } 
        else patrolManager.RequestNewPatrol(this, new Vector3(0, -500, 0));
        if (curState == EnemyState.Patrol || curState == EnemyState.lookAround)
        {
            ChangeState(EnemyState.Search);
        }
        renewPatroling = true;
        
    }
    public void RenewOringinPath(PatrolPath path) {
        findRoute = true;
        patrolPath = path;
        oringinPath = path;
        lastPatrolPath = path;
        oringinPatrolGraphNode = path.pathPatrolGraphNode;

    }
    public void TestDynamicPatrol(PatrolPath path) {
        lastPatrolPath = patrolPath;
        patrolPath = path;
        transform.position = path.startPos;
        ChangeState(EnemyState.Patrol);
        patrolEnd = false;
    }

    public void NeedFindBackPatrol(Vector3 detectPoint) {
        bool overlap = false;
        for (int j = 0; j < patrolPath.pathPoints.Count - 1; j++)
        {
            Vector3 pathDir = patrolPath.pathPoints[j + 1] - patrolPath.pathPoints[j];
            Vector3 pathDirNormal = pathDir.normalized;
            Vector3 pointDir = detectPoint - patrolPath.pathPoints[j];

            float length = Vector3.Cross(pathDirNormal, pointDir).magnitude;
            float lineDirLength = Vector3.Dot(pointDir, pathDirNormal);

            Debug.Log("lind dir " + pathDir);
            Debug.Log("length " + length);
            Debug.Log("lineDirLength " + lineDirLength);

            if (length < 0.5f && lineDirLength >= 0 && Mathf.Abs(lineDirLength) <= pathDir.magnitude)
            {
                Debug.Log("已經在路線中，不需要回原本路線  " + transform.name);
                overlap = true;
                break;
            }
        }
        if (overlap) ChangeState(EnemyState.Patrol);
        else patrolManager.RequestBackPatrol(curRequest, transform.position, this);
    }

    public void SearchUpdatePatrolPath(Vector3 newPatrolPoint, PatrolPath path, Enemy enemy, System.Action<Enemy,PatrolPath> pathChangingCBK) {
        Debug.Log(transform.name + " 完成 coroutine 巡迴路徑");
        if (this.Equals(enemy))
        {
            //newPatrol = true;
            //newPatrolPath = path;
            //DynamicChangingPathCBK = pathChangingCBK;

            Debug.Log(transform.name +  "  自行改變 路徑");
            //從新的巡邏點開始走
            waitProcess = false;
            findRoute = true;
            dynamicPatrol = true;
            patrolEnd = false;
            newPatrolPath = path;
            path.StartPatrolAtNewBranchEnd(); //更新path id
            DynamicChangingPathCBK = pathChangingCBK;
            //pathChangingCBK(patrolPath, newPatrolPath);
            //patrolPath = newPatrolPath;

            //transform.position = path.startPos;
        }
        else {
            //通知另一個敵人
            Debug.Log(transform.name + "  通知改變路徑 給 " + enemy.transform.name);
            passEnemy = enemy;
            conversation = true;
            passPath = path;
            passCBK = pathChangingCBK;
            passPoint = newPatrolPoint;

            //自己走回路線
            curRequest = patrolManager.RequestBackPatrol(curRequest, transform.position, this);

            //newPatrol = true;
            //newPatrolPath = path;
            //patrolEnd = false;
            //DynamicChangingPathCBK = pathChangingCBK;
        }
    }
    public void SetNewPatrolPath(Vector3 newPatrolPoint, PatrolPath path, System.Action<Enemy,PatrolPath> pathChangingCBK) {
        if (curState == EnemyState.Patrol || curState == EnemyState.lookAround)
        {
            Debug.Log(transform.name + "  狀態容許改變路徑 ");
            conversation = true;
            enemyManager.conversationManager.UseContent(transform, 1, 0.8f);
            newPatrol = true;
            newPatrolPath = path;
            patrolEnd = false;
            DynamicChangingPathCBK = pathChangingCBK;
        }
        else
        {
            Debug.Log(transform.name + "  狀態不~~~容許改變路徑 ");
            conversation = true;
            enemyManager.conversationManager.UseContent(transform, 2);
        }

    }

    public void GoBackToRoute(PatrolPath path)
    {
        Debug.Log(transform.name + " 完成 coroutine 巡迴自己路徑  等待search 回去");
        waitProcess = false;
        findRoute = true;
        dynamicPatrol = false;
        newPatrolPath = path;
    }


    void ChangeState(EnemyState state) {
        if(state != EnemyState.Suspect) curLookNum = 0;
        stateStep = 0;
        curState = state;
    }

    bool DetectPlayer() {
        if(patrolManager.InTest)return false;
        if (enemyManager.player.Visible) {
            Vector3 playerPos = enemyManager.player.position;

            playerDir = playerPos - transform.position;
            if (playerDir.sqrMagnitude <= sightRadius * sightRadius) {
                if (Physics.Raycast(transform.position, playerDir, playerDir.magnitude, enemyManager.obstacleMask) == false) {
                    float angle = Vector3.Angle(transform.forward, playerDir);
                    if (angle <= sightAngle * 0.5f)
                    {
                        playerLastPos = playerPos;

                        return true;
                    }
                }
            }

            //if (Physics.Linecast(playerPos, transform.position, enemyManager.obstacleMask) == false)
            //{
            //    playerDir = playerPos - transform.position;
            //    if (playerDir.sqrMagnitude <= sightRadius * sightRadius)
            //    {
            //        float angle = Vector3.Angle(transform.forward, playerDir);
            //        if (angle <= sightAngle * 0.5f)
            //        {
            //            playerLastPos = playerPos;

            //            return true;
            //        }
            //    }
            //}
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

            //檢查有沒有動態變化路線，並在走到變化路線上時變更路線和id
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
                    DynamicChangingPathCBK(this, newPatrolPath);
                    lastPatrolPath = patrolPath;
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
        if (playerDir.sqrMagnitude > 0.36f)
        {
            Vector3 moveFWD = playerDir.normalized;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
            transform.position += moveFWD * chaseSpeed * Time.deltaTime;
        }
        else {
            Debug.Log("gotccccccchhhhhhhaaaaaa  gameover");
        }
    }

    public void InTestSetSearch(Vector3 detectPoint) {
        transform.position = detectPoint;
        playerLastPos = detectPoint;
        ChangeState(EnemyState.Search);
    }

    void Searching(bool findPlayer) {
        if (findPlayer) { 
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(playerDir), Time.deltaTime * moveRotateSpeed);
            seePlayerTime += Time.deltaTime;
            if (seePlayerTime > reflectTime) {
                seePlayerTime = .0f;
                Debug.Log("searching   find player  " + transform.forward);
                //Debug.Break();
                ChangeState(EnemyState.Chase);
                if (!renewPatroling) {
                    if (waitProcess)
                    {
                        waitProcess = false;
                        patrolManager.CancleRequest(curRequest, this);
                    }
                    if (findRoute)
                    {
                        findRoute = false;
                        patrolEnd = false;
                        lastPatrolPath = patrolPath;
                        patrolPath = newPatrolPath;
                        passEnemy = null;
                    }
                }
                return;
            } 
            
        }

        Vector3 diff = new Vector3(0,0,0);
        if (stateStep == 0)
        {
            //先走到懷疑點確認，之後再送出改變路徑請求，因為有可能不斷重複找到>追丟>找到>追丟
            Debug.Log("search  " + playerLastPos);
            diff = (playerLastPos - transform.position);
            if (diff.sqrMagnitude > 0.5f)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(diff), Time.deltaTime * moveRotateSpeed);
                transform.position += diff.normalized * chaseSpeed * Time.deltaTime;
            }
            else
            {
                stateStep++;
                LSideLookDir = new Vector3(-transform.forward.z, 0, transform.forward.x);
                RSideLookDir = new Vector3(transform.forward.z, 0, -transform.forward.x);
                lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, (Random.Range(.0f, 1.0f) >= 0.5f ? LSideLookDir : RSideLookDir), Vector3.up));
            }
        }
        else if (stateStep == 1) {
            //要求動態路徑
            if (!renewPatroling) {
                Debug.Log(transform.name + "  要求動態路徑 來自enemy.cs");
                curRequest = patrolManager.RequestDynamicPatrol(curRequest, playerLastPos, this);
                waitProcess = true;
            }
            stateStep++;

        }
        else if (stateStep == 2)
        {
            //第一階段先轉到固定位
            float angle = Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * 1.2f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(LSideLookDir);
                stateStep = 3;
            }
            //Debug.Log("第一階段 轉向  angle " + angle + "   " + LSideLookDir) ;
        }
        else if (stateStep == 3)
        {
            float angle = Vector3.SignedAngle(transform.forward, RSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, -lookClockWay * lookRotateSpeed * 1.2f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(RSideLookDir);
                stateStep = 4;
                curLookNum++;
                if (curLookNum >= 3)
                {
                    lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, -diff, Vector3.up));
                    stateStep = 5;
                }
            }
            //Debug.Log("第二階段 轉向  angle " + angle + "   " + RSideLookDir);
        }
        else if (stateStep == 4)
        {
            float angle = Vector3.SignedAngle(transform.forward, LSideLookDir, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * 1.2f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(LSideLookDir);
                stateStep = 3;
                curLookNum++;
                if (curLookNum >= 3)
                {
                    lookClockWay = Mathf.Sign(Vector3.SignedAngle(transform.forward, -diff, Vector3.up));
                    stateStep = 5;
                }
            }
        }
        else
        {
            if (findRoute)
            {
                Debug.Log("搜尋完  且找到路");
                findRoute = false;
                patrolEnd = false;
                if (!renewPatroling)
                {
                    lastPatrolPath = patrolPath;
                    patrolPath = newPatrolPath;
                    if (!enemyManager.CountEnemyChangePath(this))
                    {
                        if (dynamicPatrol)
                        {
                            enemyManager.conversationManager.UseContent(transform, 0);
                            DynamicChangingPathCBK(this, patrolPath);
                            ChangeState(EnemyState.Patrol);
                        }
                        else
                        {
                            ChangeState(EnemyState.GoBackRoute);
                            //有路線傳給其他敵人
                            if (passEnemy != null)
                            {
                                enemyManager.conversationManager.UseContent(transform, 0);
                                passEnemy.SetNewPatrolPath(passPoint, passPath, passCBK);
                                passEnemy = null;
                            }

                        }
                    }
                    else {
                        stateStep = 2;
                        curLookNum = 0;
                    } 
                }
                else {
                    renewPatroling = false;
                    //看自己有沒有在新路線裡，如果沒有就要尋路
                    bool inPatrol = false;
                    int patrolID = 0;
                    for (int i = 0; i < patrolPath.pathPoints.Count - 1; i++)
                    {
                        Vector3 pathDir = patrolPath.pathPoints[i + 1] - patrolPath.pathPoints[i];
                        Vector3 pathDirNormal = pathDir.normalized;
                        Vector3 pointDir = transform.position - patrolPath.pathPoints[i];

                        float length = Vector3.Cross(pathDirNormal, pointDir).magnitude;
                        float lineDirLength = Vector3.Dot(pointDir, pathDirNormal);

                        Debug.Log("lind dir " + pathDir);
                        Debug.Log("length " + length);
                        Debug.Log("lineDirLength " + lineDirLength);

                        if (length < 1.0f && lineDirLength >= 0 && Mathf.Abs(lineDirLength) <= pathDir.magnitude)
                        {
                            Debug.Log("In new patrol path");
                            inPatrol = true;
                            patrolID = i + 1;
                            break;
                        }
                    }
                    if (inPatrol)
                    {
                        patrolPath.SetPatrolPathID(patrolID);
                        ChangeState(EnemyState.Patrol);
                    }
                    else {
                        patrolManager.RequestBackPatrol(null, transform.position, this);
                        stateStep = 2;
                        curLookNum = 0;
                    } 
                }
                
            }


            //float angle = Vector3.SignedAngle(transform.forward, -diff, Vector3.up);
            //if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, lookClockWay * lookRotateSpeed * Time.deltaTime, 0);
            //else
            //{
            //    transform.rotation = Quaternion.LookRotation(-diff);
            //}
        }
    }

    void GoingBackRoute() {
        if (patrolEnd)
        {
            float angle = Vector3.SignedAngle(transform.forward, moveFWD, Vector3.up);
            if (Mathf.Abs(angle) >= lookRotateSpeed * Time.deltaTime * 2.0f) transform.rotation *= Quaternion.Euler(0, Mathf.Sign(angle) * lookRotateSpeed * 1.5f * Time.deltaTime, 0);
            else
            {
                transform.rotation = Quaternion.LookRotation(moveFWD);
                patrolEnd = false;
            }
            return;
        }

        Vector3 targetPos = new Vector3(-100, 0, -100);
        if (patrolPath.MoveBackPatrolRoute(transform.position, ref targetPos))
        {
            Debug.Log("go back to  " + targetPos);
            moveFWD = new Vector3(targetPos.x - transform.position.x, 0, targetPos.z - transform.position.z).normalized;
            float angle = Vector3.Angle(transform.forward, moveFWD);
            if (angle > 120.0f)
            {
                patrolEnd = true;
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveFWD), Time.deltaTime * moveRotateSpeed);
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }
        }
        else {
            
            int connectID = -100;
            Debug.Log(lastPatrolPath.pathPoints.IndexOf(targetPos) + " ： connect pos  " + targetPos);
            connectID = lastPatrolPath.pathPoints.IndexOf(targetPos);
            if (connectID >= lastPatrolPath.pathPoints.Count - 1)
            {
                Debug.Log(" ChangingPathConnectID  " + (connectID) + "    " + lastPatrolPath.GetPathPoint(connectID));
                lastPatrolPath.SetPatrolPathID(connectID);
            }
            else if (connectID == 0)
            {
                Debug.Log(" ChangingPathConnectID  " + (connectID + 1) + "    " + lastPatrolPath.GetPathPoint(connectID + 1));
                lastPatrolPath.SetPatrolPathID(connectID + 1);
            }
            else {
                if (!lastPatrolPath.Reverse) connectID = lastPatrolPath.pathPoints.IndexOf(targetPos);
                else connectID = lastPatrolPath.pathPoints.Count - 1 - lastPatrolPath.pathPoints.IndexOf(targetPos);
                Debug.Log(" ChangingPathConnectID  " + (connectID + 1) + "    " + lastPatrolPath.GetPathPoint(connectID + 1));
                lastPatrolPath.SetPatrolPathID(connectID + 1);
            }
            patrolPath = lastPatrolPath;
            ChangeState(EnemyState.Patrol);
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
