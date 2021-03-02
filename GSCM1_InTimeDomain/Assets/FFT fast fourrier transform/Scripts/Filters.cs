using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Filters
{
    //high pass filter
    public static double[] high_pass_band(double[] inp, double alpha)
    {
        double[] outp = new double[inp.Length];

        outp[0] = alpha * inp[0];

        for (int jj = 1; jj < inp.Length; jj++)
        {
            outp[jj] = alpha * (outp[jj - 1]) + alpha * (inp[jj] - inp[jj - 1]);
        }

        return outp;

    }

    // low pass filter
    public static double[] low_pass_band(double[] inp, double alpha)
    {
        double[] outp = new double[inp.Length];

        outp[0] = alpha * inp[0];

        for (int jj = 1; jj < inp.Length; jj++)
        {
            outp[jj] = alpha * (inp[jj]) + (1-alpha) * (outp[jj - 1]);
        }

        return outp;

    }
}
