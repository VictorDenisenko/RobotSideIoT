using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace RobotSideUWP
{
    class VideoControl
    {
        static string forwardDirection = "0";
        static string backwardDirection = "1";
        static double speedLeft, speedRight, speedMax, speedMin;
        private static DispatcherTimer timerForDelay1 = new DispatcherTimer();
        private static DispatcherTimer timerForDelay2 = new DispatcherTimer();
        private static long ticksFaceNumber_1;
        private static long deltaTicks = 0;
        private static int cycleNumberForFace = 0, cycleNumberForFace2 = 0;
        private static int cycleNumberResult = 0, cycleNumberResult2 = 0;
        private static bool HasBeedPlayed = false;
        private static bool HasBeedPlayed2 = false;

        public static void TimerStart(int delay)
        {
            timerForDelay1.Tick += TimerForDelay_Tick;
            timerForDelay1.Interval = new TimeSpan(0, 0, delay); //(часы, мин, сек)
            timerForDelay1.Start();
            //cycleNumberResult = 0;
            cycleNumberForFace2 = 0;
        }

        private static void TimerForDelay_Tick(object sender, object e)
        {
            timerForDelay1.Tick -= TimerForDelay_Tick;
            timerForDelay1.Stop();
            cycleNumberResult = cycleNumberForFace;
            cycleNumberResult2 = cycleNumberForFace2;
            
        }

        private static bool IsFaceInCenter()
        {
            bool isFaceInCenter = false;
            if (cycleNumberForFace == 0)
            {
                ticksFaceNumber_1 = DateTime.Now.Ticks;//Ticks = 100 ns
                TimerStart(1);
            }
            cycleNumberForFace++;
            if (cycleNumberForFace >= 10)
            {
                deltaTicks = DateTime.Now.Ticks - ticksFaceNumber_1;//Ticks = 100 ns
            }
            return isFaceInCenter;
        }

        public static void FaceTrackingHorizontal()
        {
            string directionLeft = backwardDirection; //направление вращения левого колеса
            string directionRight = backwardDirection;//направление вращения правого колеса
            int minWheelsSpeedForTurning = CommonStruct.minWheelsSpeedForTurning;

            if (CommonVideoStruct.faceNumber > 0)
            {
                if (CommonVideoStruct.squareX > 1.0)//Здесь координаты уже приведены к системе, в которой ноль в центре экрана.
                {
                    directionLeft = forwardDirection;
                    directionRight = backwardDirection;
                    speedMax = 100 * 0.01 * (100 - CommonVideoStruct.squareWidth) / 2;
                    speedMin = 10;
                    speedRight = ((speedMax - speedMin) / ((100 - CommonVideoStruct.squareWidth) / 2)) * CommonVideoStruct.squareX + speedMin;
                    speedLeft = speedRight;
                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                }
                else if (CommonVideoStruct.squareX < -1.0)
                {
                    double x = -CommonVideoStruct.squareX;
                    directionLeft = backwardDirection;
                    directionRight = forwardDirection;
                    speedMax = 100 * 0.01 * (100 - CommonVideoStruct.squareWidth) / 2;
                    speedMin = 10;
                    speedRight = ((speedMax - speedMin) / ((100 - CommonVideoStruct.squareWidth) / 2)) * x + speedMin;
                    speedLeft = speedRight;
                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                }
                else
                {
                    PlcControl.WheelsStop();
                    if (cycleNumberForFace == 0)
                    {
                        TimerStart(1);
                    }
                    if ((cycleNumberResult > 3) && (HasBeedPlayed == false))
                    {
                        if ((MainPage.Current.mediaElement.CurrentState != MediaElementState.Opening) && (MainPage.Current.mediaElement.CurrentState != MediaElementState.Playing))
                        {
                            MainPage.Current.SpeechStart("Привет! Ты что тут делаешь ?");
                            HasBeedPlayed = true;
                        }
                    }
                    cycleNumberForFace++;
                }
            }
            else
            {
                PlcControl.WheelsStop();
                if (HasBeedPlayed == true)
                {
                    if ((HasBeedPlayed2 == false)&&(cycleNumberForFace2 == 0))
                    {
                        TimerStart(4);
                    }
                    if ((HasBeedPlayed == true) && (cycleNumberResult2 > 14))
                    {
                        if ((MainPage.Current.mediaElement.CurrentState != MediaElementState.Opening) && (MainPage.Current.mediaElement.CurrentState != MediaElementState.Playing))
                        {
                            MainPage.Current.SpeechStart("Ты куда ушел? Я не вижу твоего лица.");
                            HasBeedPlayed = false;
                        }
                    }
                }
                cycleNumberForFace2++;
            }
        }

      

    }
}
