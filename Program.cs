using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Numerics;
using System.Windows.Forms;

namespace gUSBampSyncDemoCS
{
    class Program
    {
        /// <summary>
        /// The number of seconds that the application should acquire data.
        /// </summary>
        //const uint NumSecondsRunning = 10;

        /// <summary>
        /// Starts data acquisition and writes received data to a binary file.
        /// </summary>
        /// <remarks>
        /// You can read the file into matlab using the following code:
        /// <code>
        /// fid = fopen('receivedData.bin', 'rb');
        /// data = fread(fid, [<i>number of total channels</i>, inf], 'float32');
        /// fclose(fid);
        /// </code>
        /// </remarks>


        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}


