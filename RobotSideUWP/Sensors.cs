using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotSideUWP
{
    class SensorControl
    {
        private MainPage rootPage = MainPage.Current;
        string sAll, s0, s1, s2, s3, s4;
        double Hx, Hy, H;
        public double alpha;
        private string forwardDirection = "0";
        private string backwardDirection = "1";
        double speedLeft, speedRight;
        string directionLeft = "1"; //направление вращения левого колеса
        string directionRight = "1";//направление вращения правого колеса
        string HyString, HxString, data;
        public double alphaFromDevice = 0;
        double alphaAfter = 0;
        double alphaBefore = 0;
        public bool oneCycleAlpha = true;

        public SensorControl()
        {
            try
            {
                MeasureAllSensors();
            }
            catch (Exception )
            {
            }
        }

        public void MeasureAllSensors()
        {
            try
            {
                PlcControl.HostWatchDog(CommonStruct.wheelsAddress, "set");
                ReadWrite.Write("^01D" + "\r");//
                //string data = MainPage.Current.PortRead(36);
                data = ReadWrite.Read();
                if (data.Length == 35)
                {
                    sAll = data.Remove(0, 4);
                    //sAll = Current.PortRead().Remove(0, 4);
                    s0 = sAll.Remove(4);
                    s1 = sAll.Remove(0, 4).Remove(4);
                    s2 = sAll.Remove(0, 8).Remove(4);
                    s3 = sAll.Remove(0, 12).Remove(4);
                    s4 = sAll.Remove(0, 16).Remove(4);
                    HyString = sAll.Remove(0, 20).Remove(6);
                    HxString = sAll.Remove(0, 26);

                    Hy = Math.Round(Convert.ToDouble(HyString));
                    Hx = Math.Round(Convert.ToDouble(HxString));
                    H = Math.Round(Math.Sqrt(Hx * Hx + Hy * Hy));


                    alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    alpha = AngleDeviceTo180(alphaFromDevice);
                    //if (oneCycleAlpha == true)
                    //{
                    //    alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //    alpha1 = AngleDeviceTo180(alphaFromDevice);
                    //}
                    //else
                    //{
                    //    if (((alpha < 90) && (alpha > 0)) || ((alpha > -90) && (alpha < 0)))
                    //    {
                    //        alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //        alpha1 = AngleDeviceTo180(alphaFromDevice);
                    //    }
                    //    else
                    //    {
                    //        if (alpha > 90) 
                    //        {
                    //            if (directionLeft == backwardDirection)
                    //            {//влево
                    //                alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //                alpha1 = AngleDeviceTo180(alphaFromDevice) - 360;
                    //            }
                    //            else
                    //            {
                    //                alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //                alpha1 = AngleDeviceTo180(alphaFromDevice);
                    //            }
                    //        }
                    //        else 
                    //        {
                    //            if (directionLeft == forwardDirection)
                    //            {//вправо
                    //                alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //                alpha1 = 360 + AngleDeviceTo180(alphaFromDevice);
                    //            }
                    //            else
                    //            {
                    //                alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //                alpha1 = AngleDeviceTo180(alphaFromDevice);
                    //            }
                    //        }
                    //    }
                    //}
                    //alpha = alpha1;


                    //if (oneCycleAlpha == true)
                    //{
                    //    alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //    alpha = AngleDeviceTo180(alphaFromDevice);
                    //}
                    //else
                    //{
                    //    if (Hx > 0)
                    //    {
                    //        alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //        alpha = AngleDeviceTo180(alphaFromDevice);
                    //    }
                    //    else if ((Hx <= 0) && (Hy < 0))
                    //    {
                    //        alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //        alpha = AngleDeviceTo180(alphaFromDevice) + 360;
                    //    }

                    //    if (Hx < 0)
                    //    {
                    //        alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //        alpha = AngleDeviceTo180(alphaFromDevice);
                    //    }
                    //    else if ((Hx > 0) && (Hy < 0))
                    //    {
                    //        alphaFromDevice = Math.Ceiling((Math.Atan2(Hx, Hy)) * 180 / Math.PI);
                    //        alpha = AngleDeviceTo180(alphaFromDevice) - 360;
                    //    }
                    //}

                }
                else
                {
                }
            }
            catch (Exception )
            {
                string s = ReadWrite.Read();
            }
        }

        public double directLeft 
        {
            get { return Convert.ToDouble(s0); }
        }

        public double directRight
        {
            get { return Convert.ToDouble(s1); }
        }

        public double right
        {
            get { return Convert.ToDouble(s2); }
        }

        public double behind
        {
            get { return Convert.ToDouble(s3); }
        }

        public double left
        {
            get { return Convert.ToDouble(s4); }
        }

        //public double compassAngle()
        //{
        //    double alpha1 = 0;
        //    if ((alpha > 150) || (alpha < -150))
        //    {
        //        if (CommonStruct.localizationAngle > 150)
        //        {
        //            if (alpha < -180)
        //            {
        //                alpha1 = -360 + alpha;
        //            }
        //        }
        //        else if (CommonStruct.localizationAngle < -150)
        //        {
        //            if (alpha > 180)
        //            {
        //                alpha1 = (360 + alpha);
        //            }
        //        }
        //    }
        //    return alpha1;
        //}

        //public double compassAngle
        //{
        //    get
        //    {
        //        double alpha1 = alpha;
        //        return alpha1;
        //    }
        //}

        public double compassHx
        {
            get { return Hx; }
        }

        public double compassHy
        {
            get { return Hy; }
        }

        public double compassH
        {
            get { return H; }
        }

        public void TurnToGivenDirection()
        {
            try
            {
                double a = alpha;
                double p = CommonStruct.localizationPoint;
                double error = 0;
                double errorAbs = 0;

                if (Math.Sign(p) == Math.Sign(a))
                {
                    error = p - a;
                    if (error >= 0)
                    {//вправо
                        directionLeft = forwardDirection;
                        directionRight = backwardDirection;
                    }
                    else
                    {//влево 
                        directionLeft = backwardDirection;
                        directionRight = forwardDirection;
                    }
                }
                else if (Math.Sign(p) != Math.Sign(a))
                {
                    if((a >= 0) && (p < 0))
                    {
                        if((a - p) > 180)
                        {// вправо
                            directionLeft = forwardDirection;
                            directionRight = backwardDirection;
                            error = a + p;
                        }
                        else if ((a - p) < 180)
                        {// влево
                            directionLeft = backwardDirection;
                            directionRight = forwardDirection;
                            error = a - p;
                        }
                    }
                    else if ((a < 0) && (p >= 0))
                    {
                        if ((p - a) > 180)
                        {//влево 
                            directionLeft = backwardDirection;
                            directionRight = forwardDirection;
                            error = p + a;
                        }
                        else if ((p - a) < 180)
                        {// вправо
                            directionLeft = forwardDirection;
                            directionRight = backwardDirection;
                            error = p - a;
                        }
                    }
                }

                errorAbs = Math.Abs(error);
                if ((errorAbs <= 50) && (errorAbs > 10))
                {
                    if ((a < 100) && (a > 0))
                    {
                        speedLeft = (CommonStruct.maxWheelsSpeed) * errorAbs / 300 + 0.3 * CommonStruct.minWheelsSpeedForTurning;
                    }
                    else
                    {
                        speedLeft = (CommonStruct.maxWheelsSpeed) * errorAbs / 200 + 0.3 * CommonStruct.minWheelsSpeedForTurning;
                    }
                    speedRight = speedLeft;
                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);

                }
                else if (errorAbs > 10)
                {
                    speedLeft = CommonStruct.maxWheelsSpeed / 2;
                    speedRight = speedLeft;
                    PlcControl.Wheels(directionLeft, speedLeft, directionRight, speedRight);
                }

                if (errorAbs < 5)
                {
                    PlcControl.WheelsStop();
                }
                
                alphaAfter = alphaBefore;
            }
            catch(Exception )
            {
            }
        }

        public double Angle180ToDevice(double x)
        {//угол с учетом всех преобразований и поправок
            double y, y2, y1, x2, x1, x4, y4, x3, y3;
            x1 = -180;//Первые две точки - -180 и 0
            y1 = -CommonStruct.AngleInBreakPoint;//В точке разрыва угол отрицательный строго равен углу опложительному, даже с учетом шума.

            x2 = 180 - CommonStruct.DistanceToZero;
            y2 = 0;

            x3 = x2;// Вторые две точки - 0 и +180
            y3 = y2;

            x4 = +180;
            y4 = CommonStruct.AngleInBreakPoint;

            if (x < x2)
            {
                y = ((y2 - y1) / (x2 - x1)) * x + y2 - ((y2 - y1) / (x2 - x1)) * x2;
            }
            else
            {
                y = ((y4 - y3) / (x4 - x3)) * x + y4 - ((y4 - y3) / (x4 - x3)) * x4;
            }
            //if (y > 180) { y = 180; }
            //if (y < -180) { y = -180; }
            return y;
        }

        public double AngleDeviceTo180(double y)
        {//угол с учетом всех преобразований и поправок
            double x, y2, y1, x2, x1, x4, y4, x3, y3;
            x1 = -180;//Первые две точки - -180 и 0
            y1 = -CommonStruct.AngleInBreakPoint;//В точке разрыва угол отрицательный строго равен углу опложительному, даже с учетом шума.

            x2 = 180 - CommonStruct.DistanceToZero;
            y2 = 0;

            x3 = x2;// Вторые две точки - 0 и +180
            y3 = y2;

            x4 = +180;
            y4 = CommonStruct.AngleInBreakPoint;

            if (y < 0)
            {
                x = (y - (y2 - ((y2 - y1) / (x2 - x1)) * x2)) / ((y2 - y1) / (x2 - x1));
            }
            else
            {
                x = (y - (y4 - ((y4 - y3) / (x4 - x3)) * x4)) / ((y4 - y3) / (x4 - x3));
            }
            return x;
        }

        public double AngleDeviceTo180Prolongated(double y)
        {//угол с учетом всех преобразований и поправок
            double x, y2, y1, x2, x1, x4, y4, x3, y3;
            x1 = -180;//Первые две точки - -180 и 0
            y1 = -CommonStruct.AngleInBreakPoint;//В точке разрыва угол отрицательный строго равен углу опложительному, даже с учетом шума.

            x2 = 180 - CommonStruct.DistanceToZero;
            y2 = 0;

            x3 = x2;// Вторые две точки - 0 и +180
            y3 = y2;

            x4 = +180;
            y4 = CommonStruct.AngleInBreakPoint;

            if (y < 0)
            {
                x = (y - (y2 - ((y2 - y1) / (x2 - x1)) * x2)) / ((y2 - y1) / (x2 - x1));
            }
            else
            {
                x = (y - (y4 - ((y4 - y3) / (x4 - x3)) * x4)) / ((y4 - y3) / (x4 - x3));
            }
            return x;
        }

        public void LookAround()
        {
            double angleLeft = CommonStruct.localizationPoint - 45;
            double angleRight = CommonStruct.localizationPoint + 45;



        }


        public void GoBackToGivenDistance()
        {
            


        }




        internal void LookForPeople()
        {
            
        }

        internal void GoToPeople()
        {
            
        }

        internal void SpeakText()
        {
            
        }


    }
}
