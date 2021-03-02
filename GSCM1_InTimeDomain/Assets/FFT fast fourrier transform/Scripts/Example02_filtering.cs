using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Numerics;

public class Example02_filtering : MonoBehaviour
{

    public enum filterMode { low_pass, high_pass };

    [Space]
    [Header("[!] set fixed time to 0.01f [!]")]
    [Header("FILTERING")]
    [Space]

    //samplingfrequency 
    public int samplingFrequency;

    //input-output values for the FFT
    double[] X_inputValues;
    double[] Y_inputValues;
    double[] Y_output;

    double[] Y_inputValues_filtered;
    double[] Y_output_filtered;

    //list with the input values
    public List<double> Y_values;

    //these are the frequencies, amplitude and phase of the sinus added to the signal
    [Space(10)]
    [Header("F,A and P must be same lenght")]
    [Tooltip("Add different values of amplitude/frequency to change the time response")]
    [Space]
    public double[] freq;
    public double[] Amp;
    public double[] phase;


    //this is the size of the window of data
    public int windowSize = 1024;


    //counts the steps taken
    long count = 0;

    //

    //the chart transforms for the time and frequency
    [Space(10)]
    [Header("CHARTS FOR DRAWING")]
    [Space]
    public Transform tfTime;
    public Transform tfFreq;
    public Transform tfTime_filt;
    public Transform tfFreq_filt;

    [Space(10)]
    [Header("Select filtering type")]
    [Space]
    public filterMode filtermode;

    //this is the filter parameter
    public double alpha = 0.2f;

    void Start()
    {
        X_inputValues = new double[windowSize];
        Y_inputValues = new double[windowSize];

        samplingFrequency = (int)(1 / Time.fixedDeltaTime);

        for (int ii = 0; ii < windowSize; ii++)
        {
            Y_values.Add(0);
        }

        StartCoroutine(filterSignal());
    }



    /// <summary>
    ///  This corrutine obtains these steps in order:
    ///  1) generates a time response using SIN() signals
    ///  2) obtains a window of the signal
    ///  3) applies the FFT
    ///  4) filters the signal
    ///  5) applies FFT of filtered signal
    /// </summary>

    IEnumerator filterSignal()
    {
        while (true)
        {

            // add point to the time chart
            double signal = 0;
            for (int jj = 0; jj < Amp.Length; jj++)
            {
                signal += (double)(Amp[jj] * Mathf.Sin(2 * Mathf.PI * (float)freq[jj] * (float)count * 1 / (float)samplingFrequency + (float)phase[jj]));
            }
            count++;

            Y_values.Add(signal);


            if (Y_values.Count >= windowSize)
            {
                //ONE IS THE INPUT VALUES

                //this is the window size
                for (int ii = 0; ii < windowSize; ii++)
                {
                    X_inputValues[ii] = ii;


                    X_inputValues[ii] = ii;
                    Y_inputValues[ii] = (Y_values[ii + Y_values.Count - 1 - windowSize]);

                }



                //perform complex opterations and set up the arrays
                Complex[] inputSignal_Time = new Complex[windowSize];
                Complex[] outputSignal_Freq = new Complex[windowSize];


                inputSignal_Time = FastFourierTransform.doubleToComplex(Y_inputValues);


                //result is the iutput values once FFT has been applied
                outputSignal_Freq = FastFourierTransform.FFT(inputSignal_Time, false);

                
                Y_output = new double[windowSize];
                //get module of complex number
                for (int ii = 0; ii < windowSize; ii++)
                {
                    //Debug.Log(ii);
                    Y_output[ii] = (double)Complex.Abs(outputSignal_Freq[ii]);
                }



                //filter process
                if (filtermode==filterMode.low_pass)
                {
                    Y_inputValues_filtered = Filters.low_pass_band(Y_inputValues, alpha);
                }
                else if (filtermode == filterMode.high_pass)
                {
                    Y_inputValues_filtered = Filters.high_pass_band(Y_inputValues, alpha);
                }

               

                //recalculate FFT of filtered 
                //perform complex opterations and set up the arrays
                inputSignal_Time = new Complex[windowSize];
                outputSignal_Freq = new Complex[windowSize];
                
                inputSignal_Time = FastFourierTransform.doubleToComplex(Y_inputValues_filtered);
                
                //result is the iutput values once FFT has been applied
                outputSignal_Freq = FastFourierTransform.FFT(inputSignal_Time, false);


                Y_output_filtered = new double[windowSize];
                //get module of complex number
                for (int ii = 0; ii < windowSize; ii++)
                {
                    //Debug.Log(ii);
                    Y_output_filtered[ii] = (double)Complex.Abs(outputSignal_Freq[ii]);
                }

            }


            yield return new WaitForFixedUpdate();


        }

    }



    void FixedUpdate()
    {
        //draw both charts: time-domain and frequency-domain
        Drawing.drawChart(tfTime, X_inputValues, Y_inputValues, "time");
        Drawing.drawChart(tfFreq, X_inputValues, Y_output, "frequency");

        //draw both charts: time-domain and frequency-domain
        Drawing.drawChart(tfTime_filt, X_inputValues, Y_inputValues_filtered, "time");
        Drawing.drawChart(tfFreq_filt, X_inputValues, Y_output_filtered, "frequency");
    }







}
