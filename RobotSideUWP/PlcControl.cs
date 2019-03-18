using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System;
using Windows.UI.Xaml;

namespace RobotSideUWP
{
    class PlcControl
		{
        static GpioPin pin6;//Выход для отключения питания 
        static TimeSpan delay = TimeSpan.FromMilliseconds(200);

        public static void CameraGoFast(string direction, int stepNumber)
            {
            try
                {
                    string hexAddress = CommonStruct.cameraAddress;
                    string s = "";
                    string PwrRange = CommonStruct.cameraPwrRange;
                    if (CommonStruct.cameraController == "GM51")
                    {
                        string speed = "002";
                        string _stepNumber = CommonFunctions.ZeroInFrontSet(stepNumber.ToString());
                        ReadWrite.Write("^RO" + CommonStruct.cameraAddress + "5" + "\r");//Установка 1/5 шага. При шаге 1/4 не получается со временем.

                        s = ReadWrite.Read();
                        if (_stepNumber != "000")
                        {
                            HostWatchDog(CommonStruct.cameraAddress, "reset");
                            string command = "^R1" + hexAddress + direction + speed + "000" + PwrRange + "\r";
                            ReadWrite.Write(command);
                            s = ReadWrite.Read();
                            int timeToSleep = 100 * Convert.ToInt32(_stepNumber);
                            HostWatchDog(CommonStruct.cameraAddress, "set");
                        }
                    }
                    else if (CommonStruct.cameraController == "RD31")
                    {
                        double speed = CommonStruct.cameraSpeed;//от 0 до 100.
                        string __speed = CommonFunctions.ZeroInFrontFromDouble(speed);
                        ReadWrite.Write("^R1" + hexAddress + direction + __speed + "000" + "4" + "\r");
                        s = ReadWrite.Read();
                    }
                }   
            catch (Exception e1)
                {
                //CommonFunctions.WriteToLog(e1.Message + " CameraGoFast");
                MainPage.Current.NotifyUserFromOtherThread("CameraGoFast" + e1.Message, NotifyType.StatusMessage);
               
                }
            }

        public static void CameraGoFast(string direction)
            {
            try {
                string hexAddress = CommonStruct.cameraAddress;
                string PwrRange = CommonStruct.cameraPwrRange;
                string s = "";
                if (CommonStruct.cameraController == "GM51")
                    {
                        string speed = CommonStruct.cameraFastSpeed;//"004"  -это предельное значене, ниже не работает
                        //string PwrRange = CommonStruct.cameraPwrRange;
                        ReadWrite.Write("^RO" + CommonStruct.cameraAddress + "4" + "\r");//Установка 1/5 шага
                        s = ReadWrite.Read();
                        string command = "^R1" + hexAddress + direction + speed + "000" + PwrRange + "\r";
                        ReadWrite.Write(command);
                        s = ReadWrite.Read();
                    }
                    else if (CommonStruct.cameraController == "RD31")
                    {
                        double speed = CommonStruct.cameraSpeed;//от 0 до 100.
                        string __speed = CommonFunctions.ZeroInFrontFromDouble(speed);
                        ReadWrite.Write("^R1" + hexAddress + direction + __speed + "000" + "4" + "\r");
                        s = ReadWrite.Read();
                    }
                }
            catch (Exception e1)
                {
                //CommonFunctions.WriteToLog(e1.Message + " CameraGoFast");
                MainPage.Current.NotifyUserFromOtherThread("CameraGoFast" + e1.Message, NotifyType.StatusMessage);
                }
            }

        public static int CameraSpeedToPWM()
            {//Формирователь зависимости скорости камеры от положения движка 
            double maxCameraSpeed = CommonStruct.cameraSpeed;//
            double _outputSpeed = - 0.2 * (maxCameraSpeed - 100) + 1;
            if (maxCameraSpeed < 50)
                {
                _outputSpeed = -4 * (maxCameraSpeed - 50) + _outputSpeed + 5;
                }
            int outputSpeed = Convert.ToInt32(Math.Round(_outputSpeed, 0, MidpointRounding.AwayFromZero));
            return outputSpeed;
            }

