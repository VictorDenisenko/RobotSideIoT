using System;
using System.Globalization;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace RobotSideUWP
{
    public sealed partial class MainPage : Page
    {
        DispatcherTimer hostWatchdogInitTimer;
        int kHostWtahdogTicks = 0;

        private void WriteDefaultSettings()
        {
            localSettings.Values["Reconnect"] = true;
            localSettings.Values["SmoothStopTime"] = 1000; 
            localSettings.Values["StopSmoothly"] = true;
            localSettings.Values["WheelsPwrRange"] = 4;
            localSettings.Values["PWMStoppingSpeed"] = 220;//В единицах ШИМа. от 0 до 255
            localSettings.Values["MaxWheelsSpeed"] = 100;
            localSettings.Values["cameraController"] = "No";
            localSettings.Values["CameraSpeed"] = 100;
            localSettings.Values["CameraFastSpeed"] = 6;//Если ШД гудит и не крутится, надо увеличить это число, т.е. уменьшить скорость 
            localSettings.Values["stepNumberForCalibration"] = 15;
            localSettings.Values["directTopDistance"] = 5;
            localSettings.Values["directBottomDistance"] = 10;
            localSettings.Values["cameraAlpha"] = null;

            localSettings.Values["k1"] = 0.7;
            localSettings.Values["k2"] = 0.4;
            localSettings.Values["k3"] = 0.2;
            localSettings.Values["k4"] = 0.1;

            localSettings.Values["minWheelsSpeedForTurning"] = 50;//В процентах (%) - скорость, ниже которой не может двигаться колесо, которое замедляется при плавном повороте

            localSettings.Values["defaultWebSiteAddress"] = "http://boteyes.com";
            localSettings.Values["webSiteAddress1"] = "http://boteyes.com";
            localSettings.Values["webSiteAddress2"] = "https://boteyes.com";
            localSettings.Values["webSiteAddress3"] = "http://boteyes.ru";
            localSettings.Values["webSiteAddress4"] = "https://boteyes.ru";
            localSettings.Values["webSiteAddress5"] = "http://robotaxino.com";
            localSettings.Values["webSiteAddress6"] = "https://robotaxino.com";
            localSettings.Values["webSiteAddress7"] = "http://localhost";
            localSettings.Values["webSiteAddress8"] = "https://localhost";

            localSettings.Values["Point10"] = 0;
            localSettings.Values["Point20"] = 0;
            localSettings.Values["Point30"] = 0;
            localSettings.Values["Point40"] = 0;
            localSettings.Values["Point50"] = 0;
            localSettings.Values["Point60"] = 0;
            localSettings.Values["Point70"] = 0;
            localSettings.Values["Point80"] = 0;
            localSettings.Values["Point90"] = 0;
            localSettings.Values["Point100"] = 0;

            localSettings.Values["speedTuningParam"] = 0;

            localSettings.Values["RobotName"] = "grey";

            localSettings.Values["Interval"] = 1.4;//HWDT в сек.
            localSettings.Values["TimeOut"] = 5000;
            localSettings.Values["Culture"] = "en-US";
            localSettings.Values["onlyLocal"] = false;

            localSettings.Values["localizationAngle"] = 0;
            localSettings.Values["comPortItem"] = "";

            localSettings.Values["initTime"] = 240; //Время перезагрузки Виндовс, в минутах (60*Hours)
            localSettings.Values["RebootAtNight"] = false;
        }

        private void ReadSettings()
        {//Инициализация всех параметров сразу после загрузки этой программы
            CommonStruct.reconnect = Convert.ToBoolean(localSettings.Values["Reconnect"]);
            CommonStruct.smoothStopTime = Convert.ToInt16(localSettings.Values["SmoothStopTime"]);
            CommonStruct.stopSmoothly = Convert.ToBoolean(localSettings.Values["StopSmoothly"]);
            CommonStruct.wheelsPwrRange = Convert.ToString(localSettings.Values["WheelsPwrRange"]);
            CommonStruct.PWMStoppingSpeed = Convert.ToInt16(localSettings.Values["PWMStoppingSpeed"]);
            textBoxPWMStoppingSpeed.Text = CommonStruct.PWMStoppingSpeed.ToString();
            CommonStruct.maxWheelsSpeed = Convert.ToInt16(localSettings.Values["MaxWheelsSpeed"]);
            CommonStruct.cameraController = Convert.ToString(localSettings.Values["cameraController"]);
            
            CommonStruct.cameraSpeed = Convert.ToDouble(localSettings.Values["CameraSpeed"]);

            CommonStruct.cameraFastSpeed = CommonFunctions.ZeroInFrontSet(Convert.ToString(localSettings.Values["CameraFastSpeed"]));
            textBoxCameraFastSpeed.Text = Convert.ToInt16(CommonStruct.cameraFastSpeed).ToString();
            CommonStruct.stepNumberForCalibration = Convert.ToString(localSettings.Values["stepNumberForCalibration"]);
            textBoxStepNumberForCalibration.Text = CommonStruct.stepNumberForCalibration.ToString();
            CommonStruct.directTopDistance = Convert.ToInt16(localSettings.Values["directTopDistance"]);
            textBoxDirectTopDistance.Text = CommonStruct.directTopDistance.ToString();
            CommonStruct.directBottomDistance = Convert.ToInt16(localSettings.Values["directBottomDistance"]);
            textBoxDirectBottomDistance.Text = CommonStruct.directBottomDistance.ToString();

            string cameraAlpha = Convert.ToString(localSettings.Values["cameraAlpha"]);
            if (cameraAlpha == "") cameraAlpha = "0";
            CommonStruct.cameraAlpha = Convert.ToInt32(cameraAlpha);

            CommonStruct.k1 = Convert.ToDouble(localSettings.Values["k1"]);
            CommonStruct.k2 = Convert.ToDouble(localSettings.Values["k2"]);
            CommonStruct.k3 = Convert.ToDouble(localSettings.Values["k3"]);
            CommonStruct.k4 = Convert.ToDouble(localSettings.Values["k4"]);

            CommonStruct.minWheelsSpeedForTurning = Convert.ToInt16(localSettings.Values["minWheelsSpeedForTurning"]);
            textBoxMinWheelsSpeedForTurning.Text = CommonStruct.minWheelsSpeedForTurning.ToString();

            CommonStruct.defaultWebSiteAddress = Convert.ToString(localSettings.Values["defaultWebSiteAddress"]);
            CommonStruct.webSiteAddress1 = Convert.ToString(localSettings.Values["webSiteAddress1"]);
            CommonStruct.webSiteAddress2 = Convert.ToString(localSettings.Values["webSiteAddress2"]);
            CommonStruct.webSiteAddress3 = Convert.ToString(localSettings.Values["webSiteAddress3"]);
            CommonStruct.webSiteAddress4 = Convert.ToString(localSettings.Values["webSiteAddress4"]);
            CommonStruct.webSiteAddress5 = Convert.ToString(localSettings.Values["webSiteAddress5"]);
            CommonStruct.webSiteAddress6 = Convert.ToString(localSettings.Values["webSiteAddress6"]);
            CommonStruct.webSiteAddress7 = Convert.ToString(localSettings.Values["webSiteAddress7"]);
            CommonStruct.webSiteAddress8 = Convert.ToString(localSettings.Values["webSiteAddress8"]);

            CommonStruct.refPoints[0] = 0.0;

            CommonStruct.speedTuningParam = Convert.ToDouble(localSettings.Values["speedTuningParam"]);
            textBoxSpeedTuningParam.Text = CommonStruct.speedTuningParam.ToString();
            
            string userInterval = Convert.ToString(localSettings.Values["Interval"]);//HWDT в сек.

            string numberSeparator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            if (numberSeparator == ".") { userInterval = userInterval.Replace(',', '.'); }//Значения inf больше нет
            else { userInterval = userInterval.Replace('.', ','); }
            if ((userInterval == "") || (userInterval == null)) { userInterval = "0"; }
            double interval1 = Convert.ToDouble(userInterval);
            interval1 = Math.Ceiling(10 * interval1);
            CommonStruct.interval = CommonFunctions.ZeroInFrontSet(interval1.ToString());

            object obj = localSettings.Values["comPortItem"];
            if (obj == null)
            {
                localSettings.Values["comPortItem"] = "Choose another port";
            }
            else
            {
                CommonStruct.comPortItem = localSettings.Values["comPortItem"].ToString();
            }

            CommonStruct.timeOut = Convert.ToString(localSettings.Values["TimeOut"]);
            CommonStruct.culture = Convert.ToString(localSettings.Values["Culture"]);
            
            if (CommonStruct.culture == "ru-RU") {ButtonLanguageRu_Click(null, null);}
            else { ButtonLanguageEng_Click(null, null);}

            CommonStruct.serial = Convert.ToString(localSettings.Values["Serial"]);
            CommonStruct.decriptedSerial = Convert.ToString(localSettings.Values["Serial"]);//Именно этот используется везде дальше (пока) 
            textBoxRobotSerial.Text = CommonStruct.decriptedSerial;
            
            checkBoxOnlyLocal.IsChecked = Convert.ToBoolean(localSettings.Values["onlyLocal"]);
            if (checkBoxOnlyLocal.IsChecked == true) { CommonStruct.checkBoxOnlyLocal = true; }
            else { CommonStruct.checkBoxOnlyLocal = false; }

            CommonStruct.localizationPoint = Convert.ToDouble(localSettings.Values["localizationAngle"]);
            trackBarLocalizationAngle.Value = CommonStruct.localizationPoint;

            CommonStruct.AngleInBreakPoint = Convert.ToDouble(localSettings.Values["angleInBreakPoint"]);
            CommonStruct.DistanceToZero = Convert.ToDouble(localSettings.Values["distanceToZero"]);

            CommonStruct.rebootAtNight = Convert.ToBoolean(localSettings.Values["RebootAtNight"]);

            CommonStruct.localizationPoint = Convert.ToDouble(localSettings.Values["localizationAngle"]);
            CommonStruct.deltaV = Convert.ToDouble(localSettings.Values["deltaV"]);
            CommonStruct.VReal = Convert.ToDouble(localSettings.Values["VReal"]);
        }

        private void InitializeRobot()
			{
            textBoxWheelsSpeedTuning.Text = CommonStruct.speedTuningParam.ToString();

            trackBarCameraSpeed.ValueChanged += TrackBarCameraSpeed_ValueChanged;
			trackBarCameraSpeed.Maximum = 100;
			trackBarCameraSpeed.TickFrequency = 20;
			trackBarCameraSpeed.LargeChange = 10;
			trackBarCameraSpeed.SmallChange = 1;
			trackBarCameraSpeed.Minimum = 0;
            trackBarCameraSpeed.Value = Convert.ToInt32(localSettings.Values["CameraSpeed"]);
            textBoxCameraSpeedTesting.Text = trackBarCameraSpeed.Value.ToString();

            trackBarWheelsSpeed.ValueChanged += TrackBarWheelsSpeed_ValueChanged;
			trackBarWheelsSpeed.Maximum = 100;
			trackBarWheelsSpeed.TickFrequency = 20;
			trackBarWheelsSpeed.LargeChange = 10;
			trackBarWheelsSpeed.SmallChange = 1;
			trackBarWheelsSpeed.Minimum = 0;
            trackBarWheelsSpeed.Value = Convert.ToInt32(localSettings.Values["MaxWheelsSpeed"]);
            textBoxWheelsSpeed.Text = trackBarWheelsSpeed.Value.ToString();

            trackBarWheelsSpeedTuning.ValueChanged += TrackBarWheelsSpeedTuning_ValueChanged;
            trackBarWheelsSpeedTuning.Maximum = 20;
            trackBarWheelsSpeedTuning.TickFrequency = 1;
            trackBarWheelsSpeedTuning.LargeChange = 1;
            trackBarWheelsSpeedTuning.SmallChange = 1;
            trackBarWheelsSpeedTuning.Minimum = -20;
            trackBarWheelsSpeedTuning.Value = Convert.ToInt32(localSettings.Values["speedTuningParam"]);
            // 
            CommonStruct.cameraSpeed = trackBarCameraSpeed.Value;
            if (CommonStruct.cameraController == "RD31")
            {
                CommonStruct.cameraSpeed = trackBarCameraSpeed.Value - 100;
            }
			
            if (CommonStruct.culture == "ru-RU") { this.ButtonLanguageRu_Click(null, null); }
			else { ButtonLanguageEng_Click(null, null); }
            
            CommonStruct.connectionNumber = 0;
            CommonStruct.top_bottom_distance = (Convert.ToInt32(CommonStruct.directTopDistance) + Convert.ToInt32(CommonStruct.directBottomDistance)).ToString();

            CultureInfo culture = CultureInfo.CurrentCulture;
            string decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
            string k1 = Convert.ToString(localSettings.Values["k1"]);
            string k2 = Convert.ToString(localSettings.Values["k2"]);
            string k3 = Convert.ToString(localSettings.Values["k3"]);
            string k4 = Convert.ToString(localSettings.Values["k4"]);
            if ((k1 == "") || (k1 == null)) { k1 = "0"; }
            if ((k2 == "") || (k2 == null)) { k2 = "0"; }
            if ((k3 == "") || (k3 == null)) { k3 = "0"; }
            if ((k4 == "") || (k4 == null)) { k4 = "0"; }
            if (decimalSeparator == ",")
                {
                k1 = k1.Replace(".", ",");
                k2 = k2.Replace(".", ",");
                k3 = k3.Replace(".", ",");
                k4 = k4.Replace(".", ",");
                }
            CommonStruct.k1 = Convert.ToDouble(k1);
            CommonStruct.k2 = Convert.ToDouble(k2);
            CommonStruct.k3 = Convert.ToDouble(k3);
            CommonStruct.k4 = Convert.ToDouble(k4);

            bool stopSmoothly = Convert.ToBoolean(localSettings.Values["StopSmoothly"]);
            if (stopSmoothly == true) { checkSmoothlyStop.IsChecked = true; }
            else { checkSmoothlyStop.IsChecked = false; }
            CommonStruct.stopSmoothly = stopSmoothly;

            bool rebootAtNight = Convert.ToBoolean(localSettings.Values["RebootAtNight"]);
            if (rebootAtNight == true) { checkRebootAtNight.IsChecked = true; }
            else { checkRebootAtNight.IsChecked = false; }
            CommonStruct.rebootAtNight = rebootAtNight;

            AllControlIsEnabled(false);

           //Код инициализации для перезагрузки Windows ночью
            object testObject3 = localSettings.Values["initTime"];
            if (!testObject3.GetType().Equals(typeof(int)))
            {
                localSettings.Values["initTime"] = 240; //Время перезагрузки Виндовс;
                CommonStruct.initTime = 240; //Время перезагрузки Виндовс;
            }
            else
            {
                CommonStruct.initTime = (int)testObject3;
            }
            int intHours = (int)Math.Round(CommonStruct.initTime / 60.0);
            int intMinutes = CommonStruct.initTime - 60 * intHours;
            TimeSpan initTime = new TimeSpan(intHours, intMinutes, 0); //(часы, мин, сек);
            setTimeToRestartPicker.Time = initTime;
            setTimeToRestartPicker.AllowDrop = true;

        }

        private void HostWatchdogInitTimer_Tick(object sender, object e) {
            kHostWtahdogTicks++;
            try {//использую четыре тика, чтобы корректно записать и считать ответ на установку сторожевого таймера
                switch (kHostWtahdogTicks) {
                    case 1:
                        CommonStruct.dataToWrite = "^A1" + CommonStruct.wheelsAddress + "\r";//Формирование команды чтения из АЦП
                        readWrite.Write(CommonStruct.dataToWrite);//
                        break;
                    case 2:
                        buttonStart_Click(null, null);
                        break;
                    case 3: plcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                        break;
                    case 4: {
                            
                            if (CommonStruct.cameraController == "No") {
                                if (CommonStruct.readData == "!0002014\r") {
                                    hostWatchdogInitTimer.Stop();
                                }
                            }
                        }
                        break;
                    case 5: {
                                plcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
                        }
                        break;
                    case 6: {
                            if (CommonStruct.readData == "!0003014\r") {
                                hostWatchdogInitTimer.Stop();
                            }
                        }
                        break;
                }
            }
            catch(Exception e1) {
                string message = e1.Message;
            }
        }

        private void AllControlIsEnabled(bool isEnabled)
        {
            comboBoxWebSiteAddress.IsEnabled = isEnabled;
            trackBarWheelsSpeed.IsEnabled = isEnabled;
            trackBarCameraSpeed.IsEnabled = isEnabled;
            trackBarWheelsSpeedTuning.IsEnabled = isEnabled;
            checkSmoothlyStop.IsEnabled = isEnabled;
            textBoxWheelsSpeed.IsEnabled = isEnabled;
            textBoxCameraSpeedTesting.IsEnabled = isEnabled;
            textBoxWheelsSpeedTuning.IsEnabled = isEnabled;
            buttonGoRight.IsEnabled = isEnabled;
            buttonGoForward.IsEnabled = isEnabled;
            buttonGoLeft.IsEnabled = isEnabled;
            buttonGoBackward.IsEnabled = isEnabled;
            buttonStopWheels.IsEnabled = isEnabled;
            buttonCameraUp.IsEnabled = isEnabled;
            buttonCameraDown.IsEnabled = isEnabled;
            buttonGoUpFast.IsEnabled = isEnabled;
            buttonGoDirect.IsEnabled = isEnabled;
            buttonGoDownFast.IsEnabled = isEnabled;
            buttonLanguageEng.IsEnabled = isEnabled;
            buttonLanguageRu.IsEnabled = isEnabled;
            buttonAbout.IsEnabled = isEnabled;
            if (isEnabled == true) { CommonStruct.allControlIsEnabled = true; }
            else { CommonStruct.allControlIsEnabled = false; }
            //textBoxRobotName.IsEnabled = isEnabled;
            buttonSettings.IsEnabled = isEnabled;
            //labelServerAddress.IsEnabled = isEnabled;
            trackBarWheelsSpeed.IsEnabled = isEnabled;
            trackBarCameraSpeed.IsEnabled = isEnabled;
            trackBarWheelsSpeedTuning.IsEnabled = isEnabled;
            //textBox1.IsEnabled = isEnabled;
            //labelComPorts.IsEnabled = isEnabled;
            //labelWheelsSpeed.IsEnabled = isEnabled;
            //labelCameraSpeedTesting.IsEnabled = isEnabled;
            //labelSpeedTuning.IsEnabled = isEnabled;
            checkBoxOnlyLocal.IsEnabled = isEnabled;
            //labelAccumulator.IsEnabled = isEnabled;
            AISettings.IsEnabled = isEnabled;
            checkBoxOnlyLocal.IsEnabled = isEnabled;
            buttonShutdown.IsEnabled = isEnabled;
            buttonRestart.IsEnabled = isEnabled;
            buttonExit.IsEnabled = isEnabled;
            buttonWiFi.IsEnabled = isEnabled;
            checkRebootAtNight.IsEnabled = isEnabled;
        }

        private void TrackBarWheelsSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.maxWheelsSpeed = trackBarWheelsSpeed.Value;
            textBoxWheelsSpeed.Text = CommonStruct.maxWheelsSpeed.ToString();
            localSettings.Values["MaxWheelsSpeed"] = CommonStruct.maxWheelsSpeed;
        }

        private void TrackBarCameraSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.cameraSpeed = trackBarCameraSpeed.Value;
            textBoxCameraSpeedTesting.Text = CommonStruct.cameraSpeed.ToString();
            localSettings.Values["CameraSpeed"] = CommonStruct.cameraSpeed;
            if (CommonStruct.cameraController == "RD31")
            {
                CommonStruct.cameraSpeed = trackBarCameraSpeed.Value - 100;
            }
        }

        private void TrackBarWheelsSpeedTuning_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.speedTuningParam = trackBarWheelsSpeedTuning.Value;
            this.textBoxWheelsSpeedTuning.Text = CommonStruct.speedTuningParam.ToString();
            localSettings.Values["speedTuningParam"] = CommonStruct.speedTuningParam;
        }

        public static void SavingApplicationStates()
            {
                try
                {
                }
                catch (Exception e1)
                {
                Current.NotifyUserFromOtherThreadAsync("SavingApplicationStates" + e1.Message + "COM port communication problem. - SessionEndingEventArgs", NotifyType.ErrorMessage);
                }
            }
		}
	}
