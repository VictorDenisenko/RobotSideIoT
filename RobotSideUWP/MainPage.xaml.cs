using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
            //TextBoxRealVoltage_TextChanged(null, null);
            Current.ChargeLevelMeasure();
        }
    }

    public class Scenario
    {
        public Type ClassType { get; set; }
    }

    public class DataFromClient
    {
        public string serialFromClient, wheelsStartStop, camera, dir, comments;
        public short x, y;
        public int packageNumber;
        public string deltaTime;
        public bool isThisData;
    }

    public class DataFromRobot
    {
        public string serialFromClient = "0";
        public string wheelsStartStop = "0";
        public string camera = "0";
        public string dir = "0";
        public string comments = "0";
        public short x = 0;
        public short y = 0;
        public int packageNumber = 0;
        public string deltaTime = "0";
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
        private static bool isConnected = false;
        DispatcherTimer pingTimer;
        //DataFromRobot dataToSend = new DataFromRobot();
        public DataFromClient receivedData = new DataFromClient();
        DispatcherTimer pongTimer;
        public string testString = "";

        DateTime now1;
        string timeNow2;
        string timeNow1;
        DateTime now2;
        long ticksSent;
        GpioPin pin13;//Вход, подключенный к клеммам зарядного устройства

        int dockingTurnsNumber = 0;
        int dockingTurnsNumberLocal = 0;
        DispatcherTimer robotTurningTimer;
        DispatcherTimer obstacleTimer;

        GpioPin pin17;// Правый датчик препятствия
        GpioPinValue val17Right = GpioPinValue.High;
        GpioPin pin18;// Левый датчик препятствия
        GpioPinValue val18Left = GpioPinValue.High;
        bool ObstacleAvoidanceIs = true;

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
            textBoxRealVoltage.TextChanged += TextBoxRealVoltage_TextChanged;
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
            }

            pingTimer = new DispatcherTimer();
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Interval = new TimeSpan(0, 0, 0, 20, 0); //Таймер для реконнекта к серверу
            pingTimer.Start();

            pongTimer = new DispatcherTimer();
            pongTimer.Tick += PongTimer_Tick;
            pongTimer.Interval = new TimeSpan(0, 0, 0, 10, 0); //Таймер для приема ответа сервера pong

            robotTurningTimer = new DispatcherTimer();
            robotTurningTimer.Interval = new TimeSpan(0, 0, 1);//Таймер для подталкивания робота при поиске док-станции
            robotTurningTimer.Tick += RobotTurningTimer_Tick;

            pin17 = GpioController.GetDefault().OpenPin(17);//Это правый датчик столкновений
            pin17.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin17.SetDriveMode(GpioPinDriveMode.Input);
            pin17.ValueChanged += Pin17_ValueChanged;

            pin18 = GpioController.GetDefault().OpenPin(18);//Это правый датчик столкновений
            pin18.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 5);//Если таймаут большой, то событие может вообще не наступить
            pin18.SetDriveMode(GpioPinDriveMode.Input);
            pin18.ValueChanged += Pin18_ValueChanged;

            obstacleTimer = new DispatcherTimer();//Таймер для датчиков препятствий
            obstacleTimer.Tick += ObstacleTimer_Tick;
            obstacleTimer.Interval = new TimeSpan(0, 0, 4);//
            
        }

        private void ObstacleTimer_Tick(object sender, object e)
        {
            CommonStruct.rightObstacle = false;
            CommonStruct.leftObstacle = false;
            obstacleTimer.Stop();
        }

        private void Pin17_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {//
            if (ObstacleAvoidanceIs == true)
            {//firstTimeObstacle = флаг запрета на повторные отправления сообщений, снимаtncz после того как пользователь опять нажмет Go 
                if (CommonStruct.firstTimeObstacle == true)
                {
                    val17Right = pin17.Read();
                    if (val17Right == GpioPinValue.Low)
                    {
                        
                        CommonStruct.rightObstacle = true;
                        CommonStruct.wheelsIsStopped = true;
                        //plcControl.WheelsStopSmoothly(50);
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
        {
            if (ObstacleAvoidanceIs == true)
            {
                if (CommonStruct.firstTimeObstacle == true)
                {
                    val18Left = pin18.Read();
                    if (val18Left == GpioPinValue.Low)
                    {
                        CommonStruct.leftObstacle = true;
                        CommonStruct.wheelsIsStopped = true;
                        //plcControl.WheelsStopSmoothly(50);
                        plcControl.WheelsStop();
                        SendComments("Obstacle on the left", "client");
                        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            obstacleTimer.Start();
                        });
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
                SendComments("Charging...");
            }
            else if (args.Edge == GpioPinEdge.RisingEdge)
            {//Выходит из дока
                GpioPinValue val13 = pin13.Read();
                CommonStruct.dockingCounter = 0;
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

        private void RobotTurningTimer_Tick(object sender, object e)
        {
            CommonStruct.autoDockingStarted = "yes";
            dockingTurnsNumber++;
            if ((CommonStruct.dockIsFound == "no") && (dockingTurnsNumber < 5))
            { 
                TurnLeft(20);
                //robotTurningTimer.Start();
            }
            else if(dockingTurnsNumber == 5)
            {
                plcControl.WheelsStop();
            }
            else if ((CommonStruct.dockIsFound == "no") && (dockingTurnsNumber > 5) && (dockingTurnsNumber < 15))
            {
                TurnRight(20);
                //robotTurningTimer.Start();
            }
            else if (CommonStruct.dockIsFound == "yes") 
            {//Если станция найдена, то переменная CommonStruct.dockIsFound = "yes" 
                if(dockingTurnsNumberLocal == 0)
                {
                    plcControl.WheelsStop();
                }
                else if (dockingTurnsNumberLocal >10)
                {
                    plcControl.WheelsStop();
                    robotTurningTimer.Stop();
                }
                else if(dockingTurnsNumberLocal < 10)
                    GoDirect(30);
                if (CommonStruct.IsChargingCondition == true)
                {
                    plcControl.WheelsStop();
                    robotTurningTimer.Stop();
                }
                dockingTurnsNumberLocal++;
            }
            else
            {
                plcControl.WheelsStop();
                robotTurningTimer.Stop();
            }
        }

        private void TurnLeft(double speed)
        {
            directionLeft = backwardDirection;
            directionRight = forwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            plcControl.Wheels(directionLeft, speed, directionRight, speed);
        }

        private void TurnRight(double speed)
        {
            directionLeft = forwardDirection;
            directionRight = backwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            plcControl.Wheels(directionLeft, speed, directionRight, speed);
        }

        private void GoDirect(double speed)
        {
            directionLeft = forwardDirection;
            directionRight = directionLeft;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            plcControl.Wheels(directionLeft, speed, directionRight, speed);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //Это может быть надо ввести в файл app.xaml.cs, private void OnSuspending(object sender, SuspendingEventArgs e)
            CloseSocket();
        }

        private void PingTimer_Tick(object sender, object e)
        {
            try
            {
                if (CommonStruct.wheelsIsStopped == true)
                {
                    DataFromRobot dataToSend = new DataFromRobot();
                    dataToSend.comments = "ping1";//Это пинг до сервера, а есть еше pong от клиента
                    dataToSend.isThisData = false;
                    dataToSend.toWhom = "client";
                    pongTimer.Start();
                    isConnected = false;
                    _ = SendData(dataToSend);
                    now1 = DateTime.Now;
                    timeNow1 = now1.ToString();
                    ticksSent = now1.Ticks;
                }
            }
            catch (Exception)
            { }
            if (readWrite.serialPort == null) readWrite.comPortInit();
        }

        private void PongTimer_Tick(object sender, object e)
        {//Событие появляется через 10 с после старта пинга
            if (isConnected == false)
            {
                try{
                    pin26.SetDriveMode(GpioPinDriveMode.Output);
                    pin26.Write(GpioPinValue.Low);//pin26 - Зеленый светодиод выключен

                    var x = receivedData.comments;
                    Connect();
                    pongTimer.Stop();
                    NotifyUser("Server is disconnected", NotifyType.StatusMessage);
                    NotifyUserForTesting("Server is disconnected " + x);
                }
                catch (Exception)
                { }
            }
            else
            {
                pongTimer.Stop();
                pin26.SetDriveMode(GpioPinDriveMode.Output);
                pin26.Write(GpioPinValue.High);//pin26 - Зеленый светодиод включен
            }
        }

        private void Connect()
        {
            messageWebSocket = new MessageWebSocket();
            messageWebSocket.Control.MessageType = SocketMessageType.Utf8;
            var serialBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(CommonStruct.robotSerial));
            messageWebSocket.SetRequestHeader("serial", serialBase64);//В заголовок добавил SN
            messageWebSocket.MessageReceived += MessageReceived;
            messageWebSocket.Closed += OnClosed;
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
            
            try{
                Task connectTask = Task.Run(() => {
                    _ = messageWebSocket.ConnectAsync(uriServerAddress);
                });
            }
            catch (Exception ex) // For debugging
            {
                messageWebSocket.Dispose();
                messageWebSocket = null;
                return;
            }
            messageWriter = new DataWriter(messageWebSocket.OutputStream);
            NotifyUser("Connected", NotifyType.StatusMessage);
        }

        private async Task SendMessageUsingMessageWebSocketAsync(string message)
        {
            try{
                if (messageWebSocket != null)
                {
                    using (var dataWriter = new DataWriter(messageWebSocket.OutputStream))
                    {
                        dataWriter.WriteString(message);
                        await dataWriter.StoreAsync();
                        dataWriter.DetachStream();
                    }
                }
            }
            catch (Exception e)
            {}
        }

        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            string read = null;
            try{
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try{
                        using (DataReader reader = args.GetDataReader())
                        {
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
                                sArr[15] = receivedData.deltaTime;

                                now2 = DateTime.Now;
                                timeNow2 = now2.ToString();
                                var ticksNow = now2.Ticks;//Один такт - 100 нс.10 мс = 100000 тактов
                                long deltaTicks = (ticksNow - ticksSent) / 10000; //Получаем в мс

                                DateTime now3 = DateTime.Now;
                                string timeNow3 = now3.ToString();

                                testString = testString + deltaTicks + "   " + receivedData.comments + "\r";

                                NotifyUserForTesting(testString);
                                if (testString.Length > 300) testString = "";

                                if (receivedData.comments.Contains("pong"))
                                {//pong1 - это от срервера, а pong - от клиента (от браузера)
                                    isConnected = true;
                                    pin26.Write(GpioPinValue.High);//pin26 - Зеленый светодиод включен
                                }
                                else if(receivedData.comments.Contains("autodocking"))
                                {
                                    sArr[1] = "0";
                                    sArr[2] = "0";
                                    if (CommonStruct.IsChargingCondition == false)
                                    {
                                        robotTurningTimer.Start();
                                        CommonStruct.dockIsFound = "no";
                                        CommonStruct.autoDockingStarted = "yes";
                                        dockingTurnsNumber = 0;
                                        dockingTurnsNumberLocal = 0;
                                    }
                                }
                                else if (receivedData.comments.Contains("obstacleAvoidanceIs"))
                                {
                                    ObstacleAvoidanceIs = true;
                                }
                                else if (receivedData.comments.Contains("obstacleAvoidanceNo"))
                                {
                                    ObstacleAvoidanceIs = false;
                                }

                                    if (read != null)
                                {
                                    isConnected = true;
                                    pin26.Write(GpioPinValue.High);//pin26 - Зеленый светодиод включен
                                }
                                else
                                {
                                    isConnected = false;
                                    pin26.Write(GpioPinValue.Low);//pin26 - Зеленый светодиод включен
                                }
                                Current.NotifyUser(receivedData.comments, NotifyType.StatusMessage);
                                //receivedData.comments = "";

                                if ((plcControl.stopTimerCounter == 0) && (receivedData.isThisData == true))
                                {
                                    Polling(sArr);
                                    //__SendReceiveAsync();
                                }
                                ////////////
                            }
                            catch (Exception ex)
                            {}
                        }
                    }
                    catch (Exception e)
                    {}
                });
            }
            catch (Exception e)
            {}
        }

        private async void OnClosed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (messageWebSocket == sender)
                {
                    CloseSocket();
                }
            });
            NotifyUser("Closed; Code: " + args.Code + ", Reason: " + args.Reason, NotifyType.StatusMessage);
        }

        private void CloseSocket()
        {
            if (messageWriter != null)
            {
                messageWriter.DetachStream();
                messageWriter.Dispose();
                messageWriter = null;
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

        private async Task SendData(DataFromRobot dataToSend)
        {
            string json = JsonConvert.SerializeObject(dataToSend);
            await SendMessageUsingMessageWebSocketAsync(json);
        }

        private async Task SendData(DataFromRobot dataToSend, string toWhom)
        {
            dataToSend.toWhom = "toWhom";
            string json = JsonConvert.SerializeObject(dataToSend);
            await SendMessageUsingMessageWebSocketAsync(json);
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
                        ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
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
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));
            }
        }

        private void TextBoxRealVoltage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxRealVoltage.Text != "") {
                CommonStruct.VReal = Convert.ToDouble(textBoxRealVoltage.Text);
                CommonStruct.textBoxRealVoltageChanged = true;
                localContainer.Containers["settings"].Values["VReal"] = CommonStruct.VReal.ToString();
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
                        labelChargeLevel.Text = CommonStruct.voltageLevelFromRobot + "%";
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

        private void __SendReceiveAsync()
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

        public void SendComments(string text)
        {
            try
            {
                DataFromRobot dataToSend = new DataFromRobot();
                dataToSend.isThisData = false;dataToSend.camera = "";dataToSend.deltaTime = "";dataToSend.dir = "";
                dataToSend.packageNumber = 0;dataToSend.serialFromClient = "";dataToSend.wheelsStartStop = "";
                dataToSend.x = 0;dataToSend.y = 0;
                dataToSend.comments = text;
                dataToSend.toWhom = "client";
                dataToSend.isThisData = false;
                _ = SendData(dataToSend);
            }
            catch (Exception ex)
            {
                Current.NotifyUser("SendVoltageToServer() " + ex.Message, NotifyType.ErrorMessage);
            }
        }

        public void SendComments(string text, string toWhom)
        {
            try
            {
                DataFromRobot dataToSend = new DataFromRobot();
                dataToSend.isThisData = false; dataToSend.camera = ""; dataToSend.deltaTime = ""; dataToSend.dir = "";
                dataToSend.packageNumber = 0; dataToSend.serialFromClient = ""; dataToSend.wheelsStartStop = "";
                dataToSend.x = 0; dataToSend.y = 0;
                dataToSend.comments = text;
                dataToSend.toWhom = toWhom;
                dataToSend.isThisData = false;
                _ = SendData(dataToSend);
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