        public static void CameraGoDirectFast()
            {
            int stepNumber = 0;
            string direction = "0";
            try
                {
                switch (CommonStruct.cameraPositionBefore)
                    {
                    case "direct": direction = "1"; stepNumber = 0;
                        break;
                    case "top": direction = "0"; stepNumber = 0;
                        break;
                    case "bottom": direction = "1"; stepNumber = CommonStruct.directBottomDistance;
                        break;
                    case "slowUp": direction = "0"; stepNumber = CommonStruct.directBottomDistance;
                        break;
                    case "slowDown": direction = "1"; stepNumber = CommonStruct.directBottomDistance;
                        break;
                    }
                if (stepNumber != 0)
                    {
                    CameraGoFast(direction, stepNumber);
                    HostWatchDog(CommonStruct.cameraAddress, "set");
                    CommonStruct.cameraPositionBefore = "direct";
                    MainPage.Current.localSettings.Values["cameraPositionBefore"] = CommonStruct.cameraPositionBefore;
                }
                }
            catch (Exception e1)
                {
                //CommonFunctions.WriteToLog(e1.Message + " CameraGoDirectFast");
                MainPage.Current.NotifyUserFromOtherThread("CameraGoDirectFast" + e1.Message, NotifyType.StatusMessage);
                }
            }
        
  		public static void HostWatchDog(string address, string setReset)
			{
			string interval = CommonStruct.interval;
            string s = "";
            try
                {
			
				if (setReset == "set")
					{
                    ReadWrite.Write("^RA" + address + interval + "\r");//Остановка через время, заданное в таймере
                    s = ReadWrite.Read();
					}
				else if (setReset == "reset")
					{
					interval = "999";
                    ReadWrite.Write("^RA" + address + interval + "\r");
                    s = ReadWrite.Read();
					}
				CommonStruct.portOpen = true;
				}
			catch (Exception e)
				{
				CommonStruct.portOpen = false;
                //CommonFunctions.WriteToLog(e.Message + " HostWatchDog");
                MainPage.Current.NotifyUserFromOtherThread("HostWatchDog " + e.Message, NotifyType.StatusMessage);
                }
			}

