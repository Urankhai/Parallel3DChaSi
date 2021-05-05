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
    [ReadOnly] public NativeArray<Vector2> Pattern;

    [WriteOnly] public NativeArray<System.Numerics.Complex> HLoS;
    public void Execute(int index)
    {
        int i_link = Mathf.FloorToInt(index / FFTSize);

        if (raycastresults[i_link].distance == 0)
        {
            int i_sub = index - i_link * FFTSize; // calculating index within FFT array
            Vector3 car1 = CarsPositions[Links[i_link].x];
            Vector3 car2 = CarsPositions[Links[i_link].y];

            Vector3 fwd1 = CarsFwd[Links[i_link].x];
            Vector3 fwd2 = CarsFwd[Links[i_link].y];

            Vector3 LoS_dir = car2 - car1;
            Vector3 LoS_dir_flat = new Vector3(LoS_dir.x, 0, LoS_dir.z);
            Vector3 LoS_dir_nrom = LoS_dir_flat.normalized;

            float phi1 = Mathf.Acos(Vector3.Dot(fwd1, LoS_dir_nrom));
            float phi2 = Mathf.Acos(Vector3.Dot(fwd2, -LoS_dir_nrom));

            float antenna_gain = EADF_rec(Pattern, phi1, phi2);

            // line of sight parameters

            float LoS_dist = LoS_dir.magnitude;
            float LoS_gain = antenna_gain / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

            double ReExpLoS = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            double ImExpLoS = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            // defining exponent
            System.Numerics.Complex ExpLoS = new System.Numerics.Complex(ReExpLoS, ImExpLoS);


            // ground reflection parameters
            float Fresnel_coef = antenna_gain * 0.5f; // TODO: should be calculated correctly
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
    
    private float EADF_rec(NativeArray<Vector2> Pattern, float angle1, float angle2)
    {
        System.Numerics.Complex Gain1 = 0;
        System.Numerics.Complex Gain2 = 0;
        int L = Pattern.Length;
        for (int i = 0; i < L; i++)
        {
            //db1 = exp(1j * ang1.* mu1);
            float mu = -(L - 1)/2 + i;
            System.Numerics.Complex db1 = new System.Numerics.Complex(Mathf.Cos(angle1 * mu), Mathf.Sin(angle1 * mu));
            System.Numerics.Complex db2 = new System.Numerics.Complex(Mathf.Cos(angle2 * mu), Mathf.Sin(angle2 * mu));

            System.Numerics.Complex complex_pattern = new System.Numerics.Complex(Pattern[i].x, Pattern[i].y);
            Gain1 += System.Numerics.Complex.Multiply(complex_pattern, db1);
            Gain2 += System.Numerics.Complex.Multiply(complex_pattern, db2);
        }
        System.Numerics.Complex mult = System.Numerics.Complex.Multiply(Gain1, Gain2);
        float Gain = (float)System.Numerics.Complex.Abs(mult);
        return Gain;
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