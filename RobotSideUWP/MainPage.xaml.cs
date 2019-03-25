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
using Windows.Devices.SerialCommunication;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
        }
    }

    public class Scenario
    {
        public Type ClassType { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        bool b = false;
        private string forwardDirection = "0";
        private string backwardDirection = "1";
        string[] dataFromRobot = new string[16] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" }; //данные из робота  
        string c = null;
        public ApplicationDataContainer localSettings = null;
        private ObservableCollection<DeviceInformation> listOfDevices;
        ReadWrite rw = null;
        public static ThreadPoolTimer DelayTimer;
        public DeviceInformation choosenDevice;
        string aqs = null;
        private ExtendedExecutionSession session = null;
        GpioPin pin26;//Зеленый светодиод
        string OsType = AnalyticsInfo.VersionInfo.DeviceFamily;

        DispatcherTimer timerCheckToRestart;
        int timeToRestartHours = 0;
        int timeToRestartMinutes = 0;
        DateTime now;

        private static int sArrLength = 16;
        private string[] sArr = new string[sArrLength];
        private string[] sArrBefore = new string[sArrLength];
        string clientId = "";

        string address = CommonStruct.defaultWebSiteAddress;
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

        DispatcherTimer watchdogTimer;

        public MainPage()
        {
            for (int i = 0; i < sArrLength - 1; i++) { sArr[i] = "0"; }
            clientId = Guid.NewGuid().ToString();

            InitializeComponent();
            listOfDevices = new ObservableCollection<DeviceInformation>();
            localSettings = ApplicationData.Current.LocalSettings;
            aqs = SerialDevice.GetDeviceSelector();//Это метод  дает GUID всех последовательных портов lf

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

            ListAvailablePorts();
            InitializeUI();
            Current = this;
            SelectItem();
            rw = new ReadWrite();

            InitializeRobot();

            //Таймер для перезагрузки (рестарта) Windows
            setTimeToRestartPicker.TimeChanged += SetTimeToRestar_TimeChanged;
            timerCheckToRestart = new DispatcherTimer();
            timerCheckToRestart.Tick += TimerCheckToRestart_Tick;
            timerCheckToRestart.Interval = new TimeSpan(0, 30, 0); //Интервал проверки времени перезагрузки Windows (часы, мин, сек)
            timerCheckToRestart.Start();

            //Калибровка измерителя напряжения на аккумуляторе
            textBoxRealVoltage.Text = CommonStruct.VReal.ToString();
            textBoxRealVoltage.TextChanged += TextBoxRealVoltage_TextChanged;

            InitializeSpeech();

            Task.Delay(1000).Wait();
            if (CommonStruct.cameraController != "No") PlcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");

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

            client = new MqttClient("localhost");
            dataFromRobot[0] = CommonStruct.decriptedSerial;
            dataFromRobot[1] = "";
            dataFromRobot[6] = CommonStruct.speedTuningParam.ToString();
            directionLeft = backwardDirection; //направление вращения левого колеса
            directionRight = backwardDirection;//направление вращения правого колеса
            CommonStruct.wheelsWasStopped = true;

            this.watchdogTimer = new DispatcherTimer();
            watchdogTimer.Tick += WatchdogTimer_Tick;
            this.watchdogTimer.Interval = new TimeSpan(0, 0, 0, 1, 200); //Интервал проверки времени перезагрузки Windows (дни, часы, мин, сек, ms)

            buttonStart_Click(null, null);
        }

        private void TextBoxRealVoltage_TextChanged(object sender, TextChangedEventArgs e)
        {
            CommonStruct.VReal = Convert.ToDouble(textBoxRealVoltage.Text);
            CommonStruct.textBoxRealVoltageChanged = true;
            localSettings.Values["VReal"] = CommonStruct.VReal.ToString();
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
                Current.NotifyUserFromOtherThread("Extended execution Allowed", NotifyType.StatusMessage);
            }
            else if (result == ExtendedExecutionResult.Denied)
            {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Current.NotifyUserFromOtherThread("Extended execution DENIED", NotifyType.StatusMessage);
                });
            }
            else
            {
                var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MainPage.Current.NotifyUserFromOtherThread("Extended execution DENIED", NotifyType.StatusMessage);
                });
            }
        }

        private void Session_Revoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Current.NotifyUserFromOtherThread("Extended execution REVOKED", NotifyType.StatusMessage);
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

        public void ListAvailablePorts()
        {
            string deviceID = "";
            try
            {
                //string aqs = SerialDevice.GetDeviceSelector();//Это метод дает GUID всех последовательных портов
                //var dis = await DeviceInformation.FindAllAsync(aqs);
                //DeviceInformationCollection dis = await DeviceInformation.FindAllAsync(aqs);
                DeviceInformationCollection dis = null;
                Task t = Task.Run(async () => { dis = await DeviceInformation.FindAllAsync(aqs); });
                t.Wait();
                //DeviceInformation.FindAllAsync(aqs).AsTask().Wait();
                string name = "";
                string deviceIDName = "";
                for (int i = 0; i < dis.Count; i++)
                {
                    listOfDevices.Add(dis[i]);
                    name = dis[i].Name;
                    deviceID = dis[i].Id;
                    deviceIDName = CommonFunctions.DeviceIDName(deviceID);
                    CommonStruct.deviceIDNames[i] = CommonFunctions.DeviceIDName(deviceID);
                }
                DeviceListSource.Source = CommonStruct.deviceIDNames;
            }
            catch (Exception ex)
            {
                MainPage.Current.NotifyUserFromOtherThread("ListAvailablePorts: Divece is not connected " + ex.Message, NotifyType.StatusMessage);
            }
        }

        public void SelectItem()
        {
          try{
                int j = 0;
                foreach (DeviceInformation comPort in listOfDevices)
                {
                    if (CommonStruct.comPortItem == CommonFunctions.DeviceIDName(listOfDevices[j].Id))
                    {
                        comboBoxComPorts.SelectedItem = CommonFunctions.DeviceIDName(listOfDevices[j].Id);
                        CommonStruct.comPortIndex = j;
                        break;
                    }
                    else
                    {
                        j++;
                    }
                }
                if (j >= listOfDevices.Count)
                {
                    comboBoxComPorts.SelectedIndex = 0;
                    CommonStruct.comPortIndex = 0;
                    Current.NotifyUser("Are you sure the choosen port is valid?", NotifyType.StatusMessage);
                }
                comboBoxComPorts.AllowDrop = true;
                comboBoxComPorts.SelectedIndex = CommonStruct.comPortIndex;
                comboBoxComPorts.SelectionChanged += ComboBoxComPorts_SelectionChanged;
                choosenDevice = listOfDevices[CommonStruct.comPortIndex];
            }
            catch (Exception e)
            {
                MainPage.Current.NotifyUser("ComboBoxFilling(): " + e.Message, NotifyType.StatusMessage);
            }
        }
        
        private async void Polling()
        {
                        
            #region Base Cycle

          //здесь начинался polling
            dataFromRobot[2] = CommonStruct.voltageLevelFromRobot;
                if (sArr != null)
                {
                    if (OsType == "Windows.IoT")
                    {
                        pin26.SetDriveMode(GpioPinDriveMode.Output);
                        pin26.Write(GpioPinValue.High);
                    }

                    //if (itIsFirstLoop == "Yes")
                    //{
                    //    for (int i = 1; i < 16; i++)
                    //    {//Начинаю от 1, чтобы не стирать серийный номер
                    //        sArr[i] = "0";
                    //    }
                    //    itIsFirstLoop = "No";
                    //}

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() =>
                    {
                        textBox_x_coord.Text = sArr[1];//x_coord
                        textBox_y_coord.Text = sArr[2];//y_coord
                        textBoxWheelsStop.Text = sArr[3];//wheelsStop
                        textBoxCameraAngle.Text = sArr[4];//сameraData
                        textBoxKeys.Text = sArr[5];//Управление клавишами
                        textBoxWheelsCorrection.Text = sArr[6];//Поправка для колес - движение прямо
                        textBoxSmileName.Text = sArr[7];//Smile Name
                        textBoxSmileName.Text = sArr[8];//Нелинейная коррекция есть = true
                    }));

                    if (sArr[8] == "false")
                    {
                        CommonStruct.wheelsNonlinearTuningIs = false;
                    }
                    else if (sArr[8] == "true")
                    {
                        CommonStruct.wheelsNonlinearTuningIs = true;
                    }

                    if ((sArr[6] != null) && (sArr[6] != "Corr") && (sArr[6] != "0") && (sArr[6] != ""))
                    {//Передача параметра подстройки скоростей колес из браузера в робот
                        try
                        {
                            CommonStruct.speedTuningParam = Convert.ToDouble(sArr[6]);
                        }
                        catch (Exception e)
                        {
                            Current.NotifyUser("if ((sArr[6] != null)" + e.Message, NotifyType.StatusMessage);
                            //CommonFunctions.WriteToLog(e.Message + "CommonStruct.speedTuningParam = Convert.ToDouble(sArr[6]);");
                        }

                        dataFromRobot[6] = CommonStruct.speedTuningParam.ToString();

                        textBoxWheelsSpeedTuning.Text = CommonStruct.speedTuningParam.ToString();
                        trackBarWheelsSpeedTuning.Value = Convert.ToInt32(CommonStruct.speedTuningParam);
                    }

                    string direction = "0";
                    switch (sArr[4])
                    {//Управление камерой
                        case "Up":
                            sArr[4] = "0";
                            direction = "1";
                            PlcControl.CameraUpDown(direction);
                            CommonStruct.cameraPositionBefore = "slowUp";
                            break;
                        case "Down":
                            sArr[4] = "0";
                            direction = "0";
                            PlcControl.CameraUpDown(direction);
                            CommonStruct.cameraPositionBefore = "slowDown";
                            break;
                        case "Stop":
                            sArr[4] = "0";
                            if (CommonStruct.cameraController != "No") PlcControl.CameraStop();
                        break;
                        case "UpFast":
                            sArr[4] = "0";
                            direction = "1";
                            CommonStruct.cameraPositionBefore = "top";
                            PlcControl.CameraGoFast(direction);
                            break;
                        case "Center":
                            sArr[4] = "0";
                            PlcControl.CameraGoDirectFast();
                            break;
                        case "DownFast":
                            sArr[4] = "0";
                            direction = "0";
                            CommonStruct.cameraPositionBefore = "bottom";
                            PlcControl.CameraGoFast(direction);
                            break;
                        case "Calibr":
                            sArr[4] = "0";
                            //PlcControl.CameraPositionCalibration();
                            break;
                    }

                    //Управление колесами. Макс. speedRadius равен 100 пикселям
                    if ((sArr[1] == "") || (sArr[1] == null)) { sArr[1] = "0"; }
                    if ((sArr[2] == "") || (sArr[2] == null)) { sArr[2] = "0"; }

                    if (sArr[5] == "Stop")
                    {//Управление мышкой  
                        double speedRadius = CommonFunctions.SpeedRadius(sArr[1], sArr[2]);//
                        if (speedRadius > 90) speedRadius = 100;
                        speedLeft0 = speedRadius;
                        speedRight0 = speedRadius;
                        alpha = CommonFunctions.Degrees(sArr[1], sArr[2]);//

                        if ((sArr[3] == "Start") && (mem2 == "Stop"))
                        {
                            counterFromStopToStart = counterFromStopToStart + 1;
                            if (counterFromStopToStart == 1)
                            {
                                fromStopToStart = true;
                                lastColorArea = "";
                            }
                        }
                        else if ((sArr[3] == "Stop") && (mem2 == "Start"))
                        {
                            fromStopToStart = false;
                            counterFromStopToStart = 0;
                        }
                        else
                        {
                            counterFromStopToStart = 0;
                        }
                        mem2 = sArr[3];

                        if ((sArr[3] == "Start") || (speedRadius > 1))
                        {//Управление мышкой  
                            sArr3Before = "КолесаВращались";

                            if ((70 < alpha && alpha <= 110) && (speedRadius > minAllowableSpeed))
                            {//0 - Только прямо в узкой зоне 
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedLeft = speedLeft0;
                                speedRight = speedRight0;
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
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
                                    PlcControl.WheelsStopSmoothly();
                                    firstEnterInGreenRight = false;
                                }
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
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
                                    PlcControl.WheelsStopSmoothly();
                                    firstEnterInGreenLeft = false;
                                }
                                else
                                {
                                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
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
                                    PlcControl.WheelsStopSmoothly();
                                    firstEnterInYellowLeft = false;
                                    //sArr3Before = "0";
                                }
                                else
                                {
                                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
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
                                    PlcControl.WheelsStopSmoothly();
                                    firstEnterInRed = false;
                                    //sArr3Before = "0";
                                }
                                else
                                {
                                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
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
                                    PlcControl.WheelsStopSmoothly();
                                    firstEnterInYellowRight = false;
                                }
                                else
                                {
                                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
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
                    if (((sArr[5] != "Stop") && (sArr[5] != null) && (sArr[5] != "0")) && (sArr[3] == "Start"))
                    {//Управление клавишами
                        sArr3Before = "КолесаВращались";
                        double xCoord = 0.0;
                        double yCoord = 0.0;
                        try
                        {
                            xCoord = Convert.ToDouble(sArr[1]);
                            yCoord = -Convert.ToDouble(sArr[2]);
                        }
                        catch (Exception e)
                        {
                            Current.NotifyUser("if (((sArr[5] != Stop)" + e.Message + "xCoord = ", NotifyType.StatusMessage);
                        }

                        double turnSpeed = 0.5 * Math.Abs(xCoord);
                        double speedY = Math.Abs(yCoord);
                        speedLeft0 = speedY;//скорость левого колеса
                        speedRight0 = speedY;//скорость правого колеса

                        switch (sArr[5])
                        {//Управление клавишами
                            case "left":
                                directionLeft = backwardDirection;
                                directionRight = forwardDirection;
                                speedRight = turnSpeed;
                                speedLeft = turnSpeed;
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "top":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedLeft0;
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "right":
                                directionLeft = forwardDirection;
                                directionRight = backwardDirection;
                                speedRight = turnSpeed;
                                speedLeft = turnSpeed;
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "topAndLeft":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedRight0 - turnSpeed;
                                if (speedLeft < 0) { speedLeft = 0; }
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "topAndRight":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedLeft0 - turnSpeed;
                                if (speedRight < 0) { speedRight = 0; }
                                speedLeft = speedLeft0;
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "bottom":
                                directionLeft = backwardDirection;
                                directionRight = backwardDirection;
                                speedRight = speedRight0;
                                speedLeft = speedLeft0;
                                PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                        }
                    }
                    ///////////////////////////////////////////
                    if (sArr[3] == "Stop") // !=Start - нельзя, т.к. с сервера идет поток нулей, который будет останавливать колеса
                    {
                        if (sArr3Before == "КолесаВращались")
                        {
                            if ((CommonStruct.stopSmoothly == true) && (((directionLeft == forwardDirection) && (directionRight == forwardDirection)) || ((directionLeft == backwardDirection) && (directionRight == backwardDirection))))
                            //if (CommonStruct.stopSmoothly == true)
                                {
                                PlcControl.WheelsStopSmoothly();
                                sArr3Before = "0";
                            }
                            else
                            {
                                PlcControl.WheelsStopSmoothly();
                                sArr3Before = "0";
                            }
                        }

                    //await SendVoltageLevelToServer();
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
            {
                pin26.Write(GpioPinValue.Low);
            }
        }

        private void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
        }

        bool isEntireMessage = true;//признак того 
        int iNumberOfMessage = 0;
        int iNubmerOfMessageBefore = 0;
        int timeNow = 0;
        int timeBefore = 0;
        int iArrayCounter = 0;//Нужно чтобы отличить первый массив от последующих

        private void Client_MqttMsgPublishReceivedAsync(object sender, MqttMsgPublishEventArgs e)
        {
            string topic = e.Topic; byte[] message = e.Message; string result = Encoding.UTF8.GetString(message);
            string s = result; string delim = "\""; s = s.Replace(delim, "");//Строка с разделителями - запятыми
            s = s.Replace(delim, ""); s = s.Replace("[", ""); s = s.Replace("]", ""); char[] separator = new char[1];
            separator[0] = ','; sArr = s.Split(separator, 16);
            iNumberOfMessage = Convert.ToInt32(sArr[14]);
            iNubmerOfMessageBefore = Convert.ToInt32(sArrBefore[14]);
            timeNow = Convert.ToInt32(sArr[15]);
            timeBefore = Convert.ToInt32(sArrBefore[15]);
            if (iArrayCounter == 0) isEntireMessage = true;


            if(sArr[3] == "Stop")
            {

            }

            //Сюда входят сообщения и массивы в них и я анализирую, пропускать их в Polling или нет 
            if (iNumberOfMessage >= iNubmerOfMessageBefore)
            {//Здесь убираем перепутывание массивов между разными сообщениями
                if (isEntireMessage == true)
                {//Знание, что все массивы из одного сообщения, нужно, чтобы начать фильтровать по временным меткам - они начинаются в каждом сообщении с нуля.
                    if ((timeNow > timeBefore) || (iArrayCounter == 0))
                    {//iArrayCounter == 0 нуужно потому, что в первом сообщении может быть timeNow =0 и тогда условие ">" не выполняется
                        if ((sArr[3] == "Start") || (sArr[5] == "Start") || (sArr[4] != "Stop"))
                        {//фильтруем по временным меткам
                            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                watchdogTimer.Stop();
                                watchdogTimer.Start();
                            });

                            iArrayCounter++;
                            sArrBefore = sArr;
                            Polling();
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
            InitialConditions();
        }

        private void InitialConditions()
        {
            isEntireMessage = false;
            timeNow = 0;
            timeBefore = 0;
            iArrayCounter = 0;
            sArr[3] = "Stop";
            sArr[5] = "Stop";
            sArrBefore[3] = "Stop";
            sArrBefore[5] = "Stop";
            sArrBefore[15] = "0";
            Polling();
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                watchdogTimer.Stop();
            });
        }

        public static async Task SendVoltageLevelToServer()
        {
            string ipAddress = CommonStruct.defaultWebSiteAddress + ":8080";

            string chargeLevel = CommonStruct.voltageLevelFromRobot;
            Uri uri = new Uri(ipAddress + "/datafromrobot?data=" + chargeLevel + "&serial=" + CommonStruct.decriptedSerial);

            try
            {
                var authData = string.Format("{0}:{1}", "admin", "admin");
                var authHeaderValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authData));

                using (HttpClient client = new HttpClient())
                {
                    client.MaxResponseContentBufferSize = 256000;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("None", authHeaderValue);
                    HttpContent content = null;

                    using (HttpResponseMessage response = await client.PutAsync(uri, content))
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
                Current.NotifyUser(ex.Message, NotifyType.ErrorMessage);
            }
        }

        #endregion Base Cycle

        #region Buttons

        private async void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            RequestExtendedExecution();

            AllControlIsEnabled(false);
            LeftGroup.Visibility = Visibility.Visible;
            b = true;
            c = null;

            buttonStop.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            buttonStop.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            buttonStop.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStop.FontSize = 20;
            buttonStop.IsEnabled = true;

            buttonStart.Background = new SolidColorBrush(Windows.UI.Colors.LightGray); ;
            buttonStart.Foreground = new SolidColorBrush(Windows.UI.Colors.Gray);
            buttonStart.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStart.IsEnabled = false;
            if (CommonStruct.cameraController != "No") PlcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
            await Task.Run(() =>
            {
                //Polling();
                //client.Connect(clientId);//Может S/N вместо этого взять?
                //client.Subscribe(new string[] { "190X-7620-35H8-9OW8-DAR4" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                //client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
                //client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

            });

            try
            {
                client.Connect(clientId);//Может S/N вместо этого взять?
                client.Subscribe(new string[] { CommonStruct.decriptedSerial }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
                client.ConnectionClosed += Client_ConnectionClosed;
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceivedAsync;
                client.MqttMsgUnsubscribed += Client_MqttMsgUnsubscribed;
            }
            catch (Exception e1)
            {
                Current.NotifyUserFromOtherThread(e1.Message, NotifyType.StatusMessage);
            }
        }

        private void Client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            Current.NotifyUserFromOtherThread("Unsubscibed", NotifyType.StatusMessage);
        }

        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            Current.NotifyUserFromOtherThread("Connection Closed", NotifyType.StatusMessage);
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
                MainPage.Current.NotifyUserFromOtherThread(e1.Message, NotifyType.StatusMessage);
            }


            b = false;
            c = "stop";

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
            //rw = new ReadWriteClass();

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

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonShutdown_Click(object sender, RoutedEventArgs e)
        {
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
        }

        private void buttonRestart_Click(object sender, RoutedEventArgs e)
        {
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(0));
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
