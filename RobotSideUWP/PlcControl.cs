using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;
using Windows.ApplicationModel.Core;
using Windows.Devices.Gpio;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace RobotSideUWP
{
    class PlcControl
		{
        //static GpioPin pin6;//Выход для отключения питания подается на один из двух диодов на входе платы таймера отключения питания
        public DispatcherTimer smoothlyStopTimer;
        public int stopTimerCounter = 0;
        public DispatcherTimer batteryMeasuringTimer;
        static DispatcherTimer timerRobotOff = new DispatcherTimer();
        private string forwardDirection = "0";
        private string backwardDirection = "1";

        public PlcControl()
        {
            smoothlyStopTimer = new DispatcherTimer();
            smoothlyStopTimer.Tick += SmoothlyStopTimer_Tick;
            smoothlyStopTimer.Interval = new TimeSpan(0, 0, 0, 0, 200); //Таймер для плавной остановки (дни, часы, мин, сек, ms)

            batteryMeasuringTimer = new DispatcherTimer();
            batteryMeasuringTimer.Tick += BatteryMeasuringTimer_Tick;
            batteryMeasuringTimer.Interval = new TimeSpan(0, 0, 20, 0, 0); //Таймер для измерения напряжения в состоянии покоя робота
            batteryMeasuringTimer.Start();

            timerRobotOff.Tick += TimerRobotOff_Tick;//Этот таймер инициирует выгрузку Windows
            timerRobotOff.Interval = new TimeSpan(0, 0, 1); //(часы, мин, сек)
        }

        
        private void BatteryMeasuringTimer_Tick(object sender, object e)
        {
            if (CommonStruct.IsChargingCondition == false)
            {
                Task.Delay(100).Wait();
                MainPage.Current.ChargeLevelMeasure();
                Task.Delay(100).Wait();
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
                CommonStruct.wheelsIsStopped = false;
                if ((CommonStruct.rightObstacle == true) || (CommonStruct.leftObstacle == true) && ((directionLeft == forwardDirection) ||(directionRight == forwardDirection)))
                {
                    _speedLeft = 0.0; _speedRight = 0.0;
                    return;
                }
                CommonStruct.firstTimeObstacle = true;
//Все, что ниже, остается без изменений после ввода датчиков столкновений
                if ((CommonStruct.stopBeforeWas == false) && ((CommonStruct.directionLeftBefore != directionLeft) || (CommonStruct.directionRightBefore != directionRight)))
                {
                    WheelsStopSmoothly(200);
                }
                else
                {
                    WheelsInside(directionLeft, _speedLeft, directionRight, _speedRight);
                }
            }
            catch (Exception e1)
            {
                //CommonFunctions.WriteToLog(e1.Message + " Wheels");
                MainPage.Current.NotifyUserFromOtherThreadAsync("Wheels " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        public void WheelsInside(string directionLeft, double _speedLeft, string directionRight, double _speedRight)
        {//Управление мышкой и клавишами, за исключением локального управления с сенсорного экрана
            try
            {
                if (((CommonStruct.wheelsGoForwardIsAllowed == false) && ((directionLeft != "0") || (directionRight != "0"))) || (CommonStruct.wheelsGoForwardIsAllowed == true))
                {
                    double speedLeft0 = WheelsSpeedTuning(_speedLeft, _speedRight)[0];
                    double speedRight0 = WheelsSpeedTuning(_speedLeft, _speedRight)[1];

                    double speedRadius = Math.Sqrt((speedLeft0 * speedLeft0) + (speedRight0 * speedRight0));
                    if (speedRadius > 1)
                    {
                        string speedLeft = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedLeft0).ToString());
                        string speedRight = CommonFunctions.ZeroInFrontSet(CommonFunctions.WheelsSpeedToPWM(speedRight0).ToString());
                        string hexAddress = CommonStruct.wheelsAddress;
                        string PwrRange = CommonStruct.wheelsPwrRange;
                        string commandLeft = directionLeft + speedLeft;
                        string commandRight = directionRight + speedRight;
                        //CommonStruct.wheelsWasStopped = false;

                        MainPage.readWrite.Write("^RB" + hexAddress + commandLeft + commandRight + "\r");//Установка скорости и направления для обоих колес
                        CommonStruct.lastSpeedLeft = _speedLeft;
                        CommonStruct.lastSpeedRight = _speedRight;
                        CommonStruct.directionLeftBefore = directionLeft;
                        CommonStruct.directionRightBefore = directionRight;
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
                    CommonStruct.wheelsGoForwardIsAllowed = true;
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
            if (CommonStruct.wheelsIsStopped == false)
            {
                try
                {
                    double speedLeft = CommonStruct.lastSpeedLeft;
                    double speedRight = CommonStruct.lastSpeedRight;
                    string directionLeft = CommonStruct.directionLeftBefore;
                    string directionRight = CommonStruct.directionRightBefore;
                    double k1 = CommonStruct.k1;
                    stopTimerCounter = 0;
                    WheelsInside(directionLeft, k1 * speedLeft, directionRight, k1 * speedRight);
                    Task t = new Task(async () =>
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() =>
                        {
                            smoothlyStopTimer.Interval = TimeSpan.FromMilliseconds(interval);
                            smoothlyStopTimer.Start();
                        }));
                    });
                    t.Start();
                }
                catch (Exception e)
                {
                    MainPage.Current.NotifyUserFromOtherThreadAsync("WheelsStopSmoothly" + e.Message, NotifyType.ErrorMessage);
                }
                CommonStruct.wheelsGoForwardIsAllowed = true;
            }
        }

        private void SmoothlyStopTimer_Tick(object sender, object e)
        {
            try {
                stopTimerCounter++;
                double speedLeft = CommonStruct.lastSpeedLeft;
                double speedRight = CommonStruct.lastSpeedRight;
                string directionLeft = CommonStruct.directionLeftBefore;
                string directionRight = CommonStruct.directionRightBefore;
                double k2 = CommonStruct.k2, k3 = CommonStruct.k3, k4 = CommonStruct.k4;
                switch (stopTimerCounter) {
                    case 1:
                        if ((k2 * speedLeft < 15) || (k2 * speedRight < 15))
                        {
                            break;
                        }
                        else
                        {
                            Wheels(directionLeft, k2 * speedLeft, directionRight, k2 * speedRight); break;
                        }
                    case 2:
                        if ((k3 * speedLeft < 15) || (k3 * speedRight < 15))
                        {
                            break;
                        }
                        else
                        {
                            Wheels(directionLeft, k3 * speedLeft, directionRight, k3 * speedRight); break;
                        }
                    case 3:
                        if ((k4 * speedLeft < 15) || (k4 * speedRight < 15))
                        {
                            break;
                        }
                        else
                        {
                            Wheels(directionLeft, k4 * speedLeft, directionRight, k4 * speedRight); break;
                        }
                    case 4: 
                            string hexAddress = CommonStruct.wheelsAddress;
                            MainPage.readWrite.Write("^RC" + hexAddress + "\r");//Стоп для обоих (Both) колес
                            //MainPage.SendComments("stopped", "tablet");
                            break;
                    case 5:
                        
                        if (CommonStruct.dockingCounter == 0)
                        {
                            MainPage.Current.ChargeLevelMeasure();
                        }
                        CommonStruct.dockingCounter++;
                        if (CommonStruct.IsChargingCondition == false)
                        {
                            MainPage.Current.SendComments(CommonStruct.voltageLevelFromRobot + "%");
                        }
                        else
                        {
                            MainPage.Current.SendComments("Charging...");
                        }
                        break;
                    case 6:
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
                        string test = "^R1" + hexAddress + direction + __speed + "000" + "4" + "\r";
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
                    string __speed = CommonFunctions.ZeroInFrontFromDoubleAsync(speed);//от 0 до 100.
                    string test = "^R1" + hexAddress + direction + __speed + "000" + "4" + "\r";
                    MainPage.readWrite.Write("^R1" + hexAddress + direction + __speed + "000" + "4" + "\r");
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
                delta = deltaAdditive;
                
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
            bool isInt;
            string s1 = input;
            string averagedVoltage;
            string exclimation = s1.Substring(0, 1);
            try
            {
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

                if (s1.Length <= 14) return "";
                averagedVoltage = s1.Substring(14, 4);
                string x = averagedVoltage.Substring(0, 1);
                if (averagedVoltage.Substring(0, 1) != "1")
                {
                    averagedVoltage = averagedVoltage.Substring(0, 3);
                }
                CommonStruct.chargeCurrentFromRobot = data1.Substring(0, 4);
                int res;
                isInt = Int32.TryParse(CommonStruct.chargeCurrentFromRobot, out res);
                if (isInt == true)
                {
                    CommonStruct.dChargeCurrent = (Convert.ToDouble(CommonStruct.chargeCurrentFromRobot));
                }

                isInt = Int32.TryParse(averagedVoltage, out res);
                if ((averagedVoltage == "") || (isInt == false))
                {
                    averagedVoltage = "1220";// Here was 1200 and I think it is why robot tuned down after discharge and recharge
                    return "";
                }
                double dAveragedVoltage = (Convert.ToDouble(averagedVoltage));
                CommonStruct.deltaV = Convert.ToDouble(MainPage.Current.localContainer.Containers["settings"].Values["deltaV"]);
                CommonStruct.dVoltageCorrected = dAveragedVoltage + CommonStruct.deltaV;

                if (CommonStruct.textBoxRealVoltageChanged == true)
                {
                    CommonStruct.deltaV = 100 * CommonStruct.VReal - dAveragedVoltage;
                    MainPage.Current.localContainer.Containers["settings"].Values["deltaV"] = CommonStruct.deltaV;
                    CommonStruct.dVoltageCorrected = dAveragedVoltage + CommonStruct.deltaV;
                    CommonStruct.textBoxRealVoltageChanged = false;
                }

                CommonStruct.numberOfVoltageMeasurings++;//Это защита от случайного срабатывания после одного измерения

                if ((CommonStruct.dVoltageCorrected < 1210) && (CommonStruct.numberOfVoltageMeasurings > 1) && (CommonStruct.dChargeCurrent < 20) && (CommonStruct.dVoltageCorrected > 600))
                {
                    MainPage.Current.SendComments("Battery is low.");
                }

                if ((CommonStruct.dVoltageCorrected < 1200) && (CommonStruct.numberOfVoltageMeasurings > 1) && (CommonStruct.dChargeCurrent < 20) && (CommonStruct.dVoltageCorrected > 600))
                {//Если порог слишком низкий, то Распберри отключается раньше, чем реле 
                    CommonStruct.numberOfVoltageMeasurings = 11;
                    //Посылаем команду "Старт таймера отключения батарей" и одновременно начинаем выгружать Виндовс 
                    timerRobotOff.Start();//Запускаем таймер, чтобы выгрузить Виндовс:
                    MainPage.Current.NotifyUserFromOtherThreadAsync("Supply Voltage less than 11.8 V.", NotifyType.ErrorMessage);
                }
                if (CommonStruct.dVoltageCorrected > 1250)
                {
                    CommonStruct.dVoltageCorrected = 1250;
                }
                double levelCeiling = 2 * Math.Ceiling(CommonStruct.dVoltageCorrected - 1200);
                if (levelCeiling < 0) levelCeiling = 0;
                
                CommonStruct.outputValuePercentage = levelCeiling.ToString();
            }
            catch (Exception e2)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("Cannot measure battery voltage. " + e2.Message, NotifyType.ErrorMessage);
            }
            return CommonStruct.outputValuePercentage;
        }

        private static void TimerRobotOff_Tick(object sender, object e)
        {//Таймер, который выключет напряжение питания через минуту после того как напряжение на аккумуляторе станет меньше 11,5 В.
            try
            {
                MainPage.Current.pin5.Write(GpioPinValue.Low);// 
                MainPage.Current.SendComments("Battery is low.");
                CommonStruct.permissionToSendToWebServer = false;
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));//Выгружаем Windows если напряжение меньше 11,5
            }
            catch (Exception e1)
            {
                MainPage.Current.pin5.Write(GpioPinValue.Low);// 
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));//Выгружаем Windows если напряжение меньше 10,5 В  
                MainPage.Current.NotifyUserFromOtherThreadAsync("TimerRobotOff_Tick " + e1.Message, NotifyType.ErrorMessage);
            }
        }
    }
}
