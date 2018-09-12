using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lmi3d.GoSdk;
using Lmi3d.GoSdk.Messages;
using Lmi3d.Zen;
using Lmi3d.Zen.Io;

namespace AcquireInterfaceLibrary.LMI_Gocator
{
    /// <summary>
    /// Using SDK version 5.1.6.9 x64
    /// </summary>
    class Gocator : IDevice
    {
        #region private field
        private GoSystem _system;
        private GoSensor _sensor;
        private List<KObject> rawDataCollection = new List<KObject>();
        #endregion


        #region backgroudworker
        BackgroundWorker dataHandlerWorker = new BackgroundWorker();
        #endregion

        #region prop
        public string IPAddress { get; set; }
        public string DeviceID { get; set; }
        public int BufferSize { get; set; }
        public List<ushort[]> PointCloudBuffer { get; set; } = new List<ushort[]>();
        #endregion

        #region ctor
        public Gocator(string ipAddr = "127.0.0.1", int bufferSize = 1, bool debugMode = false)
        {
            IPAddress = ipAddr;
            BufferSize = bufferSize;
            dataHandlerWorker.DoWork += DataHandlerWorker_DoWork;
            dataHandlerWorker.RunWorkerCompleted += DataHandlerWorker_RunWorkerCompleted;


        }

        #endregion

        #region finished event
        public event EventHandler<object> DataResolveFinishedEvent;
        #endregion

        #region     backworker
        private void DataHandlerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DataResolveFinishedEvent?.Invoke(this, null);
        }

        private void DataHandlerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Parallel.ForEach<KObject>(rawDataCollection, RawDataResolver);
        }

        #endregion


        #region data process
        private void RawDataResolver(KObject data)
        {
            GoDataSet dataSet = (GoDataSet)data;
            for (UInt32 i = 0; i < dataSet.Count; i++)
            {
                GoDataMsg dataObj = (GoDataMsg)dataSet.Get(i);
                switch (dataObj.MessageType)
                {
                    case GoDataMessageType.UniformSurface:
                        {
                            GoUniformSurfaceMsg surfaceMsg = (GoUniformSurfaceMsg)dataObj;
                            double xResolution = (double)surfaceMsg.XResolution / 1000000;
                            double zResolution = (double)surfaceMsg.ZResolution / 1000000;
                            double xOffset = (double)surfaceMsg.XOffset / 1000;
                            double zOffset = (double)surfaceMsg.ZOffset / 1000;
                            long width = surfaceMsg.Width;
                            long height = surfaceMsg.Length;
                            long bufferSize = width * height;
                            IntPtr bufferPointer = surfaceMsg.Data;
                            short[] ranges = new short[bufferSize];
                            Marshal.Copy(bufferPointer, ranges, 0, ranges.Length);
                        }
                        break;
                    case GoDataMessageType.SurfaceIntensity:
                        {
                            GoSurfaceIntensityMsg surfaceMsg = (GoSurfaceIntensityMsg)dataObj;
                            long width = surfaceMsg.Width;
                            long height = surfaceMsg.Length;
                            long bufferSize = width * height;
                            IntPtr bufferPointeri = surfaceMsg.Data;
                            byte[] ranges = new byte[bufferSize];
                            Marshal.Copy(bufferPointeri, ranges, 0, ranges.Length);
                        }
                        break;
                }
            }
        }

        #endregion

        #region Implement IDevice
        /// <summary>
        /// initial api constructor and open data channel
        /// </summary>
        /// <returns></returns>
        public bool InitialDevice()
        {
            try
            {
                KApiLib.Construct();
                GoSdkLib.Construct();
            }
            catch (KException kEx)
            {

                Trace.WriteLine(kEx.ToString());
            }

            _system = new GoSystem();
            KIpAddress kIpAddress = KIpAddress.Parse(IPAddress);
            Trace.WriteLine($"Sonsor IP {IPAddress}");
            _sensor = _system.FindSensorByIpAddress(kIpAddress);
            if (_sensor.State == GoState.Ready )
            {
                _sensor.Connect();
                Trace.WriteLine("Sonsor Connected");
            }
            else
            {
                return false;
            }
            _system.EnableData(true);
            Trace.WriteLine("Data channel open");

            _system.SetDataHandler(onData);
            if(_sensor.State == GoState.Running)
            {
                _system.Stop();
                _system.Start();
            }
            else
            {
                _system.Start();
                Trace.WriteLine("Sysem Start");
            }


            return true;
        }

        /// <summary>
        /// receive data
        /// </summary>
        /// <param name="data"></param>
        private void onData(KObject data)
        {
            rawDataCollection.Add(data);
            if (rawDataCollection.Count == BufferSize)
            {
                dataHandlerWorker.RunWorkerAsync();
            }
        }

        public bool ReleaseDevice()
        {
            throw new NotImplementedException();
        }

        public bool StartAcquire()
        {
            throw new NotImplementedException();
        }

        public bool StopAcquire()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
