using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Numerics;

public class Example03_soundTreatment : MonoBehaviour
{


    [Space]
    [Header("[!] set fixed time to 0.01f [!]")]
    [Header("sound treatment")]
    [Space]

    //samplingfrequency 
    public int samplingFrequency;

    //input-output values for the FFT
    double [] X_inputValues;
    double [] Y_inputValues;
    double[] Y_output;


    //list with the input values
    public List<double> Y_values;

    //these are the frequencies, amplitude and phase of the sinus added to the signal
    [Space(10)]
    [Header("This is the noise to analyse")]
    [Space]
    public AudioClip audioC;
    public AudioSource audioS;


    //counts the steps taken
    //long count = 0;

    //the chart transforms for the time and frequency
    [Space(10)]
    [Header("CHARTS FOR DRAWING")]
    [Space]
    public Transform tfTime;
    public Transform tfFreq;

    //window size
    public long windowSize;

    void Start()
    {
        windowSize = audioS.clip.samples * audioS.clip.channels;

        audioS.clip = audioC;
        audioS.Play();

        //initialize
        X_inputValues = new double[windowSize];
        Y_inputValues = new double[windowSize];
        Y_output = new double[windowSize];

        StartCoroutine(treatmentOfSound());

    }


    

    /// <summary>
    ///  This corrutine obtains these steps in order:
    ///  1) gets the data from the audiosource
    ///  2) displays the signal in time domain
    ///  3) displays the signal in FFT
    /// </summary>

    IEnumerator treatmentOfSound()
    {

        //ONE IS THE INPUT VALUES
        float[] samples = new float[windowSize];

        audioS.clip.GetData(samples, 0);

        
        //this is the window size
        for (int ii = 0; ii < windowSize; ii++)
        {
            X_inputValues[ii] = ii;
            Y_inputValues[ii] =(double)samples[ii];

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

        
            
        yield return null;


        

    }



    void FixedUpdate()
    {
        //draw both charts: time-domain and frequency-domain
        Drawing.drawChart(tfTime, X_inputValues, Y_inputValues, "time");
        Drawing.drawChart(tfFreq, X_inputValues, Y_output, "frequency");


    }







}
