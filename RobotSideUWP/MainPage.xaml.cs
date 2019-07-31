using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Threading;

namespace RobotSideUWP
{
    public partial class MainPage : Page
    {
        List<Scenario> scenarios = new List<Scenario>
        {
            new Scenario() { ClassType=typeof(WiFiConnect_Scenario)}
        };

        private void buttonWiFi_Click(object sender, RoutedEventArgs e)
        {
            if (!PopupWiFi.IsOpen) { PopupWiFi.IsOpen = true; }
        }

        private void buttonWiFiFormClose_Click(object sender, RoutedEventArgs e)
        {
            if (PopupWiFi.IsOpen) { PopupWiFi.IsOpen = false; }
        }

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Current.NotifyUser("НИЛ АП, http://www.boteyes.ru ", NotifyType.ErrorMessage);
        }

        private void RD31Button_Checked(object sender, RoutedEventArgs e)
        {
            if (RD31Button.IsChecked == true) { CommonStruct.cameraController = "RD31"; }
            else if (GM51Button.IsChecked == true) { CommonStruct.cameraController = "GM51"; }
            else if (NoButton.IsChecked == true) { CommonStruct.cameraController = "No"; }
            if(plcControl != null) plcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
        }

        private void buttonVoltageCalibrate_Click(object sender, RoutedEventArgs e)
        {
            //TextBoxRealVoltage_TextChanged(null, null);
            MainPage.Current.ChargeLevelMeasure();
        }
    }

    public class Scenario
    {
        public Type ClassType { get; set; }
    }

    public sealed partial class MainPage : Page
    {

        public static ReadWrite readWrite = null;
        PlcControl plcControl = null;
        public static MainPage Current;
        bool bConnect = true;
        private string forwardDirection = "0";
        private string backwardDirection = "1";
        string[] dataFromRobot = new string[16] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" }; //данные из робота  
        public ApplicationDataContainer localSettings = null;
        private ObservableCollection<DeviceInformation> listOfDevices;
        public DeviceInformation choosenDevice;
        private ExtendedExecutionSession session = null;
        GpioPin pin26;//Зеленый светодиод
        string OsType = AnalyticsInfo.VersionInfo.DeviceFamily;

        DispatcherTimer timerCheckToRestart;
        int timeToRestartHours = 0;
        int timeToRestartMinutes = 0;
        DateTime now;

        private string[] sArr = new string[16] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
        private string[] arrInitial = new string[16] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
        private string[] arrBefore = new string[16];
        string clientId = "";

        MqttClient client = null;
        /// /////////////
        string sArr3Before = "";
        string directionLeft; //направление вращения левого колеса
        string directionRight;//направление вращения правого колеса
        double speedLeft, speedRight, speedLeft0 = 0, speedRight0 = 0;
        double speedTuningParam = CommonStruct.speedTuningParam;
        double alpha = 0.0;
        string wheelsAddress = CommonStruct.wheelsAddress;
        string cameraAddress = CommonStruct.cameraAddress;
        int minWheelsSpeedForTurning = CommonStruct.minWheelsSpeedForTurning;//В процентах (%) - скорость, ниже которой не может двигаться колесо, которое замедляется при плавном повороте
        double minSpeed = 100;
        bool firstEnterInYellowLeft = true;
        bool firstEnterInYellowRight = true;
        bool firstEnterInGreenRight = true;
        bool firstEnterInGreenLeft = true;
        bool firstEnterInRed = true;
        
        bool fromStopToStart = false;
        string mem2 = "Stop";
        int counterFromStopToStart = 0;
        string lastColorArea = "";
        int minAllowableSpeed = 15;
        string cameraIsStopped = "no";
        DispatcherTimer watchdogTimer;
        DispatcherTimer reconnectTimer;
        static string oldText = "";
        GpioPin pin3;//Старая выгрузка Виндовс и новый Выкл-Вкл.
        GpioPinValue val3 = GpioPinValue.Low;
        GpioPin pin5;//Выкл - подача сигнала на таймер выключения питания

        public MainPage()
        {
            clientId = Guid.NewGuid().ToString();
            InitializeComponent();
            listOfDevices = new ObservableCollection<DeviceInformation>();
            localSettings = ApplicationData.Current.LocalSettings;

            object testObject = localSettings.Values["defaultWebSiteAddress"];
            if (testObject == null)
            {
                WriteDefaultSettings();
                ReadSettings();
            }
            else
            {
                ReadSettings();
            }
            object testObject1 = localSettings.Values["angleInBreakPoint"];
            if (testObject1 == null)
            {
                localSettings.Values["angleInBreakPoint"] = 170;
                localSettings.Values["distanceToZero"] = 145;
                CommonStruct.AngleInBreakPoint = Convert.ToDouble(localSettings.Values["angleInBreakPoint"]);
                CommonStruct.DistanceToZero = Convert.ToDouble(localSettings.Values["distanceToZero"]);
            }

            object testObject2 = localSettings.Values["cameraController"];
            if (testObject2 == null)
            {
                localSettings.Values["cameraController"] = "No";
                CommonStruct.cameraController = "No";
            }
            else
            {
                CommonStruct.cameraController = Convert.ToString(testObject2);
            }

            InitializeUI();
            Current = this;

            InitializeRobot();
            CommonStruct.decriptedSerial = Convert.ToString(localSettings.Values["Serial"]);

            //Таймер для перезагрузки (рестарта) Windows
            setTimeToRestartPicker.TimeChanged += SetTimeToRestar_TimeChanged;
            timerCheckToRestart = new DispatcherTimer();
            timerCheckToRestart.Tick += TimerCheckToRestart_Tick;
            timerCheckToRestart.Interval = new TimeSpan(0, 30, 0); //Интервал проверки времени перезагрузки Windows (часы, мин, сек)
            timerCheckToRestart.Start();

            //Калибровка измерителя напряжения на аккумуляторе
            textBoxRealVoltage.Text = CommonStruct.VReal.ToString();
            textBoxRealVoltage.TextChanged += TextBoxRealVoltage_TextChanged;
            
            Task.Delay(1000).Wait();

            ScenarioControl.ItemsSource = scenarios;
            if (Window.Current.Bounds.Width < 640)
            {
                ScenarioControl.SelectedIndex = -1;
            }
            else
            {
                ScenarioControl.SelectedIndex = 0;
            }

            if (OsType == "Windows.IoT")
            {
                pin26 = GpioController.GetDefault().OpenPin(26);
                pin26.Write(GpioPinValue.Low);// Latch HIGH value first. This ensures a default value when the pin is set as output

                checkBoxOnlyLocal.Visibility = Visibility.Collapsed;
                buttonExit.Visibility = Visibility.Collapsed;
            }

            client = MqttInitialization(CommonStruct.defaultWebSiteAddress);

            CommonStruct.decriptedSerial = textBoxRobotSerial.Text;
            dataFromRobot[0] = CommonStruct.decriptedSerial;
            dataFromRobot[1] = "";
            dataFromRobot[6] = CommonStruct.speedTuningParam.ToString();
            directionLeft = backwardDirection; //направление вращения левого колеса
            directionRight = backwardDirection;//направление вращения правого колеса
            CommonStruct.wheelsWasStopped = true;

            watchdogTimer = new DispatcherTimer();
            watchdogTimer.Tick += WatchdogTimer_Tick;
            watchdogTimer.Interval = new TimeSpan(0, 0, 0, 1, 200); //Ватчдог таймер (дни, часы, мин, сек, ms)

            reconnectTimer = new DispatcherTimer();
            reconnectTimer.Tick += ReconnectTimer_Tick;
            reconnectTimer.Interval = new TimeSpan(0, 0, 0, 3, 0); //Таймер для реконнекта к MQTT брокеру (дни, часы, мин, сек, ms)
            reconnectTimer.Start();

            //Этот таймер должен давать два тика до того, как отошлется очередная команда плавной остановки (там 200 мс) 
            hostWatchdogInitTimer = new DispatcherTimer();
            hostWatchdogInitTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);//таймер для начальной установка сторожевых таймеров в модулях
            hostWatchdogInitTimer.Tick += HostWatchdogInitTimer_Tick;
            hostWatchdogInitTimer.Start();

            readWrite = new ReadWrite();
            //Task.Delay(1000).Wait();
            plcControl = new PlcControl();
            // buttonStart_Click(null, null);

            pin3 = GpioController.GetDefault().OpenPin(3);//Это пин, на кторый опдается сигнал от кнопки включения-выключения. При нажати на кнопку нпряжение на нем поднимается от 0,9В до 3 В.
            pin3.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 10);
            pin3.SetDriveMode(GpioPinDriveMode.Input);
            pin3.ValueChanged += Pin3_ValueChanged;
            val3 = pin3.Read();

            pin5 = GpioController.GetDefault().OpenPin(5);
            pin5.SetDriveMode(GpioPinDriveMode.Output);
            pin5.Write(GpioPinValue.High);// Latch HIGH value first. This ensures a default value when the pin is set as output

        }

        private MqttClient MqttInitialization(string address)
        {
            int k = address.IndexOf("//");
            CommonStruct.webAddressForMQTT = address.Remove(0, k + 2);
            client = new MqttClient(CommonStruct.webAddressForMQTT);
            return client;
        }

        private void Pin3_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            try
            {
                //Task t = new Task(async () =>
                //{
                //    CommonStruct.permissionToSendToWebServer = false;
                //    await SendVoltageToServer("BotEyes is Off");
                //});
                //t.Start();
                //t.Wait(1000);

                if (args.Edge == GpioPinEdge.RisingEdge)
                {
                    Task t = new Task(async () =>
                    {
                        CommonStruct.permissionToSendToWebServer = false;
                        await SendVoltageToServer("BotEyes is Off");
                    });
                    t.Start();

                    pin5.Write(GpioPinValue.Low);//Аппаратный таймер выключения запускается нулем
                    Timer periodicTimer = new Timer(ShutDownLaunch, null, 2000, Timeout.Infinite);
                    //CoreApplication.Exit();
                    //ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));//Тут всегда ноль. Задержка работает только для перезагрузки.
                }
            }
            catch (Exception e)
            {
                MainPage.Current.NotifyUser("Shutdown problem " + e.Message, NotifyType.ErrorMessage);
                CoreApplication.Exit();
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
            }
        }

        private void ShutDownLaunch(object state)
        {
            try
            {
                Task t = new Task(async () =>
                {
                    CommonStruct.permissionToSendToWebServer = false;
                    await SendVoltageToServer("BotEyes is Off");
                });
                t.Start();

                CoreApplication.Exit();
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
            }
            catch(Exception e2)
            {
                MainPage.Current.NotifyUser("Shutdown problem " + e2.Message, NotifyType.ErrorMessage);
                CoreApplication.Exit();
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
            }
        }

        private void ReconnectTimer_Tick(object sender, object e)
        {
            try
            {
                bool isConnected = client.IsConnected;
                if ((isConnected == false) && (bConnect == true)) {
                    client.Connect(clientId);
                    client.Subscribe(new string[] { CommonStruct.decriptedSerial }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    client.MqttMsgPublishReceived += Client_MqttMsgPublishReceivedAsync;

                    Current.NotifyUserFromOtherThreadAsync("MQTT Reconnected Successfully", NotifyType.ErrorMessage);
                    if (OsType == "Windows.IoT") {
                        pin26.SetDriveMode(GpioPinDriveMode.Output);
                        pin26.Write(GpioPinValue.Low);
                    }
                } else {
                    if (OsType == "Windows.IoT") {
                        pin26.SetDriveMode(GpioPinDriveMode.Output);
                        pin26.Write(GpioPinValue.High);
                    }
                }

                if (readWrite.serialPort == null) readWrite.comPortInit();
            }
            catch(Exception e1)
            {
                Current.NotifyUserFromOtherThreadAsync("ReconnectTimer_Tick " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        private void TextBoxRealVoltage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxRealVoltage.Text != "") {
                CommonStruct.VReal = Convert.ToDouble(textBoxRealVoltage.Text);
                CommonStruct.textBoxRealVoltageChanged = true;
                localSettings.Values["VReal"] = CommonStruct.VReal.ToString();
            }
        }

        private void SetTimeToRestar_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            timeToRestartHours = setTimeToRestartPicker.Time.Hours;
            timeToRestartMinutes = setTimeToRestartPicker.Time.Minutes;
            localSettings.Values["initTime"] = 60 * timeToRestartHours + timeToRestartMinutes;
            CommonStruct.initTime = 60 * timeToRestartHours + timeToRestartMinutes;
        }

        private void TimerCheckToRestart_Tick(object sender, object e)
        {
            now = DateTime.Now;
            int timeNow = 60 * now.Hour + now.Minute;
            int setTime = 60 * timeToRestartHours + timeToRestartMinutes;
            if ((timeNow > setTime) && (timeNow < setTime + 30))
            {
                if (checkRebootAtNight.IsChecked == true)
                {
                    ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(0));
                }
            }
        }

        private async void RequestExtendedExecution()
        {
            session = new ExtendedExecutionSession();
            session.Description = "Location Tracker";
            session.Reason = ExtendedExecutionReason.Unspecified;
            session.Revoked += Session_Revoked;
            var result = await session.RequestExtensionAsync();
            if (result == ExtendedExecutionResult.Allowed)
            {
                Current.NotifyUserFromOtherThreadAsync("Extended execution Allowed", NotifyType.StatusMessage);
            }
            else if (result == ExtendedExecutionResult.Denied)
            {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    Current.NotifyUserFromOtherThreadAsync("Extended execution DENIED", NotifyType.StatusMessage);
                });
            }
            else
            {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Current.NotifyUserFromOtherThreadAsync("Extended execution DENIED", NotifyType.StatusMessage);
                });
            }
        }

        private void Session_Revoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Current.NotifyUserFromOtherThreadAsync("Extended execution REVOKED", NotifyType.StatusMessage);
            });
            EndExtendedExecution();
        }

        private void EndExtendedExecution()
        {
            if (session != null)
            {
                session.Revoked -= Session_Revoked;
                session.Dispose();
                session = null;
            }
        }

         private void Polling(string[] arr)
        {
            #region Base Cycle
                if (arr != null)
                {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        textBox_x_coord.Text = arr[1];//x_coord
                        textBox_y_coord.Text = arr[2];//y_coord
                        textBoxWheelsStop.Text = arr[3];//wheelsStop
                        textBoxCameraAngle.Text = arr[4];//сameraData
                        textBoxKeys.Text = arr[5];//Управление клавишами
                        textBoxWheelsCorrection.Text = arr[6];//Поправка для колес - движение прямо
                        textBoxSmileName.Text = arr[7];//Smile Name
                        textBoxSmileName.Text = arr[8];//Нелинейная коррекция есть = true
                    });

                    if (arr[8] == "false")
                    {
                        CommonStruct.wheelsNonlinearTuningIs = false;
                    }
                    else if (arr[8] == "true")
                    {
                        CommonStruct.wheelsNonlinearTuningIs = true;
                    }

                    if ((arr[6] != null) && (arr[6] != "Corr") && (arr[6] != "0") && (arr[6] != ""))
                    {//Передача параметра подстройки скоростей колес из браузера в робот
                        try
                        {
                            CommonStruct.speedTuningParam = Convert.ToDouble(arr[6]);
                        }
                        catch (Exception e)
                        {
                            Current.NotifyUser("if ((arr[6] != null)" + e.Message, NotifyType.ErrorMessage);
                            //CommonFunctions.WriteToLog(e.Message + "CommonStruct.speedTuningParam = Convert.ToDouble(arr[6]);");
                        }

                        dataFromRobot[6] = CommonStruct.speedTuningParam.ToString();
                        textBoxWheelsSpeedTuning.Text = CommonStruct.speedTuningParam.ToString();
                        trackBarWheelsSpeedTuning.Value = Convert.ToInt32(CommonStruct.speedTuningParam);
                    }

                    string direction = "0";
                    switch (arr[4]) {//Управление камерой
                        case "Up":
                            direction = "1";
                            plcControl.CameraUpDown(direction);
                            cameraIsStopped = "no";
                            break;
                        case "Down":
                            direction = "0";
                            plcControl.CameraUpDown(direction);
                            cameraIsStopped = "no";
                            break;
                        case "Stop":
                            if (cameraIsStopped == "no") {
                                if (CommonStruct.cameraController != "No") {
                                    plcControl.CameraStop();
                                    cameraIsStopped = "yes";
                                }
                            }
                            break;
                    }

                    //Управление колесами. Макс. speedRadius равен 100 пикселям
                    if ((arr[1] == "") || (arr[1] == null)) { arr[1] = "0"; }
                    if ((arr[2] == "") || (arr[2] == null)) { arr[2] = "0"; }

                    if ((arr[5] == "0") && (arr[4] == "0"))
                    {//Управление мышкой  
                        double speedRadius = CommonFunctions.SpeedRadius(arr[1], arr[2]);//
                        speedLeft0 = speedRadius;
                        speedRight0 = speedRadius;
                        alpha = CommonFunctions.Degrees(arr[1], arr[2]);//

                    if ((arr[3] == "Start") && (mem2 == "Stop"))
                        {
                            counterFromStopToStart = counterFromStopToStart + 1;
                            if (counterFromStopToStart == 1)
                            {
                                fromStopToStart = true;
                                lastColorArea = "";
                            }
                        }
                        else if ((arr[3] == "Stop") && (mem2 == "Start"))
                        {
                            fromStopToStart = false;
                            counterFromStopToStart = 0;
                        }
                        else
                        {
                            counterFromStopToStart = 0;
                        }
                        mem2 = arr[3];

                    if ((arr[3] == "Start") || (speedRadius > 1))
                        {//Управление мышкой  
                            sArr3Before = "КолесаВращались";

                            if ((70 < alpha && alpha <= 110) && (speedRadius > minAllowableSpeed))
                            {//0 - Только прямо в узкой зоне 
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedLeft = speedLeft0;
                                speedRight = speedRight0;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                            }
                                
                            if ((20 < alpha && alpha <= 70) && (speedRadius > minAllowableSpeed))
                            {//1 - Правая часть сектора "Прямо" 
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedLeft = speedLeft0;
                                minSpeed = (minWheelsSpeedForTurning / 100.0) * speedRight0;//В процентах, предельно низкая скорость, ниже которой колесо не может двигаться при повороте
                                //speedRight = (((speedRight0 - minSpeed) / (81 - 20)) * (alpha - 20) + minSpeed);
                                speedRight = speedRight0 * (1 - 0.2 * (70 - alpha ) / (70 - 20)); 
                                if ((firstEnterInGreenRight == true) && (fromStopToStart == true) && (lastColorArea == "yellow"))
                                {
                                    plcControl.WheelsStopSmoothly(200);
                                    firstEnterInGreenRight = false;
                                }
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                lastColorArea = "green";
                            }
                            else
                            {
                                firstEnterInGreenRight = true;
                            }

                            if ((110 < alpha && alpha <= 159) && (speedRadius > minAllowableSpeed))
                            {//2 - Левая часть сектора "Прямо"  
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                minSpeed = (minWheelsSpeedForTurning / 100.0) * speedLeft0;
                                //speedLeft = ((speedLeft0 - minSpeed) / (97 - 159)) * (alpha - 159) + minSpeed;
                                speedLeft = speedLeft0 * (1 - 0.2 * (alpha - 110) / (159 - 110));
                                speedRight = speedRight0;
                                if ((firstEnterInGreenLeft == true) && (fromStopToStart == true) && (lastColorArea == "yellow"))
                                {
                                    plcControl.WheelsStopSmoothly(200);
                                    firstEnterInGreenLeft = false;
                                }
                                else
                                {
                                    plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                    lastColorArea = "green";
                                }
                            }
                            else
                            {
                                firstEnterInGreenLeft = true;
                            }

                            if (159 < alpha && alpha <= 225)
                            {//3 - Разворот влево 
                                directionLeft = backwardDirection;
                                directionRight = forwardDirection;
                                speedLeft = 0.4 * speedLeft0;
                                speedRight = 0.4 * speedRight0;
                                if ((firstEnterInYellowLeft == true) && (fromStopToStart == true) && ((lastColorArea == "green") || (lastColorArea == "red")))
                                {
                                    plcControl.WheelsStopSmoothly(200);
                                    firstEnterInYellowLeft = false;
                                }
                                else
                                {
                                    plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                    lastColorArea = "yellow";
                                }
                            }
                            else
                            {
                                firstEnterInYellowLeft = true;
                            }

                            if (225 < alpha && alpha <= 316)
                            {//4 - Движение назад 
                                directionLeft = backwardDirection;
                                directionRight = backwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedLeft0;
                                if ((firstEnterInRed == true) && (fromStopToStart == true) && (lastColorArea == "yellow"))
                                {
                                    plcControl.WheelsStopSmoothly(200);
                                    firstEnterInRed = false;
                                }
                                else
                                {
                                    plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                    lastColorArea = "red";
                                }
                            }
                            else
                            {
                                firstEnterInRed = true;
                            }

                            if ((316 < alpha) || ((0 <= alpha) && (alpha <= 20)))
                            {//5 - Разворот вправо 
                                directionRight = backwardDirection;
                                directionLeft = forwardDirection;
                                speedRight = 0.4 * speedRight0;
                                speedLeft = 0.4 * speedLeft0;
                                if ((firstEnterInYellowRight == true) && (fromStopToStart == true) && ((lastColorArea == "green") || (lastColorArea == "red")))
                                {
                                    plcControl.WheelsStopSmoothly(200);
                                    firstEnterInYellowRight = false;
                                }
                                else
                                {
                                    plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                    lastColorArea = "yellow";
                                }
                            }
                            else
                            {
                                firstEnterInYellowRight = true;
                            }
                        }
                    }
                    ///////////Управление клавишами
                    if (((arr[5] != "Stop") && (arr[5] != null) && (arr[5] != "0")) && (arr[3] == "Start"))
                    {//Управление клавишами
                        sArr3Before = "КолесаВращались";
                        double xCoord = 0.0;
                        double yCoord = 0.0;
                        try
                        {
                            xCoord = Convert.ToDouble(arr[1]);
                            yCoord = -Convert.ToDouble(arr[2]);
                        }
                        catch (Exception e)
                        {
                            Current.NotifyUser("if (((sArr[5] != Stop)" + e.Message + "xCoord = ", NotifyType.ErrorMessage);
                        }

                        double turnSpeed = 0.5 * Math.Abs(xCoord);
                        double speedY = Math.Abs(yCoord);
                        speedLeft0 = speedY;//скорость левого колеса
                        speedRight0 = speedY;//скорость правого колеса

                        switch (arr[5])
                        {//Управление клавишами
                            case "left":
                                directionLeft = backwardDirection;
                                directionRight = forwardDirection;
                                speedRight = turnSpeed;
                                speedLeft = turnSpeed;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "top":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedLeft0;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "right":
                                directionLeft = forwardDirection;
                                directionRight = backwardDirection;
                                speedRight = turnSpeed;
                                speedLeft = turnSpeed;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "topAndLeft":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedRight0 - turnSpeed;
                                if (speedLeft < 0) { speedLeft = 0; }
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "topAndRight":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedLeft0 - turnSpeed;
                                if (speedRight < 0) { speedRight = 0; }
                                speedLeft = speedLeft0;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "bottom":
                                directionLeft = backwardDirection;
                                directionRight = backwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedLeft0;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                        }
                    }
                    ///////////////////////////////////////////
                    if (arr[3] == "Stop") // !=Start - нельзя, т.к. с сервера идет поток нулей, который будет останавливать колеса
                    {
                        if (sArr3Before == "КолесаВращались")
                        {
                            if ((CommonStruct.stopSmoothly == true) && (((directionLeft == forwardDirection) && (directionRight == forwardDirection)) || ((directionLeft == backwardDirection) && (directionRight == backwardDirection))))
                            {
                            plcControl.WheelsStopSmoothly(200);
                            sArr3Before = "0";
                            }
                            else
                            {
                            double speedRadius = CommonFunctions.SpeedRadius(arrBefore[1], arrBefore[2]);
                            if (speedRadius >= 50) {
                                plcControl.WheelsStopSmoothly(100);
                            } else {
                                plcControl.WheelsStop();
                            }
                            sArr3Before = "0";
                            }
                        }
                    }
                }
                else
                {
                    if (OsType == "Windows.IoT")
                    {
                        pin26.Write(GpioPinValue.Low);
                    }
                }
            if (OsType == "Windows.IoT")
            {//Чтобы зеленый не остался гореть, когда нет данных. 
                pin26.Write(GpioPinValue.Low);
            }
        }

        public void ChargeLevelMeasure()
        {
            double levelCeiling = 0.0;
            try {
                CommonStruct.dataToWrite = "^A3" + CommonStruct.wheelsAddress + "\r";//Формирование команды чтения из АЦП
                readWrite.Write(CommonStruct.dataToWrite);//

                if (CommonStruct.voltageLevelFromRobot == "")
                {
                    return;
                }
                else
                {
                    if (CommonStruct.voltageLevelFromRobot == "Charging...")
                    {
                        labelChargeLevel.Background = new SolidColorBrush(Windows.UI.Colors.Yellow);
                        labelChargeLevel.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                        labelChargeLevel.Text = CommonStruct.voltageLevelFromRobot;
                    }
                    else
                    {
                        labelChargeLevel.Text = CommonStruct.voltageLevelFromRobot + "%";
                        levelCeiling = Convert.ToDouble(CommonStruct.voltageLevelFromRobot);
                        if (levelCeiling > 40)
                        {
                            labelChargeLevel.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                            labelChargeLevel.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                        }
                        else
                        {
                            labelChargeLevel.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                            labelChargeLevel.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                        }
                    }
                }
            }
            catch(Exception e1) {
                Current.NotifyUser("ChargeLevelTimer_TickAsync " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        public void ChargeCurrentMeasure()
        {
            try
            {
                CommonStruct.dataToWrite = "^A3" + CommonStruct.wheelsAddress + "\r";//Формирование команды чтения из АЦП
                //readWrite.Write(CommonStruct.dataToWrite);//

                if (CommonStruct.chargeCurrentFromRobot == "") return;
            }
            catch (Exception e1)
            {
                Current.NotifyUser("ChargeLevelTimer_TickAsync " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        bool isEntireMessage = true;//признак того 
        int iNumberOfMessage = 0;
        int iNubmerOfMessageBefore = 0;
        int timeNow = 0;
        int timeBefore = 0;
        int iArrayCounter = 0;//Нужно чтобы отличить первый массив от последующих

        private void Client_MqttMsgPublishReceivedAsync(object sender, MqttMsgPublishEventArgs e)
        {
            //Current.NotifyUserFromOtherThread("MQTT " + CommonStruct.wheelsWasStopped.ToString(), NotifyType.StatusMessage);
            string topic = e.Topic; byte[] message = e.Message; string result = Encoding.UTF8.GetString(message);
            string s = result; string delim = "\""; s = s.Replace(delim, "");//Строка с разделителями - запятыми
            s = s.Replace(delim, ""); s = s.Replace("[", ""); s = s.Replace("]", ""); char[] separator = new char[1];
            separator[0] = ','; sArr = s.Split(separator, 16);

            if ((plcControl.stopTimerCounter == 0) && (sArr[0] == CommonStruct.decriptedSerial)) {
                __Client_MqttMsgPublishReceivedAsync(sender, e);
            }
        }

         private void __Client_MqttMsgPublishReceivedAsync(object sender, MqttMsgPublishEventArgs e)
        {
            if (sArr[14] == "0") arrBefore[14] = "0";
            iNumberOfMessage = Convert.ToInt32(sArr[14]);
            iNubmerOfMessageBefore = Convert.ToInt32(arrBefore[14]);
            timeNow = Convert.ToInt32(sArr[15]);
            timeBefore = Convert.ToInt32(arrBefore[15]);
            if (iArrayCounter == 0) isEntireMessage = true;

            //Сюда входят сообщения и массивы в них и я анализирую, пропускать их в Polling или нет 
            if (iNumberOfMessage >= iNubmerOfMessageBefore)
            {//Здесь убираем перепутывание массивов между разными сообщениями. Конкретнее, на границе между сообщениями последняя посылка из старого сообщения может прийти позже первой посылки из нового
                if (isEntireMessage == true)
                {//Знание, что все массивы из одного сообщения, нужно, чтобы начать фильтровать по временным меткам - они начинаются в каждом сообщении с нуля.
                    if ((timeNow > timeBefore) || (iArrayCounter == 0))
                    {//фильтруем по временным меткам. Здесь iArrayCounter == 0 нуужно потому, что в первом сообщении может быть timeNow =0 и тогда условие ">" не выполняется
                        if ((sArr[3] == "Start") || (sArr[4] == "Start") || ((sArr[5] != "Stop") && ((sArr[3] == "Start"))))
                        {//sArr[5] != "Stop" потому что это управление клавишами, тем нет команды Start. Там все старт кроме Stop
                            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                watchdogTimer.Stop();//Стоп и сразу Старт нужны, чтобы момент запуска таймера совпадал с временем прихода посылки
                                watchdogTimer.Start();//т.е. это дублирование сторожевого таймера внутри модулей.
                            });

                            iArrayCounter++;//счетчик количества посылок. Нужен, чтобы отличить первую посылку от остальных 
                            sArr.CopyTo(arrBefore, 0);
                            Polling(sArr);
                        }
                        else
                        {
                            InitialConditions();
                        }
                    }
                }
            }
        }

        private void WatchdogTimer_Tick(object sender, object e)
        {
            InitialConditions();//Внутри этой фоункции стоит остановка по таймеру. Т.е. если данные не пришли, то во всех модулях останется команда Стоп. 
            CommonStruct.permissionToSend = true;
        }

        private void InitialConditions()
        {//Здесь мы пропускаем все посылки для остановки, но для повышения надежности скорость делаем нулевой и 
            isEntireMessage = false;
            timeNow = 0;
            timeBefore = 0;
            iArrayCounter = 0;
            sArr[1] = "0";
            sArr[2] = "0";
            arrBefore[14] = "0";
            arrBefore[15] = "0";
            Polling(sArr);
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                watchdogTimer.Stop();
            });
        }

        public static async Task SendVoltageToServer(string text)
        {
            if (text == "") return;
            
            string ipAddress = CommonStruct.defaultWebSiteAddress + ":443";
            Uri uri = new Uri(ipAddress + "/datafromrobot?data=" + text + "&serial=" + CommonStruct.decriptedSerial);
            if (CommonStruct.decriptedSerial == "")
            {
                Current.NotifyUser("SendVoltageToServer(): Serial is not defined.", NotifyType.ErrorMessage);
                return;
            }

            try {
                //var authData = string.Format("{0}:{1}", "admin", "admin");
                var authData = string.Format("{0}:{1}", "", "");//Password don't needed for both websites
                var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authData));

                using (HttpClient client = new HttpClient()) {
                    client.MaxResponseContentBufferSize = 256000;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("None", authHeaderValue);
                    HttpContent content = null;

                    using (HttpResponseMessage response = await client.PutAsync(uri, content)) {
                        if (!response.IsSuccessStatusCode) {
                            ipAddress = "StatusCode: " + Convert.ToString(response.StatusCode);
                        }
                        if (response.Content != null) {
                            string responseBodyAsText;
                            responseBodyAsText = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
            catch (Exception ex) {
                ipAddress = "StatusCode: " + ex.Message;
                Current.NotifyUser("SendVoltageToServer() " + ex.Message, NotifyType.ErrorMessage);
            }
        }

        public static async Task SendErrorsToServer(string text)
        {
            string ipAddress = CommonStruct.defaultWebSiteAddress + ":443";
            Uri uri = null;
            if (text == "") return;
            if (oldText == text) return;

            DateTime now = DateTime.Now;
            string timeNow = now.ToString();
            uri = new Uri(ipAddress + "/errorfromrobot?timeNow=" + timeNow + "&data=" + text + "&serial=" + CommonStruct.decriptedSerial);
            try
            {
                var authData = string.Format("{0}:{1}", "", ""); //Password don't needed for both websites
                //var authData = string.Format("{0}:{1}", "Administrator", "Qqa4xJ@fE$u8VS");
                var authHeaderValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authData));

                using (HttpClient client = new HttpClient())
                {
                    client.MaxResponseContentBufferSize = 256000;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("None", authHeaderValue);
                    HttpContent content = null;

                    using (HttpResponseMessage response = await client.PostAsync(uri, content))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            ipAddress = "StatusCode: " + Convert.ToString(response.StatusCode);
                        }
                        if (response.Content != null)
                        {
                            string responseBodyAsText;
                            responseBodyAsText = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ipAddress = "StatusCode: " + ex.Message;
                Current.NotifyUser("SendToServer() " + ex.Message, NotifyType.ErrorMessage);
            }
            oldText = text;
        }

        #endregion Base Cycle

        #region Buttons

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            RequestExtendedExecution();
            
            AllControlIsEnabled(false);
            LeftGroup.Visibility = Visibility.Visible;
            bConnect = true;

            buttonStop.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            buttonStop.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            buttonStop.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStop.FontSize = 20;
            buttonStop.IsEnabled = true;

            buttonStart.Background = new SolidColorBrush(Windows.UI.Colors.LightGray); ;
            buttonStart.Foreground = new SolidColorBrush(Windows.UI.Colors.Gray);
            buttonStart.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStart.IsEnabled = false;
           
            try
            {
                if (CommonStruct.decriptedSerial.Length == 24) {
                    client.Connect(clientId);//Может S/N вместо этого взять?
                    client.Subscribe(new string[] { CommonStruct.decriptedSerial }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    client.ConnectionClosed += Client_ConnectionClosed;
                    client.MqttMsgPublishReceived += Client_MqttMsgPublishReceivedAsync;
                } else {
                    Current.NotifyUserFromOtherThreadAsync("Robot Serial Number is incorrect ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception e1)
            {
                Current.NotifyUserFromOtherThreadAsync(e1.Message, NotifyType.ErrorMessage);
            }
        }

        private void Client_ConnectionClosed(object sender, EventArgs e)
        {

            bool isConnected = client.IsConnected;
            Current.NotifyUserFromOtherThreadAsync("Connection Closed", NotifyType.StatusMessage);
            if (OsType == "Windows.IoT") {
                pin26.SetDriveMode(GpioPinDriveMode.Output);
                pin26.Write(GpioPinValue.Low);
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EndExtendedExecution();
                client.Disconnect();
            }
            catch(Exception e1)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync(e1.Message, NotifyType.ErrorMessage);
            }

            bConnect = false;

            AllControlIsEnabled(true);
            LeftGroup.Visibility = Visibility.Collapsed;

            buttonStart.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            buttonStart.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStart.FontSize = 20;
            buttonStart.IsEnabled = true;

            buttonStop.Background = new SolidColorBrush(Windows.UI.Colors.LightGray); ;
            buttonStop.Foreground = new SolidColorBrush(Windows.UI.Colors.Gray);
            buttonStop.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStop.IsEnabled = false;
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!PopupSettings.IsOpen) { PopupSettings.IsOpen = true; }
        }

        private void buttonCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            if (PopupSettings.IsOpen) { PopupSettings.IsOpen = false; }
        }

        private void buttonSetDefault_Click(object sender, RoutedEventArgs e)
        {
            WriteDefaultSettings();
            localSettings.Values["Serial"] = textBoxRobotSerial.Text;
            ReadSettings();
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(0));
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["cameraController"] = CommonStruct.cameraController;
            localSettings.Values["PWMStoppingSpeed"] = Convert.ToInt16(textBoxPWMStoppingSpeed.Text);
            CommonStruct.PWMStoppingSpeed = Convert.ToInt16(textBoxPWMStoppingSpeed.Text);
            localSettings.Values["CameraFastSpeed"] = textBoxCameraFastSpeed.Text;
            CommonStruct.cameraFastSpeed = textBoxCameraFastSpeed.Text;
            localSettings.Values["stepNumberForCalibration"] = textBoxStepNumberForCalibration.Text;
            CommonStruct.stepNumberForCalibration = textBoxStepNumberForCalibration.Text;
            localSettings.Values["directTopDistance"] = Convert.ToInt16(textBoxDirectTopDistance.Text);
            CommonStruct.directTopDistance = Convert.ToInt16(textBoxDirectTopDistance.Text);
            localSettings.Values["directBottomDistance"] = Convert.ToInt16(textBoxDirectBottomDistance.Text);
            CommonStruct.directBottomDistance = Convert.ToInt16(textBoxDirectBottomDistance.Text);
            localSettings.Values["minWheelsSpeedForTurning"] = Convert.ToInt16(textBoxMinWheelsSpeedForTurning.Text);
            CommonStruct.minWheelsSpeedForTurning = Convert.ToInt16(textBoxMinWheelsSpeedForTurning.Text);
            localSettings.Values["speedTuningParam"] = Convert.ToDouble(textBoxSpeedTuningParam.Text);
            CommonStruct.speedTuningParam = Convert.ToDouble(textBoxSpeedTuningParam.Text);
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Exit();
        }

        private void buttonRobotSerialPopup_Click(object sender, RoutedEventArgs e)
        {
            if (!PopupRobotSerial.IsOpen) { PopupRobotSerial.IsOpen = true; }
            object obj = localSettings.Values["Serial"];
            if (obj != null)
            {
                textBoxRobotSerial.Text = obj.ToString();
            }
            else
            {
                textBoxRobotSerial.Text = "";
            }
        }

        private void buttonRobotSerialCloseAndSave_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["Serial"] = textBoxRobotSerial.Text;
            CommonStruct.decriptedSerial = textBoxRobotSerial.Text;
            if (PopupRobotSerial.IsOpen) { PopupRobotSerial.IsOpen = false; }
        }

        private void buttonRobotSerialCloseNoSave_Click(object sender, RoutedEventArgs e)
        {
            if (PopupRobotSerial.IsOpen) { PopupRobotSerial.IsOpen = false; }
        }

        #endregion Buttons
       
        private void checkBoxOnlyLocal_Checked(object sender, RoutedEventArgs e)
        {
            if (checkBoxOnlyLocal.IsChecked == true)
            {
                localSettings.Values["onlyLocal"] = true;
                CommonStruct.checkBoxOnlyLocal = true;
            }

        }

        private void checkBoxOnlyLocal_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkBoxOnlyLocal.IsChecked == false)
            {
                localSettings.Values["onlyLocal"] = false;
                CommonStruct.checkBoxOnlyLocal = false;
            }
        }

        private void AISettings_Click(object sender, RoutedEventArgs e)
        {
            if (!AIPopup.IsOpen) { AIPopup.IsOpen = true; }
        }

        private void buttonLocalizationAngleSaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (AIPopup.IsOpen) { AIPopup.IsOpen = false; }
        }

        private void buttonLocalizationAngleCloseNoSave_Click(object sender, RoutedEventArgs e)
        {
            if (AIPopup.IsOpen) { AIPopup.IsOpen = false; }
        }

        private void trackBarLocalizationAngle_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.localizationPoint = trackBarLocalizationAngle.Value;
            localSettings.Values["localizationAngle"] = CommonStruct.localizationPoint;
        }

        private void textBlockAngleFromIC_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            string s = e.Key.ToString();
            if (s == "Enter")
            {
                CommonStruct.localizationPoint = trackBarLocalizationAngle.Value;
                localSettings.Values["localizationAngle"] = CommonStruct.localizationPoint;
            }
        }

        private void textBlockAngleInBreakPoint_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            string s = e.Key.ToString();
            if (s == "Enter")
            {
                CommonStruct.AngleInBreakPoint = Convert.ToDouble(textBlockAngleInBreakPoint.Text);
                localSettings.Values["angleInBreakPoint"] = CommonStruct.AngleInBreakPoint;
            }
        }

        private void textBlockDistanceToZero_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            string s = e.Key.ToString();
            if (s == "Enter")
            {
                CommonStruct.DistanceToZero = Convert.ToDouble(textBlockDistanceToZero.Text);
                localSettings.Values["distanceToZero"] = CommonStruct.DistanceToZero;
            }
        }

        private void buttonShutdown_Click(object sender, RoutedEventArgs e)
        {
            Task t = new Task(async () =>
            {
                await SendVoltageToServer("BotEyes is Off");
                CommonStruct.permissionToSendToWebServer = false;
            });
            t.Start();
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
        }

        private void buttonRestart_Click(object sender, RoutedEventArgs e)
        {
            //ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(0));//Перезагрузка Windows
            CoreApplication.Exit();
        }

        private void ScenarioControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyUser(String.Empty, NotifyType.StatusMessage);
            ListBox scenarioListBox = sender as ListBox;
            Scenario s = scenarioListBox.SelectedItem as Scenario;
            if (s != null)
            {
                ScenarioFrame.Navigate(s.ClassType);
                if (Window.Current.Bounds.Width < 640)
                {
                    //StatusBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        public List<Scenario> Scenarios
        {
            get { return this.scenarios; }
        }
    }
}
