using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows; // Für MessageBox

public static class ContextMenuInstaller
{
    private const string MenuName = "ConvertTo2";
    private const string Command = "\"C:\\Pfad\\Zu\\DeinemProgramm.exe\" \"%1\"";

    // Öffentliche Methode für die Installation
    public static void Install()
    {
        try
        {
            // Kontextwechsel für Admin-Funktion
            ExecuteWithElevation("install");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Installation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Öffentliche Methode für die Deinstallation
    public static void Uninstall()
    {
        try
        {
            // Kontextwechsel für Admin-Funktion
            ExecuteWithElevation("uninstall");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Uninstall failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Private Methode zur Ausführung der gewünschten Aktion (Install/Uninstall) mit erhöhten Rechten
    private static void ExecuteWithElevation(string action)
    {
        STARTUPINFO si = new STARTUPINFO();
        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

        // Der aktuelle Pfad des Programms
        string? application = Process.GetCurrentProcess().MainModule?.FileName;

        // Stelle sicher, dass der Dateiname nicht null ist
        if (string.IsNullOrEmpty(application))
        {
            MessageBox.Show("Failed to retrieve the application path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string command = action == "install" ? "Install" : "Uninstall";

        bool result = CreateProcessWithLogonW(
            "Administrator", null, null,
            0, null, application + " " + command,
            0, 0, null, ref si, out pi);

        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            MessageBox.Show($"Failed to elevate privileges. Error code: {errorCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            // Schließe das Handle für den gestarteten Prozess
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CreateProcessWithLogonW(
        string? lpUsername, string? lpDomain, string? lpPassword,
        uint dwLogonFlags, string? lpApplicationName, string? lpCommandLine,
        uint dwCreationFlags, uint? lpEnvironment, string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public uint cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public ushort wShowWindow;
        public ushort cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }
}
