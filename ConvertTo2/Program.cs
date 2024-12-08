using FSC.WUF;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Size = System.Windows.Size;

namespace ConvertTo2
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SetAppUserModelId("ConvertTo2.NotifyService");
            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                Environment.CurrentDirectory = exePath;

                if (!File.Exists("ffmpeg.exe") || !File.Exists("ffprobe.exe"))
                {
                    var result = MessageBox.Show("FFmpeg and FFprobe were not found. Would you like to open the FFmpeg website to download the files?",
                             "FFmpeg required", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://ffmpeg.org/download.html",
                            UseShellExecute = true
                        });
                    }

                    return;
                }

                if (args.Length > 0)
                {
                    if (args[0].ToLower() == "install")
                    {
                        InstallContextMenu();
                        return;
                    }
                    else if (args[0].ToLower() == "uninstall")
                    {
                        UninstallContextMenu();
                        return;
                    }
                }

                var window = WindowManager.Create(window => Run(window), new Size(520, 480));

                Application application = Application.Current ?? new Application();
                application.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static Action<WindowManager> Run = (WindowManager window) => new MainWindow(window);

        private static void InstallContextMenu()
        {
            try
            {
                // Registry keys for context menu
                string keyPath = @"*\shell\ConvertTo2";
                string commandPath = @"*\shell\ConvertTo2\command";

                using (var key = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        key.SetValue(null, "ConvertTo2"); // The name in the context menu
                    }
                }

                using (var key = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(commandPath))
                {
                    if (key != null)
                    {
                        // Hol den Pfad zur .exe Datei
                        string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                        key.SetValue(null, $"\"{executablePath}\" \"%1\""); // Command to run the program with the selected file
                    }
                }

                MessageBox.Show("Installed successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during installation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void UninstallContextMenu()
        {
            try
            {
                // Registry keys for context menu
                string keyPath = @"*\shell\ConvertTo2";

                Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(keyPath, false);

                MessageBox.Show("Uninstalled successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during uninstallation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SetAppUserModelId(string appId)
        {
            try
            {
                IntPtr hResult = SetCurrentProcessExplicitAppUserModelID(appId);
                if (hResult != IntPtr.Zero)
                {
                    Console.WriteLine("Failed to set AppUserModelID");
                }
            }
            catch
            {
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetCurrentProcessExplicitAppUserModelID(string appId);
    }
}