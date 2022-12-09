using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Devices.Gpio;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

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
            Current.NotifyUser("RealLab! , see https://boteyes.com ", NotifyType.StatusMessage);
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
            if (textBoxRealVoltage.Text != "")
            {
                CommonStruct.VReal = Convert.ToDouble(textBoxRealVoltage.Text);
                CommonStruct.textBoxRealVoltageChanged = true;
                localContainer.Containers["settings"].Values["VReal"] = CommonStruct.VReal.ToString();
                Current.ChargeLevelMeasure();
            }
        }
    }

    public class Scenario
    {
        public Type ClassType { get; set; }
    }

   
    public class DataFromClient
    {
        public string serialFromClient = "0";
        public string wheelsStartStop = "0";
        public string camera = "0";
        public string dir = "0";
        public string comments = "";
        public short x = 0;
        public short y = 0;
        public int packageNumber = 0;
        public int deltaTime = 0;
        public bool isThisData = true;
        public int distance = 500;
        public int alpha = 0;
        public int speed = 0;
        public double tabletAngle = 0.0;
        public double turningAngle = 0.0;
    }

    public class DataFromRobot
    {
        public string serialFromClient = "0";
        public string wheelsStartStop = "0";
        public string camera = "0";
        public string dir = "0";
        public string comments = "";
        public short x = 0;
        public short y = 0;
        public int packageNumber = 0;
        public int deltaTime = 0;
        public bool isThisData = true;
        public string toWhom = "client";
    }

    public sealed partial class MainPage : Page
    {
        public static ReadWrite readWrite = null;
        PlcControl plcControl = null;
        public static MainPage Current;
        //bool bConnect = true;
        private string forwardDirection = "0";
        private string backwardDirection = "1";
        string[] dataFromRobot = new string[16] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" }; //данные из робота  

        //private ObservableCollection<DeviceInformation> listOfDevices;
        private ExtendedExecutionSession session = null;
        GpioPin pin26;//Зеленый светодиод

        private string[] sArr = new string[16] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
        //private string[] arrInitial = new string[16] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
        private string[] arrBefore = new string[16];
        string clientId = "";

        /// /////////////
        string sArr3Before = "";
        string directionLeft; //направление вращения левого колеса
        string directionRight;//направление вращения правого колеса
        double speedLeft, speedRight, speedLeft0 = 0, speedRight0 = 0;
        //double speedTuningParam = CommonStruct.speedTuningParam;
        double alpha = 0.0;
        //string wheelsAddress = CommonStruct.wheelsAddress;
        //string cameraAddress = CommonStruct.cameraAddress;
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
        string cameraIsStoppedForDocking = "no";
        DispatcherTimer watchdogTimer;
        static string oldText = "";
        GpioPin pin6;//Это пин, на который подается сигнал от кнопки включения-выключения. При нажатии на кнопку нпряжение на нем поднимается от 0,9В до 3 В.
        GpioPinValue val6 = GpioPinValue.Low;
        public GpioPin pin5;//Выкл - подача напряжения на аппаратный таймер выключения питания
        public ApplicationDataContainer localContainer;
        ////////////
        private MessageWebSocket messageWebSocket;
        private DataWriter messageWriter;
        private string serverAddress = "";
        private Uri uriServerAddress = null;
        private static bool isConnected = true;
        DispatcherTimer pingTimer;
        //DataFromRobot dataToSend = new DataFromRobot();
        public DataFromClient receivedData = new DataFromClient();
        //DispatcherTimer pongTimer;
        public string testString = "";

        DateTime now1;
        string timeNow2;
        string timeNow1;
        DateTime now2;
        long ticksSent;
        GpioPin pin13;//Вход, подключенный к клеммам зарядного устройства

        DispatcherTimer robotTurningTimer;
        DispatcherTimer robotGoTimer;
        DispatcherTimer obstacleTimer;
        DispatcherTimer tabletPositioningTimer;

        GpioPin pin17;// Правый датчик препятствия
        GpioPinValue val17Right = GpioPinValue.High;
        GpioPin pin18;// Левый датчик препятствия
        GpioPinValue val18Left = GpioPinValue.High;
        GpioPin pin19;// Front датчик препятствия
        GpioPinValue val19Front = GpioPinValue.High;
        GpioPin pin27;// Rear датчик препятствия
        GpioPinValue val27Rear = GpioPinValue.High;
        double deltaTimeTurning = 10;
        long autodockingLimitCounter = 0;
        bool stopDockingFlag = false;

        public MainPage()
        {
            clientId = Guid.NewGuid().ToString();
            InitializeComponent();
            //listOfDevices = new ObservableCollection<DeviceInformation>();
            localContainer = ApplicationData.Current.LocalSettings;
            //localContainer.DeleteContainer("settings");
            if (localContainer.Containers.ContainsKey("settings"))
            {
                ReadAllSettings();
            }
            else
            {
                ApplicationDataContainer localSettings = localContainer.CreateContainer("settings", ApplicationDataCreateDisposition.Always);
                WriteDefaultSettings();
                ReadAllSettings();
            }

            InitializeUI();
            Current = this;
            InitializeRobot();

            //Калибровка измерителя напряжения на аккумуляторе
            textBoxRealVoltage.Text = CommonStruct.VReal.ToString();
            textBoxRobotSerial.TextChanged += TextBoxRobotSerial_TextChanged;

            Task.Delay(1000).Wait();//Да, проверил, именно в этом месте программа останавливает свое выполнение на 1 сек

            ScenarioControl.ItemsSource = scenarios;
            if (Window.Current.Bounds.Width < 640)
            {
                ScenarioControl.SelectedIndex = -1;
            }
            else
            {
                ScenarioControl.SelectedIndex = 0;
            }

            pin26 = GpioController.GetDefault().OpenPin(26);
            pin26.SetDriveMode(GpioPinDriveMode.Output);
            pin26.Write(GpioPinValue.Low);// Latch HIGH value first. This ensures a default value when the pin is set as output

            //checkBoxOnlyLocal.Visibility = Visibility.Collapsed;
            //buttonExit.Visibility = Visibility.Collapsed;

            CommonStruct.robotSerial = textBoxRobotSerial.Text;
            dataFromRobot[0] = CommonStruct.robotSerial;
            dataFromRobot[1] = "";
            dataFromRobot[6] = CommonStruct.speedTuningParam.ToString();
            directionLeft = backwardDirection; //направление вращения левого колеса
            directionRight = backwardDirection;//направление вращения правого колеса
            CommonStruct.wheelsWasStopped = true;

            watchdogTimer = new DispatcherTimer();
            watchdogTimer.Tick += WatchdogTimer_Tick;
            watchdogTimer.Interval = new TimeSpan(0, 0, 0, 1, 200); //Ватчдог таймер (дни, часы, мин, сек, ms)

            //Этот таймер должен давать два тика до того, как отошлется очередная команда плавной остановки (там 200 мс) 
            hostWatchdogInitTimer = new DispatcherTimer();
            hostWatchdogInitTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);//таймер для начальной установка сторожевых таймеров в модулях
            hostWatchdogInitTimer.Tick += HostWatchdogInitTimer_Tick;
            hostWatchdogInitTimer.Start();

            readWrite = new ReadWrite();
            //Task.Delay(1000).Wait();
            plcControl = new PlcControl();

            pin6 = GpioController.GetDefault().OpenPin(6);//Это пин, на который подается сигнал от кнопки включения-выключения. При нажатии на кнопку нпряжение на нем поднимается от 0,9В до 3 В.
            pin6.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin6.SetDriveMode(GpioPinDriveMode.Input);
            pin6.ValueChanged += Pin6_ValueChanged;
            val6 = pin6.Read();

            pin5 = GpioController.GetDefault().OpenPin(5);//Аппаратный таймер выключения робота (Севера) запускается
            pin5.SetDriveMode(GpioPinDriveMode.Output);
            pin5.Write(GpioPinValue.High);// 

            pin13 = GpioController.GetDefault().OpenPin(13);//Подключено ли зарядное устроство 
            pin13.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Большой таймаут приводит к тому, что событие вообще не появляется
            pin13.SetDriveMode(GpioPinDriveMode.Input);
            pin13.ValueChanged += Pin13_ValueChanged;
            GpioPinValue val13 = pin13.Read();
            if(val13 == GpioPinValue.Low)
            {
                CommonStruct.IsChargingCondition = true;
                SendComments("Charging...");
                autodockingLimitCounter = 0;
            }

            pingTimer = new DispatcherTimer();
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Interval = new TimeSpan(0, 0, 0, 20, 0); //Таймер для реконнекта к серверу
            pingTimer.Start();

            //pongTimer = new DispatcherTimer();
            //pongTimer.Tick += PongTimer_Tick;
            //pongTimer.Interval = new TimeSpan(0, 0, 0, 10, 0); //Таймер для приема ответа сервера pong

            robotTurningTimer = new DispatcherTimer();
            robotTurningTimer.Tick += RobotTurningTimer_Tick;

            robotGoTimer = new DispatcherTimer();
            robotGoTimer.Tick += RobotGoTimer_Tick;


            pin17 = GpioController.GetDefault().OpenPin(17);//Это правый датчик столкновений
            pin17.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin17.SetDriveMode(GpioPinDriveMode.Input);
            pin17.ValueChanged += Pin17_ValueChanged;

            pin18 = GpioController.GetDefault().OpenPin(18);//Это правый датчик столкновений
            pin18.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin18.SetDriveMode(GpioPinDriveMode.Input);
            pin18.ValueChanged += Pin18_ValueChanged;

            pin19 = GpioController.GetDefault().OpenPin(19);//Это правый датчик столкновений
            pin19.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin19.SetDriveMode(GpioPinDriveMode.Input);
            pin19.ValueChanged += Pin19_ValueChanged;

            pin27 = GpioController.GetDefault().OpenPin(27);//Это правый датчик столкновений
            pin27.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin27.SetDriveMode(GpioPinDriveMode.Input);
            pin27.ValueChanged += Pin27_ValueChanged;

            obstacleTimer = new DispatcherTimer();//Таймер для датчиков препятствий
            obstacleTimer.Tick += ObstacleTimer_Tick;
            obstacleTimer.Interval = new TimeSpan(0, 0, 3);//

            tabletPositioningTimer = new DispatcherTimer();//Таймер для наклона планшета перед автодокингом
            tabletPositioningTimer.Tick += TabletPositioningTimer_Tick;
            tabletPositioningTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
        }

        private void TabletPositioningTimer_Tick(object sender, object e)
        {
            plcControl.CameraStop();
            tabletPositioningTimer.Stop();
            if(cameraIsStoppedForDocking == "no")
            {
                SendComments("tabletPositioning", "tablet");
            }
            else
            {
                var x = "";
            }
        }

        private void ObstacleTimer_Tick(object sender, object e)
        {
            CommonStruct.rightObstacle = false;
            CommonStruct.leftObstacle = false;
            CommonStruct.frontObstacle = false;
            CommonStruct.rearObstacle = false;
            CommonStruct.wheelsIsStopped = false;
            obstacleTimer.Stop();
        }

        private void Pin17_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//Правый датчик столкновений
            if (CommonStruct.ObstacleAvoidanceIs == true && CommonStruct.wheelsIsStopped == false && CommonStruct.IsChargingCondition == false)
            {//firstTimeObstacle = флаг запрета на повторные отправления сообщений, снимается после того как пользователь опять нажмет Go 
                if (CommonStruct.firstTimeObstacle == true && (directionLeft == forwardDirection || directionRight == forwardDirection))
                {
                    val17Right = pin17.Read();
                    if (val17Right == GpioPinValue.Low)
                    {
                        CommonStruct.rightObstacle = true;
                        CommonStruct.wheelsIsStopped = true;
                        //plcControl.WheelsStopSmoothly(50);
                        sArr = dataFromRobot;
                        plcControl.WheelsStop();
                        SendComments("Obstacle on the right", "client");
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            obstacleTimer.Start();
                        });
                        CommonStruct.firstTimeObstacle = false;
                    }
                }
            }
        }

        private void Pin18_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//Левый датчик столкновений
            if (CommonStruct.ObstacleAvoidanceIs == true && CommonStruct.wheelsIsStopped == false && CommonStruct.IsChargingCondition == false)
            {
                if (CommonStruct.firstTimeObstacle == true && (directionLeft == forwardDirection || directionRight == forwardDirection))
                {
                    val18Left = pin18.Read();
                    if (val18Left == GpioPinValue.Low)
                    {
                        CommonStruct.leftObstacle = true;
                        CommonStruct.wheelsIsStopped = true;
                        //plcControl.WheelsStopSmoothly(50);
                        sArr = dataFromRobot;
                        plcControl.WheelsStop();
                        SendComments("Obstacle on the left", "client");
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            obstacleTimer.Start();
                        });
                        CommonStruct.firstTimeObstacle = false;
                    }
                }
            }
        }

        private void Pin19_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//Передний датчик столкновений
            if (CommonStruct.ObstacleAvoidanceIs == true && CommonStruct.wheelsIsStopped == false && CommonStruct.IsChargingCondition == false)
            {//firstTimeObstacle = флаг запрета на повторные отправления сообщений, снимается после того как пользователь опять нажмет Go 
                if (CommonStruct.firstTimeObstacle == true && (directionLeft == forwardDirection || directionRight == forwardDirection))
                {
                    val19Front = pin19.Read();
                    if (val19Front == GpioPinValue.Low)
                    {
                        CommonStruct.frontObstacle = true;
                        CommonStruct.wheelsIsStopped = true;
                        sArr = dataFromRobot;
                        plcControl.WheelsStop();
                        SendComments("Obstacle on the front", "client");
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            obstacleTimer.Start();
                        });
                        CommonStruct.firstTimeObstacle = false;
                    }
                }
            }
        }

        private void Pin27_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//Задний датчик столкновений
            if (CommonStruct.ObstacleAvoidanceIs == true && CommonStruct.wheelsIsStopped == false && CommonStruct.IsChargingCondition == false)
            {//firstTimeObstacle = флаг запрета на повторные отправления сообщений, снимается после того как пользователь опять нажмет Go 
                if (CommonStruct.firstTimeObstacle == true && (directionLeft == backwardDirection || directionRight == backwardDirection))
                {
                    val27Rear = pin27.Read();
                    if (val27Rear == GpioPinValue.Low)
                    {
                        CommonStruct.rearObstacle = true;
                        CommonStruct.wheelsIsStopped = true;
                        sArr = dataFromRobot;
                        plcControl.WheelsStop();
                        SendComments("Obstacle on the rear", "client");
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            obstacleTimer.Start();
                        });
                        CommonStruct.firstTimeObstacle = false;
                    }
                }
            }
        }

        private void Pin13_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//Pin13 - определяет, стоит ли робот на зарядке в доке.
            if (args.Edge == GpioPinEdge.FallingEdge)
            {//стал в док
                GpioPinValue val13 = pin13.Read();
                CommonStruct.IsChargingCondition = true;
                CommonStruct.ObstacleAvoidanceIs = false;
                SendComments("Charging...");
                SendComments("charging", "tablet");
            }
            else if (args.Edge == GpioPinEdge.RisingEdge)
            {//Выходит из дока
                GpioPinValue val13 = pin13.Read();
                CommonStruct.dockingCounter = 0;
                CommonStruct.ObstacleAvoidanceIs = Convert.ToBoolean(localContainer.Containers["settings"].Values["ObstacleAvoidanceIs"]);
                CommonStruct.IsChargingCondition = false;
                if (CommonStruct.voltageLevelFromRobot != "")
                {
                    SendComments(CommonStruct.voltageLevelFromRobot + "%");
                }
                else
                {
                    SendComments("");
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {//Это только для класса Page
        }

        private void RobotGoTimer_Tick(object sender, object e)
        {
            if (stopDockingFlag == false && CommonStruct.IsChargingCondition == false)
            {
                SendComments("lookForPosition", "tablet");
            }
            if (receivedData.camera == "goDirectFastStop")
            {
                plcControl.WheelsStop();
            }
            else
            {
                plcControl.WheelsStopSmoothly(50);
            }
            robotGoTimer.Stop();
        }

        private void RobotTurningTimer_Tick(object sender, object e)
        {
            if (stopDockingFlag == false  && CommonStruct.IsChargingCondition == false)
            {
                SendComments("lookForPosition", "tablet");
            }
            plcControl.WheelsStop();
            robotTurningTimer.Stop();
        }

        private void TurnLeft(double speed)
        {
            plcControl.Wheels(backwardDirection, speed, forwardDirection, speed);
        }

        private void TurnRight(double speed)
        {
            plcControl.Wheels(forwardDirection, speed, backwardDirection, speed);
        }

        private void GoDirect(double speed)
        {
            plcControl.Wheels(forwardDirection, speed, forwardDirection, speed);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //Это может быть надо ввести в файл app.xaml.cs, private void OnSuspending(object sender, SuspendingEventArgs e)
            CloseSocket();
        }

        private void PingTimer_Tick(object sender, object e)
        {//!!!Здесь нельзя отключать таймер на время, когда робоа стоит, птому что если во время когда он движется 
            //отключается сервер, то робот уже не поедет.
            try
            {
                if (isConnected == false)
                {
                    pin26.SetDriveMode(GpioPinDriveMode.Output);
                    pin26.Write(GpioPinValue.Low);//pin26 - Зеленый светодиод выключен
                    Connect();
                    //App.Current.Exit();
                    Current.NotifyUser("Server is disconnected", NotifyType.StatusMessage);
                    NotifyUserForTesting("Server is disconnected ");
                    isConnected = true;
                }
                else
                {
                    DataFromRobot dataToSend = new DataFromRobot();
                    dataToSend.comments = "ping";//Это пинг до сервера
                    NotifyUserForTesting("ping");
                    dataToSend.isThisData = false;
                    dataToSend.toWhom = "server";//
                    isConnected = false;
                    SendData(dataToSend);
                    now1 = DateTime.Now;
                    timeNow1 = now1.ToString();
                    ticksSent = now1.Ticks;
                }
            }
            catch (Exception)
            { }
            if (readWrite.serialPort == null) readWrite.comPortInit();
        }

        //private void PongTimer_Tick(object sender, object e)
        //{//Событие появляется через 10 с после старта пинга
        //    if (isConnected == false)
        //    {
        //        try{
        //            pin26.SetDriveMode(GpioPinDriveMode.Output);
        //            pin26.Write(GpioPinValue.Low);//pin26 - Зеленый светодиод выключен

        //            var x = receivedData.comments;
        //            Connect();
        //            //App.Current.Exit();
        //            pongTimer.Stop();
        //            NotifyUser("Server is disconnected", NotifyType.StatusMessage);
        //            NotifyUserForTesting("Server is disconnected " + x);
        //        }
        //        catch (Exception ex)
        //        { }
        //    }
        //    else
        //    {
        //        pongTimer.Stop();
        //        pin26.SetDriveMode(GpioPinDriveMode.Output);
        //        pin26.Write(GpioPinValue.High);//pin26 - Зеленый светодиод включен
        //    }
        //}

        private void Connect()
        {
            try
            {
                if (messageWebSocket != null)
                {
                    CloseSocket();
                    messageWebSocket = new MessageWebSocket();
                }
                else
                {
                    messageWebSocket = new MessageWebSocket();
                }
                messageWebSocket.Control.MessageType = SocketMessageType.Utf8;
                var serialBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(CommonStruct.robotSerial));
                messageWebSocket.SetRequestHeader("serial", serialBase64);//В заголовок добавил SN
                messageWebSocket.MessageReceived += MessageReceived;
                messageWebSocket.Closed += OnClosed;
                //messageWebSocket.ServerCustomValidationRequested += MessageWebSocket_ServerCustomValidationRequested;
                messageWebSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                messageWebSocket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

                if (CommonStruct.defaultWebSiteAddress.Contains("https"))
                {
                    serverAddress = CommonStruct.defaultWebSiteAddress.Replace("https", "wss");
                    uriServerAddress = new Uri(serverAddress + ":443/?robot");
                }
                else
                {
                    serverAddress = CommonStruct.defaultWebSiteAddress.Replace("http", "ws");
                    uriServerAddress = new Uri(serverAddress + ":8080/?robot");
                }
            }
            catch(Exception e)
            {

            }
            try{
                Task connectTask = Task.Run(() => {
                    Windows.Foundation.IAsyncAction x = messageWebSocket.ConnectAsync(uriServerAddress);
                });
                RoundTripTimeStatistics rts = new RoundTripTimeStatistics();
            }
            catch (Exception ex) // For debugging
            {
                messageWebSocket.Dispose();
                messageWebSocket = null;
                return;
            }

            

            messageWriter = new DataWriter(messageWebSocket.OutputStream);
            Current.NotifyUser("Connected", NotifyType.StatusMessage);
        }

        private async Task SendMessageUsingMessageWebSocketAsync(string message)
        {
            string test = message;
            try
            {
                if ((messageWebSocket != null) && (messageWebSocket.Information.LocalAddress != null))
                {
                    messageWriter.WriteString(message);
                    try
                    {
                        uint x = messageWriter.MeasureString("str");
                        uint  xxx = await messageWriter.StoreAsync();// Send the data as one complete message.
                        var xx = "";
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        private void MessageWebSocket_ServerCustomValidationRequested(MessageWebSocket sender, WebSocketServerCustomValidationRequestedEventArgs args)
        {
            args.Reject();
            
            //serverIsValid = true;
        }

        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            string read = null;
            DataReader reader;
            try
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        using (reader = args.GetDataReader())
                        {
                            if (reader == null)
                            {
                                return;
                            }
                            reader.UnicodeEncoding = UnicodeEncoding.Utf8;

                            try
                            {
                                read = reader.ReadString(reader.UnconsumedBufferLength);
                                receivedData = JsonConvert.DeserializeObject<DataFromClient>(read);
                                sArr[0] = receivedData.serialFromClient;
                                sArr[1] = receivedData.x.ToString();
                                sArr[2] = receivedData.y.ToString();
                                sArr[3] = receivedData.wheelsStartStop;
                                sArr[4] = receivedData.camera;
                                sArr[5] = receivedData.dir;
                                sArr[6] = receivedData.comments;
                                sArr[14] = receivedData.packageNumber.ToString();
                            }
                            catch (Exception e3)
                            {
                                var x = e3.Message;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        var x = ex.Message;
                    }

                    if ((CommonStruct.rightObstacle == true) || (CommonStruct.leftObstacle == true) || (CommonStruct.frontObstacle == true) && ((directionLeft == forwardDirection) || (directionRight == forwardDirection)))
                    //if ((CommonStruct.rightObstacle == true) || (CommonStruct.leftObstacle == true) && ((directionLeft == forwardDirection) || (directionRight == forwardDirection)))
                    {
                        sArr[1] = "0";
                        sArr[2] = "0";
                        sArr[3] = "Stop";
                    }
                    else if (CommonStruct.rearObstacle == true && ((directionLeft == backwardDirection) || (directionRight == backwardDirection)))
                    {
                        sArr[1] = "0";
                        sArr[2] = "0";
                        sArr[3] = "Stop";
                    }

                  
                    now2 = DateTime.Now;
                    timeNow2 = now2.ToString();
                    var ticksNow = now2.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов
                    long deltaTicks = (ticksNow - ticksSent) / 10000; //Получаем в мс

                    DateTime now3 = DateTime.Now;
                    string timeNow3 = now3.ToString();

                    testString = testString + deltaTicks + "   " + receivedData.comments + "\r";

                    NotifyUserForTesting(testString);
                    if (testString.Length > 300) testString = "";

                    if (receivedData.comments == null)
                    {
                        return;
                    }

                    if (receivedData.comments.Contains("pong"))
                    {//pong - от сервера
                        isConnected = true;
                        pin26.Write(GpioPinValue.High);//pin26 - Зеленый светодиод включен
                    }
                    else if(receivedData.comments == "dockingInitialization")
                    {
                        SendComments("endDockingInitialization", "tablet");
                    }
                    else if (receivedData.comments == "tabletPositioning")
                    {//0 град - горизонтально, 90 град - вертикально.Далеее - отрицательные числа (<0)
                        tabletPositioningTimer.Interval = new TimeSpan(0, 0, 0, 0, receivedData.deltaTime);
                        tabletPositioningTimer.Start();
                        string direction = "";
                        if (receivedData.camera == "Up")
                        {
                            direction = "1";
                        }
                        else
                        {
                            direction = "0";
                        }
                        plcControl.CameraUpDown(direction, receivedData.speed);//Вниз
                    }
                    else if(receivedData.comments.Contains("autodocking"))
                    {//Auto Docking
                        stopDockingFlag = false;
                        CommonStruct.ObstacleAvoidanceIs = false;
                        autodockingLimitCounter++;
                        if (autodockingLimitCounter > 50)
                        {
                            return;
                        }

                        if (CommonStruct.IsChargingCondition == true)
                        {
                            if (receivedData.camera == "goDirect")
                            {
                                deltaTimeTurning = receivedData.deltaTime;
                                double speed = receivedData.speed;
                                robotGoTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)deltaTimeTurning);
                                robotGoTimer.Start();
                                GoDirect(speed);
                            }
                            return;
                        }
                        if(receivedData.camera == "turnRight")
                        {
                            deltaTimeTurning = receivedData.deltaTime;
                            double speed = receivedData.speed;
                            robotTurningTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)deltaTimeTurning);
                            robotTurningTimer.Start();
                            TurnRight(speed);
                        }
                        if (receivedData.camera == "turnLeft")
                        {
                            deltaTimeTurning = receivedData.deltaTime;
                            double speed = receivedData.speed;
                            robotTurningTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)deltaTimeTurning);
                            robotTurningTimer.Start();
                            TurnLeft(speed);
                        }
                        if (receivedData.camera == "goDirect")
                        {
                            deltaTimeTurning = receivedData.deltaTime;
                            double speed = receivedData.speed;
                            robotGoTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)deltaTimeTurning);
                            robotGoTimer.Start();
                            GoDirect(speed);
                        }
                        if (receivedData.camera == "goDirectFastStop")
                        {
                            deltaTimeTurning = receivedData.deltaTime;
                            double speed = receivedData.speed;
                            robotGoTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)deltaTimeTurning);
                            robotGoTimer.Start();
                            GoDirect(speed);
                        }
                    }
                    else if (receivedData.comments.Contains("stopDocking"))
                    {
                        stopDockingFlag = true;
                        autodockingLimitCounter = 0;
                        CommonStruct.ObstacleAvoidanceIs = true;
                        if (receivedData.camera == "goDirect")
                        {
                            deltaTimeTurning = receivedData.deltaTime;
                            double speed = receivedData.speed;
                            robotGoTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)deltaTimeTurning);
                            robotGoTimer.Start();
                            if (speed == 0.0)
                            {
                                plcControl.WheelsStop();
                            }
                            else
                            {
                                GoDirect(speed);
                            }
                        }
                        
                        return;
                    }
                    else if (receivedData.comments.Contains("stopTurning"))
                    {
                        plcControl.WheelsStop();
                    }
                    else if (receivedData.comments.Contains("obstacleAvoidanceIs"))
                    {
                        CommonStruct.ObstacleAvoidanceIs = true;
                        localContainer.Containers["settings"].Values["ObstacleAvoidanceIs"] = true;
                    }
                    else if (receivedData.comments.Contains("obstacleAvoidanceNo"))
                    {
                        CommonStruct.ObstacleAvoidanceIs = false;
                        localContainer.Containers["settings"].Values["ObstacleAvoidanceIs"] = false;
                    }
                    else
                    {
                        var x = 0;
                    }

                    if (read != null)
                    {//Т.е.п олучение любых данных от сервера говорит о том, что соединение есть. 
                        isConnected = true;
                        pin26.Write(GpioPinValue.High);//pin26 - Зеленый светодиод включен
                    }
                    Current.NotifyUser(receivedData.comments, NotifyType.StatusMessage);
                                
                    if ((plcControl.stopTimerCounter == 0) && (receivedData.isThisData == true))
                    {
                        Polling(sArr);
                    }
                    ////////////
                });
            }
            catch (Exception e)
            {}
        }

        private async void OnClosed(IWebSocket sender, WebSocketClosedEventArgs args)
        {//вызывается событием пришедшим от сервера
            
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (messageWebSocket == sender)
                    {
                        CloseSocket();
                    }
                });
            }
            catch(Exception e1)
            {
            }
            try
            {
                //Current.NotifyUser("Closed; Code: " + args.Code + ", Reason: " + args.Reason, NotifyType.StatusMessage);
            }
            catch(Exception e)
            {
            }
        }

        private void CloseSocket()
        {
            if (messageWriter != null)
            {
                try
                {
                    messageWriter.DetachStream();
                    messageWriter.Dispose();
                    messageWriter = null;
                }
                catch(Exception e)
                {

                }
            }
            if (messageWebSocket != null)
            {
                try{
                    messageWebSocket.Close(1000, "Closed due to user request.");
                }
                catch (Exception ex)
                {
                }
                messageWebSocket = null;
            }
        }

        private void SendData(DataFromRobot dataToSend)
        {
            string json = JsonConvert.SerializeObject(dataToSend);
            _ = SendMessageUsingMessageWebSocketAsync(json);
        }

        private void SendData(DataFromRobot dataToSend, string toWhom)
        {
            dataToSend.toWhom = "toWhom";
            string json = JsonConvert.SerializeObject(dataToSend);
            _ = SendMessageUsingMessageWebSocketAsync(json);
        }

        private void TextBoxRobotSerial_TextChanged(object sender, TextChangedEventArgs e)
        {
            localContainer.Containers["settings"].Values["Serial"] = textBoxRobotSerial.Text;
            CommonStruct.robotSerial = textBoxRobotSerial.Text;
        }

        private void Pin6_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//Это пин, на который подается сигнал от кнопки включения-выключения. При нажатии на кнопку напряжение на нем поднимается от 0,9В до 3 В.
            Current.NotifyUserFromOtherThreadAsync("Shutdown Started", NotifyType.StatusMessage);
            try
            {
                if (args.Edge == GpioPinEdge.RisingEdge)
                {
                    try
                    {
                        SendComments("BotEyes is Off");
                        CommonStruct.permissionToSendToWebServer = false;
                        pin5.Write(GpioPinValue.Low);//Аппаратный таймер выключения запускается нулем
                        CloseSocket();
                        ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        CloseSocket();
                        pin5.Write(GpioPinValue.Low);//Аппаратный таймер выключения запускается нулем
                        ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Current.NotifyUser("Shutdown problem " + e.Message, NotifyType.ErrorMessage);
                pin5.Write(GpioPinValue.Low);//Аппаратный таймер выключения запускается нулем
                CloseSocket();
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));
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
                        textBoxWheelsStop.Text = sArr[3];//wheelsStop
                        textBoxCameraAngle.Text = arr[4];//сameraData
                        textBoxKeys.Text = arr[5];//Управление клавишами
                        textBoxSmileName.Text = receivedData.comments;//
                    });

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
                        if (speedRadius >= 100) speedRadius = 100;
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
                                speedRight = 0.6 * speedRight0;
                                speedLeft = 0.6 * speedLeft0;
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
                        //speedY;//скорость левого колеса
                        //speedY;//скорость правого колеса
                        if (speedY >= 100) speedY = 100;

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
                                speedRight = speedY;
                                speedLeft = speedY;
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
                                speedRight = speedY;
                                speedLeft = speedY - turnSpeed;
                                if (speedLeft < 0) { speedLeft = 0; }
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "topAndRight":
                                directionLeft = forwardDirection;
                                directionRight = forwardDirection;
                                speedRight = speedY - turnSpeed;
                                if (speedRight < 0) { speedRight = 0; }
                                speedLeft = speedY;
                                plcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                                break;
                            case "bottom":
                                directionLeft = backwardDirection;
                                directionRight = backwardDirection;
                                speedRight = 0.8 * speedY;
                                speedLeft = 0.8 * speedY;
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
                    pin26.Write(GpioPinValue.Low);
                }
        }

        public void ChargeLevelMeasure()
        {
            try
            {
                CommonStruct.dataToWrite = "^A3" + CommonStruct.wheelsAddress + "\r";//Формирование команды чтения из АЦП
                readWrite.Write(CommonStruct.dataToWrite);//

                if (CommonStruct.voltageLevelFromRobot == "")
                {// Это если из компорта вернется пустая строка, то ее надо пропустить
                    return;
                }
                else
                {
                    if (CommonStruct.IsChargingCondition == true)
                    {
                        labelChargeLevel.Background = new SolidColorBrush(Windows.UI.Colors.Yellow);
                        labelChargeLevel.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                        labelChargeLevel.Text = "Charging...";
                    }
                    else
                    {
                        labelChargeLevel.Text = 0.01 * CommonStruct.dVoltageCorrected + " V";
                        double levelCeiling = Convert.ToDouble(CommonStruct.voltageLevelFromRobot);
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

        bool isEntireMessage = true;//признак того 
        int iNumberOfMessage = 0;
        int iNubmerOfMessageBefore = 0;
        int timeNow = 0;
        int timeBefore = 0;
        int iArrayCounter = 0;//Нужно чтобы отличить первый массив от последующих

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

        public void SendComments(string text)
        {
            try
            {
                DataFromRobot dataToSend = new DataFromRobot();
                dataToSend.isThisData = false;dataToSend.camera = "";dataToSend.deltaTime = 0;dataToSend.dir = "";
                dataToSend.packageNumber = 0;dataToSend.serialFromClient = "";dataToSend.wheelsStartStop = "";
                dataToSend.x = 0;dataToSend.y = 0;
                dataToSend.comments = text;
                dataToSend.toWhom = "client";
                dataToSend.isThisData = false;
                SendData(dataToSend);
            }
            catch (Exception ex)
            {
                Current.NotifyUser("SendVoltageToServer() " + ex.Message, NotifyType.ErrorMessage);
            }
        }



        public static void SendComments(string text, string toWhom)
        {
            try
            {
                DataFromRobot dataToSend = new DataFromRobot();
                dataToSend.isThisData = false; dataToSend.camera = ""; dataToSend.deltaTime = 0; dataToSend.dir = "";
                dataToSend.packageNumber = 0; dataToSend.serialFromClient = ""; dataToSend.wheelsStartStop = "";
                dataToSend.x = 0; dataToSend.y = 0;
                dataToSend.comments = text;
                dataToSend.toWhom = toWhom;
                dataToSend.isThisData = false;
                Current.SendData(dataToSend);
            }
            catch (Exception ex)
            {
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
            uri = new Uri(ipAddress + "/errorfromrobot?timeNow=" + timeNow + "&data=" + text + "&serial=" + CommonStruct.robotSerial);
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
            //bConnect = true;

            buttonStop.Background = new SolidColorBrush(Windows.UI.Colors.Green);
            buttonStop.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            buttonStop.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStop.FontSize = 20;
            buttonStop.IsEnabled = true;

            buttonStart.Background = new SolidColorBrush(Windows.UI.Colors.LightGray); 
            buttonStart.Foreground = new SolidColorBrush(Windows.UI.Colors.Gray);
            buttonStart.FontFamily = new FontFamily("Microsoft Sans Serif");
            buttonStart.IsEnabled = false;
           
            try
            {
                if (CommonStruct.robotSerial.Length == 24) {
                    Connect();//
                } else {
                    Current.NotifyUserFromOtherThreadAsync("Robot Serial Number is incorrect ", NotifyType.ErrorMessage);
                }
            }
            catch (Exception e1)
            {
                Current.NotifyUserFromOtherThreadAsync(e1.Message, NotifyType.ErrorMessage);
            }
        }

        private void OnDisconnect()
        {
            CloseSocket();
        }

        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            Current.NotifyUserFromOtherThreadAsync("Connection Closed", NotifyType.StatusMessage);
            pin26.SetDriveMode(GpioPinDriveMode.Output);
            pin26.Write(GpioPinValue.Low);
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EndExtendedExecution();
                //client.Disconnect();
            }
            catch(Exception e1)
            {
                Current.NotifyUserFromOtherThreadAsync(e1.Message, NotifyType.ErrorMessage);
            }

            //bConnect = false;

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
            object obj = localContainer.Containers["settings"].Values["Serial"];
            if (obj != null)
            {
                textBoxRobotSerial.Text = obj.ToString();
            }
            else
            {
                textBoxRobotSerial.Text = "";
            }
        }

        private void buttonCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            localContainer.Containers["settings"].Values["Serial"] = textBoxRobotSerial.Text;
            CommonStruct.robotSerial = textBoxRobotSerial.Text;
            if (PopupSettings.IsOpen) { PopupSettings.IsOpen = false; }
        }

        private void buttonSetDefault_Click(object sender, RoutedEventArgs e)
        {
            WriteDefaultSettings();
            localContainer.Containers["settings"].Values["Serial"] = textBoxRobotSerial.Text;
            ReadAllSettings();
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(2));
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            localContainer.Containers["settings"].Values["cameraController"] = CommonStruct.cameraController;
            localContainer.Containers["settings"].Values["PWMSteppingSpeed"] = Convert.ToInt16(textBoxPWMSteppingSpeed.Text);
            CommonStruct.PWMSteppingSpeed = Convert.ToInt16(textBoxPWMSteppingSpeed.Text);
            localContainer.Containers["settings"].Values["minWheelsSpeedForTurning"] = Convert.ToInt16(textBoxMinWheelsSpeedForTurning.Text);
            CommonStruct.minWheelsSpeedForTurning = Convert.ToInt16(textBoxMinWheelsSpeedForTurning.Text);
            localContainer.Containers["settings"].Values["speedTuningParam"] = Convert.ToDouble(textBoxSpeedTuningParam.Text);
            CommonStruct.speedTuningParam = Convert.ToDouble(textBoxSpeedTuningParam.Text);
            localContainer.Containers["settings"].Values["Serial"] = textBoxRobotSerial.Text;
            CommonStruct.robotSerial = textBoxRobotSerial.Text;
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Exit();
        }

        #endregion Buttons
       
        private void checkBoxOnlyLocal_Checked(object sender, RoutedEventArgs e)
        {
            if (checkBoxOnlyLocal.IsChecked == true)
            {
                localContainer.Containers["settings"].Values["onlyLocal"] = true;
                CommonStruct.checkBoxOnlyLocal = true;
            }

        }

        private void checkBoxOnlyLocal_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkBoxOnlyLocal.IsChecked == false)
            {
                localContainer.Containers["settings"].Values["onlyLocal"] = false;
                CommonStruct.checkBoxOnlyLocal = false;
            }
        }

        private void buttonShutdown_Click(object sender, RoutedEventArgs e)
        {//Выключение кнопкой на экране HDMI монитора
            Task t = new Task(() =>
            {
                SendComments("BotEyes is Off");
                CloseSocket();
                CommonStruct.permissionToSendToWebServer = false;
            });
            t.Start();
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));
        }

        private void buttonRestart_Click(object sender, RoutedEventArgs e)
        {
            //ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(0));//Перезагрузка Windows
            CoreApplication.Exit();
        }

        private void ScenarioControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Current.NotifyUser(String.Empty, NotifyType.StatusMessage);
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

