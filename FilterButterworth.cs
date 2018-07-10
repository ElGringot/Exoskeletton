using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace gUSBampSyncDemoCS
{
    public class FilterButterworth
    {
        /// <summary>
        /// rez amount, from sqrt(2) to ~ 0.1
        /// </summary>

        private readonly double frequency;
        private readonly double sampleRate;
        private readonly int passType;
        private readonly double r = Math.Sqrt(2), c, a1, a2, a3, b1, b2;

        /// <summary>
        /// Array of input values, latest are in front
        /// </summary>
        private double[] inputHistory = new double[2];

        /// <summary>
        /// Array of output values, latest are in front
        /// </summary>
        private double[] outputHistory = new double[3];

        public FilterButterworth(double frequency, double sampleRate, int passType)
        {
            this.frequency = frequency;
            this.sampleRate = sampleRate;
            this.passType = passType;

            switch (passType)
            {
                case 1:
                    c = 1.0 / Math.Tan(Math.PI * frequency / sampleRate);
                    a1 = 1.0 / (1.0 + r * c + c * c);
                    a2 = 2 * a1;
                    a3 = a1;
                    b1 = 2.0 * (1.0 - c * c) * a1;
                    b2 = (1.0 - r * c + c * c) * a1;
                    break;

                case 2:
                    c = (float)Math.Tan(Math.PI * frequency / sampleRate);
                    a1 = 1.0/ (1.0 + r * c + c * c);
                    a2 = -2 * a1;
                    a3 = a1;
                    b1 = 2.0 * (c * c - 1.0) * a1;
                    b2 = (1.0 - r * c + c * c) * a1;
                    break;

                case 3:
                    double f1 = frequency - 0.01;
                    double f2 = frequency + 0.01;
                    double q = (f2 - f1)/Math.Sqrt(f1 * f2);
                    c=(float)Math.Cos(2 * Math.PI * frequency / sampleRate);
                    a1= (1.0 - q) * (1.0 - q) / (2.0 * (Math.Abs(c) + 1)) + q;
                    a2= -2.0 * c * a1;
                    a3 = a1;
                    b1= -2.0 * c * q;
                    b2= q * q;
                    break;
            }
        }


        public void Update(double newInput)
        {
            double newOutput = a1 * newInput + a2 * this.inputHistory[0] + a3 * this.inputHistory[1] - b1 * this.outputHistory[0] - b2 * this.outputHistory[1];

            this.inputHistory[1] = this.inputHistory[0];
            this.inputHistory[0] = newInput;

            this.outputHistory[2] = this.outputHistory[1];
            this.outputHistory[1] = this.outputHistory[0];
            this.outputHistory[0] = newOutput;
        }

        public double Value
        {
            get { return this.outputHistory[0]; }
        }
    }
}