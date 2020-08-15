using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Storage;

namespace RobotSideUWP
{
        public struct CommonStruct
		{//Сюда записываются данные в формате с нулями перед числами (ZeroInFront Format)
		static public string robotSerial { get; set; }
		public static string x1 = "";
		public static string x2 = "";
        public static double cameraSpeed = 100;
        public static string wheelsAddress = "0002";
        public static string cameraAddress = "0003";
        public static string interval = "1.4";
		public static bool portOpen { get; set; }
        public static int PWMSteppingSpeed = 220;
        public static double maxWheelsSpeed = 100;//Макс. скорость, задаваемая слайдером макс. скорости, в процентах. 
        public static int smoothStopTime = 1000;
        public static string wheelsPwrRange = "4";
        public static string cameraPwrRange = "3";
        public static string defaultWebSiteAddress = "https://boteyes.com";

        public static double lastSpeedLeft { get; set; }
        public static double lastSpeedRight { get; set; }
        public static string directionLeft = "0";
        public static bool stopBeforeWas = true;
        public static string directionRight = "0";
        public static double k1 = 0.7;
        public static double k2 = 0.4;
        public static double k3 = 0.2;
        public static double k4 = 0.1;
        public static double speedTuningParam = 0.0;

        public static int minWheelsSpeedForTurning = 50;
        public static bool stopSmoothly = true;

        public static double speedLeftLocal = 0.0;
        public static double speedRightLocal = 0.0;

        public static string culture = "Eng";

        static public string dataToWrite { get; set; }
        static public string readData { get; set; }

        static public string webSiteAddress1 = "https://boteyes.com";
        static public string webSiteAddress2 = "https://boteyes.ru";
        static public string webSiteAddress3 = "https://robotaxino.com";
        static public string webSiteAddress4 = "http://localhost";
        static public string webSiteAddress5 = "https://localhost";
        static public string webSiteAddress6 = "";
        static public string webSiteAddress7 = "";
        static public string webSiteAddress8 = "";

        public static bool allControlIsEnabled = false;

        static public string NotifyPressStop = "";

        internal static bool checkBoxOnlyLocal = false;
        internal static string cameraController = "RD31";

        public static bool wheelsWasStopped = true;
        public static bool wheelsIsStopped = true;
        public static bool wheelsGoForwardIsAllowed = true;
        public static bool wheelsAreGoBackward = false;
        public static int initTime = 240;

        public static double dVoltageCorrected;
        public static string voltageLevelFromRobot = "";
        public static string chargeCurrentFromRobot = "";
        public static bool NowIsCurrentMeasuring;

        public static bool textBoxRealVoltageChanged = false;
        internal static double VReal = 12.75;
        public static double deltaV = 0.0;
        public static bool permissionToSendToWebServer { get; set; }
        public static bool leftObstacle { get; internal set; }

        public static bool permissionToSend = true;//Эту переменную обязательно надо устанавливать в true раз в секунду с помощью ватчдог таймера
        public static long numberOfVoltageMeasurings = 0;
        public static double dChargeCurrent = 0.0;
        public static string outputValuePercentage;

        public static bool IsChargingCondition = false;
        public static bool IsRobotGoesFromDock = false;
        public static long dockingCounter = 0;
        public static string dockIsFound = "yes";
        internal static string autoDockingStarted;
        internal static bool rightObstacle;
        internal static bool firstTimeObstacle = true;
    }

	class CommonFunctions
		{
        
        public static string elementContent = null;
        
        public static double Degrees(string _x, string _y)
			{//Примечание: для системы координат с осью y, направленной вниз. Инвертировать в самой программе не получится, т.е. там string. угол на выходе меняется от 0 до 360 град.
            double output = 0;
            try {
                double x = Convert.ToDouble(_x);
                double y = -Convert.ToDouble(_y);
                double radius = Math.Sqrt(x * x + y * y);
                double alpha;
                if (radius == 0) { radius = 1; }
                alpha = (180 / Math.PI) * Math.Asin(y / radius);
                if (x >= 0 && y >= 0)
                    {
                    output = alpha;
                    }
                else if (x >= 0 && y < 0)
                    {
                    output = (360 + alpha);
                    }
                else if (x < 0 && y >= 0)
                    {
                    output = 180 - alpha;
                    }
                else if (x < 0 && y < 0)
                    {
                    output = 180 - alpha;
                    }
                return output;
                }
            catch (Exception e)
                {
                //MainPage.Current.NotifyUserFromOtherThread(e.Message + " Degrees", NotifyType.StatusMessage);
                return output;
                }
			}

		public static double SpeedRadius(string _x, string _y)
			{//Вычисляется радиус вектора к точке, указанной мышкой, с учетом заданной макс. скорости слайдером 
			    double x = Convert.ToDouble(_x);
			    double y = -Convert.ToDouble(_y);
                double radius = (Math.Sqrt(x * x + y * y));
                double k = 0.01 * CommonStruct.maxWheelsSpeed;
                radius = k * radius;
			    return radius;
			}
		
