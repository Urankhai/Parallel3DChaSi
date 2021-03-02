using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using System;

[BurstCompile]

public struct Path2ParallelSearch : IJobParallelFor
{
    // the followings are disposed in the end of simulation
    [ReadOnly] public NativeArray<V6> SeenMPC2Table;
    [ReadOnly] public NativeArray<Vector2Int> LookUpTable2ID;
    [ReadOnly] public NativeArray<GeoComp> LookUpTable2;

    // the folliwings are disposed after the parallel job
    [ReadOnly] public NativeArray<int> Rx_MPC2Array;
    [ReadOnly] public NativeArray<float> Rx_MPC2Array_att;
    [ReadOnly] public NativeArray<int> Tx_MPC2;
    [ReadOnly] public NativeArray<float> Tx_MPC2_att;

    // simple reads for the execution
    [ReadOnly] public Vector3 Rx_Position;
    [ReadOnly] public Vector3 Tx_Position;
    [ReadOnly] public int MaxListsLength;
    [ReadOnly] public float Speed_of_Light;

    // the folliwings are disposed after the parallel job
    [WriteOnly] public NativeArray<Path2> SecondOrderPaths;
    [WriteOnly] public NativeArray<float> OutputDelays;
    [WriteOnly] public NativeArray<float> OutputAmplitudes;
    public void Execute(int index)
    {
        OutputDelays[index] = 0;
        OutputAmplitudes[index] = 0;

        V6 level1 = SeenMPC2Table[Rx_MPC2Array[index]];

        float distance1 = (level1.Coordinates - Rx_Position).magnitude;

        int temp_index = Mathf.FloorToInt(index / MaxListsLength);
        int temp_i = index - temp_index * MaxListsLength;
        if (temp_i <= LookUpTable2ID[Rx_MPC2Array[index]].y - LookUpTable2ID[Rx_MPC2Array[index]].x)
        {
            int temp_l = LookUpTable2ID[Rx_MPC2Array[index]].x + temp_i;

            V6 level2 = SeenMPC2Table[LookUpTable2[temp_l].MPCIndex];
            Vector3 level2_to_level1 = (level1.Coordinates - level2.Coordinates).normalized;
            float distance2 = (level1.Coordinates - level2.Coordinates).magnitude;

            // check if level2 is seen by Tx
            int existence_flag = 0;
            float Tx_att = 0f;
            for (int i = 0; i < Tx_MPC2.Length; i++)
            {
                if (LookUpTable2[temp_l].MPCIndex == Tx_MPC2[i])
                { 
                    existence_flag = 1;
                    Tx_att = Tx_MPC2_att[i];
                    break; 
                }
            }

            if (existence_flag == 1)
            {
                Vector3 normal1 = level1.Normal;
                Vector3 n_perp1 = new Vector3(-normal1.z, 0, normal1.x);
                Vector3 Rx_to_level1 = (level1.Coordinates - Rx_Position).normalized;

                Vector3 normal2 = level2.Normal;
                Vector3 n_perp2 = new Vector3(-normal2.z, 0, normal2.x);
                Vector3 level2_to_Tx = (Tx_Position - level2.Coordinates).normalized;

                // check if the two vectors come from different sides of the normal
                if ((Vector3.Dot(n_perp2, level2_to_level1) * Vector3.Dot(n_perp2, level2_to_Tx) < 0) && (Vector3.Dot(n_perp1, level2_to_level1) * Vector3.Dot(n_perp1, Rx_to_level1) < 0))
                {
                    double theta11 = Math.Acos(Vector3.Dot(normal1, -Rx_to_level1));
                    double theta12 = Math.Acos(Vector3.Dot(normal1, -level2_to_level1));
                    int I11, I12, I13;
                    if (theta11 + theta12 < 0.35)
                    { I11 = 0; }
                    else { I11 = 1; }
                    if (theta11 < 1.22)
                    { I12 = 0; }
                    else { I12 = 1; }
                    if (theta12 < 1.22)
                    { I13 = 0; }
                    else { I13 = 1; }

                    float angular_gain1 = (float)Math.Exp(-12 * ((theta11 + theta12 - 0.35) * I11 + (theta11 - 1.22) * I12 + (theta12 - 1.22) * I13));

                    double theta21 = Math.Acos(Vector3.Dot(normal2, level2_to_Tx));
                    double theta22 = Math.Acos(Vector3.Dot(normal1, level2_to_level1));
                    int I21, I22, I23;
                    if (theta21 + theta22 < 0.35)
                    { I21 = 0; }
                    else { I21 = 1; }
                    if (theta21 < 1.22)
                    { I22 = 0; }
                    else { I22 = 1; }
                    if (theta22 < 1.22)
                    { I23 = 0; }
                    else { I23 = 1; }

                    float angular_gain2 = (float)Math.Exp(-12 * ((theta21 + theta22 - 0.35) * I21 + (theta21 - 1.22) * I22 + (theta22 - 1.22) * I23));

                    float angular_gain = angular_gain1 * angular_gain2;

                    float distance3 = level2_to_Tx.magnitude;
                    float distance = distance1 + distance2 + distance3;
                    Path2 temp_path2 = new Path2(Rx_Position, level1.Coordinates, level2.Coordinates, Tx_Position, distance, angular_gain);
                    SecondOrderPaths[index] = temp_path2;

                    OutputDelays[index] = distance;// / Speed_of_Light;
                    OutputAmplitudes[index] = Tx_att * Rx_MPC2Array_att[index]*angular_gain * (1 / distance);
                }
            }
        }


    }
}
