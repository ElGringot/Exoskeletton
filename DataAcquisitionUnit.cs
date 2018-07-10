using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace gUSBampSyncDemoCS
{
    #region Structures...

    public struct DeviceConfiguration
    {
        /// <summary>
        /// Contains the numbers of the device's channels that should be acquired.
        /// </summary>
        public List<byte> SelectedChannels;

        /// <summary>
        /// The sample rate of the device in Hz.
        /// </summary>
        public ushort SampleRate;

        /// <summary>
        /// Indicates if the digital input trigger lines should be acquired as well.
        /// </summary>
        public bool TriggerLineEnabled;

        /// <summary>
        /// The number of scans that should be aquired at once by a single <see cref="gUSBampWrapper.GetData"/> call.
        /// </summary>
        public ushort NumberOfScans;

        /// <summary>
        /// A map of bandpass filters that should be applied to specific channel numbers.
        /// The key of the dictionary represents the number of the device's channel to which the corresponding filter specified by the filter index in the dictionary's value field should be applied.
        /// No filter will be applied to channels which are not contained in the dictionary. Only one filter per channel is allowed.
        /// </summary>
        public Dictionary<byte, int> BandpassFilters;

        /// <summary>
        /// A map of notch filters that should be applied to specific channel numbers.
        /// The key of the dictionary represents the number of the device's channel to which the corresponding filter specified by the filter index in the dictionary's value field should be applied.
        /// No filter will be applied to channels which are not contained in the dictionary. Only one filter per channel is allowed.
        /// </summary>
        public Dictionary<byte, int> NotchFilters;

        /// <summary>
        /// Indicates if the shortcut function should be enabled for the device.
        /// </summary>
        public bool SCEnabled;

        /// <summary>
        /// The bipolar settings for the device.
        /// </summary>
        public gUSBampWrapper.Bipolar BipolarSettings;

        /// <summary>
        /// The common reference groups settings.
        /// </summary>
        public gUSBampWrapper.Ref CommonReference;

        /// <summary>
        /// The common ground groups settings.
        /// </summary>
        public gUSBampWrapper.Gnd CommonGround;

        /// <summary>
        /// Indicates if the device should act as a slave or as a master device.
        /// </summary>
        public bool IsSlave;

        /// <summary>
        /// The operation mode of the device.
        /// </summary>
        public gUSBampWrapper.OperationModes Mode;

        /// <summary>
        /// The settings for the device's internal signal generator.
        /// </summary>
        public gUSBampWrapper.DAC Dac;

        /// <summary>
        /// The DRL settings.
        /// </summary>
        public gUSBampWrapper.Channel Drl;
    }

    /// <summary>
    /// Structure containing one receive buffer and associated data.
    /// </summary>
    class ReceiveBuffer
    {
        /// <summary>
        /// The receive buffer where the device writes data to.
        /// </summary>
        public byte[] Buffer;

        /// <summary>
        /// The pinned handle to the <see cref="Buffer"/> array.
        /// </summary>
        public GCHandle BufferHandle;

        /// <summary>
        /// The <see cref="NativeOverlapped"/> structure that is needed for data reception.
        /// </summary>
        public NativeOverlapped Overlapped;

        /// <summary>
        /// The pinned handle to the <see cref="Overlapped"/> structure.
        /// </summary>
        public IntPtr OverlappedPointer;

        /// <summary>
        /// Destructor.
        /// </summary>
        ~ReceiveBuffer()
        {
            //free allocated memory in case
            if (BufferHandle.IsAllocated)
                BufferHandle.Free();

            if (OverlappedPointer != IntPtr.Zero)
                Marshal.FreeHGlobal(OverlappedPointer);
        }
    }

    /// <summary>
    /// Represents a g.USBamp device.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// The handle of the device.
        /// </summary>
        public IntPtr Handle;

        /// <summary>
        /// The serial number of the device.
        /// </summary>
        public string SerialNumber;

        /// <summary>
        /// The configuration of the device.
        /// </summary>
        public DeviceConfiguration Configuration;
    }

    #endregion
    #region Exceptions...

    #region DeviceException...

    /// <summary>
    /// Exception that will be thrown when an error occures during accessing the device using the C-API.
    /// </summary>
    public class DeviceException : ApplicationException
    {
        uint _errorCode;

        /// <summary>
        /// Gets the error code retrieved from the C-API of the device (for details on error codes see the manual of the amplifier's C-API).
        /// </summary>
        public uint ErrorCode
        {
            get { return _errorCode; }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DeviceException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code retrieved from the C-API of the amplifier (see manual of the amplifier's C-API for details).</param>
        /// <param name="errorMessage">The error message explaining the corresponding error code (see manual of the amplifier's C-API for details).</param>
        public DeviceException(uint errorCode, string errorMessage)
            : base(errorMessage)
        {
            _errorCode = errorCode;
        }
    }

    #endregion

    #endregion

    /// <summary>
    /// Demonstrates usage of multiple g.USBamp devices for data acquisition.
    /// </summary>
    class DataAcquisitionUnit
    {
        #region Constants...

        /// <summary>
        /// The size of <see cref="_buffer"/> in seconds.
        /// </summary>
        const int BufferSizeSeconds = 10;

        /// <summary>
        /// The number of <see cref="gUSBampWrapper.GetData"/> calls that should be queued during acquisition to avoid loss of data.
        /// </summary>
        const int QueueSize = 8;

        /// <summary>
        /// The maximum number of channels that one device can provide.
        /// </summary>
        const int MaxNumberOfChannels = 16;

        #endregion

        #region Private members...

        /// <summary>
        /// The thread acquiring data.
        /// </summary>
        Thread _acquisitionThread;

        /// <summary>
        /// Flag indicating if data acquisition is running.
        /// </summary>
        volatile bool _isRunning;

        /// <summary>
        /// The application buffer where received data will be stored for each device.
        /// </summary>
        WindowedBuffer<float> _buffer;

        /// <summary>
        /// The common number of scans to be received at once per <see cref="gUSBampWrapper.GetData"/> call for all devices.
        /// </summary>
        ushort _numberOfScans;

        /// <summary>
        /// The device configurations built by the <see cref="OpenAndInitDevices"/> method in <see cref="DoAcquisition"/> based on the last <see cref="StartAcquisition"/> call. Necessary for <see cref="ReadData"/> to decode buffer alignment.
        /// </summary>
        List<Device> _devices;

        #endregion

        #region Dll-Import directives...

        const long WAIT_TIMEOUT = 0x00000102L;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetOverlappedResult(IntPtr hFile, [In] IntPtr lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        //MAYBE TRY IT WITH SafeWaitHandle AS RETURN TYPE?
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ResetEvent(IntPtr hEvent);

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="DataAcquisitionUnit"/>.
        /// </summary>
        public DataAcquisitionUnit()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Tries to open and initialize multiple devices specified by their serial numbers and returns a collection of the corresponding device handles in the appropriate call sequence order. 
        /// The master device will be at the end of the list.
        /// </summary>
        /// <param name="deviceSerials">A dictionary of serial numbers whose corresponding devices should be opened and initialized with the corresponding <see cref="DeviceConfiguration"/>. Exactly one device has to be configured as master device, i.e. the <see cref="DeviceConfiguration.IsSlave"/> flag of exactly one <see cref="DeviceConfiguration"/> in <see cref="deviceSerials"/> has to be set to <b>false</b>.</param>
        /// <returns>A collection of <see cref="Device"/> structures in the order specified by <see cref="deviceSerials"/>.</returns>
        /// <exception cref="DeviceException">If at least one of the specified devices couldn't be opened or initialized.</exception>
        /// <exception cref="Exception">If not exactly one device in <see cref="deviceSerials"/> is configured as master, or if at least one device has a different sample rate than the others.</exception>
        private List<Device> OpenAndInitDevices(Dictionary<string, DeviceConfiguration> deviceSerials)
        {
            List<Device> openedDevices = new List<Device>();
            List<string> callSequence = new List<string>();
            string masterSerial = null;
            int sampleRate = 0;

            try
            {
                //construct call sequence
                foreach (string serialNumber in deviceSerials.Keys)
                {
                    DeviceConfiguration deviceConfiguration = deviceSerials[serialNumber];

                    //ensure that all devices have the same sample rate
                    if (callSequence.Count == 0)
                    {
                        sampleRate = deviceConfiguration.SampleRate;
                        _numberOfScans = deviceConfiguration.NumberOfScans;
                    }
                    else
                    {
                        if (sampleRate != deviceConfiguration.SampleRate)
                            throw new Exception(String.Format("Invalid sample rate for device {0}. All devices must have the same sample rate.", serialNumber));

                        if (_numberOfScans != deviceConfiguration.NumberOfScans)
                            throw new Exception(String.Format("Invalid number of scans (buffer size) for device {0}. All devices must have the same number of scans (buffer size).", serialNumber));
                    }

                    //ensure that not more than one device will be configured as master
                    if (masterSerial != null && !deviceConfiguration.IsSlave)
                        throw new Exception(String.Format("Couldn't configure device {0} as master. A master device has already been configured.", serialNumber));

                    //add the slave devices to the call sequence before the master device will be added
                    if (deviceConfiguration.IsSlave)
                        callSequence.Add(serialNumber);
                    else
                        masterSerial = serialNumber;
                }

                //ensure that there is exactly one device configured as master
                if (masterSerial == null)
                    throw new Exception("No device has been configured as master.");

                //add master serial at the end of the call sequence
                callSequence.Add(masterSerial);

                //open and initialize devices
                foreach (string serialNumber in callSequence)
                {
                    Device currentDevice = new Device();
                    currentDevice.SerialNumber = serialNumber;
                    currentDevice.Configuration = deviceSerials[serialNumber];

                    //open device
                    currentDevice.Handle = gUSBampWrapper.OpenDevice(serialNumber);
                    openedDevices.Add(currentDevice);

                    double Imp = gUSBampWrapper.GetImpedance(currentDevice.Handle, 1);

                    //set the channels to acquire
                    gUSBampWrapper.SetChannels(currentDevice.Handle, currentDevice.Configuration.SelectedChannels.ToArray());

                    //set the sample rate
                    gUSBampWrapper.SetSampleRate(currentDevice.Handle, currentDevice.Configuration.SampleRate);

                    //enable/disable trigger lines
                    gUSBampWrapper.EnableTriggerLine(currentDevice.Handle, currentDevice.Configuration.TriggerLineEnabled);

                    //set buffer size
                    gUSBampWrapper.SetBufferSize(currentDevice.Handle, currentDevice.Configuration.NumberOfScans);

                    //reset all filters
                    for (byte channel = 1; channel <= MaxNumberOfChannels; channel++)
                    {
                        //set bandpass filters
                        if (currentDevice.Configuration.BandpassFilters.ContainsKey(channel))
                            gUSBampWrapper.SetBandpass(currentDevice.Handle, channel, currentDevice.Configuration.BandpassFilters[channel]);
                        else
                            gUSBampWrapper.SetBandpass(currentDevice.Handle, channel, -1);

                        //set notch filters
                        if (currentDevice.Configuration.NotchFilters.ContainsKey(channel))
                            gUSBampWrapper.SetNotch(currentDevice.Handle, channel, currentDevice.Configuration.NotchFilters[channel]);
                        else
                            gUSBampWrapper.SetNotch(currentDevice.Handle, channel, -1);
                    }

                    //set device to master/slave mode
                    gUSBampWrapper.SetSlave(currentDevice.Handle, currentDevice.Configuration.IsSlave);

                    //enable/disable shortcut function
                    gUSBampWrapper.EnableSC(currentDevice.Handle, currentDevice.Configuration.SCEnabled);

                    //set bipolar settings
                    gUSBampWrapper.SetBipolar(currentDevice.Handle, currentDevice.Configuration.BipolarSettings);

                    //if counter is selected Mode NORMAL has to be selected first
                    if (currentDevice.Configuration.Mode == gUSBampWrapper.OperationModes.Counter)
                        gUSBampWrapper.SetMode(currentDevice.Handle, gUSBampWrapper.OperationModes.Normal);

                    //set mode of operation
                    gUSBampWrapper.SetMode(currentDevice.Handle, currentDevice.Configuration.Mode);

                    if (currentDevice.Configuration.Mode == gUSBampWrapper.OperationModes.Normal || currentDevice.Configuration.Mode == gUSBampWrapper.OperationModes.Counter)
                    {
                        //set common reference
                        gUSBampWrapper.SetReference(currentDevice.Handle, currentDevice.Configuration.CommonReference);

                        //set common ground
                        gUSBampWrapper.SetGround(currentDevice.Handle, currentDevice.Configuration.CommonGround);
                    }

                    //set DAC settings
                    gUSBampWrapper.SetDAC(currentDevice.Handle, currentDevice.Configuration.Dac);

                    //set DRL settings
                    gUSBampWrapper.SetDRLChannel(currentDevice.Handle, currentDevice.Configuration.Drl);

                    Console.WriteLine("\tg.USBamp {0} initialized as {1} (#{2} in the call sequence)!", serialNumber, (currentDevice.Configuration.IsSlave) ? "slave" : "master", openedDevices.Count);
                }
            }
            catch (Exception ex)
            {
                //in case an exception occured, close all already opened devices first...
                for (int i = 0; i < openedDevices.Count; i++)
                    gUSBampWrapper.CloseDevice(ref openedDevices[i].Handle);

                //...and rethrow the exception to notify the caller of this method about the error
                throw ex;
            }

            return openedDevices;
        }

        /// <summary>
        /// Starts data acquisition from the devices whose serial number is specified by <see cref="deviceSerials"/> in a separate thread after opening and initializing the devices.
        /// </summary>
        /// <exception cref="InvalidOperationException">If data acquisition is already running.</exception>
        public void StartAcquisition(Dictionary<string, DeviceConfiguration> deviceSerials)
        {
            //ensure that data acquisition is not already running
            if (_isRunning || (_acquisitionThread != null && _acquisitionThread.IsAlive))
                throw new InvalidOperationException("Data acquisition is already running!");

            _isRunning = true;

            //determine total number of channels
            int totalChannels = 0;
            int sampleRate = 0;

            foreach (DeviceConfiguration deviceConfiguration in deviceSerials.Values)
            {
                totalChannels += deviceConfiguration.SelectedChannels.Count + Convert.ToInt32(deviceConfiguration.TriggerLineEnabled);
                sampleRate = deviceConfiguration.SampleRate;
            }

            //initialize buffer
            _buffer = new WindowedBuffer<float>(BufferSizeSeconds * sampleRate * totalChannels);

            //start data acquisition thread
            _acquisitionThread = new Thread(DoAcquisition);
            _acquisitionThread.Name = "g.USBamp DataAcquisition Thread";
            _acquisitionThread.Priority = ThreadPriority.Highest;
            _acquisitionThread.Start(deviceSerials);
        }

        /// <summary>
        /// Tells the data acquisition thread to stop, closes all devices and blocks until data acquisition has been stopped.
        /// </summary>
        public void StopAcquisition()
        {
            //tell the data acquisition thread to stop
            _isRunning = false;

            //wait until the thread has stopped data acquisition
            if (_acquisitionThread != null)
                _acquisitionThread.Join();
        }

        /// <summary>
        /// Opens and initializes devices, and acquires data until <see cref="StopAcquisition"/> is called (i.e. <see cref="_isRunning"/> was set to <b>false</b>).
        /// Then, data acquisition will be stopped.
        /// </summary>
        /// <param name="param">A <see cref="Dictionary<string, DeviceConfiguration"/> object containing the serial numbers and their corresponding <see cref="DeviceConfiguration"/> objects.</param>
        private void DoAcquisition(object param)
        {
            Dictionary<string, DeviceConfiguration> deviceSerials = (Dictionary<string, DeviceConfiguration>)param;
            int numDevices = deviceSerials.Keys.Count;
            ReceiveBuffer[,] receiveBuffers = new ReceiveBuffer[numDevices, QueueSize];
            _devices = null;
            int queueIndex = 0;
            int deviceIndex = 0;
            uint numBytesReceived = 0;
            int convertedBufferSize = 0;

            try
            {
                Console.WriteLine("Opening and initializing devices...");

                //open and initialize devices for data acquisition
                _devices = OpenAndInitDevices(deviceSerials);

                //for each device create a number of QueueSize data buffers
                for (deviceIndex = 0; deviceIndex < numDevices; deviceIndex++)
                {
                    int numberOfSamples = _numberOfScans * (_devices[deviceIndex].Configuration.SelectedChannels.Count + Convert.ToInt32(_devices[deviceIndex].Configuration.TriggerLineEnabled));
                    int bufferSizeBytes = gUSBampWrapper.HeaderSize + numberOfSamples * sizeof(float);
                    convertedBufferSize += numberOfSamples;

                    for (queueIndex = 0; queueIndex < QueueSize; queueIndex++)
                    {
                        //initialize buffer
                        receiveBuffers[deviceIndex, queueIndex] = new ReceiveBuffer();
                        receiveBuffers[deviceIndex, queueIndex].Buffer = new byte[bufferSizeBytes];

                        //create Windows event used for data acquisition
                        receiveBuffers[deviceIndex, queueIndex].Overlapped = new NativeOverlapped();
                        receiveBuffers[deviceIndex, queueIndex].Overlapped.EventHandle = CreateEvent(IntPtr.Zero, true, false, String.Empty);

                        if (receiveBuffers[deviceIndex, queueIndex].Overlapped.EventHandle == IntPtr.Zero)
                            throw new NullReferenceException("Couldn't create windows event for data reception.");

                        receiveBuffers[deviceIndex, queueIndex].Overlapped.OffsetLow = 0;
                        receiveBuffers[deviceIndex, queueIndex].Overlapped.OffsetHigh = 0;

                        //allocate memory where the native driver writes the received data to and pin the address of this memory location
                        receiveBuffers[deviceIndex, queueIndex].BufferHandle = GCHandle.Alloc(receiveBuffers[deviceIndex, queueIndex].Buffer, GCHandleType.Pinned);

                        //allocate memory for the NativeOverlapped structure in the global unmanaged (and fixed) memory space and copy previously created content from managed structure to it
                        receiveBuffers[deviceIndex, queueIndex].OverlappedPointer = Marshal.AllocHGlobal(Marshal.SizeOf(receiveBuffers[deviceIndex, queueIndex].Overlapped));
                        Marshal.StructureToPtr(receiveBuffers[deviceIndex, queueIndex].Overlapped, receiveBuffers[deviceIndex, queueIndex].OverlappedPointer, false);
                    }
                }

                //create the data buffer that holds the proper order of the scans for all devices
                float[] convertedBuffer = new float[convertedBufferSize];

                Console.Write("Starting acquisition...");

                //start the devices (master device must be started at last)
                for (deviceIndex = 0; deviceIndex < numDevices; deviceIndex++)
                {
                    //start device
                    gUSBampWrapper.Start(_devices[deviceIndex].Handle);

                    //queue-up the first batch of transfer requests
                    for (queueIndex = 0; queueIndex < QueueSize; queueIndex++)
                        gUSBampWrapper.GetData(_devices[deviceIndex].Handle, receiveBuffers[deviceIndex, queueIndex].BufferHandle.AddrOfPinnedObject(), (uint)receiveBuffers[deviceIndex, queueIndex].Buffer.Length, receiveBuffers[deviceIndex, queueIndex].OverlappedPointer);
                }
                //Thread.Sleep(100);

                Console.Write(" started!\n");
                Console.WriteLine("Receiving data...");

                //continuous data acquisition
                while (_isRunning)
                {
                    //process each queued GetData call
                    for (queueIndex = 0; queueIndex < QueueSize; queueIndex++)
                    {
                        //receive data from each device
                        for (deviceIndex = 0; deviceIndex < numDevices; deviceIndex++)
                        {
                            int bufferSizeBytes = receiveBuffers[deviceIndex, queueIndex].Buffer.Length;
                            int scanSizeBytes = (bufferSizeBytes - gUSBampWrapper.HeaderSize) / _numberOfScans;

                            //wait for notification from the system telling that new data is available
                            if (WaitForSingleObject(receiveBuffers[deviceIndex, queueIndex].Overlapped.EventHandle, 1000) == WAIT_TIMEOUT)
                                throw new TimeoutException("Error on data transfer: timeout occured.");

                            //get number of received bytes...
                            if (!GetOverlappedResult(_devices[deviceIndex].Handle, receiveBuffers[deviceIndex, queueIndex].OverlappedPointer, out numBytesReceived, false))
                                throw new InvalidOperationException(String.Format("Error on data transfer: GetOverlappedResult returned false (error Code {0}).", Marshal.GetLastWin32Error()));

                            //...and check if we lost something (number of received bytes must be equal to the previously allocated buffer size)
                            if (numBytesReceived != bufferSizeBytes)
                                throw new Exception("Error on data transfer: samples lost.");

                            //retain order of scans between devices and convert received data to float values
                            for (int i = 0; i < _numberOfScans; i++)
                                Buffer.BlockCopy(receiveBuffers[deviceIndex, queueIndex].Buffer, gUSBampWrapper.HeaderSize + i * scanSizeBytes, convertedBuffer, scanSizeBytes * (i * numDevices + (numDevices - deviceIndex - 1)), scanSizeBytes);

                            //reset reception event
                            ResetEvent(receiveBuffers[deviceIndex, queueIndex].Overlapped.EventHandle);

                            //add new GetData call to the queue replacing the currently received one
                            gUSBampWrapper.GetData(_devices[deviceIndex].Handle, receiveBuffers[deviceIndex, queueIndex].BufferHandle.AddrOfPinnedObject(), (uint)bufferSizeBytes, receiveBuffers[deviceIndex, queueIndex].OverlappedPointer);
                            //Thread.Sleep(100);
                        }

                        //write merged data of all devices into the application buffer
                        lock (_buffer.SyncRoot)
                        {
                            //check if the buffer might overrun
                            if ((_buffer.Capacity - _buffer.Count) < convertedBuffer.Length)
                                throw new OverflowException("Error on writing data to buffer: buffer overflow.");

                            //write received data into the data buffer dropping the header
                            _buffer.Enqueue(convertedBuffer);
                        }
                    }
                    //Thread.Sleep(1000);
                }
                //Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                //print exception message
                Console.WriteLine("\t{0}", ex.Message);
            }
            finally
            {
                Console.WriteLine("Stopping devices, closing them and cleaning up...");

                for (int j = 0; j < QueueSize; j++)
                {
                    if (queueIndex == QueueSize)
                        queueIndex = 0;

                    for (int i = 0; i < numDevices; i++)
                    {
                        if (deviceIndex == numDevices)
                            deviceIndex = 0;

                        //stop devices the first time this loop iterates for a specific device
                        if (j == 0 && _devices != null)
                        {
                            //stop device
                            gUSBampWrapper.Stop(_devices[deviceIndex].Handle);

                            //reset device
                            gUSBampWrapper.ResetTransfer(_devices[deviceIndex].Handle);

                            //close device
                            gUSBampWrapper.CloseDevice(ref _devices[deviceIndex].Handle);
                        }

                        if (receiveBuffers[deviceIndex, queueIndex] != null)
                        {
                            //close event handle
                            WaitForSingleObject(receiveBuffers[deviceIndex, queueIndex].Overlapped.EventHandle, 1000);
                            CloseHandle(receiveBuffers[deviceIndex, queueIndex].Overlapped.EventHandle);

                            //free allocated pinned memory
                            receiveBuffers[deviceIndex, queueIndex].BufferHandle.Free();
                            Marshal.FreeHGlobal(receiveBuffers[deviceIndex, queueIndex].OverlappedPointer);
                            receiveBuffers[deviceIndex, queueIndex].OverlappedPointer = IntPtr.Zero;
                        }

                        deviceIndex++;
                    }

                    queueIndex++;
                }

                //reset running flag
                _isRunning = false;
            }
        }

        /// <summary>
        /// Reads a number of <see cref="numberOfScans"/> scans (samples) from the reception data buffer (<see cref="_buffer"/>).
        /// </summary>
        /// <param name="numberOfBytes">The number of bytes to read.</param>
        /// <returns>An array containing the received data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="numberOfBytes"/> is less than zero.</exception>
        /// <remarks>
        /// If there are less than <paramref name="numberOfBytes"/> bytes in the buffer, only the available elements will be removed and returned.
        /// <para/>
        /// If the buffer is empty, an array of size 0 will be returned.
        /// </remarks>
        public float[] ReadData(int numberOfBytes)
        {
            return _buffer.Dequeue(numberOfBytes);
        }
    }
}
