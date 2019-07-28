using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System;
using Windows.UI.Xaml;
using System.Net.Http;
using System.Net.Http.Headers;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace RobotSideUWP
{
    class PlcControl
		{
        static GpioPin pin6;//Выход для отключения питания 
        static TimeSpan delay = TimeSpan.FromMilliseconds(200);
        public DispatcherTimer smoothlyStopTimer;
        public int stopTimerCounter = 0;

        public PlcControl()
        {
            smoothlyStopTimer = new DispatcherTimer();
            smoothlyStopTimer.Tick += SmoothlyStopTimer_Tick;
            smoothlyStopTimer.Interval = new TimeSpan(0, 0, 0, 0, 200); //Таймер для плавной сотановки (дни, часы, мин, сек, ms)
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
        
  		public void HostWatchDog(string address, string setReset)
			{
			string interval = CommonStruct.interval;
            try
                {
			
				if (setReset == "set")
					{
                    MainPage.readWrite.Write("^RA" + address + interval + "\r");//Остановка через время, заданное в таймере
					}
				else if (setReset == "reset")
					{
					interval = "999";
                    MainPage.readWrite.Write("^RA" + address + interval + "\r");
					}
				CommonStruct.portOpen = true;
				}
			catch (Exception e)
				{
				CommonStruct.portOpen = false;
                //CommonFunctions.WriteToLog(e.Message + " HostWatchDog");
                MainPage.Current.NotifyUserFromOtherThreadAsync("HostWatchDog " + e.Message, NotifyType.ErrorMessage);
                }
			}

        public void Wheels(string directionLeft, double _speedLeft, string directionRight, double _speedRight)
        {//Управление мышкой и клавишами, за исключением локального управления с сенсорного экрана
            try
            {
                CommonStruct.numberOfTicksAfterWheelsStop = 0;
                if ((CommonStruct.stopBeforeWas == false) && ((CommonStruct.directionLeft != directionLeft) || (CommonStruct.directionRight != directionRight)))
                {
                    WheelsStopSmoothly(200);
                }
                else
                { 
                    double speedLeft0 = PlcControl.WheelsSpeedTuning(_speedLeft, _speedRight)[0];
                    double speedRight0 = PlcControl.WheelsSpeedTuning(_speedLeft, _speedRight)[1];

                    double speedRadius = Math.Sqrt((speedLeft0* speedLeft0) + (speedRight0* speedRight0));
                    if (speedRadius > 1) {

                        string speedLeft = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedLeft0).ToString());
                        string speedRight = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedRight0).ToString());
                        string hexAddress = CommonStruct.wheelsAddress;
                        string PwrRange = CommonStruct.wheelsPwrRange;
                        string commandLeft = directionLeft + speedLeft;
                        string commandRight = directionRight + speedRight;
                        //CommonStruct.wheelsWasStopped = false;
                        MainPage.readWrite.Write("^RB" + hexAddress + commandLeft + commandRight + "\r");//Установка скорости и направления для обоих колес
                        CommonStruct.lastCommandLeft = commandLeft;
                        CommonStruct.lastCommandRight = commandRight;
                        CommonStruct.lastSpeedLeft = _speedLeft;
                        CommonStruct.lastSpeedRight = _speedRight;
                        CommonStruct.directionLeft = directionLeft;
                        CommonStruct.directionRight = directionRight;
                        CommonStruct.stopBeforeWas = false;
                    }
                }
               
            }
            catch (Exception e1)
            {
                //CommonFunctions.WriteToLog(e1.Message + " Wheels");
                MainPage.Current.NotifyUserFromOtherThreadAsync("Wheels " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        public void WheelsLocal(string directionLeft, double _speedLeft, string directionRight, double _speedRight)
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
                MainPage.readWrite.Write("^RB" + hexAddress + commandLeft + commandRight + "\r");
                CommonStruct.stopBeforeWas = false;
            }
            catch (Exception e1)
                {
                //CommonFunctions.WriteToLog(e1.Message + " Wheels");
                MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsLocal" + e1.Message, NotifyType.ErrorMessage);
                }
            }

        public void WheelsStop()
            {
                try
                {
                    string hexAddress = CommonStruct.wheelsAddress;
                    MainPage.readWrite.Write("^RC" + hexAddress + "\r");//Общий стоп для всех каналов
                    CommonStruct.stopBeforeWas = true;
            }
                catch(Exception e)
                {
                    MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsStop" + e.Message, NotifyType.ErrorMessage);
                    CommonStruct.stopBeforeWas = true;
                }
            }

        public void WheelsStopSmoothly(double interval)
            {
            try
                {
                double speedLeft = CommonStruct.lastSpeedLeft;
                double speedRight = CommonStruct.lastSpeedRight;
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                double k1 = CommonStruct.k1;
                stopTimerCounter = 0;
                Wheels(directionLeft, k1 * speedLeft, directionRight, k1 * speedRight);
                Task t = new Task(async () => {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() => {
                        smoothlyStopTimer.Interval = TimeSpan.FromMilliseconds(interval);
                        smoothlyStopTimer.Start();
                    }));
                });
                t.Start();
                }
                catch(Exception e)
                {
                    MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsStopSmoothly" + e.Message, NotifyType.ErrorMessage);
                }
            }

        private void SmoothlyStopTimer_Tick(object sender, object e)
        {
            try {
                stopTimerCounter++;
                double speedLeft = CommonStruct.lastSpeedLeft;
                double speedRight = CommonStruct.lastSpeedRight;
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                double k2 = CommonStruct.k2, k3 = CommonStruct.k3, k4 = CommonStruct.k4;
                switch (stopTimerCounter) {
                    case 1: Wheels(directionLeft, k2 * speedLeft, directionRight, k2 * speedRight); break;
                    case 2: Wheels(directionLeft, k3 * speedLeft, directionRight, k3 * speedRight); break;
                    case 3: Wheels(directionLeft, k4 * speedLeft, directionRight, k4 * speedRight); break;
                    case 4: 
                            string hexAddress = CommonStruct.wheelsAddress;
                            MainPage.readWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих (Both) колес
                            break;
                    case 5:
                        //CommonStruct.NowIsCurrentMeasuring = true;
                        //MainPage.Current.ChargeCurrentMeasure();
                        break;
                    case 6:
                        //CommonStruct.NowIsCurrentMeasuring = false;
                        MainPage.Current.ChargeLevelMeasure();
                        smoothlyStopTimer.Stop();
                        stopTimerCounter = 0;
                        CommonStruct.stopBeforeWas = true;
                        break;
                }
            }
            catch(Exception e1) {
                MainPage.Current.NotifyUserFromOtherThreadAsync("SmoothlyStopTimer_Tick" + e1.Message + "WheelsStopFromMonitor", NotifyType.ErrorMessage);
            }
        }

        public void WheelsStopLocal()
            {
            try
                {
                string hexAddress = CommonStruct.wheelsAddress;
                MainPage.readWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих колес
                Task.Delay(20).Wait();
                CommonStruct.stopBeforeWas = true;
                }
            catch (Exception e1)
                {
                MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsStopLocal" + e1.Message + "WheelsStopFromMonitor", NotifyType.ErrorMessage);
                }
            }

        public void WheelsStopLocalSmoothly()
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

                MainPage.readWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих колес
                Task.Delay(20).Wait();
                CommonStruct.stopBeforeWas = true;
            }
            catch (Exception e1)
                {
                MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsStopLocalSmoothly" + e1.Message + "WheelsStopFromMonitor", NotifyType.ErrorMessage);
                }
            }

		public void CameraUpDown(string direction)
			{
			try
                {
                    string hexAddress = CommonStruct.cameraAddress;
                    string PwrRange = CommonStruct.cameraPwrRange;
                    if (CommonStruct.cameraController == "GM51")
                    {
                        double speed = CameraSpeedToPWM();
                        if (speed <= 2) speed = 2;
                        string __speed = CommonFunctions.ZeroInFrontFromDoubleAsync(speed);
                        MainPage.readWrite.Write("^RO" + CommonStruct.cameraAddress + "6" + "\r");//Установка 1/6 шага
                        string command = "^R1" + hexAddress + direction + __speed + "000" + PwrRange + "\r";
                        MainPage.readWrite.Write(command);
                    }
                    else if (CommonStruct.cameraController == "RD31")
                    {
                        double speed = CommonStruct.cameraSpeed;//от 0 до 100.
                        string __speed = CommonFunctions.ZeroInFrontFromDoubleAsync(speed);
                        MainPage.readWrite.Write("^R1" + hexAddress + direction + __speed + "000" + "4" + "\r");
                    }
                }
			catch (Exception e1)
				{
                //CommonFunctions.WriteToLog(e1.Message + " CameraUpDown");
                MainPage.Current.NotifyUserFromOtherThreadAsync("CameraUpDown" + e1.Message, NotifyType.ErrorMessage);
                }
			}

        public void CameraUpDown(string direction, double speed)
        {
            try
            {
                string hexAddress = CommonStruct.cameraAddress;
                string PwrRange = CommonStruct.cameraPwrRange;
                if (CommonStruct.cameraController == "GM51")
                {
                    if (speed <= 2) speed = 2;
                    string __speed = CommonFunctions.ZeroInFrontFromDoubleAsync(speed);
                    MainPage.readWrite.Write("^RO" + CommonStruct.cameraAddress + "4" + "\r");//Установка 1/6 шага
                    string command = "^R1" + hexAddress + direction + __speed + "000" + PwrRange + "\r";
                    MainPage.readWrite.Write(command);
                }
                else if (CommonStruct.cameraController == "RD31")
                {
                    speed = CommonStruct.cameraSpeed;//от 0 до 100.
                    MainPage.readWrite.Write("^R1" + hexAddress + direction + speed + "000" + "4" + "\r");
                }

            }
            catch(Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("CameraUpDown(string direction, double speed) " + e.Message, NotifyType.ErrorMessage);
            }
        }

        public void CameraStop()
        {//
            try
            {
                string hexAddress = CommonStruct.cameraAddress;
                if (CommonStruct.cameraController == "GM51")
                {
                    MainPage.readWrite.Write("^RC" + hexAddress + "\r");
                }
                else if (CommonStruct.cameraController == "RD31")
                {
                    double speed = CommonStruct.cameraSpeed;//от 0 до 100.
                    MainPage.readWrite.Write("^RS" + hexAddress + "1" + "\r");
                }
            }
            catch (Exception e1)
            {
                //CommonFunctions.WriteToLog(e1.Message + " CameraStop");
                MainPage.Current.NotifyUserFromOtherThreadAsync("CameraStop: " + e1.Message, NotifyType.ErrorMessage);
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
                MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsSpeedTuning " + e.Message, NotifyType.ErrorMessage);
            }
            return output;
        }

        public static string BatteryVoltageHandling(string input)
        {
            double levelCeiling = 0.0;
            double dAveragedVoltage = 0;
            double deltaV = 0;
            bool isInt;
            int res = 0;
            try
            {
                string s1 = input;
                string exclimation = s1.Substring(0, 1);
                if (((s1 == "") || (input.Length != 25)) && (exclimation != "!"))
                {
                    return "";
                }
                string data1 = s1.Remove(0, 5);
                string data2 = data1.Remove(4);
                if ((data2 == "") || (data2 == null))
                {
                    data2 = "0";
                    return "";
                }
                string averagedVoltage = s1.Substring(14, 4);
                CommonStruct.chargeCurrentFromRobot = data1.Substring(0, 4);
                isInt = Int32.TryParse(CommonStruct.chargeCurrentFromRobot, out res);
                if (isInt == true)
                {
                    CommonStruct.dChargeCurrent = (Convert.ToDouble(CommonStruct.chargeCurrentFromRobot));
                }
                
                isInt = Int32.TryParse(averagedVoltage, out res);
                if ((averagedVoltage == "") || (isInt == false)  )
                {
                    averagedVoltage = "0";
                    return "";
                }
                dAveragedVoltage = (Convert.ToDouble(averagedVoltage));
                deltaV = Convert.ToDouble(MainPage.Current.localSettings.Values["deltaV"]);
                CommonStruct.dVoltageCorrected = dAveragedVoltage + deltaV;

                if (CommonStruct.textBoxRealVoltageChanged == true)
                {
                    deltaV = 100 * CommonStruct.VReal - dAveragedVoltage;
                    MainPage.Current.localSettings.Values["deltaV"] = deltaV;
                    CommonStruct.dVoltageCorrected = dAveragedVoltage + deltaV;
                    CommonStruct.textBoxRealVoltageChanged = false;
                }

                CommonStruct.numberOfVoltageMeasurings++;
                if ((CommonStruct.dVoltageCorrected < 1050) && (CommonStruct.numberOfVoltageMeasurings > 10) && (CommonStruct.dChargeCurrent < 20) && (CommonStruct.dVoltageCorrected > 500))
                {
                    CommonStruct.numberOfVoltageMeasurings = 11;
                    CommonStruct.dVoltageCorrected = 1050;
                    CommonStruct.numberOfTicksAfterWheelsStop = 0;
                    //Посылаем команду "Старт таймера отключения батарей" и одновременно начинаем выгружать Виндовс 
                    pin6 = GpioController.GetDefault().OpenPin(6);
                    pin6.SetDriveMode(GpioPinDriveMode.Output);
                    pin6.Write(GpioPinValue.Low);// Latch HIGH value first. This ensures a default value when the pin is set as output
                    //Запускаем таймер, чтобы снять низкий уровень с выходов Распберри:
                    DispatcherTimer timerRobotOff;
                    timerRobotOff = new DispatcherTimer();
                    timerRobotOff.Tick += TimerRobotOff_Tick;
                    timerRobotOff.Interval = new TimeSpan(0, 0, 1); //(часы, мин, сек)
                    timerRobotOff.Start();
                    MainPage.Current.NotifyUserFromOtherThreadAsync("Supply Voltage less than 10.5 V.", NotifyType.ErrorMessage);
                }
                if (CommonStruct.dVoltageCorrected > 1270)
                {
                    CommonStruct.dVoltageCorrected = 1270;
                }
                levelCeiling = Math.Ceiling(CommonStruct.dVoltageCorrected - 1170);
                CommonStruct.outputValuePercentage = levelCeiling.ToString();

                if ((CommonStruct.dChargeCurrent < 20) && (CommonStruct.dVoltageCorrected > 0))
                {//пусть лучше при сбое пишет % во время зараяда, чем "Charging" во время езды.
                    CommonStruct.outputValuePercentage = levelCeiling.ToString();
                }
                else
                {
                    CommonStruct.outputValuePercentage = "Charging...";
                }
            }
            catch (Exception e2)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("Cannot measure battery voltage. " + e2.Message, NotifyType.ErrorMessage);
            }
            return CommonStruct.outputValuePercentage;
        }

        private static void TimerRobotOff_Tick(object sender, object e)
        {//Таймер, который выключет напряжение питания через минутут после того как напряжение на аккумуляторе станет меньше 10,5 В.
            try
            {
                Task t = new Task(async () =>
                {
                    await MainPage.SendVoltageToServer("BotEyes is Off");
                });
                t.Start();
                pin6.Write(GpioPinValue.High);// Latch HIGH value first. This ensures a default value when the pin is set as output
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));//Выгружаем Windows если напряжение меньше 10,5 В  
            }
            catch (Exception e1)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("TimerRobotOff_Tick " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        
    }
}
