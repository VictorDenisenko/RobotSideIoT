using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace RobotSideUWP
{
    public class ReadWrite
    {
        static ReadWriteClass rwc;

        public ReadWrite()
        {
            rwc = new ReadWriteClass();
            rwc.Open();
        }

        public static void Write(string dataToWrite)
        {
            ReadWriteClass rwc = new ReadWriteClass();
            rwc.Write(dataToWrite);
        }

        public static string Read()
        {
            ReadWriteClass rwc = new ReadWriteClass();
            Task.Delay(20).Wait();
            string readData = rwc.ReadLine();
            return readData;
        }
    }

    public class ReadWriteClass
    {
        static SerialDevice device = null;
        DataReader dataReaderObject = null;
        DataWriter dataWriteObject = null;
        string fullDataFromPort = "";
        string testVariable1 = "";

        static MainPage rootPage = MainPage.Current;

        public ReadWriteClass()
        {
        }

        public void Open()
        {
            bool openSuccess = false;
            try
            {
                openSuccess = OpenDeviceAsync();
            }
            catch (Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("Open() " + e.Message.ToString(), NotifyType.ErrorMessage);
            }
            if (openSuccess == true)
            {
                PortInit();
            }
        }

        public static bool OpenDeviceAsync()
        {
            DeviceInformation entry = rootPage.choosenDevice;
            if (device != null)
            {
                device.Dispose();
            }
            Task t = Task.Run(async () => { device = await SerialDevice.FromIdAsync(entry.Id); });
            t.Wait();
            //SerialDevice device1 = await SerialDevice.FromIdAsync(entry.Id);
            bool successfullyOpenedDevice = false;
            if (device != null) { successfullyOpenedDevice = true; }
            else { successfullyOpenedDevice = false; }
            return successfullyOpenedDevice;
        }

        private void PortInit()
        {
            if (device == null)
            {
                MainPage.Current.NotifyUserFromOtherThread("Device is not connected", NotifyType.ErrorMessage);
            }
            else
            {
                device.BaudRate = 9600;
                device.Parity = SerialParity.None;
                device.DataBits = 8;
                device.StopBits = SerialStopBitCount.One;
                device.Handshake = SerialHandshake.None;
                device.BreakSignalState = false;
                int ReadTimeoutInput = 4000;
                device.ReadTimeout = new System.TimeSpan(ReadTimeoutInput * 10000);//Одна миллисекунда = 10 тыс. тиков
                int WriteTimeoutInput = 1000;
                device.WriteTimeout = new System.TimeSpan(WriteTimeoutInput * 10000);//Одна миллисекунда = 10 тыс. тиков
                MainPage.Current.NotifyUserFromOtherThread("Connected to " + device.PortName, NotifyType.StatusMessage);
            }
        }

        public async void Write(string dataToWrite)
        {
            if (device != null)
            {
                try
                {
                    dataWriteObject = new DataWriter(device.OutputStream);
                    await WriteAsync(dataToWrite);
                }
                catch (Exception exception)
                {
                    MainPage.Current.NotifyUserFromOtherThread(exception.Message.ToString(), NotifyType.ErrorMessage);
                }
                finally
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        private async Task WriteAsync(string dataToWrite)
        {
            CommonStruct.thereAreNoIONow = false;
            Task<UInt32> storeAsyncTask;
            Object WriteCancelLock = new Object();
            if ((dataToWrite.Length != 0))
            {
                dataWriteObject.WriteString(dataToWrite);
                lock (WriteCancelLock)
                {
                    storeAsyncTask = dataWriteObject.StoreAsync().AsTask();
                }
                UInt32 bytesWritten = await storeAsyncTask;
            }
            else
            {
                rootPage.NotifyUserFromOtherThread("No input received to write", NotifyType.StatusMessage);
            }
            CommonStruct.thereAreNoIONow = true;
        }

        public string ReadLine()
        {
            string oneChar = "";
            string readData = "";
            for (int i = 0; i < 35; i++)
            {
                oneChar = Read(1);
                if (oneChar != "\r")
                {
                    readData += oneChar;
                    fullDataFromPort = readData;
                }
                else
                {
                    i = 35;
                }
            }
            return readData;
        }

        public string Read(uint ReadBufferLength)
        {
            CommonStruct.thereAreNoIONow = false;
            string readData = "";
            if (device != null)
            {
                try
                {
                    if (dataReaderObject == null)
                    {
                        dataReaderObject = new DataReader(device.InputStream);
                    }
                    readData = ReadAsync(ReadBufferLength);
                }
                catch (Exception exception)
                {
                    MainPage.Current.NotifyUserFromOtherThread(exception.Message.ToString(), NotifyType.ErrorMessage);
                }
                finally
                {
                    if (CommonStruct.portOpen == true)
                    {
                        dataReaderObject.DetachStream();
                        dataReaderObject = null;
                    }
                }
            }
            else
            {

            }
            CommonStruct.thereAreNoIONow = true;
            return readData;
        }

        private string ReadAsync(uint ReadBufferLength)
        {
            string outputData = "";
            try
            {

                UInt32 bytesRead = 0;
                Task<UInt32> loadAsyncTask;
                Object ReadCancelLock = new Object();

                lock (ReadCancelLock)
                {
                    //dataReaderObject.InputStreamOptions = InputStreamOptions.ReadAhead;
                    //dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
                    dataReaderObject.InputStreamOptions = InputStreamOptions.None;
                    loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask();
                    //string x = dataReaderObject.ReadString(loadAsyncTask);
                }
                Task t = Task.Run(async () => { bytesRead = await loadAsyncTask; });
                bool b = t.Wait(1000);
                if (bytesRead > 0)
                {
                    outputData = dataReaderObject.ReadString(bytesRead);
                    testVariable1 = outputData;
                    CommonStruct.portOpen = true;
                }
                else
                {
                    CommonStruct.portOpen = false;
                    MainPage.Current.NotifyUserFromOtherThread("ReadAsync() bytesRead<0: COM-порт не отвечает.", NotifyType.StatusMessage);
                }
                CommonStruct.thereAreNoIONow = true;
                return outputData;
            }
            catch (Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("ReadAsync(): COM-порт: " + fullDataFromPort + " " + testVariable1 + " " + e.Message, NotifyType.StatusMessage);
                CommonStruct.thereAreNoIONow = true;
                return "";
            }
        }
    }
}