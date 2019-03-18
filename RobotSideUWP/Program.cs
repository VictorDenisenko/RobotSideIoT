using System;
using System.Threading;
using System.Windows.Forms;

namespace RobotSide
{
    static class Program
    {
		//public static Form fm1;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //fm1 = new Form1();
            //Application.Run(fm1);
            using (var mutex = new Mutex(false, "RobotSideMutexName"))
                {
                if (mutex.WaitOne(TimeSpan.FromSeconds(2))) // Подождать три секунды - вдруг предыдущий экземпляр еще закрывается
                    Application.Run(new Form1());
                else
                    MessageBox.Show("Another instance of the app is already running");
                }
        }
	}
}
