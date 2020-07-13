using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayInfomation
{
    int length ;
    public int dynamicPatrolChnge = 0;
    public bool dynamicFirst;
    public float[] NormalTime;
    public float[] DynamicTime;
    public int[] NormalDeadNum;
    public int[] DynamicDeadNum;
    public int[] NormalThrowNum;
    public int[] DynamicThrowNum;

    public PlayInfomation(int _length) {
        length = _length;
        NormalDeadNum = new int[length];
        DynamicDeadNum = new int[length];
        NormalTime = new float[length];
        DynamicTime = new float[length];
        NormalThrowNum = new int[length];
        DynamicThrowNum = new int[length];
    }

    public void CountDeadNum(bool isDynamic, int id) {
        if(id >= length) id -= length;
        if (isDynamic) DynamicDeadNum[id]++;
        else NormalDeadNum[id]++;
    }
    public void SetTime(bool isDynamic, int id, float value) {
        if (id >= length) id -= length;
        if (isDynamic) DynamicTime[id] = value;
        else NormalTime[id] = value;
    }
    public void GetThrowNum(bool isDynamic, int id, int value)
    {
        if (id >= length) id -= length;
        if (isDynamic) DynamicThrowNum[id] = value;
        else NormalThrowNum[id] = value;
    }
    public void SetDynamicPatrolNum(int value) {
        dynamicPatrolChnge = value;
    }
}
