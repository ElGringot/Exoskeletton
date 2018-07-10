using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace gUSBampSyncDemoCS
{
    /// <summary>
    /// .NET-Wrapper providing access to the native C-API of g.USBamp device from "Guger Technologies OG" respectively "g.tec medical engineering GmbH".
    /// See documentation of g.USBamp C API for further information on the underlying C API functions.
    /// </summary>
    /// <remarks>
    /// The g.USBamp driver ("gUSBamp.dll") for Microsoft Windows operating systems must be installed before.
    /// </remarks>
    public static class gUSBampWrapper
    {
        #region Constants...

        /// <summary>
        /// Path and filename of the PC-API dll-file of g.USBamp.
        /// </summary>
        public const string DllName = "gUSBamp.dll";

        /// <summary>
        /// The size of the header of received data (by <see cref="GetData"/>) in bytes that precedes the acutal data samples.
        /// </summary>
        public const int HeaderSize = 38;

        #endregion

        #region Structures...

        /// <summary>
        /// Describes the digital output states for the digital channels of the g.USBamp version 3.0.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DigitalOut
        {
            /// <summary>
            /// <b>true</b>, if digital OUT0 should be set to <see cref="Value0"/>.
            /// </summary>
            public bool Set0;

            /// <summary>
            /// Value of digital OUT0 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool Value0;

            /// <summary>
            /// <b>true</b>, if digital OUT1 should be set to <see cref="Value1"/>.
            /// </summary>
            public bool Set1;

            /// <summary>
            /// Value of digital OUT1 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool Value1;

            /// <summary>
            /// <b>true</b>, if digital OUT2 should be set to <see cref="Value2"/>.
            /// </summary>
            public bool Set2;

            /// <summary>
            /// Value of digital OUT2 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool Value2;

            /// <summary>
            /// <b>true</b>, if digital OUT3 should be set to <see cref="Value3"/>.
            /// </summary>
            public bool Set3;

            /// <summary>
            /// Value of digital OUT3 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool Value3;
        }

        /// <summary>
        /// Describes the digital input and output states for the digital channels of the g.USBamp version 2.0.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DigitalIO
        {
            /// <summary>
            /// Value of the digital input DIN1 ((<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool DIn1;

            /// <summary>
            /// Value of the digital input DIN2 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool DIn2;

            /// <summary>
            /// Value of the digital output DOUT1 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool DOut1;

            /// <summary>
            /// Value of the digital output DOUT2 (<b>true</b> for 'high', <b>false</b> for 'low').
            /// </summary>
            public bool DOut2;
        }

        /// <summary>
        /// Describes filter settings.
        /// </summary>
        /// <remarks>This structure is used for the public wrapper functions. Don't use this with API calls!</remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Filter
        {
            /// <summary>
            /// Lower border frequency of the filter in Hz.
            /// </summary>
            public float LowerBorderFrequency;

            /// <summary>
            /// Upper border frequency of the filter in Hz.
            /// </summary>
            public float UpperBorderFrequency;

            /// <summary>
            /// Sample rate in Hz.
            /// </summary>
            public float SampleRate;

            /// <summary>
            /// Filter type.
            /// </summary>
            public FilterTypes Type;

            /// <summary>
            /// Order of the filter.
            /// </summary>
            public float Order;
        }

        /// <summary>
        /// Describes filter settings.
        /// </summary>
        /// <remarks>For API calls use this structure.</remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct Filt
        {
            /// <summary>
            /// Lower border frequency of the filter in Hz.
            /// </summary>
            public float LowerBorderFrequency;

            /// <summary>
            /// Upper border frequency of the filter in Hz.
            /// </summary>
            public float UpperBorderFrequency;

            /// <summary>
            /// Sample rate in Hz.
            /// </summary>
            public float SampleRate;

            /// <summary>
            /// Filter type.
            /// 1...Butterworth
            /// 2...Chebyshev
            /// </summary>
            public float Type;

            /// <summary>
            /// Order of the filter.
            /// </summary>
            public float Order;
        }

        /// <summary>
        /// Describes settings for common ground (GND).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Gnd
        {
            /// <summary>
            /// <b>true</b> to connect group A to common ground; <b>false</b> otherwise.
            /// </summary>
            public bool Gnd1;

            /// <summary>
            /// <b>true</b> to connect group B to common ground; <b>false</b> otherwise.
            /// </summary>
            public bool Gnd2;

            /// <summary>
            /// <b>true</b> to connect group C to common ground; <b>false</b> otherwise.
            /// </summary>
            public bool Gnd3;

            /// <summary>
            /// <b>true</b> to connect group D to common ground; <b>false</b> otherwise.
            /// </summary>
            public bool Gnd4;
        }

        /// <summary>
        /// Describes settings for common reference.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Ref
        {
            /// <summary>
            /// <b>true</b> to connect group A to common reference; <b>false</b> otherwise.
            /// </summary>
            public bool Ref1;

            /// <summary>
            /// <b>true</b> to connect group B to common reference; <b>false</b> otherwise.
            /// </summary>
            public bool Ref2;

            /// <summary>
            /// <b>true</b> to connect group C to common reference; <b>false</b> otherwise.
            /// </summary>
            public bool Ref3;

            /// <summary>
            /// <b>true</b> to connect group D to common reference; <b>false</b> otherwise.
            /// </summary>
            public bool Ref4;
        }

        /// <summary>
        /// Describes the mapping of the channels for bipolar derivation.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Bipolar
        {
            /// <summary>
            /// The channel number for the bipolar derivation with channel 1 (g.USBamp performs bipolar derivation between specified channel number and channel 1). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel1;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 2 (g.USBamp performs bipolar derivation between specified channel number and channel 2). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel2;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 3 (g.USBamp performs bipolar derivation between specified channel number and channel 3). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel3;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 4 (g.USBamp performs bipolar derivation between specified channel number and channel 4). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel4;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 5 (g.USBamp performs bipolar derivation between specified channel number and channel 5). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel5;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 6 (g.USBamp performs bipolar derivation between specified channel number and channel 6). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel6;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 7 (g.USBamp performs bipolar derivation between specified channel number and channel 7). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel7;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 8 (g.USBamp performs bipolar derivation between specified channel number and channel 8). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel8;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 9 (g.USBamp performs bipolar derivation between specified channel number and channel 9). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel9;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 10 (g.USBamp performs bipolar derivation between specified channel number and channel 10). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel10;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 11 (g.USBamp performs bipolar derivation between specified channel number and channel 11). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel11;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 12 (g.USBamp performs bipolar derivation between specified channel number and channel 12). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel12;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 13 (g.USBamp performs bipolar derivation between specified channel number and channel 13). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel13;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 14 (g.USBamp performs bipolar derivation between specified channel number and channel 14). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel14;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 15 (g.USBamp performs bipolar derivation between specified channel number and channel 15). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel15;

            /// <summary>
            /// The channel number for the bipolar derivation with channel 16 (g.USBamp performs bipolar derivation between specified channel number and channel 16). Set to zero if no bipolar derivation should be perform.
            /// </summary>
            public byte Channel16;
        }

        /// <summary>
        /// Defines the channels for DRL calculation.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Channel
        {
            /// <summary>
            /// <b>1</b> if channel 1 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel1;

            /// <summary>
            /// <b>1</b> if channel 2 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel2;

            /// <summary>
            /// <b>1</b> if channel 3 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel3;

            /// <summary>
            /// <b>1</b> if channel 4 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel4;

            /// <summary>
            /// <b>1</b> if channel 5 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel5;

            /// <summary>
            /// <b>1</b> if channel 6 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel6;

            /// <summary>
            /// <b>1</b> if channel 7 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel7;

            /// <summary>
            /// <b>1</b> if channel 8 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel8;

            /// <summary>
            /// <b>1</b> if channel 9 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel9;

            /// <summary>
            /// <b>1</b> if channel 10 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel10;

            /// <summary>
            /// <b>1</b> if channel 11 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel11;

            /// <summary>
            /// <b>1</b> if channel 12 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel12;

            /// <summary>
            /// <b>1</b> if channel 13 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel13;

            /// <summary>
            /// <b>1</b> if channel 14 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel14;

            /// <summary>
            /// <b>1</b> if channel 15 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel15;

            /// <summary>
            /// <b>1</b> if channel 16 should be used for driven right leg (DRL) calculation, <b>0</b> otherwise.
            /// </summary>
            public byte Channel16;
        }

        /// <summary>
        /// Defines the calibration/DRL settings.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DAC
        {
            /// <summary>
            /// The output wave shape.
            /// </summary>
            public WaveShapes WaveShape;

            /// <summary>
            /// The amplitude of the output signal (max: 2000 (250mV), min: -2000 (-250mV)).
            /// </summary>
            public ushort Amplitude;

            /// <summary>
            /// The frequency of the output signal in Hz.
            /// </summary>
            public ushort Frequency;

            /// <summary>
            /// The offset of the output signal (no offset: 2047, min: 0, max: 4096).
            /// </summary>
            public ushort Offset;
        }

        /// <summary>
        /// Contains offset and scaling for each channel.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Scale
        {
            /// <summary>
            /// An array containing the scaling factor of each channel. <see cref="Factor"/>[i] contains scale factor for channel i.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] Factor;

            /// <summary>
            /// An array containing the offset of each channel in microvolts (µV). <see cref="Offset"/>[i] contains offset for channel i.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] Offset;
        }

        /// <summary>
        /// Contains information about the device represented by a string.
        /// </summary>
        public struct GT_DeviceInfo
        {
            /// <summary>
            /// Textual summary of the device's configuration.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DeviceInfo;
        }

        #endregion

        #region Enumerations...

        /// <summary>
        /// Operation modes of the g.USBamp.
        /// </summary>
        public enum OperationModes : byte
        {
            /// <summary>
            /// Acquire data from the 16 input channels.
            /// </summary>
            Normal = 0,

            /// <summary>
            /// Measure the electrode impedance.
            /// </summary>
            Impedance = 1,

            /// <summary>
            /// Calibrate the input channels. Applies a calibration signal onto all input channels.
            /// </summary>
            Calibrate = 2,

            /// <summary>
            /// If channel 16 is selected there is a counter on this channel (overrun at 1e6).
            /// </summary>
            Counter = 3
        };

        /// <summary>
        /// Output waveshapes of the g.USBamp.
        /// </summary>
        public enum WaveShapes : byte
        {
            /// <summary>
            /// Generate square wave signal.
            /// </summary>
            Square = 1,

            /// <summary>
            /// Generate sawtooth signal.
            /// </summary>
            Sawtooth = 2,

            /// <summary>
            /// Generate sine wave
            /// </summary>
            Sine = 3,

            /// <summary>
            /// Generate DRL signal.
            /// </summary>
            DRL = 4,

            /// <summary>
            /// Generate white noise.
            /// </summary>
            Noise = 5
        };

        /// <summary>
        /// Types of filters of the g.USBamp.
        /// </summary>
        public enum FilterTypes : byte
        {
            /// <summary>
            /// Butterworth filter type.
            /// </summary>
            Butterworth = 1,

            /// <summary>
            /// Chebychev filter type.
            /// </summary>
            Chebychev = 2
        };

        #endregion

        #region Dll-Import directives...

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static IntPtr GT_OpenDevice(int iPortNumber);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static IntPtr GT_OpenDeviceEx([MarshalAs(UnmanagedType.LPStr)] string lpSerial);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_CloseDevice(ref IntPtr hDevice);

        /// <summary>
        /// Extracts data from the driver buffer.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="pData">A pinned handle to an array of type <see cref="float"/> (the buffer).</param>
        /// <param name="dwSzBuffer">The number of bytes to receive from the driver (equals the size of <paramref name="data"/> in bytes.</param>
        /// <param name="overlapped">A pointer to a memory space allocated in unmanaged global memory representing a <see cref="NativeOverlapped"/> structure (Win32-API: OVERLAPPED) that performs the I/O transfer.</param>
        /// <returns><b>true</b> if the call succeeded; <b>false</b> otherwise.</returns>
        /// <remarks>
        /// The function doesn't block the calling thread because of the overlapped mode. The function call returns immediately but data is not valid until the event in the <see cref="NativeOverlapped"/> structure is triggered. Use the <c>WaitForSingleObject()</c> method from the Win32-API to determine if the transfer has finished. Use <c>GetOverlappedResult()</c> from the Win32-API to retrieve the number of bytes that are available.
        /// <para/>
        /// <paramref name="bufferSize"/> must correspond to the number of scans that has been set by <see cref="SetBufferSize"/>. Furthermore a number of <see cref="HeaderSize"/> bytes precede the acquired data and have to be discarded. E.g. for 16 channels sampled at 256Hz and a number of 8 scans set by <see cref="SetBufferSize"/> the <paramref name="bufferSize"/> for this method equals: <paramref name="bufferSize"/> = 8 scans * 16 channels * sizeof(float)
        /// </remarks>
        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetData(IntPtr hDevice, IntPtr pData, uint dwSzBuffer, IntPtr overlapped);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetBufferSize(IntPtr hDevice, ushort wBufferSize);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetSampleRate(IntPtr hDevice, ushort wSampleRate);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_Start(IntPtr hDevice);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_Stop(IntPtr hDevice);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetChannels(IntPtr hDevice, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] byte[] ucChannels, byte ucChannelSize);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_ResetTransfer(IntPtr hDevice);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetDigitalOut(IntPtr hDevice, byte ucNumber, byte ucValue);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetDigitalOutEx(IntPtr hDevice, DigitalOut dout);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetDigitalIO(IntPtr hDevice, ref DigitalIO dio);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetDigitalOut(IntPtr hDevice, ref DigitalOut dout);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_EnableTriggerLine(IntPtr hDevice, bool bEnable);

        /// <summary>
        /// Reads in the available bandpass filter settings.
        /// </summary>
        /// <param name="filterSpecs">A pointer to an array of <see cref="Filt"/> structures.</param>
        /// <returns><b>true</b> if the call succeeded; <b>false</b> otherwise.</returns>
        /// <remarks>The size of the array that <paramref name="filterSpecs"/> points to can be determined by <see cref="GT_GetNumberOfFilter"/>.</remarks>
        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetFilterSpec(IntPtr filterSpecs);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetNumberOfFilter(ref int nof);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetBandPass(IntPtr hDevice, byte ucChannel, int index);

        /// <summary>
        /// Reads in the available notch filter settings.
        /// </summary>
        /// <param name="filterSpecs">A pointer to an array of <see cref="Filt"/> structures.</param>
        /// <returns><b>true</b> if the call succeeded; <b>false</b> otherwise.</returns>
        /// <remarks>The size of the array that <paramref name="filterSpecs"/> points to can be determined by <see cref="GT_GetNumberOfNotch"/>.</remarks>
        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetNotchSpec(IntPtr filterSpecs);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetNumberOfNotch(ref int nof);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetNotch(IntPtr hDevice, byte ucChannel, int index);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetMode(IntPtr hDevice, OperationModes ucMode);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetMode(IntPtr hDevice, ref OperationModes ucMode);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetGround(IntPtr hDevice, Gnd commonGround);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetGround(IntPtr hDevice, ref Gnd commonGround);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetReference(IntPtr hDevice, Ref commonReference);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetReference(IntPtr hDevice, ref Ref commonReference);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetBipolar(IntPtr hDevice, Bipolar bipoChannel);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetDRLChannel(IntPtr hDevice, Channel drlChannel);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_EnableSC(IntPtr hDevice, bool bEnable);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetSlave(IntPtr hDevice, bool bSlave);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetDAC(IntPtr hDevice, DAC analogOut);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_SetScale(IntPtr hDevice, Scale scaling);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetScale(IntPtr hDevice, ref Scale scaling);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_Calibrate(IntPtr hDevice, ref Scale scaling);

        /// <summary>
        /// Gets the last occured error code and corresponding error message.
        /// </summary>
        /// <param name="wErrorCode">Holds the error code of the last occured error after this method returns.</param>
        /// <param name="pLastError">Holds the corresponding error message after this method returns.</param>
        /// <returns><b>true</b> if the call succeeded; <b>false</b> otherwise.</returns>
        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetLastError(ref ushort wErrorCode, IntPtr pLastError);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static float GT_GetDriverVersion();

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static float GT_GetHWVersion(IntPtr hDevice);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetSerial(IntPtr hDevice, IntPtr lpstrSerial, uint uiSize);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        extern static bool GT_GetImpedance(IntPtr hDevice, byte channel, ref double impedance);

        #endregion

        #region Wrapper functions...

        /// <summary>
        /// Opens a g.USBamp on the specified USB port number and returns a handle to it.
        /// </summary>
        /// <param name="portNumber">The number of the USB port where the g.USBamp is connected to.</param>
        /// <returns>A handle to the device (needed for further function calls).</returns>
        /// <exception cref="DeviceException">Will be thrown, if the device couldn't be opened on the specified port.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_OpenDevice</c>.</remarks>
        public static IntPtr OpenDevice(int portNumber)
        {
            IntPtr deviceHandle = GT_OpenDevice(portNumber);

            if (deviceHandle == IntPtr.Zero)
                HandleError();

            return deviceHandle;
        }

        /// <summary>
        /// Opens the g.USBamp with the specified serial number.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the g.USBamp to open.</param>
        /// <returns>A handle to the device (needed for further function calls).</returns>
        /// <exception cref="DeviceException">Will be thrown, if the device with the specified serial number couldn't be opened.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_OpenDeviceEx</c>.</remarks>
        public static IntPtr OpenDevice(string deviceSerialNumber)
        {
            IntPtr deviceHandle = GT_OpenDeviceEx(deviceSerialNumber);

            if (deviceHandle == IntPtr.Zero)
                HandleError();

            return deviceHandle;
        }

        /// <summary>
        /// Closes the connection with the g.USBamp device identified by the handle <paramref name="hDevice"/>.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_CloseDevice</c>.</remarks>
        public static void CloseDevice(ref IntPtr hDevice)
        {
            if(!GT_CloseDevice(ref hDevice))
                HandleError();
        }

        /// <summary>
        /// Extracts data from the driver buffer and retrieves a pointer to that buffer.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="data">A pinned handle to an array of type <see cref="byte"/> (the buffer).</param>
        /// <param name="bufferSize">The number of bytes to receive from the driver (must equal the size of the buffer where <paramref name="data"/> points to in bytes.</param>
        /// <param name="overlapped">A pointer to a memory space allocated in unmanaged global memory representing a <see cref="NativeOverlapped"/> structure (Win32-API: OVERLAPPED) that performs the I/O transfer.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// The content of the returned buffer (to which <paramref name="data"/> points to) contains <paramref name="bufferSize"/> bytes and consists of the following:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Bytes (of the buffer; zero-based index)</term>
        ///         <description>Content</description>
        ///     </listheader>
        ///     <item>
        ///         <term>0 to <see cref="HeaderSize"/>-1</term>
        ///         <description>Header</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="HeaderSize"/> to <paramref name="bufferSize"/>-1</term>
        ///         <description>Starting from byte index <see cref="HeaderSize"/> groups of 4 bytes compose one <see cref="float"/> value. Assuming that values of a number of <i>n</i> channels are delivered per scan, there are <i>n</i> consecutive groups of 4 bytes which form sample values of all <i>n</i> channels (one value for each channel) of one scan. Assuming that <i>m</i> scans should be delivered with one call of <see cref="GetData"/> (see <see cref="SetBufferSize"/>), there are <i>m</i> consecutive groups of sample values each representing one scan.</description>
        ///     </item>
        /// </list>
        /// Therefore <paramref name="bufferSize"/> calculates as follows: <paramref name="bufferSize"/> = <see cref="HeaderSize"/> + <i>m</i> * <i>n</i> * 4
        /// <para/>
        /// The function doesn't block the calling thread because of the overlapped mode. The function call returns immediately but data is not valid until the event in the <see cref="NativeOverlapped"/> structure is triggered. Use the <c>WaitForSingleObject()</c> method from the Win32-API to determine if the transfer has finished. Use <c>GetOverlappedResult()</c> from the Win32-API to retrieve the number of bytes that are available.
        /// <para/>
        /// <paramref name="bufferSize"/> must correspond to the number of scans that has been set by <see cref="SetBufferSize"/>. Furthermore a number of <see cref="HeaderSize"/> bytes precede the acquired data and have to be discarded. E.g. for 16 channels sampled at 256Hz and a number of 8 scans set by <see cref="SetBufferSize"/> the <paramref name="bufferSize"/> for this method equals: <paramref name="bufferSize"/> = 8 scans * 16 channels * sizeof(float)
        /// <para/>
        /// Corresponding C-API method is <c>GT_GetData</c>.
        /// </remarks>
        public static void GetData(IntPtr hDevice, IntPtr data, uint bufferSize,IntPtr overlapped)
        {
            if (!GT_GetData(hDevice, data, bufferSize, overlapped))
                HandleError();
        }

        /// <summary>
        /// Sets the number of scans to receive per buffer.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="numberOfScans">The number of scans to receive per buffer.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// One scan contains exactly one sample from each channel.
        /// <para/>
        /// Buffer size should be at least 20-30ms (60ms recommended). To calculate a 60ms buffer use following equation: <paramref name="numberOfScans"/> >= (sample rate) * (60*10^-3). For example, sample rate = 128Hz: 128*60*10^-3 = 7.68 --> <paramref name="numberOfScans"/> = 8.
        /// <para/>
        /// <paramref name="numberOfScans"/> shouldn't exceed 512.
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetBufferSize</c>.
        /// </remarks>
        /// <seealso cref="GetData"/>
        /// <seealso cref="SetSampleRate"/>
        public static void SetBufferSize(IntPtr hDevice, ushort numberOfScans)
        {
            if (!GT_SetBufferSize(hDevice, numberOfScans))
                HandleError();
        }

        /// <summary>
        /// Sets the sample frequency of the g.USBamp in Hz.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="sampleRate">The sample rate of the g.USBamp in Hz (see remarks section!).</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// The possible sample rates, their corresponding over sampling ratios and the recommended number of scans that should be used in <see cref="SetBufferSize"/> are listed int the table below:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Sampling Rate [Hz]</term>
        ///         <description>Over sampling rate [Hz] | Recommended number of scans</description>
        ///     </listheader>
        ///     <item>
        ///         <term>32</term>
        ///         <description>1200 | 1</description>
        ///     </item>
        ///     <item>
        ///         <term>64</term>
        ///         <description>600 | 2</description>
        ///     </item>
        ///     <item>
        ///         <term>128</term>
        ///         <description>300 | 4</description>
        ///     </item>
        ///     <item>
        ///         <term>256</term>
        ///         <description>150 | 8</description>
        ///     </item>
        ///     <item>
        ///         <term>512</term>
        ///         <description>75 | 16</description>
        ///     </item>
        ///     <item>
        ///         <term>600</term>
        ///         <description>64 | 32</description>
        ///     </item>
        ///     <item>
        ///         <term>1200</term>
        ///         <description>32 | 64</description>
        ///     </item>
        ///     <item>
        ///         <term>2400</term>
        ///         <description>16 | 128</description>
        ///     </item>
        ///     <item>
        ///         <term>4800</term>
        ///         <description>8 | 256</description>
        ///     </item>
        ///     <item>
        ///         <term>9600</term>
        ///         <description>4 | 512</description>
        ///     </item>
        ///     <item>
        ///         <term>19200</term>
        ///         <description>2 | 512</description>
        ///     </item>
        ///     <item>
        ///         <term>38400</term>
        ///         <description>1 | 512</description>
        ///     </item>
        /// </list>
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetSampleRate</c>.
        /// </remarks>
        /// <seealso cref="SetBufferSize"/>
        public static void SetSampleRate(IntPtr hDevice, ushort sampleRate)
        {
            if (!GT_SetSampleRate(hDevice, sampleRate))
                HandleError();
        }

        /// <summary>
        /// Starts the data acquisition.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// The sampling frequency, buffer configuration and channels must be set before.
        /// <para/>
        /// Data must be extracted permanently with <see cref="GetData"/> from the driver buffer to prevent a buffer overrun.
        /// <para/>
        /// Corresponding C-API method is <c>GT_Start</c>.
        /// </remarks>
        public static void Start(IntPtr hDevice)
        {
            if (!GT_Start(hDevice))
                HandleError();
        }

        /// <summary>
        /// Stops the data acquisition.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_Stop</c>.</remarks>
        public static void Stop(IntPtr hDevice)
        {
            if (!GT_Stop(hDevice))
                HandleError();
        }

        /// <summary>
        /// Defines the channels that should be recorded.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="channels">An array where each element contains the number of a channel that should be acquired.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// Valid numbers for channels are 1-16. Therefore the length of <paramref name="channels"/> must not exceed 16.
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetChannels</c>.
        /// </remarks>
        public static void SetChannels(IntPtr hDevice, byte[] channels)
        {
            if (!GT_SetChannels(hDevice, channels, (byte) channels.Length))
                HandleError();
        }

        /// <summary>
        /// Resets the driver data pipe after data transmission error (e.g. time out).
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_ResetTransfer</c>.</remarks>
        public static void ResetTransfer(IntPtr hDevice)
        {
            if (!GT_ResetTransfer(hDevice))
                HandleError();
        }

        /// <summary>
        /// Sets digital outputs for g.USBamp version 2.0
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="outputNumber">The number of the digital output to set (only values 1 and 2 are valid).</param>
        /// <param name="value">The value for the specified digital output to set.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp or if method is applied on g.USBamp version 3.0.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetDigitalOut</c>.</remarks>
        public static void SetDigitalOut(IntPtr hDevice, byte outputNumber, bool value)
        {
            if (!GT_SetDigitalOut(hDevice, outputNumber, Convert.ToByte(value)))
                HandleError();
        }

        /// <summary>
        /// Sets digital outputs for g.USBamp version 3.0
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="dout">A <see cref="DigitalOut"/> structure containing the values of the outputs.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp or if method is applied on g.USBamp version 2.0.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetDigitalOutEx</c>.</remarks>
        public static void SetDigitalOutEx(IntPtr hDevice, DigitalOut dout)
        {
            if (!GT_SetDigitalOutEx(hDevice, dout))
                HandleError();
        }

        /// <summary>
        /// Reads the state of the digital inputs and outputs of the g.USBamp version 2.0
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>A <see cref="DigitalIO"/> structure holding the state of the digital inputs and outputs.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp or if method is applied on g.USBamp version 3.0.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetDigitalIO</c>.</remarks>
        public static DigitalIO GetDigitalIO(IntPtr hDevice)
        {
            DigitalIO dio = new DigitalIO();

            if (!GT_GetDigitalIO(hDevice, ref dio))
                HandleError();

            return dio;
        }

        /// <summary>
        /// Reads the state of the digital outputs of the g.USBamp version 3.0
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>A <see cref="DigitalOut"/> structure holding the state of the digital outputs.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp or if method is applied on g.USBamp version 2.0.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetDigitalOut</c>.</remarks>
        public static DigitalOut GetDigitalOut(IntPtr hDevice)
        {
            DigitalOut dout = new DigitalOut();

            if (!GT_GetDigitalOut(hDevice, ref dout))
                HandleError();

            return dout;
        }

        /// <summary>
        /// Enables or disables the digital trigger line.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="enable"><b>true</b> enables the digital trigger line, <b>false</b> disables it.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// If enabled, the trigger lines are sampled synchronously with the analog channels' data rate. Therefore an additional <see cref="float"/> value is attached to the analog channels' values.
        /// There is a difference between g.USBamp version 3.0 and version 2.0: In version 2.0 there is just one trigger line so the values of the trigger channel can be 0 (LOW) and 250000 (HIGH). 
        /// In version 3.0 there are 8 trigger lines coded as <see cref="byte"/> (= uint8) on the trigger channel. If all inputs are HIGH the value of the channel is 255.0. If e.g. inputs 0 to 3 are HIGH the result is 15.0 and so on.
        /// <para/>
        /// Corresponding C-API method is <c>GT_EnableTriggerLine</c>.
        /// </remarks>
        public static void EnableTriggerLine(IntPtr hDevice, bool enable)
        {
            if (!GT_EnableTriggerLine(hDevice, enable))
                HandleError();
        }

        /// <summary>
        /// Returns the available bandpass filter settings.
        /// </summary>
        /// <returns>An array of <see cref="Filter"/> structures holding all available bandpass filters.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// The method calls <see cref="GetNumberOfFilter"/> first to determine the number of available filters.
        /// <para/>
        /// Corresponding C-API method is <c>GT_GetFilterSpec</c>.
        /// </remarks>
        public static Filter[] GetFilterSpec()
        {
            int numFilters = GetNumberOfFilter();
            Filter[] returnFilterSpecs = new Filter[numFilters];
            Filt[] filterSpecs = new Filt[numFilters];
            GCHandle hFilterSpecs = new GCHandle();

            //initialize filterSpecs
            for (int i = 0; i < filterSpecs.Length; i++)
                filterSpecs[i] = new Filt();

            try
            {
                //allocate pinned memory where the API function should write the returned data to
                hFilterSpecs = GCHandle.Alloc(filterSpecs, GCHandleType.Pinned);

                //retrieve data from device
                if (!GT_GetFilterSpec(hFilterSpecs.AddrOfPinnedObject()))
                    HandleError();

                //cast retrieved elements to the output structure
                for (int i = 0; i < numFilters; i++)
                {
                    //cast current element to output structure
                    returnFilterSpecs[i] = new Filter();
                    returnFilterSpecs[i].LowerBorderFrequency = filterSpecs[i].LowerBorderFrequency;
                    returnFilterSpecs[i].UpperBorderFrequency = filterSpecs[i].UpperBorderFrequency;
                    returnFilterSpecs[i].SampleRate = filterSpecs[i].SampleRate;
                    returnFilterSpecs[i].Type = (FilterTypes) Convert.ToByte(Math.Round(filterSpecs[i].Type));
                    returnFilterSpecs[i].Order = filterSpecs[i].Order;
                }

                return returnFilterSpecs;
            }
            finally
            {
                //free allocated memory
                hFilterSpecs.Free();
            }
        }

        /// <summary>
        /// Returns the total number of available filter settings.
        /// </summary>
        /// <returns>The total number of available filter settings.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetNumberOfFilter</c>.</remarks>
        public static int GetNumberOfFilter()
        {
            int numFilters = 0;

            if (!GT_GetNumberOfFilter(ref numFilters))
                HandleError();

            return numFilters;
        }

        /// <summary>
        /// Sets the digital bandpass filter coefficients for a specific channel.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="channel">The number of the channel for which the filter should be set (valid values are 1 to 16).</param>
        /// <param name="index">The index (ID) of the filter to apply to the specified channel. -1 if no filter should be applied.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// Use <see cref="GetFilterSpec"/> to get the filter matrix <paramref name="index"/>.
        /// <para/>
        /// Note: There is a hardware anti-aliasing filter.
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetBandPass</c>.
        /// </remarks>
        public static void SetBandpass(IntPtr hDevice, byte channel, int index)
        {
            if (!GT_SetBandPass(hDevice, channel, index))
                HandleError();
        }

        /// <summary>
        /// Returns the available notch filter settings.
        /// </summary>
        /// <returns>An array of <see cref="Filt"/> structures holding all available notch filters.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// The method calls <see cref="GetNumberOfNotch"/> first to determine the number of available notch filters.
        /// <para/>
        /// Corresponding C-API method is <c>GT_GetNotchSpec</c>.
        /// </remarks>
        public static Filter[] GetNotchSpec()
        {
            int numNotches = GetNumberOfNotch();
            Filter[] returnNotchSpecs = new Filter[numNotches];
            Filt[] notchSpecs = new Filt[numNotches];
            GCHandle hNotchSpecs = new GCHandle();

            //initialize notchSpecs
            for (int i = 0; i < notchSpecs.Length; i++)
                notchSpecs[i] = new Filt();

            try
            {
                //allocate pinned memory where the API function should write the returned data to
                hNotchSpecs = GCHandle.Alloc(notchSpecs, GCHandleType.Pinned);

                //retrieve data from device
                if (!GT_GetNotchSpec(hNotchSpecs.AddrOfPinnedObject()))
                    HandleError();

                //cast retrieved elements to the output structure
                for (int i = 0; i < numNotches; i++)
                {
                    returnNotchSpecs[i] = new Filter();
                    returnNotchSpecs[i].LowerBorderFrequency = notchSpecs[i].LowerBorderFrequency;
                    returnNotchSpecs[i].UpperBorderFrequency = notchSpecs[i].UpperBorderFrequency;
                    returnNotchSpecs[i].SampleRate = notchSpecs[i].SampleRate;
                    returnNotchSpecs[i].Type = (FilterTypes) Convert.ToByte(Math.Round(notchSpecs[i].Type));
                    returnNotchSpecs[i].Order = notchSpecs[i].Order;
                }

                return returnNotchSpecs;
            }
            finally
            {
                //free allocated memory
                hNotchSpecs.Free();
            }
        }

        /// <summary>
        /// Returns the total number of available notch filter settings.
        /// </summary>
        /// <returns>The total number of available notch filter settings.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetNumberOfNotch</c>.</remarks>
        public static int GetNumberOfNotch()
        {
            int numNotches = 0;

            if (!GT_GetNumberOfNotch(ref numNotches))
                HandleError();

            return numNotches;
        }

        /// <summary>
        /// Sets the digital notch filter coefficients for a specific channel.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="channel">The number of the channel for which the filter should be set (valid values are 1 to 16).</param>
        /// <param name="index">The index (ID) of the filter to apply to the specified channel. -1 if no filter should be applied.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// Use <see cref="GetNotchSpec"/> to get the filter matrix <paramref name="index"/>.
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetNotch</c>.
        /// </remarks>
        public static void SetNotch(IntPtr hDevice, byte channel, int index)
        {
            if (!GT_SetNotch(hDevice, channel, index))
                HandleError();
        }

        /// <summary>
        /// Sets the operation mode of the device.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="mode">The operation mode of the device.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetMode</c>.</remarks>
        public static void SetMode(IntPtr hDevice, OperationModes mode)
        {
            if (!GT_SetMode(hDevice, mode))
                HandleError();
        }

        /// <summary>
        /// Returns the operation mode of the device.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>The operation mode of the device.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetMode</c>.</remarks>
        public static OperationModes GetMode(IntPtr hDevice)
        {
            OperationModes mode = OperationModes.Normal;

            if (!GT_GetMode(hDevice, ref mode))
                HandleError();

            return mode;
        }

        /// <summary>
        /// Connects or disconnects the grounds of the four groups A, B, C and D.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="commonGround">A <see cref="Gnd"/> structure defining the common ground connection of the four groups.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetGround</c>.</remarks>
        public static void SetGround(IntPtr hDevice, Gnd commonGround)
        {
            if (!GT_SetGround(hDevice, commonGround))
                HandleError();
        }

        /// <summary>
        /// Returns a <see cref="Gnd"/> structure holding the state of the ground connections of the four groups A, B, C and D.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>A <see cref="Gnd"/> structure holding the state of the ground connections of the four groups A, B, C and D.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetGround</c>.</remarks>
        public static Gnd GetGround(IntPtr hDevice)
        {
            Gnd commonGround = new Gnd();

            if (!GT_GetGround(hDevice, ref commonGround))
                HandleError();

            return commonGround;
        }

        /// <summary>
        /// Connects or disconnects the references of the four groups A, B, C and D.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="commonReference">A <see cref="Ref"/> structure defining the common reference connection of the four groups.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetReference</c>.</remarks>
        public static void SetReference(IntPtr hDevice, Ref commonReference)
        {
            if (!GT_SetReference(hDevice, commonReference))
                HandleError();
        }

        /// <summary>
        /// Returns a <see cref="Ref"/> structure holding the state of the reference connections of the four groups A, B, C and D.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>A <see cref="Ref"/> structure holding the state of the reference connections of the four groups A, B, C and D.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetReference</c>.</remarks>
        public static Ref GetReference(IntPtr hDevice)
        {
            Ref commonReference = new Ref();

            if (!GT_GetReference(hDevice, ref commonReference))
                HandleError();

            return commonReference;
        }

        /// <summary>
        /// Defines the channels for bipolar derivation.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="bipoChannels">A <see cref="Bipolar"/> structure defining the channels for bipolar derivation.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetBipolar</c>.</remarks>
        public static void SetBipolar(IntPtr hDevice, Bipolar bipoChannels)
        {
            if (!GT_SetBipolar(hDevice, bipoChannels))
                HandleError();
        }

        /// <summary>
        /// Defines the channels for DRL (driven right leg) calculation.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="drlChannels">A <see cref="Channel"/> structure defining the channels used for calculating DRL.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetDRLChannel</c>.</remarks>
        public static void SetDRLChannel(IntPtr hDevice, Channel drlChannels)
        {
            if (!GT_SetDRLChannel(hDevice, drlChannels))
                HandleError();
        }

        /// <summary>
        /// Enables or disables the short cut function. If short cut is enabled a HIGH level on the SC input socket of the amplifier disconnects the electrodes from the amplifier input stage.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="enable"><b>true</b> enables the short cut function; <b>false</b> disables it.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_EnableSC</c>.</remarks>
        /// <seealso cref="SetDAC"/>
        public static void EnableSC(IntPtr hDevice, bool enable)
        {
            if (!GT_EnableSC(hDevice, enable))
                HandleError();
        }

        /// <summary>
        /// Set the amplifier to slave/master mode.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="isSlave"><b>true</b> to set specified device to slave mode; <b>false</b> to set it to master mode.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// To synchronize multiple g.USBamps perform the following steps in your application:
        /// <list type="number">
        ///     <item><description>There must be only one device configured as master. The others must be configured as slave devices.</description></item>
        ///     <item><description>The sampling rate has to be the same for all amplifiers.</description></item>
        ///     <item><description>Call <see cref="Start"/> for the slave devices first. The master device has to be started at last.</description></item>
        ///     <item><description>During acquisition call <see cref="GetData"/> for all devices before invoking <c>WaitForSingleObject()</c> (see <see cref="GetData"/>).</description></item>
        ///     <item><description>To stop the acquisition call <see cref="Stop"/> for all slave devices first. The master device has to be stopped at last.</description></item>
        /// </list>
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetSlave</c>.
        /// </remarks>
        public static void SetSlave(IntPtr hDevice, bool isSlave)
        {
            if (!GT_SetSlave(hDevice, isSlave))
                HandleError();
        }

        /// <summary>
        /// Defines the calibration/DRL settings.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="analogOut">A <see cref="DAC"/> structure defining the calibration/DRL settings.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_SetDAC</c>.</remarks>
        public static void SetDAC(IntPtr hDevice, DAC analogOut)
        {
            if (!GT_SetDAC(hDevice, analogOut))
                HandleError();
        }

        /// <summary>
        /// Sets the scaling factor and offset values for all channels.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="scaling">A <see cref="Scale"/> structure defining the scaling factor and offset values for all channels.</param>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// Values are stored in permanent memory.
        /// <para/>
        /// Calculation: y = (x-d)*k
        /// <br/>
        /// y...values retrieved with <see cref="GetData"/> (calculated values) in microvolts (µV)
        /// x...acquired data
        /// d...offset value in microvolts (µV)
        /// k...scaling factor
        /// <para/>
        /// Corresponding C-API method is <c>GT_SetScale</c>.
        /// </remarks>
        public static void SetScale(IntPtr hDevice, Scale scaling)
        {
            if (!GT_SetScale(hDevice, scaling))
                HandleError();
        }

        /// <summary>
        /// Returns a <see cref="Scale"/> structure holding the scaling factor and offset values for all channels.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>A <see cref="Scale"/> structure holding the scaling factor and offset values for all channels.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetScale</c>.</remarks>
        /// <seealso cref="SetScale"/>
        public static Scale GetScale(IntPtr hDevice)
        {
            Scale scaling = new Scale();

            if (!GT_GetScale(hDevice, ref scaling))
                HandleError();

            return scaling;
        }

        /// <summary>
        /// Calculates scaling factor and offset values for all channels.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>A <see cref="Scale"/> structure holding the scaling factor and offset values for all channels.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// The function blocks for about 4 seconds.
        /// <para/>
        /// When calling this function, scaling and offset values in permanent memory of the amplifier will be reset to 1 and 0, respectively.
        /// To write the new values received by this method to the storage use <see cref="SetScale"/>.
        /// To verify stored values use <see cref="GetScale"/>.
        /// <para/>
        /// Calling this function modifies the g.USBamp configuration. You need to configure the device after this call to meet your requirements.
        /// <para/>
        /// Corresponding C-API method is <c>GT_Calibrate</c>.
        /// </remarks>
        /// <seealso cref="SetScale"/>
        /// <seealso cref="GetScale"/>
        public static Scale Calibrate(IntPtr hDevice)
        {
            Scale scaling = new Scale();

            if (!GT_Calibrate(hDevice, ref scaling))
                HandleError();

            return scaling;
        }

        /// <summary>
        /// Gets the error code and error message of the last occured error.
        /// </summary>
        /// <param name="errorCode">The error code of the occured error.</param>
        /// <param name="errorMessage">The corresponding error message.</param>
        /// <returns><b>true</b> if the call succeeded; <b>false</b> otherwise.</returns>
        /// <remarks>Corresponding C-API method is <c>GT_GetLastError</c>.</remarks>
        public static bool GetLastError(ref ushort errorCode, ref string errorMessage)
        {
            byte[] errorString = new byte[256];
            GCHandle hErrorString = GCHandle.Alloc(errorString, GCHandleType.Pinned);

            try
            {
                if (!GT_GetLastError(ref errorCode, hErrorString.AddrOfPinnedObject()))
                    return false;

                errorMessage = Encoding.ASCII.GetString(errorString).Trim();

                return true;
            }
            finally
            {
                //free allocated memory
                hErrorString.Free();
            }
        }

        /// <summary>
        /// Returns the g.USBamp driver version.
        /// </summary>
        /// <returns>The g.USBamp driver version.</returns>
        /// <remarks>Corresponding C-API method is <c>GT_GetDriverVersion</c>.</remarks>
        public static float GetDriverVersion()
        {
            return GT_GetDriverVersion();
        }

        /// <summary>
        /// Returns the hardware version of the device.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>The hardware version of the device (2.0 or 3.0).</returns>
        /// <remarks>Corresponding C-API method is <c>GT_GetHWVersion</c>.</remarks>
        public static float GetHWVersion(IntPtr hDevice)
        {
            return GT_GetHWVersion(hDevice);
        }

        /// <summary>
        /// Reads the serial number of the device.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <returns>The serial number of the device.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>Corresponding C-API method is <c>GT_GetSerial</c>.</remarks>
        public static string GetSerial(IntPtr hDevice)
        {
            uint serialSize = 16;
            byte[] serialNumber = new byte[serialSize];
            GCHandle hSerialNumber = GCHandle.Alloc(serialNumber, GCHandleType.Pinned);

            try
            {
                if (!GT_GetSerial(hDevice, hSerialNumber.AddrOfPinnedObject(), serialSize))
                    HandleError();

                return Encoding.ASCII.GetString(serialNumber).Trim('\0', ' ');
            }
            finally
            {
                hSerialNumber.Free();
            }
        }

        /// <summary>
        /// Measures and returns the electrode impedance for the specified channel.
        /// </summary>
        /// <param name="hDevice">The handle to the device (see <see cref="OpenDevice(int)"/>).</param>
        /// <param name="channel">The number of the channel whose impedance should be measured (valid values are 1 to 20; see the remarks section for details!).</param>
        /// <returns>The measured impedance for the specified channel.</returns>
        /// <exception cref="DeviceException">Will be thrown, if an error occurred while using C-API of the g.USBamp.</exception>
        /// <remarks>
        /// All grounds are connected to common ground. Impedance is measured between ground and the specified channel. If you want to get the impedance of the group's reference electrodes (Ref) you must enter the following channel number:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Channel number</term>
        ///         <description>Electrode</description>
        ///     </listheader>
        ///     <item>
        ///         <term>1..16</term>
        ///         <description>1..16</description>
        ///     </item>
        ///     <item>
        ///         <term>17</term>
        ///         <description>Ref A</description>
        ///     </item>
        ///     <item>
        ///         <term>18</term>
        ///         <description>Ref B</description>
        ///     </item>
        ///     <item>
        ///         <term>19</term>
        ///         <description>Ref C</description>
        ///     </item>
        ///     <item>
        ///         <term>20</term>
        ///         <description>Ref D</description>
        ///     </item>
        /// </list>
        /// <para/>
        /// Corresponding C-API method is <c>GT_GetImpedance</c>.
        /// </remarks>
        public static double GetImpedance(IntPtr hDevice, byte channel)
        {
            double impedance = 0;

            if (!GT_GetImpedance(hDevice, channel, ref impedance))
                HandleError();

            return impedance;
        }

        #endregion

        #region Error Handling...

        /// <summary>
        /// Called, if an error occured. Throws exception with details on occurred error.
        /// </summary>
        /// <exception cref="DeviceException">Will be thrown if an error occurred while using C-API of g.USBamp.</exception>
        /// <remarks>If <see cref="GetLastError"/> returns <b>false</b>, the thrown <see cref="DeviceException"/> has value 0 for <see cref="DeviceException.ErrorCode"/> and the string <i>Unknown Error</i> in its message.</remarks>
        private static void HandleError()
        {
            ushort errorCode = 0;
            string errorMessage = String.Empty;

            //try to retrieve error message
            if (!GetLastError(ref errorCode, ref errorMessage))
                throw new DeviceException(0, "Unknown error");

            //if error message could be retrieved, throw exception with error details
            throw new DeviceException(errorCode, errorMessage.Trim('\0'));
        }

        #endregion
    }
}
