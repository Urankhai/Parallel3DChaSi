using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


[BurstCompile]
public struct ParallelLoSChannel : IJobParallelFor
{
    [ReadOnly] public bool OmniAntennaFlag;
    [ReadOnly] public int FFTSize;
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector3> CarsFwd;
    [ReadOnly] public NativeArray<Vector2Int> Links;
    [ReadOnly] public NativeArray<RaycastHit> raycastresults;
    [ReadOnly] public NativeArray<float> inverseLambdas;
    [ReadOnly] public NativeArray<Vector2> Pattern;

    [ReadOnly] public NativeArray<Vector3> Corners; // [coord, norm, perp][coord, norm, perp]...[coord, norm, perp] given number of corners

    [WriteOnly] public NativeArray<System.Numerics.Complex> HLoS;
    
    public void Execute(int index)
    {
        int i_link = Mathf.FloorToInt(index / FFTSize);

        


        int i_sub = index - i_link * FFTSize; // calculating index within FFT array

        Vector3 car1 = CarsPositions[Links[i_link].x];
        Vector3 car2 = CarsPositions[Links[i_link].y];

        Vector3 fwd1 = CarsFwd[Links[i_link].x];
        Vector3 fwd2 = CarsFwd[Links[i_link].y];

        float GroundReflCoef = 0.7f;


        Vector3 LoS_dir = car2 - car1;
        Vector3 LoS_dir_flat = new Vector3(LoS_dir.x, 0, LoS_dir.z);
        Vector3 LoS_dir_nrom = LoS_dir_flat.normalized;



        float antenna_gain = 1;
        if (OmniAntennaFlag == false)
        {
            float phi1 = Mathf.Acos(Vector3.Dot(fwd1, LoS_dir_nrom));
            float phi2 = Mathf.Acos(Vector3.Dot(fwd2, -LoS_dir_nrom));

            antenna_gain = EADF_rec(Pattern, phi1, phi2);
        }

        // line of sight parameters

        float LoS_dist = LoS_dir.magnitude;
        float LoS_gain = antenna_gain / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);
        //float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

