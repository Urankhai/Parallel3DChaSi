/*

 public double Gain_x_in_radians(double x, double a0, double a1, double a2, double a3, double a4,
     double a5, double b1, double b2, double b3, double b4, double b5, double w)
 {
     return a0 + a1 * Math.Cos(x * w) + b1 * Math.Sin(x * w) +
            a2 * Math.Cos(2 * x * w) + b2 * Math.Sin(2 * x * w) + a3 * Math.Cos(3 * x * w) +
            b3 * Math.Sin(3 * x * w) +
            a4 * Math.Cos(4 * x * w) + b4 * Math.Sin(4 * x * w) + a5 * Math.Cos(5 * x * w) +
            b5 * Math.Sin(5 * x * w);
 }

 public double Gain_x_in_degrees(double x, double a0, double a1, double a2, double a3, double a4,
     double a5, double b1, double b2, double b3, double b4, double b5, double w)
 {
    x = (x*180)/Math.PI;
     return a0 + a1 * Math.Cos(x * w) + b1 * Math.Sin(x * w) +
            a2 * Math.Cos(2 * x * w) + b2 * Math.Sin(2 * x * w) + a3 * Math.Cos(3 * x * w) +
            b3 * Math.Sin(3 * x * w) +
            a4 * Math.Cos(4 * x * w) + b4 * Math.Sin(4 * x * w) + a5 * Math.Cos(5 * x * w) +
            b5 * Math.Sin(5 * x * w);
 }

// antenna1 parameters
double a0 = -4.594e+11;
 double a1 =   7.501e+11;
 double b1 =   1.614e+11;
 double a2 =  -4.023e+11;
 double b2 =  -1.814e+11;
 double a3 =   1.347e+11;
 double b3 =   9.922e+10;
 double a4 =  -2.502e+10;
 double b4 =  -2.824e+10;
 double a5 =   1.895e+09;
 double b5 =   3.346e+09;
 double w =     0.05624;

// antenna2 parameters
double a0 =      -4.438;
double a1 =       -1.34;
double b1 =      -1.225;
double a2 =      0.4442;
double b2 =     -0.1569;
double a3 =       0.464;
double b3 =      0.1278;
double a4 =      0.7272;
double b4 =     -0.1863;
double a5 =      0.2234;
double b5 =      0.2196;
double w =       1.076;
*/