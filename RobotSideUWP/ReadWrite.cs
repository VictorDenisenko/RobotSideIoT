using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Core;

namespace RobotSideUWP
{
    public class ReadWrite
    {
        public SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        DispatcherTimer sendAfterDelayTimer;
        DateTime timeNow;
        DateTime timeSent;
        long ticksSent = 0;
        string _dataToWrite = "";
        

        public ReadWrite()
        {
            comPortInit();
            sendAfterDelayTimer = new DispatcherTimer();
            sendAfterDelayTimer.Interval = new TimeSpan(0, 0, 0, 0, 100); //Таймер для прореживания данных перед посылкой в порт (дни, часы, мин, сек, ms)
            sendAfterDelayTimer.Tick += SendAfterDelayTimer_Tick;
        }

        private void SendAfterDelayTimer_Tick(object sender, object e)
        {
            WriteNested(_dataToWrite);
            sendAfterDelayTimer.Stop();
            timeSent = DateTime.Now;
            ticksSent = timeNow.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов
        }

        public async void comPortInit()
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
                dataReaderObject = new DataReader(serialPort.InputStream);
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("comPortInit() " + ex.Message, NotifyType.ErrorMessage);
            }
        }

        public void Write(string dataToWrite)
        {
            try {
                timeNow = DateTime.Now;
                var ticksNow = timeNow.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов

                long deltaTicks = (ticksNow - ticksSent) / 10000;

                if (deltaTicks < 30) {
                    if (dataToWrite == "Stop") {
                        var _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            sendAfterDelayTimer.Start();
                        });
                        MainPage.Current.NotifyUserFromOtherThread("Write() " + "deltaTicks < 30 мкс", NotifyType.ErrorMessage);
                    }
                } else {
                    WriteNested(dataToWrite);
                    timeSent = DateTime.Now;
                    ticksSent = timeNow.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов
                }
            }
            catch (Exception e) {
                MainPage.Current.NotifyUserFromOtherThread("Write() " + "deltaTicks < 30 мкс", NotifyType.ErrorMessage);
            }
        }

        public async void WriteNested(string dataToWrite)
        {
            CommonStruct.permissionToSend = false;
            _dataToWrite = dataToWrite;
            try {
                if (serialPort != null) {
                    Task<UInt32> storeAsyncTask;
                    dataWriteObject.WriteString(dataToWrite);
                    storeAsyncTask = dataWriteObject.StoreAsync().AsTask();
                    UInt32 bytesWritten = await storeAsyncTask;
                    if (bytesWritten > 0) {
                        await ReadAsync();
                        CommonStruct.permissionToSend = true;
                    }
                } else {
                    MainPage.Current.NotifyUserFromOtherThread("Connection to commport error ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex) {
                MainPage.Current.NotifyUserFromOtherThread("WriteNested() " + ex.Message, NotifyType.ErrorMessage);
            }
        }

        string testString = "";
        private async Task ReadAsync()
        {
            var receivedStrings = "";
            var receivedSimbol = "";
            Task<UInt32> loadAsyncTask;
            uint ReadBufferLength = 1;
            UInt32 bytesRead = 0;
            int slashIndex = 0;
            int k = 0;//Счетчик предельно допустимого количества символов - предохранение от зависания в цикле
            try
            {
                dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
                while ((receivedSimbol != "\r") && (k < 35))
                {
                    loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask();
                    bytesRead = await loadAsyncTask;
                    receivedSimbol = dataReaderObject.ReadString(1);
                    receivedStrings += receivedSimbol;
                    k++;
                    //MainPage.Current.NotifyUserFromOtherThread(receivedStrings + "  ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("ReadAsync() " + ex.Message, NotifyType.ErrorMessage);
            }
            try
            {
                var _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    slashIndex = receivedStrings.IndexOf("\r", 0);
                    if (slashIndex != 5) CommonStruct.readData = receivedStrings;

                    if (receivedStrings.Length > 10) {
                        string batteryVoltage = PlcControl.BatteryVoltageHandling(receivedStrings);
                        await MainPage.SendVoltageLevelToServer(batteryVoltage + "%");
                        CommonStruct.voltageLevelFromRobot = batteryVoltage;
                    }

                    testString = testString + "   " + receivedStrings;
                    MainPage.Current.NotifyUserForTesting(testString); 
                });
                //MainPage.Current.NotifyUserFromOtherThreadForTesting(testString, NotifyType.ErrorMessage);
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("ReadAsync() " + ex.Message, NotifyType.ErrorMessage);
            }
           
        }

    }
}