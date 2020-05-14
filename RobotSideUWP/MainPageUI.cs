using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
            
            buttonLanguageEng.Click += ButtonLanguageEng_Click;
            buttonLanguageRu.Click += ButtonLanguageRu_Click;
            
            checkSmoothlyStop.Checked += checkSmoothlyStop_Checked;
            checkSmoothlyStop.Unchecked += CheckSmoothlyStop_Unchecked;

            comboBoxWebSiteAddress.AllowDrop = true;
            addresses[0] = Convert.ToString(localContainer.Containers["settings"].Values["defaultWebSiteAddress"]);
            addresses[1] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress1"]);
            addresses[2] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress2"]);
            addresses[3] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress3"]);
            addresses[4] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress4"]);
            addresses[5] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress5"]);
            addresses[6] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress6"]);
            addresses[7] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress7"]);
            addresses[8] = Convert.ToString(localContainer.Containers["settings"].Values["webSiteAddress8"]);

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
            localContainer.Containers["settings"].Values["defaultWebSiteAddress"] = addresses[0];
            CommonStruct.defaultWebSiteAddress = (string)localContainer.Containers["settings"].Values["defaultWebSiteAddress"];

            client = MqttInitialization(CommonStruct.defaultWebSiteAddress);

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

            Task t = new Task(async () =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(async () =>
                {
                    if ((strMessage != "") && (type == NotifyType.ErrorMessage)) {
                        await SendErrorsToServer(strMessage);
                    }
                }));
            });
            t.Start();
        }

        public void  NotifyUserFromOtherThreadAsync(string strMessage, NotifyType type)
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

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(async () =>
                    {
                        if ((strMessage != "") && (type == NotifyType.ErrorMessage)) {
                            await SendErrorsToServer(strMessage);
                        }
                    }));
                });
                t.Start();
            }
            catch(Exception e)
            {
            }
        }

        public void NotifyUserForTesting(string strMessage)
        {
            try {
                 Current.StatusBlockForTesting.Text = strMessage;
            }
            catch (Exception e) {
            }
        }

        void checkSmoothlyStop_Checked(object sender, RoutedEventArgs e)
        {
            CommonStruct.stopSmoothly = true;
            localContainer.Containers["settings"].Values["StopSmoothly"] = true;
        }

        void CheckSmoothlyStop_Unchecked(object sender, RoutedEventArgs e)
        {
            CommonStruct.stopSmoothly = false;
            localContainer.Containers["settings"].Values["StopSmoothly"] = false;
        }

        private void buttonGoForward_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            string directionLeft = forwardDirection;
            string directionRight = forwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = CommonStruct.maxWheelsSpeed;
            double speedRight = CommonStruct.maxWheelsSpeed;
            double speedLeft1 = speedLeft, speedRight1 = speedRight;
            CommonStruct.speedLeftLocal = speedLeft1;
            CommonStruct.speedRightLocal = speedRight1;
            plcControl.WheelsLocal(directionLeft, speedLeft1, directionRight, speedRight1);
        }

        private void buttonGoForward_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            buttonEventIs = true;
            if (CommonStruct.stopSmoothly == true)
            {
                plcControl.WheelsStopLocalSmoothly();
            }
            else
            {
                plcControl.WheelsStopLocal();
            }
        }

        private void buttonGoForward_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            if (buttonEventIs == false)
            {
                if (CommonStruct.stopSmoothly == true)
                {
                    plcControl.WheelsStopLocalSmoothly();
                }
                else
                {
                    plcControl.WheelsStopLocal();
                }
            }
            buttonEventIs = false;
        }


        private void buttonGoLeft_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            string directionLeft = backwardDirection;
            string directionRight = forwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = 0.3 * CommonStruct.maxWheelsSpeed;
            double speedRight = 0.3 * CommonStruct.maxWheelsSpeed;
            plcControl.WheelsLocal(directionLeft, speedLeft, directionRight, speedRight);
        }

        private void buttonGoLeft_PointerUp(object sender, PointerRoutedEventArgs e)
        {
           plcControl.WheelsStopLocal();
        }
        

        private void buttonGoBackward_PointerDown(object sender, PointerRoutedEventArgs e)
        {
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
            plcControl.WheelsLocal(directionLeft, speedLeft, directionRight, speedRight);
        }

        private void buttonGoBackward_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            buttonEventIs = true;
            if (CommonStruct.stopSmoothly == true)
            {
                plcControl.WheelsStopLocalSmoothly();
            }
            else
            {
                plcControl.WheelsStopLocal();
            }
        }

        private void buttonGoBackward_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            if (buttonEventIs == false)
            {
                if (CommonStruct.stopSmoothly == true)
                {
                    plcControl.WheelsStopLocalSmoothly();
                }
                else
                {
                    plcControl.WheelsStopLocal();
                }
            }
            buttonEventIs = false;
        }
        

        private void buttonGoRight_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            string directionLeft = forwardDirection;
            string directionRight = backwardDirection;
            CommonStruct.directionLeft = directionLeft;
            CommonStruct.directionRight = directionRight;
            double speedLeft = 0.3 * CommonStruct.maxWheelsSpeed;
            double speedRight = 0.3 * CommonStruct.maxWheelsSpeed;
            plcControl.WheelsLocal(directionLeft, speedLeft, directionRight, speedRight);
        }

        private void buttonGoRight_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            plcControl.WheelsStopLocal();
        }

        private void buttonGoRight_PointerExit(object sender, PointerRoutedEventArgs e)
        {
            buttonGoRight_PointerUp(null, null);
        }
        

        private void buttonStopWheels_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                string hexAddress = CommonStruct.wheelsAddress;
                CommonStruct.dataToWrite = "^RC" + hexAddress + "\r";//GO для обоих (Both) колес

                readWrite.Write(CommonStruct.dataToWrite);
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
                    string directionLeft = CommonStruct.directionLeft;
                    string directionRight = CommonStruct.directionRight;
                    string hexAddress = CommonStruct.wheelsAddress;
                    plcControl.WheelsStopLocal();
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
                string directionLeft = CommonStruct.directionLeft;
                string directionRight = CommonStruct.directionRight;
                string hexAddress = CommonStruct.wheelsAddress;
                CommonStruct.dataToWrite = "^RC" + hexAddress + "\r";//
                readWrite.Write(CommonStruct.dataToWrite);
            }
            catch (Exception e1)
            {
                Current.NotifyUser("buttonStopWheels_PointerExit" + e1.Message + " COM port do not answer + buttonStopWheels_Click", NotifyType.ErrorMessage);
            }
        }

        private void buttonCameraUp_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            //plcControl.HostWatchDog(CommonStruct.cameraAddress, "reset");
            string direction = "1";
            plcControl.CameraUpDown(direction);
        }

        private void buttonCameraUp_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            //plcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
            plcControl.CameraStop();
        }

        private void buttonCameraDown_PointerDown(object sender, PointerRoutedEventArgs e)
        {
            //plcControl.HostWatchDog(CommonStruct.cameraAddress, "reset");
            string direction = "0";
            plcControl.CameraUpDown(direction);
        }

        private void buttonCameraDown_PointerUp(object sender, PointerRoutedEventArgs e)
        {
            //plcControl.HostWatchDog(CommonStruct.cameraAddress, "set");
            plcControl.CameraStop();
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
            labelServerAddress.Text = "Https web address";
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
            localContainer.Containers["settings"].Values["Culture"] = "en-US";
            labelSpeedTuning.Text = "Speed Tuning";
            textBlockSettings.Text = "Robot Settings";
            buttonCloseSettings.Content = "Close";
            buttonSave.Content = "Save";
            buttonSetDefault.Content = "Restore Defaults";
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
            localContainer.Containers["settings"].Values["Culture"] = "ru-RU";
            labelSpeedTuning.Text = "Подстройка";
            textBlockSettings.Text = "Настройки робота";
            buttonCloseSettings.Content = "Закрыть";
            buttonSave.Content = "Сохранить";
            buttonSetDefault.Content = "По умолчанию";
            buttonSettings.Content = "Настройки";
            AISettings.Content = "ИИ";
            checkBoxOnlyLocal.Content = "Локально";
            buttonShutdown.Content = "Выкл.";
            buttonRestart.Content = "Перезагрузка";
            checkRebootAtNight.Content = "Перезагрузить ночью";
        }

        private void LabelRobotName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //localContainer.Containers["settings"].Values["RobotName"] = textBoxRobotName.Text;
        }

    }

 public enum NotifyType
{
    StatusMessage,
    ErrorMessage
};

}