		public static string ZeroInFrontSet(string number)
			{//Подставляет нули впереди числа до получени ятрех знаков
            string output = "";
            try
                {
                char[] quotes = { '\u0022' };
                number = number.TrimStart(quotes);
                number = number.TrimEnd(quotes);
                int digitNumber = number.Length;
                switch (digitNumber)
                    {
                    case 0:
                        output = "000";
                        break;
                    case 1:
                        output = "00" + number;
                        break;
                    case 2:
                        output = "0" + number;
                        break;
                    case 3:
                        output = number;
                        break;
                    }
                return output;
                }
            catch (Exception e)
                {
                MainPage.Current.NotifyUserFromOtherThreadAsync("ZeroInFrontSet" + e.Message, NotifyType.ErrorMessage);
                return output;
                }
			}

		public static string ZeroInFrontReset(string number)
			{
            string output = null;
            try
                {
                char[] quotes = { '\u0022' };
                number = number.TrimStart(quotes);
                number = number.TrimEnd(quotes);
                char[] zeroes = { '0' };
                output = number.TrimStart(zeroes);
                return output;
                }
            catch (Exception e)
                {
                //await CommonFunctions.WriteToLog(e.Message + " ZeroInFrontReset");
                MainPage.Current.NotifyUserFromOtherThreadAsync("ZeroInFrontReset: " + e.Message, NotifyType.ErrorMessage);
                return output;
                }
			}

		public static string ZeroInFrontFromDoubleAsync(double number)
			{
            string output = "";
            try
                {
                int ioutput = Convert.ToInt32(number);
                string soutput = ioutput.ToString();
                output = CommonFunctions.ZeroInFrontSet(soutput);
                return output;
                }
            catch (Exception e)
                {
                MainPage.Current.NotifyUserFromOtherThreadAsync("ZeroInFrontFromDouble: " + e.Message, NotifyType.ErrorMessage);
                return output;
                }
			}

        public static async Task WriteToLog(string message)
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                string filePath = storageFolder.Path + "\\log.xml";
                String dateFormatted = String.Format("{00:dd.MM.yyyy}", DateTime.Today);
                String timeFormatted = String.Format("{00:HH:mm:ss}", DateTime.Now);

                if (File.Exists(filePath) == false)
                {
                    StorageFile sFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("log.xml");
                    XDocument xdoc = new XDocument(new XElement("Settings", new XElement("Date", new XAttribute("Дата", dateFormatted), new XElement("Message", new XAttribute("Текст", message), new XAttribute("Время", timeFormatted)))));
                    string xmlDocument = xdoc.Document.ToString();
                    await FileIO.WriteTextAsync(sFile, xmlDocument);
                }
                else
                {
                    XDocument xdoc = XDocument.Load(filePath);
                    XElement logFile = xdoc.Element("Settings");
                    XElement date = logFile.Element("Date");//Первая из дат
                    IEnumerable<XElement> dates = logFile.Elements();
                    IEnumerator<XElement> datesEnumerator = dates.GetEnumerator();
                    XElement elem3 = null;
                    while (datesEnumerator.MoveNext())
                    {
                        elem3 = datesEnumerator.Current;
                    }
                    if (elem3 == null)
                    {
                    }
                    else
                    {
                        XElement lastDateElement = elem3;
                        string lastDate1 = lastDateElement.Value;
                        string today1 = DateTime.Today.ToLocalTime().ToString();
                        IEnumerable<XAttribute> attributes = dates.Attributes();
                        IEnumerator<XAttribute> xAtributes = attributes.GetEnumerator();
                        XAttribute str3 = null;
                        while (xAtributes.MoveNext())
                        {
                            str3 = xAtributes.Current;
                        }
                        XAttribute lastDateAttribute = str3;
                        string lastDate = lastDateAttribute.Value;
                        string today = dateFormatted;

                        if (lastDate == today)
                        {
                            elem3.Add(new XElement("Message", new XAttribute("Текст", message), new XAttribute("Время", timeFormatted)));
                        }
                        else
                        {
                            logFile.Add(new XElement("Date", new XAttribute("Дата", dateFormatted), new XElement("Message", new XAttribute("Текст", message), new XAttribute("Время", timeFormatted))));
                        }
                        string xmlDocument = xdoc.Document.ToString();
                        StorageFile sFile = await storageFolder.GetFileAsync("log.xml");
                        await FileIO.WriteTextAsync(sFile, xmlDocument);
                    }
                }
            }
            catch (Exception e1)
            {
                MainPage.Current.NotifyUserFromOtherThreadAsync("WriteToLog " + e1.Message, NotifyType.ErrorMessage);
            }
        }

        public static double WheelsSpeedToPWM(double x)
        {
            double y = 0.0;
            if ((x >= 0.0) && (x < 10.0))
            {
                y = 10.0;//Было все время 10. Почему - не могу вспомнить. 
            }
            else
            {
                y = 255.0 / 100.0 * x; 
            }
            y = Math.Round(y, MidpointRounding.AwayFromZero);
            return y;
        }
    }
}
