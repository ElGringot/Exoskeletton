using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace gUSBampSyncDemoCS
{
    class fourier
    {
        public static Complex[] FFT(Complex[] x)
        {
            int N = x.Length;
            Complex[] X = new Complex[N];
            Complex[] o, O, e, E;
            if (N == 1)
            {
                X[0] = x[0];
                return X;
            }
            int k;
            e = new Complex[N / 2];
            o = new Complex[N / 2];
            for (k = 0; k < N / 2; k++)
            {
                e[k] = x[2 * k];
                o[k] = x[2 * k + 1];
            }
            O = FFT(o);
            E = FFT(e);
            for (k = 0; k < N / 2; k++)
            {
                Complex temp = Complex.FromPolarCoordinates(1, -2 * Math.PI * k / N);
                O[k] *= temp;
            }
            for (k = 0; k < N / 2; k++)
            {
                X[k] = E[k] + O[k];
                X[k + N / 2] = E[k] - O[k];
            }
            return X;
        }

        public static double[] IFFT(Complex[] y)
        {
            int N = y.Length;
            double[] output = new double[N];
            for (int i = 0; i < N; ++i)
            {
                output[i] = 0;
                for (int j = 0; j < N / 2; ++j)
                {
                    double w = 2 * 3.14159 * i / N;
                    output[i] += y[j].Real * Math.Cos(j * w) - y[j].Imaginary * Math.Sin(j * w);
                }

            }
            return output;
        }
    }
}