        double ReExpLoS = Mathf.Cos(2.0f * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
        double ImExpLoS = Mathf.Sin(2.0f * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
        // defining exponent
        System.Numerics.Complex ExpLoS = new System.Numerics.Complex(ReExpLoS, ImExpLoS);


        // ground reflection parameters
        float Fresnel_coef = antenna_gain * GroundReflCoef; // TODO: should be calculated correctly
        float totalhight = car1.y + car2.y;
        float ground_dist = Mathf.Sqrt(LoS_dist * LoS_dist + totalhight * totalhight);
        float ground_gain = Fresnel_coef / (inverseLambdas[i_sub] * 4 * Mathf.PI * ground_dist);

        double ReExpGround = Mathf.Cos(2.0f * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
        double ImExpGround = Mathf.Sin(2.0f * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
        // defining exponent
        System.Numerics.Complex ExpGround = new System.Numerics.Complex(ReExpGround, ImExpGround);

        HLoS[index] = LoS_gain * ExpLoS + ground_gain * ExpGround;

        /*
=======
>>>>>>> parent of 06e66a3 (additional fixes)
        if (raycastresults[i_link].distance == 0)
        {

            Vector3 LoS_dir = car2 - car1;
            Vector3 LoS_dir_flat = new Vector3(LoS_dir.x, 0, LoS_dir.z);
            Vector3 LoS_dir_nrom = LoS_dir_flat.normalized;



            float antenna_gain = 1;
            if (OmniAntennaFlag == false)
            {
                float phi1 = Mathf.Acos(Vector3.Dot(fwd1, LoS_dir_nrom));
                float phi2 = Mathf.Acos(Vector3.Dot(fwd2, -LoS_dir_nrom));

                antenna_gain = EADF_rec(Pattern, phi1, phi2);
            }

            // line of sight parameters

            float LoS_dist = LoS_dir.magnitude;
            float LoS_gain = antenna_gain / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);
            //float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

            double ReExpLoS = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            double ImExpLoS = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            // defining exponent
            System.Numerics.Complex ExpLoS = new System.Numerics.Complex(ReExpLoS, ImExpLoS);


            // ground reflection parameters
            float Fresnel_coef = antenna_gain * GroundReflCoef; // TODO: should be calculated correctly
            float totalhight = car1.y + car2.y;
            float ground_dist = Mathf.Sqrt(LoS_dist * LoS_dist + totalhight * totalhight);
            float ground_gain = Fresnel_coef / (inverseLambdas[i_sub] * 4 * Mathf.PI * ground_dist);

            double ReExpGround = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
            double ImExpGround = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
            // defining exponent
            System.Numerics.Complex ExpGround = new System.Numerics.Complex(ReExpGround, ImExpGround);

            HLoS[index] = (LoS_gain * ExpLoS + ground_gain * ExpGround);
        }
        else
        {
            Vector3 corner = Corners[6];

            Vector3 LoS_dir1 = corner - car1;
            Vector3 LoS_dir2 = corner - car2;

            Vector3 LoS_dir1_flat = new Vector3(LoS_dir1.x, 0, LoS_dir1.z);
            Vector3 LoS_dir1_nrom = LoS_dir1_flat.normalized;

            Vector3 LoS_dir2_flat = new Vector3(LoS_dir2.x, 0, LoS_dir2.z);
            Vector3 LoS_dir2_nrom = LoS_dir2_flat.normalized;


            float antenna_gain = 1;
            if (OmniAntennaFlag == false)
            {
                float phi1 = Mathf.Acos(Vector3.Dot(fwd1, LoS_dir1_nrom));
                float phi2 = Mathf.Acos(Vector3.Dot(fwd2, -LoS_dir2_nrom));

                antenna_gain = EADF_rec(Pattern, phi1, phi2);
            }

            // line of sight parameters

            float LoS_dist = LoS_dir1.magnitude + LoS_dir1.magnitude;
            float LoS_gain = antenna_gain / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);
            //float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

            double ReExpLoS = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            double ImExpLoS = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * LoS_dist);
            // defining exponent
            System.Numerics.Complex ExpLoS = new System.Numerics.Complex(ReExpLoS, ImExpLoS);


            // ground reflection parameters
            float Fresnel_coef = antenna_gain * GroundReflCoef; // TODO: should be calculated correctly
            float totalhight = car1.y + car2.y;
            float ground_dist = Mathf.Sqrt(LoS_dist * LoS_dist + totalhight * totalhight);
            float ground_gain = Fresnel_coef / (inverseLambdas[i_sub] * 4 * Mathf.PI * ground_dist);

            double ReExpGround = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
            double ImExpGround = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * ground_dist);
            // defining exponent
            System.Numerics.Complex ExpGround = new System.Numerics.Complex(ReExpGround, ImExpGround);

            HLoS[index] = (LoS_gain * ExpLoS + ground_gain * ExpGround);
        }

        

        /*
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

            

            float antenna_gain = 1;
            if (OmniAntennaFlag == false)
            {
                float phi1 = Mathf.Acos(Vector3.Dot(fwd1, LoS_dir_nrom));
                float phi2 = Mathf.Acos(Vector3.Dot(fwd2, -LoS_dir_nrom));

                antenna_gain = EADF_rec(Pattern, phi1, phi2);
            }

            // line of sight parameters

            float LoS_dist = LoS_dir.magnitude;
            float LoS_gain = antenna_gain / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);
            //float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

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

            ////////////////////////////////////////
            // Calculating diffraction coefficient//
            ////////////////////////////////////////
            Vector3 Dcar1 = CarsPositions[0]; // police car
            Vector3 corner1 = new Vector3(Corners[3].x, Dcar1.y, Corners[3].z); // we elevate the corner to the hight of the car
            Vector3 normal1 = Corners[4]; // since we consider only two cars

            Vector3 Dcar2 = CarsPositions[1]; // red car
            Vector3 corner2 = new Vector3(Corners[0].x, Dcar2.y, Corners[0].z); // we elevate the corner to the hight of the car
            Vector3 normal2 = Corners[1];

            float diffraction_coefficient = Diffraction_LoS(Dcar1, Dcar2, corner1, corner2, normal1, normal2, inverseLambdas[i_sub]);
            DiffCoeff = diffraction_coefficient;
            ////////////////////////////////////////
            // Calculating diffraction coefficient//
            ////////////////////////////////////////

            HLoS[index] = (LoS_gain * ExpLoS + ground_gain * ExpGround)/diffraction_coefficient;
        }
        else
        {
            int diffraction_scenario = 1; // 0 no diffraction, 1 knife edge model, 2 synthetic diffraction model


            if (diffraction_scenario == 0)
            {
                HLoS[index] = 0;
            }
            else if (diffraction_scenario == 1)
            {
                int i_sub = index - i_link * FFTSize; // calculating index within FFT array

                // implementation of diffraction effect. Works only for two cars: red and police
                // keep in mind construction of the Corners array: Corners = [[coord, norm, perp][coord, norm, perp]...[coord, norm, perp]] given number of corners
                Vector3 car1 = CarsPositions[0]; // police car
                Vector3 corner1 = new Vector3(Corners[3].x, car1.y, Corners[3].z); // we elevate the corner to the hight of the car
                Vector3 normal1 = Corners[4]; // since we consider only two cars

                Vector3 car2 = CarsPositions[1]; // red car
                Vector3 corner2 = new Vector3(Corners[0].x, car2.y, Corners[0].z); // we elevate the corner to the hight of the car
                Vector3 normal2 = Corners[1];
                
                Vector3 car1_corner = corner1 - car1;
                Vector3 car2_corner = corner2 - car2;

                float corners_dist = (corner2 - corner1).magnitude;
                float dist1 = (car1_corner).magnitude + corners_dist / 2;
                float dist2 = (car2_corner).magnitude + corners_dist / 2;
                #region
                /*
                Vector3 direction1 = car1_corner.normalized;
                Vector3 direction2 = car2_corner.normalized;

                // angles from Carl's matlab code
                //float phi = Mathf.Acos(Vector3.Dot(direction1, -direction2));
                float alpha1 = Mathf.PI / 2.0f - Mathf.Acos(Vector3.Dot(direction1, normal1));
                float alpha2 = Mathf.PI / 2.0f - Mathf.Acos(Vector3.Dot(direction2, normal2));
                float phi = alpha1 + alpha2;


                
                //float lambda = 3.0f / 57.0f;
                float nu = phi * Mathf.Sqrt(2.0f * inverseLambdas[i_sub] / (1.0f / dist1 + 1.0f / dist2));
                
                
                
                float diff_K = 2.2131f; // it's 10^(6.9/20)
                float diffraction_coefficient =  diff_K * ( nu - 0.1f + Mathf.Sqrt( (nu - 0.1f) * (nu - 0.1f) + 1.0f )); // the main formula for diffraction
                
                #endregion

                float diffraction_coefficient = Diffraction_LoS(car1, car2, corner1, corner2, normal1, normal2, inverseLambdas[i_sub]);
                DiffCoeff = diffraction_coefficient;

                // standard stuff
                float total_distance = dist1 + dist2;
                float grond_distance = Mathf.Sqrt( (car1.y + car2.y) * (car1.y + car2.y) + total_distance * total_distance );

                ///////////////////////////////////////////////////
                // calculation of attenuations
                ///////////////////////////////////////////////////
                float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * total_distance);
                // ground reflection parameters
                float Fresnel_coef = 0.5f; // TODO: should be calculated correctly
                float ground_gain = Fresnel_coef / (inverseLambdas[i_sub] * 4 * Mathf.PI * grond_distance);

                // LoS Exp
                double ReExpLoS = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * total_distance);
                double ImExpLoS = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * total_distance);
                // defining exponent
                System.Numerics.Complex ExpLoS = new System.Numerics.Complex(ReExpLoS, ImExpLoS);

                // Ground Exp
                double ReExpGround = Mathf.Cos(2 * Mathf.PI * inverseLambdas[i_sub] * grond_distance);
                double ImExpGround = Mathf.Sin(2 * Mathf.PI * inverseLambdas[i_sub] * grond_distance);
                // defining exponent
                System.Numerics.Complex ExpGround = new System.Numerics.Complex(ReExpGround, ImExpGround);

                // finally, channel calculation
                HLoS[index] = (LoS_gain * ExpLoS + ground_gain * ExpGround)/diffraction_coefficient;
                //System.Numerics.Complex test_value2 = LoS_gain * ExpLoS + ground_gain * ExpGround;
                
            }
            else
            {
                // the following shows how much of the distance is covered by the buildings
                float penetration_distance = (raycastresults[0].point - raycastresults[3].point).magnitude;
                float D0 = 17.0f; // a threshold distance in meters

                if (penetration_distance < D0)
                {
                    // sinusoid function
                    float diffraction_coefficient = 0.5f * (Mathf.Sin(Mathf.PI * (D0 - penetration_distance) / D0 - 0.5f * Mathf.PI) + 1.0f);

                    int i_sub = index - i_link * FFTSize; // calculating index within FFT array
                    Vector3 car1 = CarsPositions[Links[i_link].x];
                    Vector3 car2 = CarsPositions[Links[i_link].y];

                    
                    Vector3 LoS_dir = car2 - car1;
                    
                    float antenna_gain = 1;
                    

                    // line of sight parameters

                    float LoS_dist = LoS_dir.magnitude;
                    float LoS_gain = antenna_gain / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);
                    //float LoS_gain = 1 / (inverseLambdas[i_sub] * 4 * Mathf.PI * LoS_dist);

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

                    HLoS[index] = diffraction_coefficient * (LoS_gain * ExpLoS + ground_gain * ExpGround);
                }
                else
                { HLoS[index] = 0; }
            }
        }*/
    }
    
    private float Diffraction_LoS(Vector3 car1, Vector3 car2, Vector3 corner1, Vector3 corner2, Vector3 normal1, Vector3 normal2, float inverselambda)
    {
        // Knife edge effect calculating function (from Carl's paper with slight modification in angle calculation)
        Vector3 virtual_corner = (corner1 + corner2) / 2.0f;
        Vector3 virtual_normal = (normal1 + normal2) / 2.0f;

        Vector3 car1_corner = virtual_corner - car1;
        Vector3 car2_corner = virtual_corner - car2;

        float dist1 = (car1_corner).magnitude;
        float dist2 = (car2_corner).magnitude;

        Vector3 direction1 = car1_corner.normalized;
        Vector3 direction2 = car2_corner.normalized;

        // angles from Carl's matlab code
        // float phi = Mathf.Acos(Vector3.Dot(direction1, -direction2));
        float alpha1 = Mathf.PI / 2.0f - Mathf.Acos(Vector3.Dot(direction1, virtual_normal));
        float alpha2 = Mathf.PI / 2.0f - Mathf.Acos(Vector3.Dot(direction2, virtual_normal));
        float phi = alpha1 + alpha2;

        float nu = phi * Mathf.Sqrt(2.0f * inverselambda / (1.0f / dist1 + 1.0f / dist2));

        float diff_K = 2.2131f; // it's 10^(6.9/20)
        float DiffractionCoefficient = 1.0f;
        if (nu > -0.7)
        {
            DiffractionCoefficient = diff_K * (nu - 0.1f + Mathf.Sqrt((nu - 0.1f) * (nu - 0.1f) + 1.0f)); // the main formula for diffraction
        }
        
        return DiffractionCoefficient;
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
    /*
    private float EADF_Reconstruction(NativeArray<Vector2> Pattern, float angle_azimuth, float angle_elevation)
    {
        System.Numerics.Complex Gain1 = 0;
        int L1 = Pattern.Length;
        int L2 = Pattern.Length;
        // Analog of Inverse Fourier Transform
        for (int i = 0; i < L1; i++)
        {
            float mu1 = -(L1 - 1) / 2 + i;
            float mu2 = -(L2 - 1) / 2 + i;
            System.Numerics.Complex db_azimuth = new System.Numerics.Complex(Mathf.Cos(angle_azimuth * mu1), Mathf.Sin(angle_azimuth * mu1));
            System.Numerics.Complex complex_pattern = new System.Numerics.Complex(Pattern[i].x, Pattern[i].y);
            Gain1 += System.Numerics.Complex.Multiply(complex_pattern, db_azimuth);

        }
        float Gain = (float)System.Numerics.Complex.Abs(Gain1);
        return Gain;
    }
    */
}



[BurstCompile]
public struct ParallelLoSDetection : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> CarsPositions;
    [ReadOnly] public NativeArray<Vector2Int> Links;
    [WriteOnly] public NativeArray<RaycastCommand> commands;
    public void Execute(int index)
    {
        if (index < 3)
        {
            int TxCarID = Links[index].x;
            int RxCarID = Links[index].y;
            Vector3 temp_direction = CarsPositions[RxCarID] - CarsPositions[TxCarID];
            commands[index] = new RaycastCommand(CarsPositions[TxCarID], temp_direction.normalized, temp_direction.magnitude);
        }
        else
        {
            int TxCarID = Links[index - 3].y;
            int RxCarID = Links[index - 3].x;
            Vector3 temp_direction = CarsPositions[RxCarID] - CarsPositions[TxCarID];
            commands[index] = new RaycastCommand(CarsPositions[TxCarID], temp_direction.normalized, temp_direction.magnitude);
        }
    }
}