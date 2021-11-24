using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Drawing
{
    //transform parameters to draw the chart;
    public static double  a, b, x0, y0;
    public static LineRenderer linR;

    public static void drawChart(Transform tf, double[] X_inputValues, double[] Y_inputValues, string type)
    {
        linR =tf.GetComponent<LineRenderer>();
        
        
        //transform parameters to draw the heart rate
        a = tf.localScale.x;
        b = tf.localScale.y;
        x0 = tf.position.x;
        y0 = tf.position.y;


        //do not resize array
        if (type=="time")
        {
            //X_inputValues = ArrayResize(X_inputValues, (int)X_inputValues.Length / 4);
            //Y_inputValues = ArrayResize(Y_inputValues, (int)Y_inputValues.Length / 4);

        }
        //resize array
        else if(type=="frequency")
        {
            //X_inputValues=ArrayResize(X_inputValues, (int)X_inputValues.Length/2);
            //Y_inputValues=ArrayResize(Y_inputValues, (int)Y_inputValues.Length/2);
        }

        linR.positionCount = X_inputValues.Length;

        double max_X = FastFourierTransform.MaxD(X_inputValues);
        double min_X = FastFourierTransform.MinD(X_inputValues);
        double max_Y = 0;// FastFourierTransform.MaxD(Y_inputValues);
        double min_Y = -170;// FastFourierTransform.MinD(Y_inputValues);

        //drawing factors
        double factorA_X = (x0 + a / 2 - (x0 - a / 2)) / (max_X - min_X + 0.01f);
        double factorB_X = factorA_X * (-min_X) + (x0 - a / 2);
        double factorA_Y = (y0 + b / 2 - (y0 - b / 2)) / (max_Y - min_Y + 0.01f);
        double factorB_Y = factorA_Y * (-min_Y) + (y0 - b / 2);


        //draw using the lineRender
        for (int ii = 0; ii < linR.positionCount; ii++)
        {
            // Debug.Log(ii);
            double xt = X_inputValues[ii] * factorA_X + factorB_X;
            double yt = Y_inputValues[ii] * factorA_Y + factorB_Y;

            linR.SetPosition(ii, new UnityEngine.Vector3((float)xt, (float)yt, tf.position.z));
        }
    }

    //gets an array and resizes it to a fixed value
    public static double[] ArrayResize(double[] a, int Size)
    {

        double[] temp = new double[Size];
        for (int c = 0; c < Mathf.Min(Size, a.Length); c++)
        {
            temp[c] = a[c];
        }
        
        return temp;
    }

}
