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
		static public string decriptedSerial { get; set; }
		static public string timeOut { get; set; }
		public static string x1 = "";
		public static string x2 = "";
		public static double cameraSpeed { get; set; }
        public static string wheelsAddress = "0002";
        public static string cameraAddress = "0003";
		public static string interval { get; set; }
		public static string portName { get; set; }
		public static string bodRate { get; set; }
		public static bool reconnect { get; set; }
		public static bool portOpen { get; set; }
		public static int PWMStoppingSpeed { get; set; }
		public static double maxWheelsSpeed { get; set; }//Макс. скорость, задаваемая слайдером макс. скорости, в процентах. 
		public static string lastCommandRight { get; set; }
		public static string lastCommandLeft { get; set; }
		public static int smoothStopTime { get; set; }
		public static string wheelsPwrRange { get; set; }
        public static string cameraPwrRange = "3";
		public static int connectionNumber { get; set; }
        public static string defaultWebSiteAddress { get; set; }
        public static string cameraPositionBefore { get; set; }
        public static string stepNumberForCalibration { get; set; }
        public static int directTopDistance = 0;
        public static int directBottomDistance { get; set; }
        public static string top_bottom_distance { get; set; }
        public static int cameraAlpha { get; set; }
        public static string PowerControl { get; set; }
        public static double lastSpeedLeft { get; set; }
        public static double lastSpeedRight { get; set; }
        public static string directionLeft = "0";
        public static bool stopBeforeWas = true;
        public static string directionRight = "0";
        public static double k1 { get; set; }
        public static double k2 { get; set; }
        public static double k3 { get; set; }
        public static double k4 { get; set; }
        public static double speedTuningParam { get; set; }

        public static int minWheelsSpeedForTurning { get; set; }
        public static string smileFilePath { get; set; }

        public static string downloadsPath { get; set; }
        public static string pictureAndMoviesPath { get; set; }

        public static string updateCheckMessageCompleted = "";
        public static string updateCheckMessageNotNeed = "";
        public static string downloadProgressText1 = "";
        public static string downloadProgressText2 = "";
        public static string buttonOpenFileFolder = "";
        public static bool exitProgramm = false;
        public static bool stopSmoothly = true;
        public static bool rebootAtNight = true;

        public static double speedLeftLocal = 0.0;
        public static double speedRightLocal = 0.0;

        public static bool rightClick = false;

        public static double[] refPoints = new double[11];

        public static bool wheelsNonlinearTuningIs = true;
        public static string cameraFastSpeed = "006";
        public static string culture = "Eng";

        static public string dataToWrite { get; set; }
        static public string readData { get; set; }

        static public string webSiteAddress1 = "";
        static public string webSiteAddress2 = "";
        static public string webSiteAddress3 = "";
        static public string webSiteAddress4 = "";
        static public string webSiteAddress5 = "";
        static public string webSiteAddress6 = "";
        static public string webSiteAddress7 = "";
        static public string webSiteAddress8 = "";

        //static public string robotName = "";
        static public string serial = "";

        public static bool allControlIsEnabled = false;

        static public string NotifyPressStop = "";

        static public string comPortItem = "";
        static public int comPortIndex = 0;

        static public string textToRead { get; set; }
        static public string SSMLFilePath { get; set; }
        public static double AngleInBreakPoint { get; internal set; }
        public static double DistanceToZero { get; internal set; }

        public static double localizationPoint;
        internal static bool checkBoxOnlyLocal;
        //public static double[] anglesFromIC = new double[16];
        public static string[] deviceIDNames = new string[10];
        internal static string cameraController;

        public static bool wheelsWasStopped = true;
        public static int initTime;
        public static bool itIsTimeToAskVoltage = false;

        public static double dVoltageCorrected = 12500;
        public static string voltageLevelFromRobot = "";
        internal static bool thereAreNoIONow = true;
        public static int numberOfTicksAfterWheelsStop = 0;//Количество измерений напряжения на аккумуляторах. Необходимо для задержи измерений после остановки
        public static bool textBoxRealVoltageChanged = false;
        internal static double VReal = 12.7;
        public static double deltaV = 0.0;
        public static int numberOfStops = 0;
        public static long startTimeOfStop { get; set; }
        public static string outputString = "";


    }

    public struct CommonVideoStruct
    {
        static public double squareWidth = 0.0;
        static public double squareHeight = 0.0;
        static public double squareX = 0.0;
        static public double squareY = 0.0;
        static public int faceNumber = 0;//Количество лиц
        static public int indexOfBiggestFace = 0;
    }

        enum MonitorState
        {
        ON = -1,
        OFF = 2,
        STANDBY = 1
        }

	class CommonFunctions
		{
        private const int MOVE = 0x0001;
        private const int HWND_BROADCAST = 0xffff;

        public static string elementContent = null;

        public static string DeviceIDName(string deviceID)
        {
            string deviceIDName = "";
            int startIndex = deviceID.IndexOf("+");
            int indexOfPlus = deviceID.IndexOf("+", startIndex + 1);
            string tempVariable1 = deviceID.Remove(0, indexOfPlus + 1);
            int indexOfGrid = tempVariable1.IndexOf("#");
            deviceIDName = tempVariable1.Remove(indexOfGrid);
            return deviceIDName;
        }

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
            catch (Exception )
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
            if (radius > 90) radius = 100;
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
                MainPage.Current.NotifyUserFromOtherThread("ZeroInFrontSet" + e.Message, NotifyType.StatusMessage);
                return output;
                }
			}

		public static async Task<string> ZeroInFrontReset(string number)
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
                await CommonFunctions.WriteToLog(e.Message + " ZeroInFrontReset");
                MainPage.Current.NotifyUserFromOtherThread("ZeroInFrontReset: " + e.Message, NotifyType.StatusMessage);
                return output;
                }
			}

		public static string ZeroInFrontFromDouble(double number)
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
                MainPage.Current.NotifyUserFromOtherThread("ZeroInFrontFromDouble: " + e.Message, NotifyType.StatusMessage);
                return output;
                }
			}

		public static async Task<string> Encryption(string inputWord)// Шифрование 
			{
			try
				{
				string codedWord = null;
				int n = inputWord.Length;
				string[] arrayPassword = new string[n];
				char[] chr = new char[n];
				chr = inputWord.ToCharArray();

				for (int i = 0; i < n; i++)
					{
					arrayPassword[i] = chr[i].ToString();
					}
				int m = 10 * n; //Длина выходного слова.
				Random rnd = new Random(0);
				byte[] byteTemplate = new byte[m];
				string stringTemplate;

				rnd.NextBytes(byteTemplate);
				int intNumber;
				ASCIIEncoding ascii = new ASCIIEncoding();

				for (int i = 0; i < m; i++)
					{
					intNumber = rnd.Next(48, 122);
					while ((intNumber == 58) || (intNumber == 59) || (intNumber == 60) || (intNumber == 61) || (intNumber == 62) || (intNumber == 92) || (intNumber == 94) || (intNumber == 96))
						{
						intNumber = rnd.Next(97, 122);
						}
					byteTemplate[i] = (byte)intNumber;
					}
				stringTemplate = ascii.GetString(byteTemplate);
				int k = stringTemplate.Length;
				codedWord = stringTemplate;
				k = codedWord.Length;
				for (int i = 0; i < n; i++)
					{
					codedWord = codedWord.Insert(10 * i, arrayPassword[i]);
					k = codedWord.Length;
					}
				return codedWord;
				}
			catch (Exception e)
				{
                await CommonFunctions.WriteToLog(e.Message + " Encryption");
                MainPage.Current.NotifyUserFromOtherThread("Encryption: " + e.Message, NotifyType.StatusMessage);
                return e.Message;
				}
			}

		public static async Task<string> Decryption(string inputWord)// Дешифрование 
			{
			try
				{
				string decryptedWord = null;
				int n = (inputWord.Length - 10) / 10 + 1;
				string[] decodedWordArray = new string[n];
				for (int i = 0; i < n; i++)
					{
					decodedWordArray[i] = inputWord.Substring(10 * i, 1);
					}
				decryptedWord = string.Concat(decodedWordArray);
				return decryptedWord;
				}
			catch (Exception e)
				{
                await CommonFunctions.WriteToLog(e.Message + " Decryption");
                MainPage.Current.NotifyUserFromOtherThread("Decryption: " + e.Message, NotifyType.StatusMessage);
                return e.Message;
				}
			}

		public static uint CalculateCRC(string filePath)
			{
			try
				{
				FileStream stream = File.OpenRead(filePath);
				const int buffer_size = 1024;
				const uint POLYNOMIAL = 0xEDB88320;
				uint result = 0xFFFFFFFF;
				uint Crc32;
				byte[] buffer = new byte[buffer_size];
				uint[] table_CRC32 = new uint[256];

				unchecked
					{// Инициалиазация таблицы
					for (int i = 0; i < 256; i++)
						{
						Crc32 = (uint)i;
						for (int j = 8; j > 0; j--)
							{
							if ((Crc32 & 1) == 1)
								Crc32 = (Crc32 >> 1) ^ POLYNOMIAL;
							else
								Crc32 >>= 1;
							}
						table_CRC32[i] = Crc32;
						}
					// Чтение из буфера
					int count = stream.Read(buffer, 0, buffer_size);
					// Вычисление CRC
					while (count > 0)
						{
						for (int i = 0; i < count; i++)
							{
							result = ((result) >> 8)
								^ table_CRC32[(buffer[i])
								^ ((result) & 0x000000FF)];
							}
						count = stream.Read(buffer, 0, buffer_size);
						}
					}
				stream.Flush();
				stream.Dispose();
                return ~result;
				}
			catch (Exception e)
				{
                //CommonFunctions.WriteToLog(e.Message + " CalculateCRC");
                MainPage.Current.NotifyUserFromOtherThread("CalculateCRC: " + e.Message, NotifyType.StatusMessage);
                return 0;
				}
			}
	
		public static string CommandWithCRC(string commandWithOutCRC)
			{
			try{
				string commandWithCRC = "";
				Encoding ascii = Encoding.ASCII;
				Byte[] encodedBytes = ascii.GetBytes(commandWithOutCRC);//Получим десятичный формат
				int summDec = 0;
				for (int i = 0; i < encodedBytes.Length; i++)
					{
					summDec = summDec + encodedBytes[i];
					}
				string hexT1 = IntToHex(summDec);
				string hex = hexT1.Substring(hexT1.Length - 2);
				commandWithCRC = commandWithOutCRC + hex;
				return commandWithCRC;
			}
			catch (Exception ex)
				{
                //CommonFunctions.WriteToLog(ex.Message + " CommandWithCRC");
                MainPage.Current.NotifyUserFromOtherThread("CommandWithCRC: " + ex.Message, NotifyType.StatusMessage);
                return "0";
				}
			}

		static public int HexToDec(string hex)
			{
            int res = 0;
            try
                {
                char[] c = hex.ToCharArray();
                int d0 = CharToDec(c[1]);
                int d1 = CharToDec(c[0]);
                res = (16 * d1 + d0);
                return res;
                }
            catch (Exception e)
                {
                //CommonFunctions.WriteToLog(e.Message + " HexToDec");
                MainPage.Current.NotifyUserFromOtherThread("HexToDec: " + e.Message, NotifyType.StatusMessage);
                return res;
                }
			}

		public static string IntToHex(int intAddress)
			{ 
			try
				{
				string hexAddressT1 = "0";
				byte[] byteArr = BitConverter.GetBytes(intAddress);
				hexAddressT1 = BitConverter.ToString(byteArr);
				string hexAddress = hexAddressT1[3].ToString() + hexAddressT1[4].ToString() + hexAddressT1[0].ToString() + hexAddressT1[1].ToString();
				return hexAddress;
				}
			catch (Exception e1)
				{
                //CommonFunctions.WriteToLog(e1.Message + " IntToHex");
                MainPage.Current.NotifyUserFromOtherThread("IntToHex: " + e1.Message, NotifyType.StatusMessage);
                return e1.Message;
				}
			}

        public static async Task WriteToHardData(string elementName, string elementContent)
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile sFile = await storageFolder.GetFileAsync("hardData.xml");
                string iniFullPath = sFile.Path;
                XDocument xdoc = XDocument.Load(iniFullPath);
                XElement xElement = xdoc.Element("Settings");
                XElement xElement2 = xElement.Element(elementName);
                xElement2.ReplaceNodes(elementContent);
                string xmlDocument = xdoc.Document.ToString();
                await FileIO.WriteTextAsync(sFile, xmlDocument);
            }
            catch (Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("WriteToHardData: " + e.Message, NotifyType.StatusMessage);
            }
        }

        public static async Task<string> ReadFromHardData(string element1Name)
        {
            string elemntContent = null;
            try
            {
                XDocument xdoc = null;
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;//Путь  папке LocalFolder. Там лежат данные после инсталляции
                StorageFolder installedLocation = Package.Current.InstalledLocation;
                StorageFile fileLocal = await storageFolder.GetFileAsync("hardData1.xml");
                string innerFileName = fileLocal.Path;
                var task = Task.Run(() =>
                {
                    xdoc = XDocument.Load(innerFileName);
                    XElement xElement = xdoc.Element("Settings");
                    XElement xElement2 = xElement.Element(element1Name);
                    elementContent = xElement2.Value;
                });
                task.Wait();
                return elementContent;
            }
            catch (Exception e)
            {
                //CommonFunctions.WriteToLog(e.Message + " ReadFromHardData");
                MainPage.Current.NotifyUserFromOtherThread("ReadFromHardData: " + e.Message, NotifyType.StatusMessage);
                return elemntContent;
            }
        }

        static private int CharToDec(char c0)
			{
            int digit = 0;
            try
                {
                string s0 = c0.ToString();
                if (Char.IsDigit(c0))
                    {
                    digit = Convert.ToInt32(s0);
                    }
                else if (Char.IsLetter(c0))
                    {
                    switch (c0)
                        {
                        case 'A': digit = 10; break;
                        case 'B': digit = 11; break;
                        case 'C': digit = 12; break;
                        case 'D': digit = 13; break;
                        case 'E': digit = 14; break;
                        case 'F': digit = 15; break;
                        default: break;
                        }
                    }
                else
                    {
                    digit = -1;
                    }
                return digit;
                }
            catch (Exception e)
                {
                //CommonFunctions.WriteToLog(e.Message + " CharToDec");
                MainPage.Current.NotifyUserFromOtherThread("CharToDec" + e.Message, NotifyType.StatusMessage);
                return digit;
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
            catch (Exception )
            {
            }
        }

        public static double WheelsSpeedToPWM(double x)
        {
            double y = 0.0;
            if ((x >= 0.0) && (x < 10.0))
            {
                y = 10.0;
            }
            else
            {
                y = 255.0 / 100.0 * x; 
            }
            y = Math.Round(y, MidpointRounding.AwayFromZero);
            return y;
        }

        public static double WheelsSpeedToPWM_1(double x)
            {
            double y = 0.0;
            if ((x >= 0) && (x < 50))
                {
                y = 250 - ((250 - 100.0) / 50.0) * (x - 0.0);
                }
            else if ((x >= 50) && (x < 60))
                {
                y = 100 - ((100 - 50) / 10.0) * (x - 50);
                }
            else if ((x >= 60) && (x < 70))
                {
                y = 50 - ((50 - 20) / 10.0) * (x - 60);
                }
            else if ((x >= 70) && (x < 80))
                {
                y = 20 - ((20 - 10) / 10.0) * (x - 70);
                }
            else if ((x >= 80) && (x < 90))
                {
                y = 10 - ((10 - 5) / 10.0) * (x - 80);
                }
            else if ((x >= 90) && (x <= 100))
                {
                y = 5 - ((5 - 1) / 10.0) * (x - 90);
                }
            else
                {
                y = 255;
                }
            y = Math.Round(y, MidpointRounding.AwayFromZero);
            return y;
            }

        public static double WheelsNonlinearTuning(double x)
            {
            double y = 0.0;
            for (int i = 0; i < 10; i++)
                {
                if ((x > 10 * i) && (x <= 10 * (i + 1)))
                    {
                    y = CommonStruct.refPoints[i] + ((CommonStruct.refPoints[i + 1] - CommonStruct.refPoints[i]) / 10.0) * (x - 10.0 * i);
                    }
                }
            return y;
            }

        public static void Paths()
        {
            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                CommonStruct.SSMLFilePath = "TextSSMLSource.xml";
            }
            catch (Exception )
            {
            }
        }

        public static string Frase(string fileKeyword)
        {//Подставляет нули впереди числа до получени ятрех знаков
            try
            {
                switch (fileKeyword)
                {
                    case "Привет":
                        CommonStruct.SSMLFilePath = "TextSSMLSource.xml";
                        break;
                    case "Ушел":
                        CommonStruct.SSMLFilePath = "TextSSMLSource1.xml";
                        break;
                    
                }
                return CommonStruct.SSMLFilePath;
            }
            catch (Exception e)
            {
                MainPage.Current.NotifyUserFromOtherThread("Frase" + e.Message, NotifyType.StatusMessage);
                return "";
            }
        }


        public static string ReadFromSSMLSource()
        {
            string elementContent = null;
            try
            {
                elementContent = File.ReadAllText(CommonStruct.SSMLFilePath);
            }
            catch (Exception )
            {
            }
            return elementContent;
        }
    }
}
