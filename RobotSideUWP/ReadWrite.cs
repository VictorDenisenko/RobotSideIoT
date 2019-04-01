using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;

namespace RobotSideUWP
{
    public class ReadWrite
    {
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        public ReadWrite()
        {
            comPortInit();
        }

        private async void comPortInit()
        {
            string aqs = SerialDevice.GetDeviceSelector("UART0");
            var dis = await DeviceInformation.FindAllAsync(aqs);
            try
            {
                serialPort = await SerialDevice.FromIdAsync(dis[0].Id);
                if (serialPort == null) return;
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;
                dataWriteObject = new DataWriter(serialPort.OutputStream);
                Listen();
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("comPortInit() ", NotifyType.ErrorMessage);
            }
        }

        public void Write(string dataToWrite)
        {
            try
            {
                if (serialPort != null)
                {
                    //await WriteAsync(dataToWrite);
                    dataWriteObject.WriteString(dataToWrite);
                    dataWriteObject.StoreAsync();
                }
                else
                {
                    MainPage.Current.NotifyUserFromOtherThread("Connection to commport error ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("Write() " + ex.Message, NotifyType.StatusMessage);
            }
        }

        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);
                    while (true)
                    {
                        await ReadAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("Listen() " + ex.Message, NotifyType.StatusMessage);
            }
            finally
            {
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private async Task ReadAsync()
        {
            Task<UInt32> loadAsyncTask;
            uint ReadBufferLength = 64;
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask();
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                string receivedData = dataReaderObject.ReadString(bytesRead);
            }
        }

    }
}