using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Numerics;

public class Example01_time_to_frequency : MonoBehaviour
{
    [Space]
    [Header("[!] set fixed time to 0.01f [!]")]
    [Header("TIME SIGNAL TO FREQUENCY FFT")]
    [Space]

    //samplingfrequency 
    public int samplingFrequency;

    //input-output values for the FFT
    double[] X_inputValues;
    double[] Y_inputValues;
    double[] Y_output;

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

    //this is used to obtain the frequency with a peak
    [Space(10)]
    [Header("MOST IMPORTANT FREQUENCY")]
    [Tooltip("This corresponds to the peak on the FFT")]
    [Space]
    public double maxf_obtained;

    //counts the steps taken
    long count = 0;

    //the chart transforms for the time and frequency
    [Space(10)]
    [Header("CHARTS FOR DRAWING")]
    [Space]
    public Transform tfTime;
    public Transform tfFreq;

    
    void Start()
    {
        X_inputValues = new double[windowSize];
        Y_inputValues = new double[windowSize];

        samplingFrequency = (int)(1 / Time.fixedDeltaTime);

        for (int ii = 0; ii < windowSize; ii++)
        {
            Y_values.Add(0);
        }

        StartCoroutine(time_to_frequency());
    }


    /// <summary>
    ///  This corrutine obtains these steps in order:
    ///  1) generates a time response using SIN() signals
    ///  2) obtains a window of the signal
    ///  3) applies the FFT
    /// </summary>

    IEnumerator time_to_frequency()
    {
        while (true)
        {
            
            // add point to the time chart
            double signal = 0;
            for (int jj = 0; jj < Amp.Length; jj++)
            {
                signal+= (double)(   Amp[jj] * Mathf.Sin(2 * Mathf.PI * (float)freq[jj] * (float)count *1/(float)samplingFrequency + (float)phase[jj])  );
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


                //result is the iutput values once DFT has been applied
                outputSignal_Freq = FastFourierTransform.FFT(inputSignal_Time,false);



                Y_output = new double[windowSize];
                //get module of complex number
                for (int ii = 0; ii < windowSize; ii++)
                {
                    //Debug.Log(ii);
                    Y_output[ii] = (double)Complex.Abs(outputSignal_Freq[ii]);
                }


                // find peak VALUES on the FFT
                double MaxPeak = -1000;
                int peakIndex = 0;
                for (int i = 1; i < Y_output.Length/2 - 1; i++)
                {
                    if (Y_output[i]>MaxPeak)
                    {
                        MaxPeak = Y_output[i];
                        peakIndex = i;
                    }
                }

                //store results
                maxf_obtained = (double)peakIndex / (double)windowSize/2* (double)samplingFrequency;
                
            }

           
            yield return new WaitForFixedUpdate();

     
        }

    }



    void FixedUpdate()
    {
        //draw both charts: time-domain and frequency-domain
        Drawing.drawChart(tfTime,X_inputValues, Y_inputValues, "time");
        Drawing.drawChart(tfFreq, X_inputValues, Y_output, "frequency");
    }


        
   



}
