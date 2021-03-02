using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


public struct Overlap
{
    public Vector2 InfInf;
    public Vector2 SupSup;

    public Overlap(Vector2 ii, Vector2 ss)
    {
        InfInf = ii;
        SupSup = ss;
    }
}

public partial class ChannelGenManager
{
    [BurstCompile]
    public struct AreaOverlaps : IJobParallelFor
    {
        [ReadOnly] public float MaxDist;
        [ReadOnly] public NativeArray<Vector3> Coordinates;
        [ReadOnly] public NativeArray<Vector2Int> Links;

        [WriteOnly] public NativeArray<Overlap> AreaArray1;
        [WriteOnly] public NativeArray<Overlap> TxAreaArray2;
        [WriteOnly] public NativeArray<Overlap> RxAreaArray2;
        [WriteOnly] public NativeArray<Overlap> TxAreaArray3;
        [WriteOnly] public NativeArray<Overlap> RxAreaArray3;
        public void Execute(int index)
        {
            Vector3 pos1 = Coordinates[Links[index].x]; // Note: X
            Vector3 pos2 = Coordinates[Links[index].y]; // Note: Y
            if ( Mathf.Abs(pos1.x - pos2.x) < 2 * MaxDist && Mathf.Abs(pos1.z - pos2.z) < 2 * MaxDist )
            {
                // finding edges of the first seen area
                Overlap seenarea1 = EdgesFind(pos1, MaxDist);
                // finding edges of the second seen area
                Overlap seenarea2 = EdgesFind(pos2, MaxDist);
                
                // finding edges of the overlapping area
                Vector2 ii = new Vector2(Mathf.Max(seenarea1.InfInf.x, seenarea2.InfInf.x), Mathf.Max(seenarea1.InfInf.y, seenarea2.InfInf.y));
                Vector2 ss = new Vector2(Mathf.Min(seenarea1.SupSup.x, seenarea2.SupSup.x), Mathf.Min(seenarea1.SupSup.y, seenarea2.SupSup.y));
                
                // writing it in the nativearray
                Overlap overlap1 = new Overlap(ii, ss);
                AreaArray1[index] = overlap1;

                // searching MPC2 areas overlap
                if (Mathf.Abs(pos1.x - pos2.x) < 1.5 * MaxDist && Mathf.Abs(pos1.z - pos2.z) < 1.5 * MaxDist)
                {
                    // Tx areas for MPC2
                    Overlap Tx_seenarea2 = EdgesFind(pos1, MaxDist/2);
                    Vector2 Tx_ii = new Vector2(Mathf.Max(overlap1.InfInf.x, Tx_seenarea2.InfInf.x), Mathf.Max(overlap1.InfInf.y, Tx_seenarea2.InfInf.y));
                    Vector2 Tx_ss = new Vector2(Mathf.Min(overlap1.SupSup.x, Tx_seenarea2.SupSup.x), Mathf.Min(overlap1.SupSup.y, Tx_seenarea2.SupSup.y));
                    Overlap Tx_overlap2 = new Overlap(Tx_ii, Tx_ss);
                    TxAreaArray2[index] = Tx_overlap2;

                    // Rx areas for MPC2
                    Overlap Rx_seenarea2 = EdgesFind(pos2, MaxDist/2);
                    Vector2 Rx_ii = new Vector2(Mathf.Max(overlap1.InfInf.x, Rx_seenarea2.InfInf.x), Mathf.Max(overlap1.InfInf.y, Rx_seenarea2.InfInf.y));
                    Vector2 Rx_ss = new Vector2(Mathf.Min(overlap1.SupSup.x, Rx_seenarea2.SupSup.x), Mathf.Min(overlap1.SupSup.y, Rx_seenarea2.SupSup.y));
                    Overlap Rx_overlap2 = new Overlap(Rx_ii, Rx_ss);
                    RxAreaArray2[index] = Rx_overlap2;

                    // searching MPC3 areas overlap
                    if (Mathf.Abs(pos1.x - pos2.x) < (4 * MaxDist)/3 && Mathf.Abs(pos1.z - pos2.z) < (4 * MaxDist)/3)
                    {
                        // Tx areas for MPC3
                        Overlap Tx_seenarea3 = EdgesFind(pos1, MaxDist / 3);
                        Vector2 Tx_ii3 = new Vector2(Mathf.Max(overlap1.InfInf.x, Tx_seenarea3.InfInf.x), Mathf.Max(overlap1.InfInf.y, Tx_seenarea3.InfInf.y));
                        Vector2 Tx_ss3 = new Vector2(Mathf.Min(overlap1.SupSup.x, Tx_seenarea3.SupSup.x), Mathf.Min(overlap1.SupSup.y, Tx_seenarea3.SupSup.y));
                        Overlap Tx_overlap3 = new Overlap(Tx_ii3, Tx_ss3);
                        TxAreaArray3[index] = Tx_overlap3;

                        // Rx areas for MPC3
                        Overlap Rx_seenarea3 = EdgesFind(pos2, MaxDist / 3);
                        Vector2 Rx_ii3 = new Vector2(Mathf.Max(overlap1.InfInf.x, Rx_seenarea3.InfInf.x), Mathf.Max(overlap1.InfInf.y, Rx_seenarea3.InfInf.y));
                        Vector2 Rx_ss3 = new Vector2(Mathf.Min(overlap1.SupSup.x, Rx_seenarea3.SupSup.x), Mathf.Min(overlap1.SupSup.y, Rx_seenarea3.SupSup.y));
                        Overlap Rx_overlap3 = new Overlap(Rx_ii3, Rx_ss3);
                        RxAreaArray3[index] = Rx_overlap3;
                    }
                }
            }
        }

        private Overlap EdgesFind(Vector3 p, float d)
        {
            float x_inf = p.x - d;
            float x_sup = p.x + d;
            float z_inf = p.z - d;
            float z_sup = p.z + d;
            Overlap overlap = new Overlap(new Vector2(x_inf, z_inf), new Vector2(x_sup, z_sup));
            return overlap;
        }
    }
}

