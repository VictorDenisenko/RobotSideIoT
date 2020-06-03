using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace RobotSideUWP
{
    public class ReadWrite
    {
        public SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        DispatcherTimer sendAfterDelayTimer;
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
            ticksSent = DateTime.Now.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов
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
                MainPage.Current.NotifyUserFromOtherThreadAsync("comPortInit() " + ex.Message, NotifyType.ErrorMessage);
            }
        }

        public void Write(string dataToWrite)
        {
            _dataToWrite = dataToWrite;
            try {
                var ticksNow = DateTime.Now.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов

                long deltaTicks = (ticksNow - ticksSent) / 10000;

                if (deltaTicks < 30) {
                    if ((dataToWrite == "^RC0002\r")||(dataToWrite == "^RS00031\r")) {
                        var _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            sendAfterDelayTimer.Start();
                        });
                        MainPage.Current.NotifyUserFromOtherThreadAsync("Write() " + dataToWrite + ", deltaTicks < 30 мкс", NotifyType.ErrorMessage);
                    }
                } else {
                    WriteNested(dataToWrite);
                }
                ticksSent = DateTime.Now.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов
            }
            catch (Exception e) {
                MainPage.Current.NotifyUserFromOtherThreadAsync("Write() " + "deltaTicks < 30 мкс", NotifyType.ErrorMessage);
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
                    MainPage.Current.NotifyUserFromOtherThreadAsync("Connection to commport error ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex) {
                MainPage.Current.NotifyUserFromOtherThreadAsync("WriteNested() " + ex.Message, NotifyType.ErrorMessage);
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
                    if (loadAsyncTask != null)
                    {
                        bytesRead = await loadAsyncTask;
                        receivedSimbol = dataReaderObject.ReadString(1);
                        receivedStrings += receivedSimbol;
                        k++;
                    }
                    else
                    {
                        return;
                    }
                    //MainPage.Current.NotifyUserFromOtherThread(receivedStrings + "  ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("ReadAsync() " + ex.Message, NotifyType.ErrorMessage);
                return;
            }
            try
            {
                var _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    slashIndex = receivedStrings.IndexOf("\r", 0);
                    if (slashIndex != 5) CommonStruct.readData = receivedStrings;

                    if (receivedStrings.Length > 10) {
                        string batteryVoltage = PlcControl.BatteryVoltageHandling(receivedStrings);

                        int res = 0;
                        bool isInt = Int32.TryParse(batteryVoltage, out res);
                        double dBatteryVoltage = 0.0;
                        if (isInt == true)
                        {
                            dBatteryVoltage = Convert.ToDouble(batteryVoltage);
                        }

                        if ((batteryVoltage != "") && (CommonStruct.permissionToSendToWebServer == true)) {
                            if ((batteryVoltage == "Charging...") && (CommonStruct.permissionToSendToWebServer == true))
                            {
                                CommonStruct.voltageLevelFromRobot = batteryVoltage;
                                MainPage.Current.SendCommentsToServer(batteryVoltage);
                            }
                            else if((dBatteryVoltage <=100) && (dBatteryVoltage >=1) && (CommonStruct.permissionToSendToWebServer == true))
                            {
                                CommonStruct.voltageLevelFromRobot = batteryVoltage;
                                MainPage.Current.SendCommentsToServer(batteryVoltage + "%");
                            }
                            else
                            {
                                CommonStruct.voltageLevelFromRobot = "0";
                                MainPage.Current.SendCommentsToServer("0%");
                            }
                        }
                    }
                    testString = testString + "   " + receivedStrings;
                    MainPage.Current.NotifyUserForTesting(testString);
                    if (testString.Length > 400) testString = "";
                });
                MainPage.Current.NotifyUserFromOtherThreadAsync("", NotifyType.ErrorMessage);
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("ReadAsync() " + ex.Message, NotifyType.ErrorMessage);
            }
        }
    }
}