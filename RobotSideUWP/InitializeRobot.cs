using System;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace RobotSideUWP
{
    public sealed partial class MainPage : Page
    {
        DispatcherTimer hostWatchdogInitTimer;
        int kHostWtahdogTicks = 0;

        private void WriteDefaultSettings()
        {
            localContainer.Containers["settings"].Values["SmoothStopTime"] = 1000;
            localContainer.Containers["settings"].Values["StopSmoothly"] = true;
            localContainer.Containers["settings"].Values["WheelsPwrRange"] = 4;
            localContainer.Containers["settings"].Values["PWMSteppingSpeed"] = 220;//В единицах ШИМа. от 0 до 255
            localContainer.Containers["settings"].Values["MaxWheelsSpeed"] = 100;
            localContainer.Containers["settings"].Values["cameraController"] = "RD31";
            localContainer.Containers["settings"].Values["CameraSpeed"] = 100;

            localContainer.Containers["settings"].Values["k1"] = 0.7;
            localContainer.Containers["settings"].Values["k2"] = 0.4;
            localContainer.Containers["settings"].Values["k3"] = 0.2;
            localContainer.Containers["settings"].Values["k4"] = 0.1;

            localContainer.Containers["settings"].Values["minWheelsSpeedForTurning"] = 50;//В процентах (%) - скорость, ниже которой не может двигаться колесо, которое замедляется при плавном повороте

            localContainer.Containers["settings"].Values["defaultWebSiteAddress"] = "https://boteyes.com";
            localContainer.Containers["settings"].Values["webSiteAddress1"] = "https://boteyes.com";
            localContainer.Containers["settings"].Values["webSiteAddress2"] = "https://boteyes.ru";
            localContainer.Containers["settings"].Values["webSiteAddress3"] = "https://robotaxino.com";
            localContainer.Containers["settings"].Values["webSiteAddress4"] = "http://localhost";
            localContainer.Containers["settings"].Values["webSiteAddress5"] = "https://localhost";
            localContainer.Containers["settings"].Values["webSiteAddress6"] = "";
            localContainer.Containers["settings"].Values["webSiteAddress7"] = "";
            localContainer.Containers["settings"].Values["webSiteAddress8"] = "";

            localContainer.Containers["settings"].Values["speedTuningParam"] = 0;

            localContainer.Containers["settings"].Values["Interval"] = 1.4;//HWDT в сек.
            localContainer.Containers["settings"].Values["Culture"] = "en-US";
            localContainer.Containers["settings"].Values["onlyLocal"] = false;

            localContainer.Containers["settings"].Values["initTime"] = 240; //Время перезагрузки Виндовс, в минутах (60*Hours)
            localContainer.Containers["settings"].Values["VReal"] = 12.75;

            localContainer.Containers["settings"].Values["ObstacleAvoidanceIs"] = true;
            
        }

        private void ReadAllSettings()
        {//Инициализация всех параметров сразу после загрузки этой программы
            CommonStruct.smoothStopTime = Convert.ToInt16(localContainer.Containers["settings"].Values["SmoothStopTime"]);
            CommonStruct.stopSmoothly = Convert.ToBoolean(localContainer.Containers["settings"].Values["StopSmoothly"]);
            CommonStruct.wheelsPwrRange = Convert.ToString(localContainer.Containers["settings"].Values["WheelsPwrRange"]);
            CommonStruct.PWMSteppingSpeed = Convert.ToInt16(localContainer.Containers["settings"].Values["PWMSteppingSpeed"]);
            textBoxPWMSteppingSpeed.Text = CommonStruct.PWMSteppingSpeed.ToString();
            CommonStruct.maxWheelsSpeed = Convert.ToInt16(localContainer.Containers["settings"].Values["MaxWheelsSpeed"]);
            CommonStruct.cameraController = Convert.ToString(localContainer.Containers["settings"].Values["cameraController"]);
            
            CommonStruct.cameraSpeed = Convert.ToDouble(localContainer.Containers["settings"].Values["CameraSpeed"]);

            CommonStruct.robotSerial = Convert.ToString(localContainer.Containers["settings"].Values["Serial"]);

            CommonStruct.k1 = Convert.ToDouble(localContainer.Containers["settings"].Values["k1"]);
            CommonStruct.k2 = Convert.ToDouble(localContainer.Containers["settings"].Values["k2"]);
            CommonStruct.k3 = Convert.ToDouble(localContainer.Containers["settings"].Values["k3"]);
            CommonStruct.k4 = Convert.ToDouble(localContainer.Containers["settings"].Values["k4"]);

            CommonStruct.minWheelsSpeedForTurning = Convert.ToInt16(localContainer.Containers["settings"].Values["minWheelsSpeedForTurning"]);
            textBoxMinWheelsSpeedForTurning.Text = CommonStruct.minWheelsSpeedForTurning.ToString();

            CommonStruct.defaultWebSiteAddress = Convert.ToString(localContainer.Containers["settings"].Values["defaultWebSiteAddress"]);
            CommonStruct.webSiteAddress1 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress1"]);
            CommonStruct.webSiteAddress2 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress2"]);
            CommonStruct.webSiteAddress3 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress3"]);
            CommonStruct.webSiteAddress4 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress4"]);
            CommonStruct.webSiteAddress5 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress5"]);
            CommonStruct.webSiteAddress6 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress6"]);
            CommonStruct.webSiteAddress7 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress7"]);
            CommonStruct.webSiteAddress8 = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress8"]);

            CommonStruct.speedTuningParam = Convert.ToDouble(localContainer.Containers["settings"].Values["speedTuningParam"]);
            textBoxSpeedTuningParam.Text = CommonStruct.speedTuningParam.ToString();
            
            string userInterval = Convert.ToString(localContainer.Containers["settings"].Values["Interval"]);//HWDT в сек.

            string numberSeparator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            if (numberSeparator == ".") { userInterval = userInterval.Replace(',', '.'); }//Значения inf больше нет
            else { userInterval = userInterval.Replace('.', ','); }
            if ((userInterval == "") || (userInterval == null)) { userInterval = "0"; }
            double interval1 = Convert.ToDouble(userInterval);
            interval1 = Math.Ceiling(10 * interval1);
            CommonStruct.interval = CommonFunctions.ZeroInFrontSet(interval1.ToString());

            CommonStruct.culture = Convert.ToString(localContainer.Containers["settings"].Values["Culture"]);
            
            if (CommonStruct.culture == "ru-RU") {ButtonLanguageRu_Click(null, null);}
            else { ButtonLanguageEng_Click(null, null);}

            textBoxRobotSerial.Text = CommonStruct.robotSerial;
            
            checkBoxOnlyLocal.IsChecked = Convert.ToBoolean(localContainer.Containers["settings"].Values["onlyLocal"]);
            if (checkBoxOnlyLocal.IsChecked == true) { CommonStruct.checkBoxOnlyLocal = true; }
            else { CommonStruct.checkBoxOnlyLocal = false; }

            CommonStruct.deltaV = Convert.ToDouble(localContainer.Containers["settings"].Values["deltaV"]);
            CommonStruct.VReal = Convert.ToDouble(localContainer.Containers["settings"].Values["VReal"]);

            CommonStruct.ObstacleAvoidanceIs = Convert.ToBoolean(localContainer.Containers["settings"].Values["ObstacleAvoidanceIs"]);
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
            trackBarCameraSpeed.Value = Convert.ToInt32(localContainer.Containers["settings"].Values["CameraSpeed"]);
            textBoxCameraSpeedTesting.Text = trackBarCameraSpeed.Value.ToString();

            trackBarWheelsSpeed.ValueChanged += TrackBarWheelsSpeed_ValueChanged;
			trackBarWheelsSpeed.Maximum = 100;
			trackBarWheelsSpeed.TickFrequency = 20;
			trackBarWheelsSpeed.LargeChange = 10;
			trackBarWheelsSpeed.SmallChange = 1;
			trackBarWheelsSpeed.Minimum = 0;
            trackBarWheelsSpeed.Value = Convert.ToInt32(localContainer.Containers["settings"].Values["MaxWheelsSpeed"]);
            textBoxWheelsSpeed.Text = trackBarWheelsSpeed.Value.ToString();

            trackBarWheelsSpeedTuning.ValueChanged += TrackBarWheelsSpeedTuning_ValueChanged;
            trackBarWheelsSpeedTuning.Maximum = 20;
            trackBarWheelsSpeedTuning.TickFrequency = 1;
            trackBarWheelsSpeedTuning.LargeChange = 1;
            trackBarWheelsSpeedTuning.SmallChange = 1;
            trackBarWheelsSpeedTuning.Minimum = -20;
            trackBarWheelsSpeedTuning.Value = Convert.ToInt32(localContainer.Containers["settings"].Values["speedTuningParam"]);
            // 
            CommonStruct.cameraSpeed = trackBarCameraSpeed.Value;
            if (CommonStruct.cameraController == "RD31")
            {
                CommonStruct.cameraSpeed = trackBarCameraSpeed.Value - 100;
            }
			
            if (CommonStruct.culture == "ru-RU") { this.ButtonLanguageRu_Click(null, null); }
			else { ButtonLanguageEng_Click(null, null); }
            

            CultureInfo culture = CultureInfo.CurrentCulture;
            string decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
            string k1 = Convert.ToString(localContainer.Containers["settings"].Values["k1"]);
            string k2 = Convert.ToString(localContainer.Containers["settings"].Values["k2"]);
            string k3 = Convert.ToString(localContainer.Containers["settings"].Values["k3"]);
            string k4 = Convert.ToString(localContainer.Containers["settings"].Values["k4"]);
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

            bool stopSmoothly = Convert.ToBoolean(localContainer.Containers["settings"].Values["StopSmoothly"]);
            if (stopSmoothly == true) { checkSmoothlyStop.IsChecked = true; }
            else { checkSmoothlyStop.IsChecked = false; }
            CommonStruct.stopSmoothly = stopSmoothly;

            AllControlIsEnabled(false);

            CommonStruct.permissionToSendToWebServer = true;
        }

        private void HostWatchdogInitTimer_Tick(object sender, object e) {//Начальная установка в модулях
            kHostWtahdogTicks++;
            try {//использую четыре тика, чтобы корректно записать и считать ответ на установку сторожевого таймера
                switch (kHostWtahdogTicks) {
                    case 1:
                        CommonStruct.dataToWrite = "^A3" + CommonStruct.wheelsAddress + "\r";//Формирование команды чтения из АЦП
                        readWrite.Write(CommonStruct.dataToWrite);//эта команда посылается при инициализации робота
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
            buttonLanguageEng.IsEnabled = isEnabled;
            buttonLanguageRu.IsEnabled = isEnabled;
            buttonAbout.IsEnabled = isEnabled;
            if (isEnabled == true) { CommonStruct.allControlIsEnabled = true; }
            else { CommonStruct.allControlIsEnabled = false; }
            buttonSettings.IsEnabled = isEnabled;
            trackBarWheelsSpeed.IsEnabled = isEnabled;
            trackBarCameraSpeed.IsEnabled = isEnabled;
            trackBarWheelsSpeedTuning.IsEnabled = isEnabled;
            checkBoxOnlyLocal.IsEnabled = isEnabled;
            buttonShutdown.IsEnabled = isEnabled;
            buttonRestart.IsEnabled = isEnabled;
            buttonExit.IsEnabled = isEnabled;
            buttonWiFi.IsEnabled = isEnabled;
        }

        private void TrackBarWheelsSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.maxWheelsSpeed = trackBarWheelsSpeed.Value;
            textBoxWheelsSpeed.Text = CommonStruct.maxWheelsSpeed.ToString();
            localContainer.Containers["settings"].Values["MaxWheelsSpeed"] = CommonStruct.maxWheelsSpeed;
        }

        private void TrackBarCameraSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.cameraSpeed = trackBarCameraSpeed.Value;
            textBoxCameraSpeedTesting.Text = CommonStruct.cameraSpeed.ToString();
            localContainer.Containers["settings"].Values["CameraSpeed"] = CommonStruct.cameraSpeed;
            if (CommonStruct.cameraController == "RD31")
            {
                CommonStruct.cameraSpeed = trackBarCameraSpeed.Value - 100;
            }
        }

        private void TrackBarWheelsSpeedTuning_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CommonStruct.speedTuningParam = trackBarWheelsSpeedTuning.Value;
            this.textBoxWheelsSpeedTuning.Text = CommonStruct.speedTuningParam.ToString();
            localContainer.Containers["settings"].Values["speedTuningParam"] = CommonStruct.speedTuningParam;
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