        public static void Wheels(string directionLeft, double _speedLeft, string directionRight, double _speedRight)
        {//Управление мышкой и клавишами, за исключением локального управления с сенсорного экрана
            try
            {
                CommonStruct.numberOfTicksAfterWheelsStop = 0;
                if ((CommonStruct.stopBeforeWas == false) && ((CommonStruct.directionLeft != directionLeft) || (CommonStruct.directionRight != directionRight)))
                {
                    WheelsStopSmoothly();
                }
                else
                { 
                    double speedLeft0 = PlcControl.WheelsSpeedTuning(_speedLeft, _speedRight)[0];
                    double speedRight0 = PlcControl.WheelsSpeedTuning(_speedLeft, _speedRight)[1];

                    string speedLeft = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedLeft0).ToString());
                    string speedRight = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedRight0).ToString());
                    string hexAddress = CommonStruct.wheelsAddress;
                    string PwrRange = CommonStruct.wheelsPwrRange;
                    string commandLeft = directionLeft + speedLeft;
                    string commandRight = directionRight + speedRight;
                    CommonStruct.wheelsWasStopped = false;
                    ReadWrite.Write("^RB" + hexAddress + commandLeft + commandRight + "\r");//Установка скорости и направления для обоих колес
                    string s = ReadWrite.Read();
                    CommonStruct.lastCommandLeft = commandLeft;
                    CommonStruct.lastCommandRight = commandRight;
                    CommonStruct.lastSpeedLeft = _speedLeft;
                    CommonStruct.lastSpeedRight = _speedRight;
                    CommonStruct.directionLeft = directionLeft;
                    CommonStruct.directionRight = directionRight;
                    CommonStruct.stopBeforeWas = false;
                }
               
            }
            catch (Exception e1)
            {
                //CommonFunctions.WriteToLog(e1.Message + " Wheels");
                MainPage.Current.NotifyUserFromOtherThread("Wheels " + e1.Message, NotifyType.StatusMessage);
            }
        }

        public static void WheelsLocal(string directionLeft, double _speedLeft, string directionRight, double _speedRight)
            {//Управление кнопками на экране робота
            try {
                double speedLeft0 = PlcControl.WheelsSpeedTuning(_speedLeft, _speedRight)[0];
                double speedRight0 = PlcControl.WheelsSpeedTuning(_speedLeft, _speedRight)[1];

                string speedLeft = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedLeft0).ToString());
                string speedRight = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedRight0).ToString()); 
                string hexAddress = CommonStruct.wheelsAddress;
                string PwrRange = CommonStruct.wheelsPwrRange;
                string commandLeft = directionLeft + speedLeft;
                string commandRight = directionRight + speedRight;
                ReadWrite.Write("^RB" + hexAddress + commandLeft + commandRight + "\r");
                string s = ReadWrite.Read();
                CommonStruct.stopBeforeWas = false;
            }
            catch (Exception e1)
                {
                //CommonFunctions.WriteToLog(e1.Message + " Wheels");
                MainPage.Current.NotifyUserFromOtherThread("WheelsLocal" + e1.Message, NotifyType.StatusMessage);
                }
            }

        public static void WheelsStop()
            {
                try
                {
                    string hexAddress = CommonStruct.wheelsAddress;
                    ReadWrite.Write("^RC" + hexAddress + "\r");//Общий стоп для всех каналов
                    string s = ReadWrite.Read();
                    CommonStruct.stopBeforeWas = true;
                }
                catch(Exception e)
                {
                    MainPage.Current.NotifyUserFromOtherThread("WheelsStop" + e.Message, NotifyType.StatusMessage);
                }
        }

        public static void WheelsStopSmoothly()
            {
            try
            {
                double speedLeft = CommonStruct.lastSpeedLeft;
                double speedRight = CommonStruct.lastSpeedRight;
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                double k1 = CommonStruct.k1, k2 = CommonStruct.k2, k3 = CommonStruct.k3, k4 = CommonStruct.k4;
                int T = CommonStruct.smoothStopTime;
                int deltaT = Convert.ToInt32(T / 5.0);//Длительность тороможения каждого участка, в миллисекундах 
                if ((speedLeft > 30) && (speedRight > 30))
                {
                    Wheels(directionLeft, k1 * speedLeft, directionRight, k1 * speedRight);
                    Task.Delay(200).Wait();
                    Wheels(directionLeft, k2 * speedLeft, directionRight, k2 * speedRight);
                    Task.Delay(200).Wait();
                    Wheels(directionLeft, k3 * speedLeft, directionRight, k3 * speedRight);
                    Task.Delay(200).Wait();
                    Wheels(directionLeft, k4 * speedLeft, directionRight, k4 * speedRight);
                    Task.Delay(200).Wait();
                    string hexAddress = CommonStruct.wheelsAddress;
                    ReadWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих (Both) колес
                    CommonStruct.wheelsWasStopped = true;
                    string s = ReadWrite.Read();

                }
                else
                {
                    Wheels(directionLeft, speedLeft, directionRight, speedRight);
                    string hexAddress = CommonStruct.wheelsAddress;
                    ReadWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих (Both) колес
                    CommonStruct.wheelsWasStopped = true;
                    string s = ReadWrite.Read();
                }
                CommonStruct.stopBeforeWas = true;
            }
            catch(Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("WheelsStopSmoothly" + e.Message, NotifyType.StatusMessage);
            }
            }

        public static void WheelsStopLocal()
            {
            try
                {
                string hexAddress = CommonStruct.wheelsAddress;
                ReadWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих колес
                Task.Delay(20).Wait();
                string s = ReadWrite.Read();
                CommonStruct.stopBeforeWas = true;
                }
            catch (Exception e1)
                {
                MainPage.Current.NotifyUserFromOtherThread("WheelsStopLocal" + e1.Message + "WheelsStopFromMonitor", NotifyType.ErrorMessage);
                }
            }

        public static void WheelsStopLocalSmoothly()
            {
            try {
                string hexAddress = CommonStruct.wheelsAddress;
                int T = CommonStruct.smoothStopTime;
                int deltaT = Convert.ToInt32(T / 10.0);//Длительность тороможения каждого участка, в миллисекундах 
                double speedLeft = CommonStruct.speedLeftLocal;
                double speedRight = CommonStruct.speedRightLocal; 
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                double k1 = CommonStruct.k1, k2 = CommonStruct.k2, k3 = CommonStruct.k3, k4 = CommonStruct.k4, k5 = 0.0;

                WheelsLocal(directionLeft, k1 * speedLeft, directionRight, k1 * speedRight);
                Task.Delay(200).Wait();
                WheelsLocal(directionLeft, k2 * speedLeft, directionRight, k2 * speedRight);
                Task.Delay(200).Wait();
                WheelsLocal(directionLeft, k3 * speedLeft, directionRight, k3 * speedRight);
                Task.Delay(200).Wait();
                WheelsLocal(directionLeft, k4 * speedLeft, directionRight, k4 * speedRight);
                Task.Delay(300).Wait();
                WheelsLocal(directionLeft, k5 * speedLeft, directionRight, k5 * speedRight);

                ReadWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих колес
                Task.Delay(20).Wait();
                string s = ReadWrite.Read();
                CommonStruct.stopBeforeWas = true;
            }
            catch (Exception e1)
                {
                MainPage.Current.NotifyUserFromOtherThread("WheelsStopLocalSmoothly" + e1.Message + "WheelsStopFromMonitor", NotifyType.ErrorMessage);
                }
            }

		public static void CameraUpDown(string direction)
			{
			try
                {
                    string hexAddress = CommonStruct.cameraAddress;
                    string s = "";
                    string PwrRange = CommonStruct.cameraPwrRange;
                    if (CommonStruct.cameraController == "GM51")
                    {
                        double speed = CameraSpeedToPWM();
                        if (speed <= 2) speed = 2;
                        string __speed = CommonFunctions.ZeroInFrontFromDouble(speed);
                        ReadWrite.Write("^RO" + CommonStruct.cameraAddress + "6" + "\r");//Установка 1/6 шага
                        s = ReadWrite.Read();
                        string command = "^R1" + hexAddress + direction + __speed + "000" + PwrRange + "\r";
                        ReadWrite.Write(command);
                        s = ReadWrite.Read();
                    }
                    else if (CommonStruct.cameraController == "RD31")
                    {
                        double speed = CommonStruct.cameraSpeed;//от 0 до 100.
                        string __speed = CommonFunctions.ZeroInFrontFromDouble(speed);
                        ReadWrite.Write("^R1" + hexAddress + direction + __speed + "000" + "4" + "\r");
                        s = ReadWrite.Read();
                    }
                }
			catch (Exception e1)
				{
                //CommonFunctions.WriteToLog(e1.Message + " CameraUpDown");
                MainPage.Current.NotifyUserFromOtherThread("CameraUpDown" + e1.Message, NotifyType.StatusMessage);
                }
			}

        public static void CameraUpDown(string direction, double speed)
        {
            try
            {
                string hexAddress = CommonStruct.cameraAddress;
                string s = "";
                string PwrRange = CommonStruct.cameraPwrRange;
                if (CommonStruct.cameraController == "GM51")
                {
                    if (speed <= 2) speed = 2;
                    string __speed = CommonFunctions.ZeroInFrontFromDouble(speed);
                    ReadWrite.Write("^RO" + CommonStruct.cameraAddress + "4" + "\r");//Установка 1/6 шага
                    s = ReadWrite.Read();
                    string command = "^R1" + hexAddress + direction + __speed + "000" + PwrRange + "\r";
                    ReadWrite.Write(command);
                    s = ReadWrite.Read();
                }
                else if (CommonStruct.cameraController == "RD31")
                {
                    speed = CommonStruct.cameraSpeed;//от 0 до 100.
                    ReadWrite.Write("^R1" + hexAddress + direction + speed + "000" + "4" + "\r");
                    s = ReadWrite.Read();
                }

            }
            catch(Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("CameraUpDown(string direction, double speed) " + e.Message, NotifyType.StatusMessage);
            }
        }

        public static void CameraStop()
			{//
			try{
                    string hexAddress = CommonStruct.cameraAddress;
                try
                {
                    if (CommonStruct.cameraController == "GM51")
                    {
                        ReadWrite.Write("^RC" + hexAddress + "\r");
                    }
                    else if (CommonStruct.cameraController == "RD31")
                    {
                        double speed = CommonStruct.cameraSpeed;//от 0 до 100.
                        ReadWrite.Write("^RS" + hexAddress + "1" + "\r");
                    }
                }
                catch(Exception )
                {
                }
                try
                {
                    string s = ReadWrite.Read();
                }
                catch (Exception )
                {
                }
				}
			catch (Exception e1)
				{
                //CommonFunctions.WriteToLog(e1.Message + " CameraStop");
                MainPage.Current.NotifyUserFromOtherThread("CameraStop: " + e1.Message, NotifyType.StatusMessage);
                }
			}

        private static double[] WheelsSpeedTuning(double speedLeft, double speedRight)
            {//Общая для всех режимов функция коррекции разброса скоростей колес
            double[] output = new double[2];//output[0] - левое колесо, output[1] - правое
            try
            {
                double outputLeft = 0.0;
                double outputRight = 0.0;
                double delta = 0.0;
                double deltaAdditive = Convert.ToDouble(CommonStruct.speedTuningParam);
                double speed = Math.Max(speedLeft, speedRight);

                if (CommonStruct.wheelsNonlinearTuningIs == false)
                {
                    delta = deltaAdditive;
                }
                else
                {
                    delta = CommonFunctions.WheelsNonlinearTuning(speed) + deltaAdditive;
                }

                if (delta <= 0)
                {
                    outputLeft = speedLeft - Math.Abs(delta);
                    outputRight = speedRight;
                }
                else
                {
                    outputLeft = speedLeft;
                    outputRight = speedRight - Math.Abs(delta);
                }
                output[0] = outputLeft;
                output[1] = outputRight;
                return output;
            }
            catch(Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("WheelsSpeedTuning " + e.Message, NotifyType.StatusMessage);
            }
            return output;
        }

        public static void BatteryVoltageMeasuring()
        {
            try
            {
                CommonStruct.dataToWrite = "^A1" + CommonStruct.wheelsAddress + "\r";//Формирование команды чтения из АЦП
                ReadWrite.Write(CommonStruct.dataToWrite);//Вывод команды чтения из АЦП
                CommonStruct.readData = ReadWrite.Read();//В скобках надо писать количество символов в ответе, включая конец строки, иначе будет отвечать только черезх время таймаута
                string s1 = CommonStruct.readData;

                string data1 = s1.Remove(0, 5);
                data1 = data1.Remove(4);
                if ((data1 == "") || (data1 == null)) { data1 = "0"; }
                //double supplyVoltage = (Convert.ToDouble(data1) / 100.0);
                string voltageRange = s1.Substring(10, 1);
                string flag154 = s1.Substring(12, 1);
                string voltageAveraged = s1.Substring(14, 5);
                if ((voltageAveraged == "") || (voltageAveraged == null)) { voltageAveraged = "0"; }
                double dVoltageAveraged = (Convert.ToDouble(voltageAveraged));

                double deltaV = Convert.ToDouble(MainPage.Current.localSettings.Values["deltaV"]);
                CommonStruct.dVoltageCorrected = dVoltageAveraged + deltaV;


                if (CommonStruct.textBoxRealVoltageChanged == true)
                {
                    deltaV = 1000 * CommonStruct.VReal - dVoltageAveraged;
                    MainPage.Current.localSettings.Values["deltaV"] = deltaV;
                    CommonStruct.dVoltageCorrected = dVoltageAveraged + deltaV;
                    CommonStruct.textBoxRealVoltageChanged = false;
                }


                if (CommonStruct.dVoltageCorrected < 10500)
                {
                    CommonStruct.dVoltageCorrected = 10500;
                    CommonStruct.numberOfTicksAfterWheelsStop = 0;
                    //Посылаем команду "Старт таймера отключения батарей" и однвременно начинаем выгружать Виндовс 
                    pin6 = GpioController.GetDefault().OpenPin(6);
                    pin6.SetDriveMode(GpioPinDriveMode.Output);
                    pin6.Write(GpioPinValue.Low);// Latch HIGH value first. This ensures a default value when the pin is set as output
                                                 //Запускаем таймер чтобы снять низкий уровень с выходв Распберри:
                    DispatcherTimer timerRobotOff;
                    timerRobotOff = new DispatcherTimer();
                    timerRobotOff.Tick += TimerRobotOff_Tick;
                    timerRobotOff.Interval = new TimeSpan(0, 0, 1); //(часы, мин, сек)
                    timerRobotOff.Start();
                }

                double levelCeiling = Math.Ceiling((CommonStruct.dVoltageCorrected - 10500) / 23);
                if (levelCeiling >= 100) levelCeiling = 100;

                CommonStruct.voltageLevelFromRobot = levelCeiling.ToString() + "%";
            }
            catch (Exception e2)
            {
            }
        }

        private static void TimerRobotOff_Tick(object sender, object e)
        {
            try
            {
                pin6.Write(GpioPinValue.High);// Latch HIGH value first. This ensures a default value when the pin is set as output
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));//Выгружаем Windows если напряжение меньше 10,5 В  
            }
            catch (Exception e1)
            {
            }
        }
    }
	}
