using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gUSBampSyncDemoCS
{
    public partial class Form1 : Form
    {
        private delegate void CommonTaskDelegate();

        private Thread DAQ_hThread;
        private Thread FFT_hThread;
        private Thread Filter_hThread;
        private Thread WP_hThread;

        private const int DAQ_hThread_PERIOD = 250;         // Has to be equal to NumSecondsRunning*1000
        private const int FFT_hThread_PERIOD = 250;
        private const int Filter_hThread_PERIOD = 250;
        private const int WP_hThread_PERIOD = 250;          // better to set these variables to the same value

        private bool DAQ_isRunning = false;
        private bool Filter_isRunning = false;
        private bool WP_isRunning = false;
        private bool FFT_isRunning = false;             // Make sure the thread are initialized before running

        private double[] WP_Magnitude;

        static double NumSecondsRunning = 0.25;     // Better if power of 2


        DataAcquisitionUnit acquisitionUnit;

        FilterButterworth Filterb;                  //
        FilterButterworth Filterh;                  //
        FilterButterworth Filterc;                  // Not necessary unless you want to add numerical filters

        double[][] f;                               // Contains the data
        static int numScans = 512;
        Complex[] ff = new Complex[(int)Math.Floor(5 * numScans * NumSecondsRunning)];  //Contains filtered data, unecessary if you don't use the filters above
        Complex[][] TF;                                                                 //Contains Fourier transform
        Complex[] TFf = new Complex[(int)Math.Floor(5 * numScans * NumSecondsRunning)]; //Contains filtered Fourier transform, unecessary if you don't use the filters above
        int numChannels;
        int numValuesAtOnce;
        int A = 0, t2 = 0;                                                              //A : for artefact, t2 : to periodically refresh the buffer
        double[] Beta, Alpha, Gamma, Theta, Beta0, Alpha0, Gamma0;                      //Contains the power of the different frequency bands
        double Amp, Treshl1 = 0, Treshl2 = 0, Treshl3 = 0, Treshh1 = 0, Treshh2 = 0, Treshh3 = 0;   // amp : value for the different frequency, Tresh : set when press buttons = tresholds for detection
        bool artefact = false, Ftisopen = false;                                        // for artefact and make sure the saving file is open
        FileStream Ft;                                                                  // to save the values when pressing the buttons
        BinaryWriter bw;
        string nom = "test_8";                                                          //name of the file created

        //create device configurations
        Dictionary<string, DeviceConfiguration> devices = CreateDefaultDeviceConfigurations("UB-2011.11.15");



        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------



        public Form1()
        {
            InitializeComponent();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void DAQ_TaskInit()      // data acquisition
        {
            DAQ_isRunning = true;

            acquisitionUnit = new DataAcquisitionUnit();


            //determine how many bytes should be read and processed at once by the processing thread (not the acquisition thread!)

            numChannels = 0;

            foreach (DeviceConfiguration deviceConfiguration in devices.Values)
                numChannels += (deviceConfiguration.SelectedChannels.Count + Convert.ToInt32(deviceConfiguration.TriggerLineEnabled));

            numValuesAtOnce = (int)Math.Floor(numScans * numChannels * NumSecondsRunning);
            acquisitionUnit.StartAcquisition(devices);

            f = new double[numChannels][];
            for (int j = 0; j < numChannels; ++j)
            {
                f[j] = new double[(int)Math.Floor(5 * numScans * NumSecondsRunning)];   //save 5*NumSecondsRunning
            }

        }

        public void DAQ_TaskLoop()
        {
            CommonTaskDelegate LocalTaskInstance;
            while (true)
            {
                //this is the data processing thread; data received from the devices will be written out to a file here
                if (DAQ_isRunning) // Start, Stop
                {
                    DAQ_TaskFunction();

                    LocalTaskInstance = new CommonTaskDelegate(DAQ_DisplayFunction);
                    BeginInvoke(LocalTaskInstance);
                }
                Thread.Sleep(DAQ_hThread_PERIOD);
            }
        }

        public void DAQ_TaskFunction()
        {
            float[] data = acquisitionUnit.ReadData(numValuesAtOnce);

            if (data.Length > 0)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    f[i % numChannels][(i / numChannels)] = f[i % numChannels][(i / numChannels) + (data.Length / numChannels)];                                    //left shift of the values
                    f[i % numChannels][(i / numChannels) + (data.Length / numChannels)] = f[i % numChannels][(i / numChannels) + (2 * data.Length / numChannels)];
                    f[i % numChannels][(i / numChannels) + (2 * data.Length / numChannels)] = f[i % numChannels][(i / numChannels) + (3 * data.Length / numChannels)];
                    f[i % numChannels][(i / numChannels) + (3 * data.Length / numChannels)] = f[i % numChannels][(i / numChannels) + (4 * data.Length / numChannels)];
                    f[i % numChannels][(i / numChannels) + (4 * data.Length / numChannels)] = data[i];


                }

                FFT_isRunning = true;
                Filter_isRunning = true; // Make sure there are data to filter before filtering them

            }
            //


            if (t2 > (120 * 1000 / DAQ_hThread_PERIOD))     //refresh the buffer to avoid overflow : introduce artefact every 2minutes, but doesn't really bother the detection
            {
                acquisitionUnit.StopAcquisition();
                acquisitionUnit.StartAcquisition(devices);
                t2 = 0;
            }
            ++t2;

        }

        public void DAQ_DisplayFunction()
        {
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------

        public void FFT_TaskInit() // Fourier transform
        {
        }

        public void FFT_TaskLoop()
        {
            CommonTaskDelegate LocalTaskInstance;
            while (true)
            {
                //this is the data processing thread; data received from the devices will be written out to a file here
                if (FFT_isRunning) // Start, Stop
                {
                    FFT_TaskFunction();

                    LocalTaskInstance = new CommonTaskDelegate(FFT_DisplayFunction);
                    BeginInvoke(LocalTaskInstance);
                }
                Thread.Sleep(FFT_hThread_PERIOD);
            }
        }

        public void FFT_TaskFunction()
        {
            TF = new Complex[numChannels][];
            Complex[] Cf = new Complex[(int)Math.Floor(5 * numScans * NumSecondsRunning)]; // intermediate variable

            for (int i = 0; i < numChannels; ++i)
            {
                for (int j = 0; j < f[i].Length; ++j)
                {
                    Cf[j] = new Complex(f[i][j], 0);
                }
                TF[i] = fourier.FFT(Cf);        // fourier transform saved in TF

            }

        }

        public void FFT_DisplayFunction()
        {
        }

        private void chart3_Click(object sender, EventArgs e)       //useless
        {

        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------

        public void Filter_TaskInit()   // apply numerical filter (unused) + artefact detection and use of intermediate variables
        {
            double freq1 = 60; // f for low-pass
            double freq2 = 0.5; //f dor high-pass
            double freq3 = 50; // f for notch
            double samplerate = numScans;
            Filterb = new FilterButterworth(freq1, samplerate, 1); //low-pass
            Filterh = new FilterButterworth(freq2, samplerate, 2); //high-pass
            Filterc = new FilterButterworth(freq3, samplerate, 3); //notch
        }



        public void Filter_TaskLoop()
        {
            CommonTaskDelegate LocalTaskInstance;
            while (true)
            {

                //this is the data processing thread; data received from the devices will be written out to a file here
                if (Filter_isRunning) // Start, Stop
                {
                    Filter_TaskFunction();

                    LocalTaskInstance = new CommonTaskDelegate(Filter_DisplayFunction);
                    BeginInvoke(LocalTaskInstance);
                }
                Thread.Sleep(Filter_hThread_PERIOD);
            }
        }

        public void Filter_TaskFunction()
        {


            double[] ffi = new double[f[0].Length / 5]; // intermediate variable


            artefact = false;

            for (int j = 0; j < f[0].Length / 5; j++)
            {

                ff[j] = ff[j + (f[0].Length / 5)];                                
                ff[j + (f[0].Length / 5)] = ff[j + (2 * f[0].Length / 5)];
                ff[j + (2 * f[0].Length / 5)] = ff[j + (3 * f[0].Length / 5)];
                ff[j + (3 * f[0].Length / 5)] = ff[j + (4 * f[0].Length / 5)];
                ff[j + (4 * f[0].Length / 5)] = new Complex(f[0][j + 4 * f[0].Length / 5], 0);  // comment to use filters

                /*Filterc.Update(f[0][j + (4 * f[0].Length / 5)]);                // uncomment to use filters
                ffi[j] = Filterc.Value;
                Filterh.Update(ffi[j]);
                ffi[j] = Filterh.Value;
                Filterb.Update(ffi[j]);
                ff[j + (4 * f[0].Length / 5)] = new Complex(Filterb.Value, 0);*/


                if (f[0][j + (4 * f[0].Length / 5)] > 60 || f[0][j + (4 * f[0].Length / 5)] < -60)  // detection of artefact on the last Filter_hThread_PERIOD, can be moved elsewhere
                    artefact = true;

            }

            TFf = fourier.FFT(ff);

            WP_isRunning = true; // to make sure values have been filtered before processing them

        }

        public void Filter_DisplayFunction()
        {

            chart2.Series["Frequency_Domain"].Points.Clear();
            chart1.Series["Filtered_Signal"].Points.Clear();

            for (int i = (int)Math.Floor(0 * NumSecondsRunning); i < /*TFf.Length/ 2*/500 * NumSecondsRunning; ++i) // display frequency between 0 and 500/5=100Hz
            {
                Amp = Math.Sqrt(Math.Pow(TFf[i].Real, 2) + Math.Pow(TFf[i].Imaginary, 2));
                chart2.Series["Frequency_Domain"].Points.AddXY((i / NumSecondsRunning) / 5, Amp);

            }

            chart2.ChartAreas[0].AxisY.Maximum = 2500;
            for (int i = 0; i < ff.Length/*60 * NumSecondsRunning*/; ++i)
            {
                chart1.Series["Filtered_Signal"].Points.AddY(ff[i].Real);
            }

            chart1.ChartAreas[0].AxisY.Maximum = 150;
            chart1.ChartAreas[0].AxisY.Minimum = -150;


        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void button4_Click(object sender, EventArgs e) // close save file. Neccessary to not delete the values when killing the programm
        {
            Ft.Close();
            Ftisopen = false;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void Button2_Click(object sender, EventArgs e) // set the treshold when focusing
        {
            Alpha0 = Alpha; //
            Beta0 = Beta;   //
            Gamma0 = Gamma; // Maybe useless

            for (int i = 0; i < 10; ++i)
            {
                Treshh1 += Alpha0[i] / Beta0[i];
            }
            Treshh1 /= 10;

            for (int i = 0; i < 10; ++i)
            {
                Treshh2 += Beta0[i] / Gamma0[i];
            }
            Treshh2 /= 10;

            for (int i = 0; i < 10; ++i)
            {
                Treshh3 += Gamma0[i] / Alpha0[i];
            }
            Treshh3 /= 10;


            Console.WriteLine("Treshh "+Treshh1);
            if (Ftisopen == false) // open saving file the first time it is called
            {
                Ft = File.OpenWrite("C:\\Users\\Thomas\\Desktop\\" + nom);
                Ftisopen = true;
            }
            bw = new BinaryWriter(Ft);
            bw.Write("concab" + Treshh1 + "  ");
            bw.Write("concbg" + Treshh2 + "  ");
            bw.Write("concag" + Treshh3 + "  \n");
            //Ft.Close();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------

        private void button1_Click(object sender, EventArgs e)  // set the treshold when resting
        {
            //Alpha0 = new double[10];
            //Beta0 = new double[10];

            Alpha0 = Alpha;
            Beta0 = Beta;
            Gamma0 = Gamma;

            for (int i = 0; i < 10; ++i)
            {
                Treshl1 += Alpha0[i] / Beta0[i];
            }
            Treshl1 /= 10;

            for (int i = 0; i < 10; ++i)
            {
                Treshl2 += Beta0[i] / Gamma0[i];
            }
            Treshl2 /= 10;

            for (int i = 0; i < 10; ++i)
            {
                Treshl3 += Gamma0[i] / Alpha0[i];
            }
            Treshl3 /= 10;


            Console.WriteLine("Treshl "+Treshl1);
            if (Ftisopen == false)
            {
                Ft = File.OpenWrite("C:\\Users\\Thomas\\Desktop\\" + nom);
                Ftisopen = true;
            }

            bw = new BinaryWriter(Ft);
            bw.Write("restab" + Treshl1 + "  ");
            bw.Write("restbg" + Treshl2 + "  ");
            bw.Write("restag" + Treshl3 + "  \n");
        }




        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------


        public void WP_TaskInit() // data processing : calculate power for each band
        {
            //C = 0;
            Beta = new double[10]; // register the 10 last values for each band
            Alpha = new double[10];
            Gamma = new double[10];
            Theta = new double[10];

        }

        public void WP_TaskLoop()
        {
            CommonTaskDelegate LocalTaskInstance;
            while (true)
            {
                if (WP_isRunning) // Start, Stop
                {
                    WP_TaskFunction();

                    LocalTaskInstance = new CommonTaskDelegate(WP_DisplayFunction);
                    BeginInvoke(LocalTaskInstance);
                }

                Thread.Sleep(WP_hThread_PERIOD);
            }
        }

        public void WP_TaskFunction()
        {
            int j, i = 0;
            WP_Magnitude = new double[] { 0, 0, 0, 0, 0 };
            Complex[] temp = TFf;       //avoid any change of TFf during the execution of the thread

            for (j = 5 * (int)Math.Floor(NumSecondsRunning / 2); j <= 5 * 3 * NumSecondsRunning; ++j) //Power of delta band
            {
                WP_Magnitude[0] += temp[j].Magnitude;
                ++i;
            }
            WP_Magnitude[0] /= i;
            i = 0;
            for (j = (int)Math.Floor(5 * 4 * NumSecondsRunning); j < 5 * 8 * NumSecondsRunning; ++j) //Power of theta band
            {
                WP_Magnitude[1] += temp[j].Magnitude;
                ++i;
            }
            WP_Magnitude[1] /= i;
            i = 0;
            for (j = (int)Math.Floor(5 * 8 * NumSecondsRunning); j <= 5 * 13 * NumSecondsRunning; ++j) // Power of Alpha band
            {
                WP_Magnitude[2] += temp[j].Magnitude;
                ++i;
            }
            WP_Magnitude[2] /= i;
            i = 0;
            for (j = (int)Math.Floor(5 * 14 * NumSecondsRunning); j <= 5 * 33 * NumSecondsRunning; ++j) //Power of Beta band
            {
                WP_Magnitude[3] += temp[j].Magnitude;
                ++i;
            }
            WP_Magnitude[3] /= i;
            i = 0;
            for (j = (int)Math.Floor(5 * 34 * NumSecondsRunning); j < 5 * 45 * NumSecondsRunning; ++j) // Power of Gamma band
            {
                WP_Magnitude[4] += temp[j].Magnitude;
                ++i;
            }
            WP_Magnitude[4] /= i;
            i = 0;

            for (i = 0; i < 9; ++i) // FIFO
            {
                Beta[i] = Beta[i + 1];
                Alpha[i] = Alpha[i + 1];
                Gamma[i] = Gamma[i + 1];
                Theta[i] = Theta[i + 1];
            }

            Beta[9] = WP_Magnitude[3] / (WP_Magnitude[0] + WP_Magnitude[1] + WP_Magnitude[2] + WP_Magnitude[3] + WP_Magnitude[4]);  //
            Alpha[9] = WP_Magnitude[2] / (WP_Magnitude[0] + WP_Magnitude[1] + WP_Magnitude[2] + WP_Magnitude[3] + WP_Magnitude[4]); //
            Gamma[9] = WP_Magnitude[4] / (WP_Magnitude[0] + WP_Magnitude[1] + WP_Magnitude[2] + WP_Magnitude[3] + WP_Magnitude[4]); //
            Theta[9] = WP_Magnitude[1] / (WP_Magnitude[0] + WP_Magnitude[1] + WP_Magnitude[2] + WP_Magnitude[3] + WP_Magnitude[4]); // Set new values as percentage of the whole power


        }

        public void WP_DisplayFunction()
        {
            chart3.Series["Utility"].Points.Clear();

            if (artefact == true)
            {
                chart3.Series["Utility"].Points.AddXY("art", 2);
                A += 1;
            }
            else
            {
                A = 0;
                chart3.Series["Utility"].Points.AddXY("art", 0);
            }
            chart3.ChartAreas[0].AxisY.Maximum = 8;


            string[] S = new string[] { "Delta", "Theta", "Alpha", "Beta", "Gamma" };
            string[] D = new string[] { "Init", "Curent" };

            //chart4.Series["Alpha"].Points.AddXY(D[0], Alpha0 + Beta0);
            chart4.Series["Alpha"].Points.AddY(Alpha[9]);       //
            if (chart4.Series["Alpha"].Points.Count > 20)       //
                chart4.Series["Alpha"].Points.RemoveAt(0);      //
            chart4.ChartAreas[0].AxisY.Maximum = 0.25;          //

            chart4.Series["Beta"].Points.AddY(Beta[9]);         //
            if (chart4.Series["Beta"].Points.Count > 20)        //
                chart4.Series["Beta"].Points.RemoveAt(0);       //

            chart4.Series["Gamma"].Points.AddY(Gamma[9]);       //
            if (chart4.Series["Gamma"].Points.Count > 20)       //
                chart4.Series["Gamma"].Points.RemoveAt(0);      // Display the 20 last valus for these 3 bands

            if (Treshl1 > 0 && Treshl2 > 0 && Treshl3 > 0)
            {
                double ab = 0;
                double bg = 0;
                double ga = 0;
                double Conc;
                for (int i = 0; i < 10; ++i) // calculates the average ratios with the 10 last values
                {
                    ab += Alpha[i] / Beta[i];
                    bg += Beta[i] / Gamma[i];
                    ga += Gamma[i] / Alpha[i];
                }
                ab /= 10;
                bg /= 10;
                ga /= 10;
                Conc = Math.Abs(((ab - Treshl1) / (Treshh1 - Treshl1)) + ((bg - Treshl2) / (Treshh2 - Treshl2)) + ((ga - Treshl3) / (Treshh3 - Treshl3))); // Formula for concentration : arbitrary but works

                

                chart3.Series["Utility"].Points.AddXY("ab", ab / Treshl1);
                chart3.Series["Utility"].Points.AddXY("bg", bg / Treshl2);
                chart3.Series["Utility"].Points.AddXY("ga", ga / Treshl3);
                chart3.Series["Utility"].Points.AddXY("Concentration", Conc); // to switch on and off the lights : give smthg to focus on


                if (Conc > 1.5)
                    pictureBox3.BackColor = Color.FromArgb(255, 70, 70);
                else
                    pictureBox3.BackColor = Color.FromArgb(100, 100, 100);
                if (Conc > 2.5 && A < 2)
                    pictureBox2.BackColor = Color.FromArgb(210, 150, 100);
                else
                    pictureBox2.BackColor = Color.FromArgb(100, 100, 100);
                if (Conc > 4 && artefact == false)
                    pictureBox1.BackColor = Color.FromArgb(80, 255, 80);
                else
                    pictureBox1.BackColor = Color.FromArgb(100, 100, 100);


            }

        }








        //------------------------------------------------------------------------------------------------------------




        private static long Length(string v)
        {
            throw new NotImplementedException();
        }
        static Dictionary<string, DeviceConfiguration> CreateDefaultDeviceConfigurations(params string[] serialNumbers)
        {
            Dictionary<string, DeviceConfiguration> deviceConfigurations = new Dictionary<string, DeviceConfiguration>();

            for (int i = 0; i < serialNumbers.Length; i++)
            {
                DeviceConfiguration deviceConfiguration = new DeviceConfiguration();
                deviceConfiguration.SelectedChannels = new List<byte>(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
                deviceConfiguration.NumberOfScans = 16;
                deviceConfiguration.SampleRate = 512;
                deviceConfiguration.IsSlave = (i > 0);

                deviceConfiguration.TriggerLineEnabled = false;
                deviceConfiguration.SCEnabled = false;
                deviceConfiguration.Mode = gUSBampWrapper.OperationModes.Normal;
                deviceConfiguration.BandpassFilters = new Dictionary<byte, int>();
                deviceConfiguration.NotchFilters = new Dictionary<byte, int>();
                deviceConfiguration.BipolarSettings = new gUSBampWrapper.Bipolar();
                deviceConfiguration.CommonGround = new gUSBampWrapper.Gnd();
                deviceConfiguration.CommonReference = new gUSBampWrapper.Ref();
                deviceConfiguration.Drl = new gUSBampWrapper.Channel();

                for (byte ch = 1; ch <= 16; ch++)                   // set the integrated filters (0.5-60Hz and notch at 50Hz)
                {
                    deviceConfiguration.BandpassFilters.Add(ch, 71);
                    deviceConfiguration.NotchFilters.Add(ch, 4);
                }

                deviceConfiguration.Dac = new gUSBampWrapper.DAC();
                deviceConfiguration.Dac.WaveShape = gUSBampWrapper.WaveShapes.Sine;
                deviceConfiguration.Dac.Amplitude = 2000;
                deviceConfiguration.Dac.Frequency = 30;
                deviceConfiguration.Dac.Offset = 2047;

                deviceConfigurations.Add(serialNumbers[i], deviceConfiguration);
            }

            return deviceConfigurations;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            gUSBampWrapper.Filter[] Y = gUSBampWrapper.GetFilterSpec(); //
            gUSBampWrapper.Filter[] V = gUSBampWrapper.GetNotchSpec();  // To see the values of the different filters
            pictureBox1.BackColor = Color.FromArgb(100, 100, 100);  //
            pictureBox2.BackColor = Color.FromArgb(100, 100, 100);  //
            pictureBox3.BackColor = Color.FromArgb(100, 100, 100);  // Set the colors for the lights

            CommonTaskDelegate DAQ_ThreadInstance = new CommonTaskDelegate(DAQ_TaskLoop);   // Start the different Threads
            DAQ_hThread = new Thread(new ThreadStart(DAQ_ThreadInstance));
            DAQ_TaskInit();
            DAQ_hThread.Start();

            CommonTaskDelegate FFT_ThreadInstance = new CommonTaskDelegate(FFT_TaskLoop);
            FFT_hThread = new Thread(new ThreadStart(FFT_ThreadInstance));
            FFT_TaskInit();
            FFT_hThread.Start();

            CommonTaskDelegate Filter_ThreadInstance = new CommonTaskDelegate(Filter_TaskLoop);
            Filter_hThread = new Thread(new ThreadStart(Filter_ThreadInstance));
            Filter_TaskInit();
            Filter_hThread.Start();

            CommonTaskDelegate WP_ThreadInstance = new CommonTaskDelegate(WP_TaskLoop);
            WP_hThread = new Thread(new ThreadStart(WP_ThreadInstance));
            WP_TaskInit();
            WP_hThread.Start();
        }
    }
}
