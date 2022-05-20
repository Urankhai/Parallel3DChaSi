using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct ParallelPath2Search : IJobParallelFor
{
    [ReadOnly] public NativeArray<V6> MPC_Array; // coordinates and normals
    [ReadOnly] public NativeArray<Vector3> MPC_Perpendiculars; // Who is writing code like this??? It's actually Active_MPC2_Perpendiculars (perpendiculars Karl!!!)

    [ReadOnly] public NativeArray<RaycastCommand> commands;
    [ReadOnly] public NativeArray<RaycastHit> results;
    [ReadOnly] public NativeArray<Vector2Int> ID;

    [ReadOnly] public float maxDistance;
    [ReadOnly] public float angleThreshold;

    [WriteOnly] public NativeArray<SeenPath2> PossiblePath2;
    public void Execute(int index)
    {
        if (results[index].distance == 0)
        {
            float temp_dist = commands[index].distance;
            if (temp_dist < maxDistance)
            {
                //Vector3 c1 = MPC_Array[ID[index].x].Coordinates;
                Vector3 n1 = MPC_Array[ID[index].x].Normal;
                Vector3 p1 = MPC_Perpendiculars[ID[index].x];
                //Vector3 c2 = MPC_Array[ID[index].y].Coordinates;
                Vector3 n2 = MPC_Array[ID[index].y].Normal;
                Vector3 p2 = MPC_Perpendiculars[ID[index].y];

                //Vector3 temp_direction = (c2 - c1).normalized;

                Vector3 dir = commands[index].direction;

                if (Vector3.Dot(dir, n1) > angleThreshold && Vector3.Dot(dir, n2) < -angleThreshold)
                {
                    // MPC from
                    float signDep = Mathf.Sign(Vector3.Dot(dir, p1));
                    float aod = Mathf.Acos(Vector3.Dot(dir, n1));

                    // MPC to
                    float signArr = Mathf.Sign(Vector3.Dot(-dir, p2));
                    float aoa = Mathf.Acos(Vector3.Dot(-dir, n2));
                    
                    //float thr = (float)1.22;// Mathf.Acos(angleThreshold);

                    // calculating angular gain
                    //float gd = AngularGainFunc(aod, thr);
                    //float ga = AngularGainFunc(aoa, thr);
                    //float g0 = gd * ga; // according to the Carl's paper, it should be commented
                    float g0 = 1; // according to Carl's Matlab code

                    PossiblePath2[index] = new SeenPath2(ID[index], temp_dist, aod, aoa, signDep, signArr, g0);
                }
            }

        }
    }
    /*
    private float AngularGainFunc(float angle, float threshold)
    {
        float Gain = 1;
        if (Mathf.Abs(angle) > threshold)
        { Gain = Mathf.Exp(-12 * (Mathf.Abs(angle) - threshold)); }

        return Gain;
    }*/
}
