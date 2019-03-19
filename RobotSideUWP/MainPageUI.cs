using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace RobotSideUWP
{
    public sealed partial class MainPage : Page
    {
        private bool buttonEventIs = false;
        private object[] addresses = new string[9];
        

        public void InitializeUI()
        {
            int i = 0;

            string appVersion = string.Format("v.: {0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision);

            labelVersion.Text = "RobotSideUWP " + appVersion;

            if (CommonStruct.cameraController == "RD31") { RD31Button.IsChecked = true; }
            else if (CommonStruct.cameraController == "GM51") { GM51Button.IsChecked = true; }
            else if (CommonStruct.cameraController == "No") { NoButton.IsChecked = true; }

            buttonEventIs = false;

            buttonGoForward.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoForward_PointerDown), true);
            buttonGoForward.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonGoForward_PointerUp), true);
            //buttonGoForward.AddHandler(PointerExitedEvent, new PointerEventHandler(buttonGoForward_PointerExit), true);

            buttonGoLeft.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoLeft_PointerDown), true);
            buttonGoLeft.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonGoLeft_PointerUp), true);
            //buttonGoLeft.AddHandler(PointerExitedEvent, new PointerEventHandler(buttonGoLeft_PointerExit), true);

            buttonGoBackward.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoBackward_PointerDown), true);
            buttonGoBackward.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonGoBackward_PointerUp), true);
            //buttonGoBackward.AddHandler(PointerExitedEvent, new PointerEventHandler(buttonGoBackward_PointerExit), true);

            buttonGoRight.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoRight_PointerDown), true);
            buttonGoRight.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonGoRight_PointerUp), true);
            //buttonGoRight.AddHandler(PointerExitedEvent, new PointerEventHandler(buttonGoRight_PointerExit), true);

            buttonStopWheels.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonStopWheels_PointerDown), true);
            buttonStopWheels.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonStopWheels_PointerUp), true);
            //buttonStopWheels.AddHandler(PointerExitedEvent, new PointerEventHandler(buttonStopWheels_PointerExit), true);


            buttonCameraUp.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonCameraUp_PointerDown), true);
            buttonCameraUp.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonCameraUp_PointerUp), true);

            buttonCameraDown.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonCameraDown_PointerDown), true);
            buttonCameraDown.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonCameraDown_PointerUp), true);



            buttonGoUpFast.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoUpFast_PointerDown), true);
            buttonGoUpFast.AddHandler(PointerReleasedEvent, new PointerEventHandler(buttonGoUpFast_PointerUp), true);

            buttonGoDownFast.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoDownFast_PointerDown), true);
            buttonGoDownFast.AddHandler(PointerReleasedEvent, new PointerEventHandler(ButtonGoDownFast_PointerUp), true);

            buttonGoDirect.AddHandler(PointerPressedEvent, new PointerEventHandler(buttonGoDirect_PointerDown), true);

            

            buttonLanguageEng.Click += ButtonLanguageEng_Click;
            buttonLanguageRu.Click += ButtonLanguageRu_Click;
            
            checkSmoothlyStop.Checked += checkSmoothlyStop_Checked;
            checkSmoothlyStop.Unchecked += CheckSmoothlyStop_Unchecked;

            checkRebootAtNight.Checked += CheckRebootAtNight_Checked;
            checkRebootAtNight.Unchecked += CheckRebootAtNight_Unchecked;

            //labelRobotName.TextChanged += LabelRobotName_TextChanged;

            comboBoxWebSiteAddress.AllowDrop = true;
            addresses[0] = Convert.ToString(localSettings.Values["defaultWebSiteAddress"]);
            addresses[1] = Convert.ToString(localSettings.Values["webSiteAddress1"]);
            addresses[2] = Convert.ToString(localSettings.Values["webSiteAddress2"]);
            addresses[3] = Convert.ToString(localSettings.Values["webSiteAddress3"]);
            addresses[4] = Convert.ToString(localSettings.Values["webSiteAddress4"]);
            addresses[5] = Convert.ToString(localSettings.Values["webSiteAddress5"]);
            addresses[6] = Convert.ToString(localSettings.Values["webSiteAddress6"]);
            addresses[7] = Convert.ToString(localSettings.Values["webSiteAddress7"]);
            addresses[8] = Convert.ToString(localSettings.Values["webSiteAddress8"]);

            foreach (string address in addresses)
            {
                this.comboBoxWebSiteAddress.Items.Add(addresses[i]);
                i++;
            }
            comboBoxWebSiteAddress.SelectedItem = comboBoxWebSiteAddress.Items[0];
            comboBoxWebSiteAddress.SelectionChanged += ComboBoxWebSiteAddress_SelectionChanged;
            LeftGroup.PointerPressed += LeftGroup_PointerPressed;
            RightGroup.PointerPressed += RightGroup_PointerPressed;
            SettingsBorder.PointerPressed += SettingsBorder_PointerPressed;
        }

        private void CheckRebootAtNight_Unchecked(object sender, RoutedEventArgs e)
        {
            CommonStruct.rebootAtNight = false;
            localSettings.Values["RebootAtNight"] = false;
        }

        private void CheckRebootAtNight_Checked(object sender, RoutedEventArgs e)
        {
            CommonStruct.rebootAtNight = true;
            localSettings.Values["RebootAtNight"] = true;
        }

        private void SettingsBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            SettingsBorder.UpdateLayout();
        }

        private void RightGroup_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Current.NotifyUser("RightGroup_PointerPressed ", NotifyType.ErrorMessage);
        }

        private void LeftGroup_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (CommonStruct.allControlIsEnabled == false)
            {
                Current.NotifyUser(CommonStruct.NotifyPressStop, NotifyType.StatusMessage);
                
            }
            else
            {
                Current.NotifyUser(" ", NotifyType.StatusMessage);
                
            }
        }

        private void ComboBoxWebSiteAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            addresses[0] = comboBoxWebSiteAddress.SelectedValue.ToString();
            localSettings.Values["defaultWebSiteAddress"] = addresses[0];
            CommonStruct.defaultWebSiteAddress = (string)localSettings.Values["defaultWebSiteAddress"];
            buttonStart_Click(null, null);
        }

        public void NotifyUser(string strMessage, NotifyType type)
        {
            Current.StatusBlock.Text = strMessage;
            Current.StatusBlock1.Text = strMessage;
            if (type == NotifyType.ErrorMessage)
            {
                Current.StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            }
            else
            {
                Current.StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            }
        }

        public void NotifyUserFromOtherThread(string strMessage, NotifyType type)
        {
            try
            {
                Task t = new Task(async () =>
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() =>
                     {
                         Current.StatusBlock.Text = strMessage;
                         Current.StatusBlock1.Text = strMessage;
                         if (type == NotifyType.ErrorMessage)
                         {
                             Current.StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                         }
                         else
                         {
                             Current.StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                         }
                     }));
                });
                t.Start();
                //t.Wait();
                //CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() =>
                //{
                //    Current.StatusBlock.Text = strMessage;
                //    Current.StatusBlock1.Text = strMessage;
                //}));
                

                //if (type == NotifyType.ErrorMessage)
                //{
                //    Current.StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                //}
                //else
                //{
                //    Current.StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                //}
            }
            catch(Exception e)
            {

            }
        }

        void checkSmoothlyStop_Checked(object sender, RoutedEventArgs e)
        {
            CommonStruct.stopSmoothly = true;
            localSettings.Values["StopSmoothly"] = true;
        }

        void CheckSmoothlyStop_Unchecked(object sender, RoutedEventArgs e)
        {
            CommonStruct.stopSmoothly = false;
            localSettings.Values["StopSmoothly"] = false;
        }

        private void buttonGoForward_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "reset");
            string directionLeft = forwardDirection;
            string directionRight = forwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = CommonStruct.maxWheelsSpeed;
            double speedRight = CommonStruct.maxWheelsSpeed;
            double speedLeft1 = speedLeft, speedRight1 = speedRight;
            CommonStruct.speedLeftLocal = speedLeft1;
            CommonStruct.speedRightLocal = speedRight1;
            PlcControl.WheelsLocal(directionLeft, speedLeft1, directionRight, speedRight1);
        }

        private void buttonGoForward_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            buttonEventIs = true;
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
            if (CommonStruct.stopSmoothly == true)
            {
                PlcControl.WheelsStopLocalSmoothly();
            }
            else
            {
                PlcControl.WheelsStopLocal();
            }
        }

        private void buttonGoForward_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            if (buttonEventIs == false)
            {
                PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                if (CommonStruct.stopSmoothly == true)
                {
                    PlcControl.WheelsStopLocalSmoothly();
                }
                else
                {
                    PlcControl.WheelsStopLocal();
                }
            }
            buttonEventIs = false;
        }


        private void buttonGoLeft_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "reset");
            string directionLeft = backwardDirection;
            string directionRight = forwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = 0.3 * CommonStruct.maxWheelsSpeed;
            double speedRight = 0.3 * CommonStruct.maxWheelsSpeed;
            PlcControl.WheelsLocal(directionLeft, speedLeft, directionRight, speedRight);
        }

        private void buttonGoLeft_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
            PlcControl.WheelsStopLocal();
        }
        

        private void buttonGoBackward_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "reset");//Потому что у сенсорного экрана нет состояния "нажато". Есть только клик.
            string directionLeft = backwardDirection;
            string directionRight = backwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = CommonStruct.maxWheelsSpeed;
            double speedRight = CommonStruct.maxWheelsSpeed;
            //double speedTuningParam = CommonStruct.speedTuningParam;
            double speedLeft1 = speedLeft, speedRight1 = speedRight;
            CommonStruct.speedLeftLocal = speedLeft1;
            CommonStruct.speedRightLocal = speedRight1;
            PlcControl.WheelsLocal(directionLeft, speedLeft, directionRight, speedRight);
        }

        private void buttonGoBackward_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            buttonEventIs = true;
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
            if (CommonStruct.stopSmoothly == true)
            {
                PlcControl.WheelsStopLocalSmoothly();
            }
            else
            {
                PlcControl.WheelsStopLocal();
            }
        }

        private void buttonGoBackward_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            if (buttonEventIs == false)
            {
                PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                if (CommonStruct.stopSmoothly == true)
                {
                    PlcControl.WheelsStopLocalSmoothly();
                }
                else
                {
                    PlcControl.WheelsStopLocal();
                }
            }
            buttonEventIs = false;
        }
        

        private void buttonGoRight_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "reset");
            string directionLeft = forwardDirection;
            string directionRight = backwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = 0.3 * CommonStruct.maxWheelsSpeed;
            double speedRight = 0.3 * CommonStruct.maxWheelsSpeed;
            PlcControl.WheelsLocal(directionLeft, speedLeft, directionRight, speedRight);
        }

        private void buttonGoRight_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
            PlcControl.WheelsStopLocal();
        }

        private void buttonGoRight_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
            buttonGoRight_PointerUp(null, null);
        }
        

        private void buttonStopWheels_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                string hexAddress = CommonStruct.wheelsAddress;
                CommonStruct.dataToWrite = "^RC" + hexAddress + "\r";//GO для обоих (Both) колес

                ReadWrite.Write(CommonStruct.dataToWrite);
                CommonStruct.readData = ReadWrite.Read();//В скобках надо писать количество символов в ответе, иначе будет отвечать только черезх время таймаута
                string s = CommonStruct.readData;
            }
            catch (Exception e1)
            {
                MainPage.Current.NotifyUser("buttonStopWheels_PointerDown" + e1.Message + " COM port do not answer + buttonStopWheels_Click", NotifyType.ErrorMessage);
            }
        }

        private void buttonStopWheels_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            buttonEventIs = true;
            try
                {
                    PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                    string directionLeft = CommonStruct.directionLeft;
                    string directionRight = CommonStruct.directionRight;
                    string hexAddress = CommonStruct.wheelsAddress;
                    PlcControl.WheelsStopLocal();
                }
                catch (Exception e1)
                {
                    MainPage.Current.NotifyUser("buttonStopWheels_PointerUp" + e1.Message + " COM port do not answer + buttonStopWheels_Click", NotifyType.ErrorMessage);
            }
        }

        private void buttonStopWheels_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                string hexAddress = CommonStruct.wheelsAddress;
                CommonStruct.dataToWrite = "^RC" + hexAddress + "\r";//
                ReadWrite.Write(CommonStruct.dataToWrite);
                CommonStruct.readData = ReadWrite.Read();//В скобках надо писать количество символов в ответе, иначе будет отвечать только черезх время таймаута
            }
            catch (Exception e1)
            {
                Current.NotifyUser("buttonStopWheels_PointerExit" + e1.Message + " COM port do not answer + buttonStopWheels_Click", NotifyType.ErrorMessage);
            }
        }

        private void TimerChargeLevel_Tick(object sender, object e)
        {
            timerChargeLevel.Stop();
            try
            {
                CommonStruct.itIsTimeToAskVoltage = true;
                if (CommonStruct.stopBeforeWas == true)
                {
                    CommonStruct.numberOfTicksAfterWheelsStop += 1;//Это сделано, чтобы измерения не выполнялись во время движения. Только в покое
                    if (CommonStruct.numberOfTicksAfterWheelsStop >= 2) PlcControl.BatteryVoltageMeasuring();
                    //Я обнуляю этот счетчик в функции Wheels()
                }
                //else if (CommonStruct.stopBeforeWas == false)
                //{
                //    CommonStruct.numberOfTicksAfterWheelsStop = 0;
                //}

                double levelCeiling = Math.Ceiling((CommonStruct.dVoltageCorrected - 10500) / 23);
                if (levelCeiling >= 80) levelCeiling = 100;

                if (levelCeiling < 0 )
                {
                    labelChargeLevel.Text = "Measure...";
                }
                else
                {
                    labelChargeLevel.Text = levelCeiling.ToString() + "%";
                }
                timerChargeLevel.Start();
                if (levelCeiling > 40)
                {
                    labelChargeLevel.Background =  new SolidColorBrush(Windows.UI.Colors.Green);
                    labelChargeLevel.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                }
                else
                {
                    labelChargeLevel.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    labelChargeLevel.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                }
            }
            catch (Exception e1)
            {
                Current.NotifyUser("TimerChargeLevel_Tick" + e1.Message + "supplyVoltage", NotifyType.ErrorMessage);
            }
        }

        private void buttonCameraUp_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            //PlcControl.HostWatchDog(CommonStruct.cameraAddress, "reset");
            string direction = "1";
            PlcControl.CameraUpDown(direction);
            CommonStruct.cameraPositionBefore = "slowUp";
        }

        private void buttonCameraUp_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            //PlcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
            PlcControl.CameraStop();
        }

        private void buttonCameraDown_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            //PlcControl.HostWatchDog(CommonStruct.cameraAddress, "reset");
            string direction = "0";
            PlcControl.CameraUpDown(direction);
            CommonStruct.cameraPositionBefore = "slowDown";
        }

        private void buttonCameraDown_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            //PlcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
            PlcControl.CameraStop();
        }
        

        private void ButtonLanguageEng_Click(object sender, RoutedEventArgs e)
        {
            labelWheelsSpeed.Text = "Wheels Speed";
            labelCameraSpeedTesting.Text = "Camera Speed";
            buttonGoLeft.Content = "Left";
            buttonGoRight.Content = "Right";
            buttonGoForward.Content = "Forward";
            buttonGoBackward.Content = "Backward";
            buttonCameraUp.Content = "Camera Up";
            buttonCameraDown.Content = "Camera Down";
            groupBoxReceived.Text = "Web Control";
            label_x_coord.Text = "х coordinate";
            label_y_coord.Text = "y coordinate";
            labelWheels.Text = "Wheels";
            labelCamera.Text = "Camera";
            labelKeysStop.Text = "Keys";
            labelMonitor.Text = "Correction";
            buttonStart.Content = "Start";
            buttonStop.Content = "Stop";
            buttonExit.Content = "Exit";
            labelChooseLanguage.Text = "Выберите язык:";
            buttonAbout.Content = "About";
            labelServerAddress.Text = "Http web address";
            CommonStruct.NotifyPressStop = "If you want the settings to be accessible, press the button 'Stop'";
            labelSmileName.Text = "Smile";
            buttonGoUpFast.Content = "GoUpFast";
            buttonGoDirect.Content = "GoDirect";
            buttonGoDownFast.Content = "DownFast";
            //labelRobotName.Text = "Robot Name:";
            CommonStruct.updateCheckMessageCompleted = "New version has beed downloaded to path ";
            CommonStruct.updateCheckMessageNotNeed = "You use the last version of product.";
            CommonStruct.downloadProgressText1 = "Downloading in progress. Downloaded ";
            CommonStruct.downloadProgressText2 = " from total ";
            labelAccumulator.Text = "Charge level";
            checkSmoothlyStop.Content = "Smoothly Stop";
            CommonStruct.buttonOpenFileFolder = "Open Folder";
            labelKeysKontrol.Text = "Keys";
            buttonStopWheels.Content = "Wheels Stop";
            localSettings.Values["Culture"] = "en-US";
            labelSpeedTuning.Text = "Speed Tuning";
            textBlockSettings.Text = "Robot Settings";
            buttonCloseSettings.Content = "Close";
            buttonSave.Content = "Save";
            buttonSetDefault.Content = "Restore Defaults";
            labelComPorts.Text = "Connect to";
            buttonSettings.Content = "Settings";
            AISettings.Content = "AI Settings";
            checkBoxOnlyLocal.Content = "OnlyLocal";
            buttonShutdown.Content = "Shutdown";
            buttonRestart.Content = "buttonRestart";
            checkRebootAtNight.Content = "Reboot at night";
        }

        private void ButtonLanguageRu_Click(object sender, RoutedEventArgs e)
        {
            labelWheelsSpeed.Text = "Cкорость колес";
            labelCameraSpeedTesting.Text = "Скорость камеры";
            buttonGoLeft.Content = "Влево";
            buttonGoRight.Content = "Вправо";
            buttonGoForward.Content = "Вперед";
            buttonGoBackward.Content = "Назад";
            buttonCameraUp.Content = "Камера вверх";
            buttonCameraDown.Content = "Камера вниз";
            groupBoxReceived.Text = "Веб-управление";
            label_x_coord.Text = "Координата х";
            label_y_coord.Text = "Координата y";
            labelWheels.Text = "Колеса";
            labelCamera.Text = "Камера";
            labelKeysStop.Text = "Клавиши";
            labelMonitor.Text = "Поправка";
            buttonStart.Content = "Пуск";
            buttonStop.Content = "Стоп";
            buttonExit.Content = "Закрыть";
            labelChooseLanguage.Text = "Choose Language";
            buttonAbout.Content = "О нас";
            labelServerAddress.Text = "http адрес вебсервера";
            CommonStruct.NotifyPressStop = "Чтобы настройки стали доступны, нажмите кнопку 'Стоп'";
            labelSmileName.Text = "Смайл";
            buttonGoUpFast.Content = "ВверхБыстро";
            buttonGoDirect.Content = "Прямо";
            buttonGoDownFast.Content = "ВнизБыстро";
            //labelRobotName.Text = "Имя робота:";
            CommonStruct.updateCheckMessageCompleted = "Новая версия загружена в ";
            CommonStruct.updateCheckMessageNotNeed = "Вы используете последнюю версию продукта.";
            CommonStruct.downloadProgressText1 = "Загрузка файла. Загружено ";
            CommonStruct.downloadProgressText2 = " из ";
            labelAccumulator.Text = "Заряд аккумулятора";
            checkSmoothlyStop.Content = "Плавный стоп";
            CommonStruct.buttonOpenFileFolder = "Открыть папку";
            labelKeysKontrol.Text = "Клавиши";
            buttonStopWheels.Content = "Стоп (колеса)";
            localSettings.Values["Culture"] = "ru-RU";
            labelSpeedTuning.Text = "Подстройка";
            textBlockSettings.Text = "Настройки робота";
            buttonCloseSettings.Content = "Закрыть";
            buttonSave.Content = "Сохранить";
            buttonSetDefault.Content = "По умолчанию";
            labelComPorts.Text = "Подключить к";
            buttonSettings.Content = "Настройки";
            AISettings.Content = "ИИ";
            checkBoxOnlyLocal.Content = "Локально";
            buttonShutdown.Content = "Выкл.";
            buttonRestart.Content = "Перезагрузка";
            checkRebootAtNight.Content = "Перезагрузить ночью";
        }

        private void LabelRobotName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //localSettings.Values["RobotName"] = textBoxRobotName.Text;
        }

    }

 public enum NotifyType
{
    StatusMessage,
    ErrorMessage
};

}
