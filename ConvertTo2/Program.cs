using FSC.WUF;
using System.Windows;

namespace ConvertTo2
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var window = WindowManager.Create(window => Run(window), new Size(520, 480));

            Application application = new Application();
            application.Run();
        }

        public static Action<WindowManager> Run = (WindowManager window) => new MainWindow(window);
    }
}