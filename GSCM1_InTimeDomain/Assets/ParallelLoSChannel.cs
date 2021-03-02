using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[BurstCompile]
public struct ParallelLoSChannel : IJobParallelFor
{
    [ReadOnly] public int FFTSize;
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector3> CarsFwd;
    [ReadOnly] public NativeArray<Vector2Int> Links;
    [ReadOnly] public NativeArray<RaycastHit> raycastresults;
    [ReadOnly] public NativeArray<float> inverseLambdas;

    [WriteOnly] public NativeArray<System.Numerics.Complex> HLoS;
    public void Execute(int index)
    {
        int i_link = Mathf.FloorToInt(index / FFTSize);

        if (raycastresults[i_link].distance == 0)
        {
            int i_sub = index - i_link * FFTSize; // calculating index within FFT array
            Vector3 car1 = CarsPositions[Links[i_link].x];
            Vector3 car2 = CarsPositions[Links[i_link].y];

            // line of sight parameters
            float LoS_dist = (car2 - car1).magnitude;
            float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

            double ReExpLoS = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            double ImExpLoS = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            // defining exponent
            System.Numerics.Complex ExpLoS = new System.Numerics.Complex(ReExpLoS, ImExpLoS);


            // ground reflection parameters
            float Fresnel_coef = 0.5f; // TODO: should be calculated correctly
            float totalhight = car1.y + car2.y;
            float ground_dist = Mathf.Sqrt(LoS_dist * LoS_dist + totalhight * totalhight);
            float ground_gain = Fresnel_coef / (inverseLambdas[i_sub] * 4 * Mathf.PI * ground_dist);

            double ReExpGround = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
            double ImExpGround = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
            // defining exponent
            System.Numerics.Complex ExpGround = new System.Numerics.Complex(ReExpGround, ImExpGround);

            HLoS[index] = LoS_gain * ExpLoS + ground_gain * ExpGround;
        }
        else
        { HLoS[index] = 0; }
    }
}



[BurstCompile]
public struct ParallelLoSDetection : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector2Int> Links;
    [WriteOnly] public NativeArray<RaycastCommand> commands;
    public void Execute(int index)
    {
        int TxCarID = Links[index].x;
        int RxCarID = Links[index].y;
        Vector3 temp_direction = CarsPositions[RxCarID] - CarsPositions[TxCarID];
        commands[index] = new RaycastCommand(CarsPositions[TxCarID], temp_direction.normalized, temp_direction.magnitude);
    }
}